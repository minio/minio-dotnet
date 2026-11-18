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
    /// Creates a provider for the AWS web-identity flow, configured from the environment:
    /// <c>AWS_WEB_IDENTITY_TOKEN_FILE</c> (required, read on every call), <c>AWS_ROLE_ARN</c> and <c>AWS_ROLE_SESSION_NAME</c> (optional),
    /// and the STS endpoint, resolved as: <paramref name="stsEndpoint"/>, <c>AWS_ENDPOINT_URL_STS</c>,
    /// then <c>sts[.AWS_REGION].amazonaws.com[.cn]</c>. The global <c>AWS_ENDPOINT_URL</c> is not read.
    /// <c>AWS_ENDPOINT_URL_STS</c> is ignored when <c>AWS_IGNORE_CONFIGURED_ENDPOINT_URLS</c> is <c>true</c>;
    /// any URL path is kept. Only when the endpoint came from <c>AWS_ENDPOINT_URL_STS</c> and
    /// <paramref name="httpClient"/> is <see langword="null"/>, <c>AWS_CA_BUNDLE</c> (PEM file, read on every
    /// credential retrieval) is trusted in addition to the system certificates; in every other case the variable
    /// is not read. A supplied client is used as is and is never modified or disposed.
    /// </summary>
    /// <param name="stsEndpoint">Optional explicit STS endpoint, which takes precedence over the environment. <c>AWS_CA_BUNDLE</c> does not apply to it.</param>
    /// <param name="httpClient">Optional client for the STS request.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by this method when <c>AWS_WEB_IDENTITY_TOKEN_FILE</c> is not set. An unreadable
    /// <c>AWS_CA_BUNDLE</c>, or one without certificates, is thrown later, from
    /// <see cref="GetCredentialsAsync"/>, because the provider reads the bundle when it creates its own client.
    /// </exception>
    public static WebIdentityProvider FromAwsEnvironment(string? stsEndpoint = null, HttpClient? httpClient = null)
    {
        var tokenFile = AwsWebIdentityEnvironment.GetEnv("AWS_WEB_IDENTITY_TOKEN_FILE")
            ?? throw new InvalidOperationException("AWS_WEB_IDENTITY_TOKEN_FILE is not set.");
        var options = new WebIdentityCredentialsOptions
        {
            StsEndPoint = AwsWebIdentityEnvironment.ResolveStsEndpoint(stsEndpoint, out var fromStsVariable),
            RoleARN = AwsWebIdentityEnvironment.GetEnv("AWS_ROLE_ARN")!,
            RoleSessionName = AwsWebIdentityEnvironment.GetEnv("AWS_ROLE_SESSION_NAME"),
            AwsRequestFormat = true,
        };
        return new WebIdentityProvider(
            AwsWebIdentityEnvironment.CreateFactory(httpClient, fromStsVariable),
            new FileAccessTokenProvider(tokenFile),
            Options.Create(options));
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
        query.AddIfNotNullOrEmpty(opts.AwsRequestFormat ? "RoleArn" : "RoleARN", opts.RoleARN);
        query.AddIfNotNullOrEmpty("TokenRevokeType", opts.TokenRevokeType);
        var sessionName = opts.RoleSessionName;
        if (opts.AwsRequestFormat && string.IsNullOrEmpty(sessionName) && !string.IsNullOrEmpty(opts.RoleARN))
        {
            // AWS STS requires a session name whenever a role is assumed (minio-go does the same).
            sessionName = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        }
        query.AddIfNotNullOrEmpty("RoleSessionName", sessionName);

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
