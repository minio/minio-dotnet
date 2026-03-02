using Microsoft.Extensions.Options;

namespace Minio.CredentialProviders;

/// <summary>
/// An <see cref="ICredentialsProvider"/> implementation that returns a fixed, pre-configured set
/// of credentials. Suitable for scenarios where the access key and secret key are known ahead of
/// time and do not change during the lifetime of the application.
/// </summary>
public class StaticCredentialsProvider : ICredentialsProvider
{
    private readonly IOptions<StaticCredentialsOptions> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="StaticCredentialsProvider"/> with the supplied options.
    /// </summary>
    /// <param name="options">
    /// The <see cref="IOptions{TOptions}"/> wrapper containing the static credential values
    /// (access key, secret key, and optional session token).
    /// </param>
    public StaticCredentialsProvider(IOptions<StaticCredentialsOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// Returns the statically configured credentials immediately, without any network or I/O calls.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests (not used by this implementation).</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that resolves synchronously to a <see cref="Credentials"/> instance
    /// built from the configured <see cref="StaticCredentialsOptions"/>.
    /// </returns>
    public ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        return new ValueTask<Credentials>(new Credentials(options.AccessKey, options.SecretKey));
    }
}
