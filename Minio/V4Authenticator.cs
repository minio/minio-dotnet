/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017, 2020 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Minio.Helper;

namespace Minio;

/// <summary>
///     V4Authenticator implements IAuthenticator interface.
/// </summary>
internal class V4Authenticator
{
    private const string Scheme = "AWS4";
    private const string SigningAlgorithm = "HMAC-SHA256";

    public const string Terminator = "aws4_request";

    //
    // Excerpts from @lsegal - https://github.com/aws/aws-sdk-js/issues/659#issuecomment-120477258
    //
    // User-Agent:
    //
    //     This is ignored from signing because signing this causes problems with generating pre-signed URLs
    //     (that are executed by other agents) or when customers pass requests through proxies, which may
    //     modify the user-agent.
    //
    // Authorization:
    //
    //     Is skipped for obvious reasons
    //
    private static readonly HashSet<string> ignoredHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "user-agent"
    };

    public static readonly byte[] TerminatorBytes = Encoding.UTF8.GetBytes(Terminator);

    private readonly string accessKey;
    public readonly string AWS4AlgorithmTag = string.Format("{0}-{1}", Scheme, SigningAlgorithm);
    private readonly string region;
    private readonly string secretKey;
    private readonly string sessionToken;

    private readonly string sha256EmptyFileHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>
    ///     Authenticator constructor.
    /// </summary>
    /// <param name="secure"></param>
    /// <param name="accessKey">Access key id</param>
    /// <param name="secretKey">Secret access key</param>
    /// <param name="region">Region if specifically set</param>
    /// <param name="sessionToken">sessionToken</param>
    public V4Authenticator(bool secure, string accessKey, string secretKey, string region = "",
        string sessionToken = "")
    {
        IsSecure = secure;
        this.accessKey = accessKey;
        this.secretKey = secretKey;
        IsAnonymous = Utils.IsAnonymousClient(accessKey, secretKey);
        this.region = region;
        this.sessionToken = sessionToken;
    }

    internal bool IsAnonymous { get; }
    internal bool IsSecure { get; }

    private string GetRegion(string endpoint)
    {
        if (!string.IsNullOrEmpty(region)) return region;

        var endpointRegion = RegionHelper.GetRegionFromEndpoint(endpoint);
        return string.IsNullOrEmpty(endpointRegion) ? "us-east-1" : endpointRegion;
    }

    /// <summary>
    ///     Implements Authenticate interface method for IAuthenticator.
    /// </summary>
    /// <param name="requestBuilder">Instantiated IRestRequest object</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    public string Authenticate(HttpRequestMessageBuilder requestBuilder, bool isSts = false)
    {
        var signingDate = DateTime.UtcNow;

        SetContentSha256(requestBuilder, isSts);

        requestBuilder.RequestUri = requestBuilder.Request.RequestUri;
        var requestUri = requestBuilder.RequestUri;

        if (requestUri.Port is 80 or 443)
            SetHostHeader(requestBuilder, requestUri.Host);
        else
            SetHostHeader(requestBuilder, requestUri.Host + ":" + requestUri.Port);
        SetDateHeader(requestBuilder, signingDate);
        SetSessionTokenHeader(requestBuilder, sessionToken);

        var headersToSign = GetHeadersToSign(requestBuilder);
        var signedHeaders = GetSignedHeaders(headersToSign);

        var canonicalRequest = GetCanonicalRequest(requestBuilder, (SortedDictionary<string, string>)headersToSign);
        ReadOnlySpan<byte> canonicalRequestBytes = Encoding.UTF8.GetBytes(canonicalRequest);
        var hash = Utils.ComputeSha256(canonicalRequestBytes);
        var canonicalRequestHash = Utils.BytesToHex(hash);
        var endpointRegion = GetRegion(requestUri.Host);
        var stringToSign = GetStringToSign(endpointRegion, signingDate, canonicalRequestHash, isSts);
        var signingKey = GenerateSigningKey(endpointRegion, signingDate, isSts);
        ReadOnlySpan<byte> stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);
        var signatureBytes = Utils.SignHmac(signingKey, stringToSignBytes);
        var signature = Utils.BytesToHex(signatureBytes);
        return GetAuthorizationHeader(signedHeaders, signature, signingDate, endpointRegion, isSts);
    }

    /// <summary>
    ///     Get credential string of form {ACCESSID}/date/region/serviceKind/aws4_request.
    /// </summary>
    /// <param name="signingDate">Signature initiated date</param>
    /// <param name="region">Region for the credential string</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>Credential string for the authorization header</returns>
    public string GetCredentialString(DateTime signingDate, string region, bool isSts = false)
    {
        var scope = GetScope(region, signingDate, isSts);
        return $"{accessKey}/{scope}";
    }

    /// <summary>
    ///     Constructs an authorization header.
    /// </summary>
    /// <param name="signedHeaders">All signed http headers</param>
    /// <param name="signature">Hexadecimally encoded computed signature</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="region">Requested region</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>Fully formed authorization header</returns>
    private string GetAuthorizationHeader(string signedHeaders, string signature, DateTime signingDate, string region,
        bool isSts = false)
    {
        var scope = GetScope(region, signingDate, isSts);
        return $"AWS4-HMAC-SHA256 Credential={accessKey}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}";
    }

    /// <summary>
    ///     Concatenates sorted list of signed http headers.
    /// </summary>
    /// <param name="headersToSign">Sorted dictionary of headers to be signed</param>
    /// <returns>All signed headers</returns>
    private string GetSignedHeaders(IDictionary<string, string> headersToSign)
    {
        return string.Join(";", headersToSign.Keys);
    }

    /// <summary>
    ///     Determines and returns the kind of service
    /// </summary>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>returns the kind of service as a string</returns>
    private string GetService(bool isSts)
    {
        return isSts ? "sts" : "s3";
    }

    /// <summary>
    ///     Generates signing key based on the region and date.
    /// </summary>
    /// <param name="region">Requested region</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>bytes of computed hmac</returns>
    private ReadOnlySpan<byte> GenerateSigningKey(string region, DateTime signingDate, bool isSts = false)
    {
        byte[] key = null;
        try
        {
            key = Encoding.UTF8.GetBytes(string.Format("{0}{1}", Scheme, secretKey));
            var dateKey = Utils.SignHmac(key, Encoding.UTF8.GetBytes(Utils.FormatDate(signingDate)));
            var dateRegionKey = Utils.SignHmac(dateKey, Encoding.UTF8.GetBytes(region));
            var dateRegionServiceKey = Utils.SignHmac(dateRegionKey, Encoding.UTF8.GetBytes(GetService(isSts)));
            return Utils.SignHmac(dateRegionServiceKey, TerminatorBytes);
        }
        finally
        {
            if (key is not null)
                Array.Clear(key, 0, key.Length);
        }
    }

    /// <summary>
    ///     Get string to sign.
    /// </summary>
    /// <param name="region">Requested region</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="canonicalRequestHash">Hexadecimal encoded sha256 checksum of canonicalRequest</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>String to sign</returns>
    private string GetStringToSign(
        string region,
        DateTime signingDate,
        string canonicalRequestHash,
        bool isSts = false)
    {
        var scope = GetScope(region, signingDate, isSts);
        var stringToSignBuilder = new StringBuilder();
        _ = stringToSignBuilder.AppendFormat(
            CultureInfo.InvariantCulture, "{0}-{1}\n{2}\n{3}\n",
            Scheme, SigningAlgorithm, Utils.FormatDateTime(signingDate), scope);
        _ = stringToSignBuilder.Append(canonicalRequestHash);
        return stringToSignBuilder.ToString();
    }

    /// <summary>
    ///     Get scope.
    /// </summary>
    /// <param name="region">Requested region</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>Scope string</returns>
    private string GetScope(string region, DateTime signingDate, bool isSts = false)
    {
        return $"{Utils.FormatDate(signingDate)}/{region}/{GetService(isSts)}/aws4_request";
    }

    /// <summary>
    ///     Generate signature for post policy.
    /// </summary>
    /// <param name="region">Requested region</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="policyBase64">Base64 encoded policy JSON</param>
    /// <returns>Computed signature</returns>
    public string PresignPostSignature(string region, DateTime signingDate, string policyBase64)
    {
        var signingKey = GenerateSigningKey(region, signingDate);
        ReadOnlySpan<byte> stringToSignBytes = Encoding.UTF8.GetBytes(policyBase64);
        var signatureBytes = Utils.SignHmac(signingKey, stringToSignBytes);
        var signature = Utils.BytesToHex(signatureBytes);
        return signature;
    }

    /// <summary>
    ///     Presigns any input client object with a requested expiry.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder</param>
    /// <param name="expires">Expiration in seconds</param>
    /// <param name="region">Region of storage</param>
    /// <param name="sessionToken">Value for session token</param>
    /// <param name="reqDate"> Optional requestBuilder date and time in UTC</param>
    /// <returns>Presigned url</returns>
    internal string PresignURL(
        HttpRequestMessageBuilder requestBuilder,
        int expires,
        string region = "",
        string sessionToken = "",
        DateTime? reqDate = null)
    {
        var signingDate = reqDate ?? DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(region)) region = GetRegion(requestBuilder.RequestUri.Host);
        var requestUri = requestBuilder.RequestUri;
        if (requestUri.Port is 80 or 443)
            SetHostHeader(requestBuilder, requestUri.Host);
        else
            SetHostHeader(requestBuilder, requestUri.Host + ":" + requestUri.Port);

        SetSessionTokenHeader(requestBuilder, sessionToken);

        var headersToSign = GetHeadersToSign(requestBuilder);
        var signedHeaders = GetSignedHeaders(headersToSign);

        var requestQuery = GetCanonicalQueryString(requestBuilder.RequestUri, reqDate, region, expires, signedHeaders);
        var canonicalRequest =
            GetPresignCanonicalRequest(requestBuilder.Method, requestUri, headersToSign, requestQuery);

        var canonicalRequestHash = Utils.BytesToHex(Utils.ComputeSha256(canonicalRequest));
        var stringToSign = GetStringToSign(region, signingDate, canonicalRequestHash);
        var signingKey = GenerateSigningKey(region, signingDate);

        var signatureBytes = Utils.SignHmac(signingKey, Encoding.UTF8.GetBytes(stringToSign));
        var signature = Utils.BytesToHex(signatureBytes);
        return ComposePresignedPutUrl(requestUri, requestQuery, signature);
    }

    private string ComposePresignedPutUrl(
        Uri presignUri,
        string queryParams,
        string signature)
    {
        var authParams = new StringBuilder(queryParams)
            .AppendFormat(CultureInfo.InvariantCulture, "&{0}={1}", Utils.UrlEncode(Constants.XAmzSignature),
                Utils.UrlEncode(signature));

        var signedUri = new UriBuilder(presignUri) { Query = authParams.ToString() };
        if (signedUri.Uri.IsDefaultPort) signedUri.Port = -1;
        return Convert.ToString(signedUri, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Generates canonical query string.
    /// </summary>
    /// <param name="requestUri"></param>
    /// <param name="reqDate"></param>
    /// <param name="region"></param>
    /// <param name="expires"></param>
    /// <param name="signedHeaders"></param>
    /// <param name="isSts"></param>
    /// <returns>Canonical query string</returns>
    internal string GetCanonicalQueryString(
        Uri requestUri,
        DateTime? reqDate,
        string region,
        int expires,
        string signedHeaders,
        bool isSts = false)
    {
        var canonicalQueryString = new StringBuilder(requestUri.Query);
        if (canonicalQueryString.Length != 0) _ = canonicalQueryString.Append("&");
        var signingDate = reqDate ?? DateTime.UtcNow;
        var creds = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}/{3}/{4}", accessKey,
            Utils.FormatDate(signingDate), region, GetService(isSts), Terminator);
        var queryParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { Constants.XAmzAlgorithm, AWS4AlgorithmTag },
            { Constants.XAmzCredential, creds },
            { Constants.XAmzDate, Utils.FormatDateTime(signingDate) },
            { Constants.XAmzExpires, Convert.ToString(expires) },
            { Constants.XAmzSignedHeaders, signedHeaders }
        };
        foreach (var query in queryParams)
        {
            if (canonicalQueryString.Length > 0)
                _ = canonicalQueryString.Append("&");
            _ = canonicalQueryString.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", Utils.UrlEncode(query.Key),
                Utils.UrlEncode(query.Value));
        }

        return canonicalQueryString.ToString();
    }

    /// <summary>
    ///     Generates canonical headers
    /// </summary>
    /// <param name="headers">Headers that will be formatted</param>
    /// <returns>Formatted headers</returns>
    internal string GetCanonicalHeaders(IDictionary<string, string> headers)
    {
        if (headers == null || headers.Count() == 0)
            return string.Empty;

        var canonicalHeaders = new StringBuilder();

        foreach (var header in headers)
        {
            _ = canonicalHeaders.Append(header.Key.ToLowerInvariant());
            _ = canonicalHeaders.Append(":");
            _ = canonicalHeaders.Append(S3utils.TrimAll(header.Value));
            _ = canonicalHeaders.Append("\n");
        }

        return canonicalHeaders.ToString();
    }

    /// <summary>
    ///     Get presign canonical requestBuilder.
    /// </summary>
    /// <param name="requestMethod">HTTP method used for this requestBuilder</param>
    /// <param name="uri">
    ///     Full url for this requestBuilder, including all query parameters except for headers and
    ///     X-Amz-Signature
    /// </param>
    /// <param name="headersToSign">The key-value of headers.</param>
    /// <param name="canonicalQueryString">Canonical query string</param>
    /// <returns>Presigned canonical requestBuilder</returns>
    internal string GetPresignCanonicalRequest(
        HttpMethod requestMethod,
        Uri uri,
        IDictionary<string, string> headersToSign,
        string canonicalQueryString)
    {
        var canonicalRequest = new StringBuilder();
        _ = canonicalRequest.AppendFormat(CultureInfo.InvariantCulture, "{0}\n", requestMethod.ToString());
        var canonicalUri = uri.AbsolutePath;
        _ = canonicalRequest.AppendFormat(CultureInfo.InvariantCulture, "{0}\n", canonicalUri);
        _ = canonicalRequest.AppendFormat(CultureInfo.InvariantCulture, "{0}\n", canonicalQueryString);
        _ = canonicalRequest
            .AppendFormat(CultureInfo.InvariantCulture, "{0}\n", GetCanonicalHeaders(headersToSign));
        _ = canonicalRequest
            .AppendFormat(CultureInfo.InvariantCulture, "{0}\n", GetSignedHeaders(headersToSign));
        _ = canonicalRequest.Append("UNSIGNED-PAYLOAD");
        return canonicalRequest.ToString();
    }

    /// <summary>
    ///     Get canonical requestBuilder.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="headersToSign">Dictionary of http headers to be signed</param>
    /// <returns>Canonical Request</returns>
    private string GetCanonicalRequest(HttpRequestMessageBuilder requestBuilder,
        IDictionary<string, string> headersToSign)
    {
        var canonicalStringList = new LinkedList<string>();
        // METHOD
        _ = canonicalStringList.AddLast(requestBuilder.Method.ToString());

        var queryParamsDict = new Dictionary<string, string>(StringComparer.Ordinal);
        if (requestBuilder.QueryParameters is not null)
            foreach (var kvp in requestBuilder.QueryParameters)
                queryParamsDict[kvp.Key] = Uri.EscapeDataString(kvp.Value);

        var queryParams = "";
        if (queryParamsDict.Count > 0)
        {
            var sb1 = new StringBuilder();
            var queryKeys = new List<string>(queryParamsDict.Keys);
            queryKeys.Sort(StringComparer.Ordinal);
            foreach (var p in queryKeys)
            {
                if (sb1.Length > 0)
                    _ = sb1.Append('&');
                _ = sb1.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", p, queryParamsDict[p]);
            }

            queryParams = sb1.ToString();
        }

        var isFormData = false;
        if (requestBuilder.Request.Content?.Headers?.ContentType is not null)
            isFormData = string.Equals(requestBuilder.Request.Content.Headers.ContentType.ToString(),
                "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(queryParams) && isFormData)
        {
            // Convert stream content to byte[]
            var cntntByteData = Span<byte>.Empty;
            if (requestBuilder.Request.Content is not null)
                cntntByteData = requestBuilder.Request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

            // UTF conversion - String from bytes
            queryParams = Encoding.UTF8.GetString(cntntByteData);
        }

        if (!string.IsNullOrEmpty(queryParams) &&
            !isFormData &&
            !string.Equals(requestBuilder.RequestUri.Query, "?location=", StringComparison.OrdinalIgnoreCase))
            requestBuilder.RequestUri = new Uri(requestBuilder.RequestUri + "?" + queryParams);

        _ = canonicalStringList.AddLast(requestBuilder.RequestUri.AbsolutePath);
        _ = canonicalStringList.AddLast(queryParams);

        // Headers to sign
        foreach (var header in headersToSign.Keys)
            _ = canonicalStringList.AddLast(header + ":" + S3utils.TrimAll(headersToSign[header]));
        _ = canonicalStringList.AddLast(string.Empty);
        _ = canonicalStringList.AddLast(string.Join(";", headersToSign.Keys));
        _ = headersToSign.TryGetValue("x-amz-content-sha256", out var value)
            ? canonicalStringList.AddLast(value)
            : canonicalStringList.AddLast(sha256EmptyFileHash);
        return string.Join("\n", canonicalStringList);
    }

    /// <summary>
    ///     Get headers to be signed.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requesst</param>
    /// <returns>Sorted dictionary of headers to be signed</returns>
    private IDictionary<string, string> GetHeadersToSign(HttpRequestMessageBuilder requestBuilder)
    {
        var headers = requestBuilder.HeaderParameters;
        var sortedHeaders = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var header in headers)
        {
            var headerName = header.Key.ToLowerInvariant();
            var headerValue = header.Value;
            if (string.Equals(header.Key, "versionId", StringComparison.Ordinal)) headerName = "versionId";
            if (!ignoredHeaders.Contains(headerName)) sortedHeaders.Add(headerName, headerValue);
        }

        return sortedHeaders;
    }

    /// <summary>
    ///     Sets 'x-amz-date' http header.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    private void SetDateHeader(HttpRequestMessageBuilder requestBuilder, DateTime signingDate)
    {
        requestBuilder.AddOrUpdateHeaderParameter("x-amz-date", Utils.FormatDateTime(signingDate));
    }

    /// <summary>
    ///     Set 'Host' http header.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="hostUrl">Host url</param>
    private void SetHostHeader(HttpRequestMessageBuilder requestBuilder, string hostUrl)
    {
        requestBuilder.AddOrUpdateHeaderParameter("Host", hostUrl);
    }

    /// <summary>
    ///     Set 'X-Amz-Security-Token' http header.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="sessionToken">session token</param>
    private void SetSessionTokenHeader(HttpRequestMessageBuilder requestBuilder, string sessionToken)
    {
        if (!string.IsNullOrEmpty(sessionToken))
            requestBuilder.AddOrUpdateHeaderParameter("X-Amz-Security-Token", sessionToken);
    }

    /// <summary>
    ///     Set 'x-amz-content-sha256' http header.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    private void SetContentSha256(HttpRequestMessageBuilder requestBuilder, bool isSts = false)
    {
        if (IsAnonymous)
            return;
        // No need to compute SHA256 if the endpoint scheme is https
        // or the command method is not a Post to delete multiple files
        var isMultiDeleteRequest = false;
        if (requestBuilder.Method == HttpMethod.Post)
            isMultiDeleteRequest =
                requestBuilder.QueryParameters.Any(p => p.Key.Equals("delete", StringComparison.OrdinalIgnoreCase));

        if ((IsSecure && !isSts) || isMultiDeleteRequest)
        {
            requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", "UNSIGNED-PAYLOAD");
            return;
        }

        // For insecure, authenticated requests set sha256 header instead of MD5.
        if (requestBuilder.Method.Equals(HttpMethod.Put) ||
            requestBuilder.Method.Equals(HttpMethod.Post))
        {
            var body = requestBuilder.Content;
            if (body.IsEmpty)
            {
                requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", sha256EmptyFileHash);
                return;
            }
#if NETSTANDARD
            using var sha = SHA256.Create();
            var hash
                = sha.ComputeHash(body.ToArray());
#else
            var hash = SHA256.HashData(body.Span);
#endif
            var hex = BitConverter.ToString(hash).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
            requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", hex);
        }
        else if (!IsSecure && !requestBuilder.Content.IsEmpty)
        {
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(requestBuilder.Content.ToString());

#if NETSTANDARD
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
            using var md5
                = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
            var hash
                = md5.ComputeHash(bytes.ToArray());
#else
            ReadOnlySpan<byte> hash = MD5.HashData(bytes);
#endif
            var base64 = Convert.ToBase64String(hash);
            requestBuilder.AddHeaderParameter("Content-Md5", base64);
        }
        else
        {
            requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", sha256EmptyFileHash);
        }
    }
}
