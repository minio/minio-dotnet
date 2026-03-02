namespace Minio.CredentialProviders;

/// <summary>
/// Represents a set of credentials used to authenticate requests to a MinIO or S3-compatible server,
/// including an access key, secret key, an optional session token for temporary credentials,
/// and an optional expiration time.
/// </summary>
/// <param name="AccessKey">The access key identifier used to identify the credential principal.</param>
/// <param name="SecretKey">The secret access key used to sign requests.</param>
/// <param name="SessionToken">
/// An optional session token for temporary or STS-issued credentials.
/// Defaults to an empty string when not applicable.
/// </param>
/// <param name="Expiration">
/// The UTC date and time at which the credentials expire, or <see langword="null"/> if they do not expire.
/// </param>
public readonly record struct Credentials(string AccessKey, string SecretKey, string SessionToken = "", DateTime? Expiration = null);
