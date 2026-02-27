using Microsoft.Extensions.Options;

namespace Minio.CredentialProviders;

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