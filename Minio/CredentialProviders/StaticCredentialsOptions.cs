namespace Minio.CredentialProviders;

/// <summary>
/// Configuration options for the <see cref="StaticCredentialsProvider"/>.
/// Holds the fixed access key, secret key, and optional session token used to authenticate
/// requests to a MinIO or S3-compatible server.
/// </summary>
public class StaticCredentialsOptions
{
    /// <summary>
    /// Gets or sets the access key identifier used to identify the credential principal.
    /// This value is required.
    /// </summary>
    public required string AccessKey { get; set; }

    /// <summary>
    /// Gets or sets the secret access key used to sign requests.
    /// This value is required.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Gets or sets an optional session token for temporary or pre-issued credentials.
    /// When <see langword="null"/>, no session token is included in authenticated requests.
    /// </summary>
    public string? SessionToken { get; set; }
}
