namespace Minio.CredentialProviders;

/// <summary>
/// Defines a provider that supplies credentials for authenticating requests to a MinIO or S3-compatible server.
/// </summary>
public interface ICredentialsProvider
{
    /// <summary>
    /// Asynchronously retrieves the current credentials to use for request authentication.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that resolves to a <see cref="Credentials"/> instance
    /// containing the access key, secret key, and optional session token.
    /// </returns>
    ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken);
}
