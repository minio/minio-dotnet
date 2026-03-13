using System.Net.Http.Headers;

namespace Minio.Model;

/// <summary>
/// Implements S3-managed server-side encryption (SSE-S3) for S3 object operations.
/// With SSE-S3, the MinIO server manages the encryption keys using AES-256 encryption.
/// </summary>
public class SseS3Config : IServerSideEncryption
{
    /// <summary>
    /// Gets the encryption type identifier. Always returns <c>SSE-S3</c> for S3-managed encryption.
    /// </summary>
    public string Type => "SSE-S3";

    /// <summary>
    /// Writes the SSE-S3 HTTP headers to the provided <see cref="HttpHeaders"/> collection.
    /// Adds the <c>X-Amz-Server-Side-Encryption</c> header with value <c>AES256</c>.
    /// </summary>
    /// <param name="headers">The HTTP headers collection to which the SSE-S3 headers are added.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> is <c>null</c>.</exception>
    public void WriteHeaders(HttpHeaders headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        headers.Add("X-Amz-Server-Side-Encryption", "AES256");
    }
}
