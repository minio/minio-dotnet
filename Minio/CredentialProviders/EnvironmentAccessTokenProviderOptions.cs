namespace Minio.CredentialProviders;

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