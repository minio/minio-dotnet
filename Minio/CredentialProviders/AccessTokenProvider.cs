using Microsoft.Extensions.Options;

namespace Minio.CredentialProviders;

/// <summary>
/// Defines a provider that supplies an OIDC or JWT bearer access token, used as the
/// <c>WebIdentityToken</c> when calling the STS <c>AssumeRoleWithWebIdentity</c> action.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Asynchronously retrieves the current access token.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that resolves to the access token string.
    /// Returns an empty string when no token is available.
    /// </returns>
    ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Configuration options for <see cref="StaticAccessTokenProvider"/>.
/// Holds a fixed access token value used as the web identity token.
/// </summary>
public class StaticAccessTokenProviderOptions
{
    /// <summary>
    /// Gets or sets the static access token string to be returned by the provider.
    /// This value is required.
    /// </summary>
    public required string AccessToken { get; set; }
}

/// <summary>
/// An <see cref="IAccessTokenProvider"/> implementation that returns a fixed, pre-configured
/// access token. Suitable for testing or scenarios where the token is known at startup and
/// does not need to be refreshed.
/// </summary>
public class StaticAccessTokenProvider : IAccessTokenProvider
{
    private readonly IOptions<StaticAccessTokenProviderOptions> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="StaticAccessTokenProvider"/> with the supplied options.
    /// </summary>
    /// <param name="options">
    /// The <see cref="IOptions{TOptions}"/> wrapper containing the static access token value.
    /// </param>
    public StaticAccessTokenProvider(IOptions<StaticAccessTokenProviderOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="StaticAccessTokenProvider"/> with a literal token string.
    /// </summary>
    /// <param name="accessToken">The access token string to return on every call.</param>
    public StaticAccessTokenProvider(string accessToken) : this(Options.Create(new StaticAccessTokenProviderOptions { AccessToken = accessToken }))
    {
    }

    /// <summary>
    /// Returns the statically configured access token immediately, without any network or I/O calls.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests (not used by this implementation).</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that resolves synchronously to the configured access token string.
    /// </returns>
    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<string>(_options.Value.AccessToken);
    }
}

/// <summary>
/// Configuration options for <see cref="EnvironmentAccessTokenProvider"/>.
/// Specifies the name of the environment variable that holds the access token.
/// </summary>
public class EnvironmentAccessTokenProviderOptions
{
    /// <summary>
    /// Gets or sets the name of the environment variable from which the access token is read.
    /// This value is required.
    /// </summary>
    public required string AccessTokenVariable { get; set; }
}

/// <summary>
/// An <see cref="IAccessTokenProvider"/> implementation that reads the access token from an
/// environment variable. Useful when the token is injected into the process environment by
/// an external system such as a Kubernetes projected volume or a secrets manager.
/// </summary>
public class EnvironmentAccessTokenProvider : IAccessTokenProvider
{
    private readonly IOptions<EnvironmentAccessTokenProviderOptions> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="EnvironmentAccessTokenProvider"/> with the supplied options.
    /// </summary>
    /// <param name="options">
    /// The <see cref="IOptions{TOptions}"/> wrapper specifying the environment variable name.
    /// </param>
    public EnvironmentAccessTokenProvider(IOptions<EnvironmentAccessTokenProviderOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EnvironmentAccessTokenProvider"/> with the name
    /// of the environment variable to read the token from.
    /// </summary>
    /// <param name="accessTokenVariable">The name of the environment variable that holds the access token.</param>
    public EnvironmentAccessTokenProvider(string accessTokenVariable) : this(Options.Create(new EnvironmentAccessTokenProviderOptions { AccessTokenVariable = accessTokenVariable }))
    {
    }

    /// <summary>
    /// Reads the access token from the configured environment variable and returns it synchronously.
    /// Returns an empty string if the environment variable is not set.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests (not used by this implementation).</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that resolves synchronously to the value of the environment variable,
    /// or an empty string if the variable is absent.
    /// </returns>
    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var accessToken = Environment.GetEnvironmentVariable(_options.Value.AccessTokenVariable) ?? string.Empty;
        return new ValueTask<string>(accessToken);
    }
}

/// <summary>
/// Configuration options for <see cref="FileAccessTokenProvider"/>.
/// Specifies the path to a file whose contents represent the access token.
/// </summary>
public class FileAccessTokenProviderOptions
{
    /// <summary>
    /// Gets or sets the absolute or relative path to the file containing the access token.
    /// This value is required.
    /// </summary>
    public required string AccessTokenPath { get; set; }
}

/// <summary>
/// An <see cref="IAccessTokenProvider"/> implementation that reads the access token from a file
/// on disk. Designed for Kubernetes workloads where a service account token or OIDC token is
/// mounted as a file (e.g. via a projected volume at <c>/var/run/secrets/...</c>).
/// Leading and trailing whitespace is trimmed from the file contents before returning.
/// </summary>
public class FileAccessTokenProvider : IAccessTokenProvider
{
    private readonly IOptions<FileAccessTokenProviderOptions> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="FileAccessTokenProvider"/> with the supplied options.
    /// </summary>
    /// <param name="options">
    /// The <see cref="IOptions{TOptions}"/> wrapper containing the path to the token file.
    /// </param>
    public FileAccessTokenProvider(IOptions<FileAccessTokenProviderOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FileAccessTokenProvider"/> with the path to the token file.
    /// </summary>
    /// <param name="accessTokenPath">The path to the file that contains the access token.</param>
    public FileAccessTokenProvider(string accessTokenPath) : this(Options.Create(new FileAccessTokenProviderOptions { AccessTokenPath = accessTokenPath }))
    {
    }

    /// <summary>
    /// Asynchronously reads the access token from the configured file path.
    /// The file contents are trimmed of surrounding whitespace before being returned.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that resolves to the trimmed contents of the token file.
    /// </returns>
    public async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var accessToken = await File.ReadAllTextAsync(_options.Value.AccessTokenPath, cancellationToken).ConfigureAwait(false);
        return accessToken.Trim();
    }
}
