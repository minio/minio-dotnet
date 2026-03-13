using System.Net.Http.Headers;

namespace Minio.Model;

/// <summary>
/// Implements AWS Key Management Service (KMS)-managed server-side encryption (SSE-KMS) for S3 object operations.
/// With SSE-KMS, the MinIO server uses keys from AWS KMS to manage encryption.
/// </summary>
public class SseKmsConfig : IServerSideEncryption
{
    private readonly string? _keyId;

    /// <summary>
    /// Initializes a new <see cref="SseKmsConfig"/> instance with an optional KMS key ID.
    /// </summary>
    /// <param name="keyId">The optional AWS KMS key ID to use for encryption. If <c>null</c> or empty, the default key is used.</param>
    public SseKmsConfig(string? keyId = null)
    {
        _keyId = keyId;
    }

    /// <summary>
    /// Gets the encryption type identifier. Always returns <c>SSE-KMS</c> for KMS-managed encryption.
    /// </summary>
    public string Type => "SSE-KMS";

    /// <summary>
    /// Writes the SSE-KMS HTTP headers to the provided <see cref="HttpHeaders"/> collection.
    /// Adds the <c>X-Amz-Server-Side-Encryption</c> header with value <c>aws:kms</c>, and optionally
    /// the <c>X-Amz-Server-Side-Encryption-Aws-Kms-Key-Id</c> header if a key ID was provided.
    /// </summary>
    /// <param name="headers">The HTTP headers collection to which the SSE-KMS headers are added.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> is <c>null</c>.</exception>
    public void WriteHeaders(HttpHeaders headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        headers.Add("X-Amz-Server-Side-Encryption", "aws:kms");
        if (!string.IsNullOrEmpty(_keyId))
            headers.Add("X-Amz-Server-Side-Encryption-Aws-Kms-Key-Id", _keyId);
    }
}
