using Microsoft.Extensions.Options;

namespace Minio.CredentialProviders;

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