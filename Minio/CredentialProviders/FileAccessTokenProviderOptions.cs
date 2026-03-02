namespace Minio.CredentialProviders;

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