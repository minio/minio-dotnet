namespace Minio.CredentialProviders;

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