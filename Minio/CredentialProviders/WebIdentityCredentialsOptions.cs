namespace Minio.CredentialProviders;

/// <summary>
/// Configuration options for <see cref="WebIdentityProvider"/>.
/// Controls the STS endpoint, session duration, and optional role or policy constraints
/// used when calling the <c>AssumeRoleWithWebIdentity</c> action to obtain temporary credentials.
/// </summary>
public class WebIdentityCredentialsOptions
{
    /// <summary>
    /// Gets or sets the URL of the STS endpoint that implements the
    /// <c>AssumeRoleWithWebIdentity</c> action (e.g. <c>https://minio.example.com</c> or
    /// the AWS STS regional endpoint). This value is required.
    /// </summary>
    public required string StsEndPoint { get; set; }

    /// <summary>
    /// Gets or sets the duration, in seconds, for which the temporary credentials should remain valid.
    /// Defaults to <c>3600</c> (one hour). The STS server may enforce its own minimum and maximum limits.
    /// </summary>
    public int DurationSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets an optional IAM policy document (JSON) to attach to the assumed-role session,
    /// further restricting the permissions granted to the temporary credentials.
    /// When <see langword="null"/> or empty, no additional policy is applied.
    /// </summary>
    public string Policy { get; set; }

    /// <summary>
    /// Gets or sets the Amazon Resource Name (ARN) of the IAM role to assume.
    /// Required when calling AWS STS; optional for MinIO, which may use a default role.
    /// </summary>
    public string RoleARN { get; set; }

    /// <summary>
    /// Gets or sets an optional token revocation type hint passed to the STS endpoint.
    /// Used in certain MinIO or custom STS configurations to indicate how token revocation
    /// should be handled. When <see langword="null"/> or empty, no revocation type is specified.
    /// </summary>
    public string TokenRevokeType { get; set; }

    /// <summary>
    /// Gets or sets the name of the named <see cref="System.Net.Http.HttpClient"/> registered
    /// in the <see cref="System.Net.Http.IHttpClientFactory"/> to use when calling the STS endpoint.
    /// Defaults to <c>"Minio"</c>.
    /// </summary>
    public string MinioHttpClient { get; set; } = "Minio";
}
