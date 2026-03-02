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