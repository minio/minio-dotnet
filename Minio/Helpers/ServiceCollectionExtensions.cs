using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Minio.CredentialProviders;
using Minio.Implementation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A fluent builder interface returned by <see cref="ServiceCollectionServiceExtensions.AddMinio(IServiceCollection, Action{Minio.ClientOptions}?, ServiceLifetime)"/>
/// that allows callers to configure the credential provider used by the MinIO client registered in the
/// dependency injection container.
/// </summary>
public interface IMinioBuilder
{
    /// <summary>
    /// Configures the MinIO client to authenticate using static (hardcoded) credentials,
    /// applying the supplied delegate to an <see cref="StaticCredentialsOptions"/> instance.
    /// </summary>
    /// <param name="configure">A delegate that sets access key, secret key, and optional session token on the options object.</param>
    /// <returns>The current <see cref="IMinioBuilder"/> instance to allow further chaining.</returns>
    IMinioBuilder WithStaticCredentials(Action<StaticCredentialsOptions> configure);

    /// <summary>
    /// Configures the MinIO client to authenticate using the supplied static credentials.
    /// </summary>
    /// <param name="accessKey">The access key (username) used to authenticate requests.</param>
    /// <param name="secretKey">The secret key (password) used to sign requests.</param>
    /// <param name="sessionToken">An optional temporary session token for STS-based credentials.</param>
    /// <returns>The current <see cref="IMinioBuilder"/> instance to allow further chaining.</returns>
    IMinioBuilder WithStaticCredentials(string accessKey, string secretKey, string? sessionToken = null);

    /// <summary>
    /// Configures the MinIO client to read credentials from the <c>MINIO_ROOT_USER</c> and
    /// <c>MINIO_ROOT_PASSWORD</c> environment variables at runtime.
    /// </summary>
    /// <returns>The current <see cref="IMinioBuilder"/> instance to allow further chaining.</returns>
    IMinioBuilder WithEnvironmentCredentials();
}

/// <summary>
/// Provides extension methods on <see cref="IServiceCollection"/> for registering the MinIO client
/// and its dependencies into an ASP.NET Core or generic-host dependency injection container.
/// </summary>
public static class ServiceCollectionServiceExtensions
{
    private sealed class MinioBuilder : IMinioBuilder
    {
        private readonly IServiceCollection _serviceCollection;

        public MinioBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public IMinioBuilder WithStaticCredentials(Action<StaticCredentialsOptions>? configure = null)
        {
            if (configure != null)
                _serviceCollection.Configure(configure);
            _serviceCollection.AddSingleton<ICredentialsProvider, StaticCredentialsProvider>();
            return this;
        }
        
        public IMinioBuilder WithStaticCredentials(string accessKey, string secretKey, string? sessionToken = null)
            => WithStaticCredentials(opts =>
            {
                opts.AccessKey = accessKey;
                opts.SecretKey = secretKey;
                opts.SessionToken = sessionToken;
            });

        public IMinioBuilder WithEnvironmentCredentials()
        {
            _serviceCollection.AddSingleton<ICredentialsProvider, EnvironmentCredentialsProvider>();
            return this;
        }
    }
    
    /// <summary>
    /// Registers the MinIO client services — including an <see cref="IMinioClient"/>,
    /// <see cref="IRequestAuthenticator"/>, and the underlying named <see cref="System.Net.Http.HttpClient"/> with
    /// a Polly retry policy — into the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">
    /// An optional delegate used to configure <see cref="Minio.ClientOptions"/> (e.g. the endpoint URL).
    /// When <see langword="null"/>, the endpoint must be configured separately.
    /// </param>
    /// <param name="lifetime">
    /// The <see cref="ServiceLifetime"/> of the <see cref="IMinioClient"/> and <see cref="IRequestAuthenticator"/>
    /// registrations. Defaults to <see cref="ServiceLifetime.Singleton"/>.
    /// </param>
    /// <returns>An <see cref="IMinioBuilder"/> that allows further configuration of the credential provider.</returns>
    public static IMinioBuilder AddMinio(
        this IServiceCollection services,
        Action<ClientOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        services.AddHttpClient("Minio").AddPolicyHandler(MinioClientBuilder.GetRetryPolicy());
        services.TryAddSingleton<ITimeProvider, DefaultTimeProvider>();
        services.TryAdd(new ServiceDescriptor(typeof(IRequestAuthenticator), typeof(V4RequestAuthenticator), lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IMinioClient), typeof(MinioClient), lifetime));
        if (configure != null)
            services.Configure(configure);
        return new MinioBuilder(services);
    }

    /// <summary>
    /// Registers the MinIO client services and sets the server endpoint from the supplied <see cref="Uri"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="endPoint">The base URI of the MinIO or S3-compatible server (e.g. <c>https://minio.example.com</c>).</param>
    /// <returns>An <see cref="IMinioBuilder"/> that allows further configuration of the credential provider.</returns>
    public static IMinioBuilder AddMinio(this IServiceCollection services, Uri endPoint)
        => services.AddMinio(opts => opts.EndPoint = endPoint);

    /// <summary>
    /// Registers the MinIO client services and sets the server endpoint from the supplied URI string.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="endPoint">The base URI string of the MinIO or S3-compatible server (e.g. <c>"https://minio.example.com"</c>).</param>
    /// <returns>An <see cref="IMinioBuilder"/> that allows further configuration of the credential provider.</returns>
    public static IMinioBuilder AddMinio(this IServiceCollection services, string endPoint)
        => services.AddMinio(opts => opts.EndPoint = new Uri(endPoint));
}