using System.Net;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio.CredentialProviders;
using Minio.Implementation;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Minio;

/// <summary>
/// Fluent builder for constructing an <see cref="IMinioClient"/> instance without a dependency
/// injection container. Configure the endpoint, region, and credentials using the
/// <c>With*</c> methods, then call <see cref="Build"/> to create the client.
/// </summary>
/// <example>
/// <code>
/// var client = new MinioClientBuilder("https://minio.example.com")
///     .WithStaticCredentials("accessKey", "secretKey")
///     .Build();
/// </code>
/// </example>
public sealed class MinioClientBuilder
{
    /// <summary>
    /// Gets the URI of the MinIO or S3-compatible endpoint.
    /// </summary>
    public Uri EndPoint { get; }

    /// <summary>
    /// Gets the AWS region used for request signing. Defaults to <c>us-east-1</c>.
    /// Can be overridden with <see cref="WithRegion"/>.
    /// </summary>
    public string Region { get; private set; } = "us-east-1";

    /// <summary>
    /// Gets the credentials provider that will be used to authenticate requests.
    /// Set via one of the <c>With*Credentials*</c> methods or <see cref="WithCredentialsProvider"/>.
    /// </summary>
    public ICredentialsProvider? CredentialsProvider { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="MinioClientBuilder"/> with the specified endpoint URI string.
    /// </summary>
    /// <param name="endPoint">The endpoint URL of the MinIO or S3-compatible service (e.g., <c>https://minio.example.com</c>).</param>
    public MinioClientBuilder(string endPoint) : this(new Uri(endPoint))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MinioClientBuilder"/> with the specified endpoint URI.
    /// </summary>
    /// <param name="endPoint">The <see cref="Uri"/> of the MinIO or S3-compatible service endpoint.</param>
    public MinioClientBuilder(Uri endPoint)
    {
        EndPoint = endPoint;
    }

    /// <summary>
    /// Builds and returns a configured <see cref="IMinioClient"/> instance using the current
    /// builder settings.
    /// </summary>
    /// <returns>A fully configured <see cref="IMinioClient"/> ready for use.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no credentials provider has been configured. Call one of the
    /// <c>With*Credentials*</c> methods before calling <see cref="Build"/>.
    /// </exception>
    public IMinioClient Build()
    {
        if (CredentialsProvider == null)
            throw new InvalidOperationException("No credentials specified");

        var clientOptions = Options.Create(new ClientOptions
        {
            EndPoint = EndPoint,
            Region = Region,
        });
        var timeProvider = new DefaultTimeProvider();
        var authLogger = NullLoggerFactory.Instance.CreateLogger<V4RequestAuthenticator>();
        var authenticator = new V4RequestAuthenticator(CredentialsProvider, timeProvider, authLogger);
        var httpClientFactory = new HttpClientFactory();
        var minioLogger = NullLoggerFactory.Instance.CreateLogger<MinioClient>();
        return new MinioClient(clientOptions, timeProvider, authenticator, httpClientFactory, minioLogger);
    }

    /// <summary>
    /// Sets the AWS region used for request signing and returns the builder for chaining.
    /// </summary>
    /// <param name="region">The AWS region identifier (e.g., <c>eu-west-1</c>).</param>
    /// <returns>The current <see cref="MinioClientBuilder"/> instance for fluent chaining.</returns>
    public MinioClientBuilder WithRegion(string region)
    {
        Region = region;
        return this;
    }

    /// <summary>
    /// Sets a custom <see cref="ICredentialsProvider"/> and returns the builder for chaining.
    /// Use this overload to supply a fully custom or STS-based credentials provider.
    /// </summary>
    /// <param name="credentialsProvider">The credentials provider to use for authenticating requests.</param>
    /// <returns>The current <see cref="MinioClientBuilder"/> instance for fluent chaining.</returns>
    public MinioClientBuilder WithCredentialsProvider(ICredentialsProvider credentialsProvider)
    {
        CredentialsProvider = credentialsProvider;
        return this;
    }

    /// <summary>
    /// Configures the client to authenticate using a fixed access key, secret key, and optional
    /// session token, then returns the builder for chaining.
    /// </summary>
    /// <param name="accessKey">The access key ID.</param>
    /// <param name="secretKey">The secret access key.</param>
    /// <param name="sessionToken">
    /// An optional temporary session token (e.g., from STS AssumeRole). Pass <see langword="null"/>
    /// for long-term credentials.
    /// </param>
    /// <returns>The current <see cref="MinioClientBuilder"/> instance for fluent chaining.</returns>
    public MinioClientBuilder WithStaticCredentials(string accessKey, string secretKey, string? sessionToken = null)
    {
        var credentialOptions = Options.Create(new StaticCredentialsOptions
        {
            AccessKey = accessKey,
            SecretKey = secretKey,
            SessionToken = sessionToken,
        });
        return WithCredentialsProvider(new StaticCredentialsProvider(credentialOptions));
    }

    /// <summary>
    /// Configures the client to read credentials from the <c>MINIO_ROOT_USER</c> and
    /// <c>MINIO_ROOT_PASSWORD</c> environment variables, then returns the builder for chaining.
    /// </summary>
    /// <returns>The current <see cref="MinioClientBuilder"/> instance for fluent chaining.</returns>
    public MinioClientBuilder WithEnvironmentCredentials()
    {
        return WithCredentialsProvider(new EnvironmentCredentialsProvider());
    }

    internal static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return Policy<HttpResponseMessage>.Handle<HttpRequestException>().OrResult(resp =>
        {
            switch (resp.StatusCode)
            {
                case HttpStatusCode.RequestTimeout /* 408 */:
                case HttpStatusCode.Locked /* 423 */:
                case HttpStatusCode.TooManyRequests /* 429 */:
                case HttpStatusCode.InternalServerError /* 500 */:
                case HttpStatusCode.BadGateway /* 502 */:
                case HttpStatusCode.ServiceUnavailable /* 503 */:
                case HttpStatusCode.GatewayTimeout /* 504 */:
                    return true;
            }
            return false;
        }).WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromMilliseconds(250),
            retryCount: 5
        ));

    }

    private sealed class HttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _httpMessageHandler;

        public HttpClientFactory()
        {
            var socketHandler = new SocketsHttpHandler();
            var pollyHttpMessageHandler = new PolicyHttpMessageHandler(GetRetryPolicy());
            pollyHttpMessageHandler.InnerHandler = socketHandler;
            _httpMessageHandler = pollyHttpMessageHandler;
        }

        public HttpClient CreateClient(string name) => new(_httpMessageHandler, false);
    }
}
