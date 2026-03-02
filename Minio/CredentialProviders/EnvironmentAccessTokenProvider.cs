using Microsoft.Extensions.Options;

namespace Minio.CredentialProviders;

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