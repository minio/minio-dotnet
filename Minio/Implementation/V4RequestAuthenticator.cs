using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Minio.CredentialProviders;
using Minio.Helpers;

#if NET6_0
using SHA256 = Shims.SHA256;
#endif

namespace Minio.Implementation;

/// <summary>
/// Implements AWS Signature Version 4 (SigV4) request signing for MinIO and S3-compatible services.
/// Signs outgoing <see cref="HttpRequestMessage"/> instances by computing a canonical request,
/// a string-to-sign, and an HMAC-SHA256 signature, then attaching the resulting
/// <c>AWS4-HMAC-SHA256</c> authorization header.
/// </summary>
public partial class V4RequestAuthenticator : IRequestAuthenticator
{
    private static readonly KeyOnlySort KeyOnlySorter = new();
    private readonly ICredentialsProvider _credentialsProvider;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<V4RequestAuthenticator> _logger;

    private class KeyOnlySort : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            return string.Compare(Key(x), Key(y), StringComparison.Ordinal);
        }

        private static string Key(string? queryParam)
        {
            if (queryParam == null) return string.Empty;
            var valueIndex = queryParam.IndexOf('=', StringComparison.Ordinal);
            return valueIndex == -1 ? queryParam : queryParam[..valueIndex];
        }
    }

#if !NET6_0
    [GeneratedRegex(@"\s\s+")]
    private static partial Regex RegexMultiSpace();
#else
    private static readonly Regex _regexMultiSpace = new(@"\s\s+");
    private static Func<Regex> RegexMultiSpace = () => _regexMultiSpace; 
#endif

    private static readonly Action<ILogger, string, Exception?> LogCanonicalRequest =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(id: 1, name: "CANONICAL_REQUEST"), "Canonical request:\n{CanonicalRequest}");
    private static readonly Action<ILogger, string, Exception?> LogStringToSign =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(id: 2, name: "STRING_TO_SIGN"), "StringToSign:\n{StringToSign}");
    private static readonly Action<ILogger, string, Exception?> LogSignature =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(id: 3, name: "SIGNATURE"), "Signature:\n{Signature}");
    
    /// <summary>
    /// Initializes a new instance of <see cref="V4RequestAuthenticator"/>.
    /// </summary>
    /// <param name="credentialsProvider">The provider used to obtain AWS credentials for signing.</param>
    /// <param name="timeProvider">The time provider used to obtain the current UTC time for the signing date.</param>
    /// <param name="logger">The logger used to emit trace-level diagnostic messages for canonical requests, strings-to-sign, and signatures.</param>
    public V4RequestAuthenticator(ICredentialsProvider credentialsProvider, ITimeProvider timeProvider, ILogger<V4RequestAuthenticator> logger)
    {
        _credentialsProvider = credentialsProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates the specified HTTP request by computing and attaching an AWS Signature Version 4
    /// authorization header. If the credentials include a session token, the <c>X-Amz-Security-Token</c>
    /// header is also added.
    /// </summary>
    /// <param name="request">The outgoing HTTP request message to authenticate.</param>
    /// <param name="region">The AWS region identifier (e.g. <c>us-east-1</c>) used in the credential scope.</param>
    /// <param name="service">The AWS service identifier (e.g. <c>s3</c>) used in the credential scope.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous authentication operation.</returns>
    public async ValueTask AuthenticateAsync(HttpRequestMessage request, string region, string service, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var credentials = await _credentialsProvider.GetCredentialsAsync(cancellationToken).ConfigureAwait(false);
        var authorization = CalculateAuthorization(credentials, region, service, request);
        request.Headers.Authorization = new AuthenticationHeaderValue("AWS4-HMAC-SHA256", authorization);
        if (!string.IsNullOrEmpty(credentials.SessionToken))
            request.Headers.Add("X-Amz-Security-Token", credentials.SessionToken);
    }

    private string CalculateAuthorization(Credentials credentials, string region, string service, HttpRequestMessage request)
    {
        if (request.RequestUri is null) throw new InvalidOperationException("Cannot calculate signature without URI");

        // Use the X-Amz-Date header (if present) to avoid differences between timestamps
        var signingDate = request.Headers.TryGetValues("X-Amz-Date", out var dateValues) ? dateValues.First() : null;
        signingDate ??= _timeProvider.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        
        // Determine canonical URI
        var canonicalUri = request.RequestUri.AbsolutePath; // TODO: Check if it starts with a '/'
        if (string.IsNullOrEmpty(canonicalUri)) canonicalUri = "/";
        
        // Determine canonical query
        var canonicalQueryString = string.Empty;
        var query = request.RequestUri.Query;
        if (!string.IsNullOrEmpty(query))
        {
            if (query[0] == '?')
                query = query[1..];
            var queryItems = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            // Canonical query string should be sorted on the key values, but
            // if a key is specified more than once, then it should maintain
            // the same order. Therefor, "OrderBy" should be "stable" (which it is)
            canonicalQueryString = string.Join('&', queryItems.OrderBy(s => s, KeyOnlySorter));
        }
        
        // Determine canonical and signed headers
        var headerItems = new List<string>();
        var signedHeaderItems = new List<string>();
        headerItems.Add($"host:{request.RequestUri.Authority}\n");
        signedHeaderItems.Add("host");
        foreach (var (name, values) in request.Headers)
        {
#pragma warning disable CA1308  
            // AWS S3 needs lower-case
            var headerName = name.ToLowerInvariant();
#pragma warning restore CA1308
            if (headerName =="content-type" || headerName ==":authority" || headerName.StartsWith("x-amz-", StringComparison.Ordinal))
            {
                var coercedValue = string.Join(',', values.Select(value => RegexMultiSpace().Replace(value.Trim(), " ")));
                headerItems.Add($"{headerName}:{coercedValue}\n");
                signedHeaderItems.Add(headerName);
            }
        }
        headerItems.Sort();
        signedHeaderItems.Sort();
        var canonicalHeaders = string.Join("", headerItems);
        var signedHeaders = string.Join(';', signedHeaderItems);
        
        // Use the X-Amz-Content-Sha256 header (if present) to avoid recalculating hashes
        var payloadHash = request.Headers.TryGetValues("X-Amz-Content-Sha256", out var payloadHashValues) ? payloadHashValues.First() : null;
        payloadHash ??= SHA256.HashData(request.Content?.ReadAsStream() ?? Stream.Null).ToHexStringLowercase();

        var canonicalRequestBuilder = new StringBuilder();
        canonicalRequestBuilder.Append(request.Method.Method);
        canonicalRequestBuilder.Append('\n');
        canonicalRequestBuilder.Append(canonicalUri);
        canonicalRequestBuilder.Append('\n');
        canonicalRequestBuilder.Append(canonicalQueryString);
        canonicalRequestBuilder.Append('\n');
        canonicalRequestBuilder.Append(canonicalHeaders);
        canonicalRequestBuilder.Append('\n');
        canonicalRequestBuilder.Append(signedHeaders);
        canonicalRequestBuilder.Append('\n');
        canonicalRequestBuilder.Append(payloadHash);
        var canonicalRequest = canonicalRequestBuilder.ToString();
        LogCanonicalRequest(_logger, canonicalRequest, null);
        
        var canonicalRequestHash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)).ToHexStringLowercase();

        var stringToSignBuilder = new StringBuilder();
        stringToSignBuilder.Append("AWS4-HMAC-SHA256\n");
        stringToSignBuilder.Append(CultureInfo.InvariantCulture, $"{signingDate}\n");
        stringToSignBuilder.Append(CultureInfo.InvariantCulture, $"{signingDate[..8]}/{region}/{service}/aws4_request\n");
        stringToSignBuilder.Append(canonicalRequestHash);
        var stringToSign = stringToSignBuilder.ToString();
        LogStringToSign(_logger, stringToSign, null);

        var dateKey = HmacSha256($"AWS4{credentials.SecretKey}", signingDate[..8]);
        var dateRegionKey = HmacSha256(dateKey, region);
        var dateRegionServiceKey = HmacSha256(dateRegionKey, service); 
        var signingKey = HmacSha256(dateRegionServiceKey, "aws4_request");

        var signature = HmacSha256(signingKey, stringToSign).ToHexStringLowercase();
        LogSignature(_logger, signature, null);

        return $"Credential={credentials.AccessKey}/{signingDate[..8]}/{region}/{service}/aws4_request, SignedHeaders={signedHeaders}, Signature={signature}";
    }

    private static byte[] HmacSha256(string key, string data) => HmacSha256(Encoding.UTF8.GetBytes(key), data);
    private static byte[] HmacSha256(byte[] key, string data) => HmacSha256(key, Encoding.UTF8.GetBytes(data));
    
    private static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }
}
