using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Minio.Helpers;
using Minio.Model;

namespace Minio.CredentialProviders;

/// <summary>
/// An <see cref="ICredentialsProvider"/> implementation that obtains temporary AWS/MinIO credentials
/// by calling the STS <c>AssumeRoleWithWebIdentity</c> action. An OIDC or JWT bearer token is
/// obtained via an <see cref="IAccessTokenProvider"/> and exchanged at the configured STS endpoint
/// for short-lived credentials (access key, secret key, and session token) that can be used to
/// authenticate S3-compatible requests.
/// </summary>
public class WebIdentityProvider : ICredentialsProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly IOptions<WebIdentityCredentialsOptions> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="WebIdentityProvider"/>.
    /// </summary>
    /// <param name="httpClientFactory">
    /// The factory used to create the <see cref="HttpClient"/> for calling the STS endpoint.
    /// The named client specified by <see cref="WebIdentityCredentialsOptions.MinioHttpClient"/> is used.
    /// </param>
    /// <param name="accessTokenProvider">
    /// The provider that supplies the OIDC or JWT token passed as the <c>WebIdentityToken</c>
    /// to the STS <c>AssumeRoleWithWebIdentity</c> action.
    /// </param>
    /// <param name="options">
    /// The <see cref="IOptions{TOptions}"/> wrapper containing the STS endpoint URL, role ARN,
    /// session duration, and other configuration for the web identity exchange.
    /// </param>
    public WebIdentityProvider(IHttpClientFactory httpClientFactory, IAccessTokenProvider accessTokenProvider, IOptions<WebIdentityCredentialsOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _accessTokenProvider = accessTokenProvider;
        _options = options;
    }

    /// <summary>
    /// Asynchronously obtains temporary credentials by exchanging a web identity token with the
    /// STS <c>AssumeRoleWithWebIdentity</c> action at the configured endpoint.
    /// The returned credentials include an access key, secret key, session token, and expiration time.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that resolves to a <see cref="Credentials"/> instance containing
    /// the temporary access key, secret key, session token, and expiration returned by STS.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <see cref="IAccessTokenProvider"/> returns a null or empty access token,
    /// or when the STS success response does not contain a <c>Credentials</c> element.
    /// </exception>
    /// <exception cref="MinioHttpException">
    /// Thrown when the STS endpoint returns a non-success HTTP status code.
    /// If the response body is XML, the error code and message are extracted from the
    /// <c>&lt;Error&gt;</c> child element of the STS error response.
    /// </exception>
    public async ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        var accessToken = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(accessToken)) throw new InvalidOperationException("No access token");

        var opts = _options.Value;
        var query = new QueryParams();
        query.Add("Action", "AssumeRoleWithWebIdentity");
        query.Add("Version", "2011-06-15");
        query.Add("WebIdentityToken", accessToken);
        query.Add("DurationSeconds", opts.DurationSeconds.ToString(CultureInfo.InvariantCulture));
        query.AddIfNotNullOrEmpty("Policy", opts.Policy);
        query.AddIfNotNullOrEmpty("RoleARN", opts.RoleARN);
        query.AddIfNotNullOrEmpty("TokenRevokeType", opts.TokenRevokeType);

        var builder = new UriBuilder(opts.StsEndPoint)
        {
            Query = query.ToString()
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, builder.Uri);
        using var httpClient = _httpClientFactory.CreateClient(opts.MinioHttpClient);
        using var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var responseData = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var contentType = resp.Content.Headers.ContentType?.MediaType;
            if (contentType != "application/xml" || string.IsNullOrEmpty(responseData))
                throw new MinioHttpException(req, resp, null);
            
            var xRoot = XDocument.Parse(responseData).Root;
            if (xRoot == null) throw new MinioHttpException(req, resp, null);
            
            var xError = xRoot.Element("Error");
            var err = new ErrorResponse
            {
                Code = xError?.Element("Code")?.Value ?? string.Empty,
                Message = xError?.Element("Message")?.Value ?? string.Empty,
                RequestId = xRoot.Element("RequestId")?.Value ?? string.Empty,
                BucketName = string.Empty,
                Key = string.Empty,
                Resource = string.Empty,
                HostId = string.Empty,
                Region = string.Empty,
                Server = string.Empty,
            };
            throw new MinioHttpException(req, resp, err);

        }

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (responseBody.ConfigureAwait(false))
        {
            var xResponse = await XmlHelper.LoadXDocumentAsync(responseBody, cancellationToken).ConfigureAwait(false);
            var xCreds = xResponse.Root?.Element("AssumeRoleWithWebIdentityResult")?.Element("Credentials");
            if (xCreds == null)
                throw new InvalidOperationException("STS response did not contain a Credentials element.");
            var exp = xCreds.Element("Expiration")?.Value;
            return new Credentials
            {
                AccessKey = xCreds.Element("AccessKeyId")?.Value ?? string.Empty,
                SecretKey = xCreds.Element("SecretAccessKey")?.Value ?? string.Empty,
                SessionToken = xCreds.Element("SessionToken")?.Value ?? string.Empty,
                Expiration = exp != null ? DateTime.Parse(exp, null, DateTimeStyles.RoundtripKind) : null,
            };
        }
    }
}
