using System.Net.Http.Headers;

namespace Minio.Model;

/// <summary>
/// Defines the contract for a server-side encryption configuration that can be
/// applied to S3 object operations. Implementations include SSE-C (customer-provided keys),
/// SSE-S3 (S3-managed keys), and SSE-KMS (AWS KMS-managed keys).
/// </summary>
public interface IServerSideEncryption
{
    /// <summary>
    /// Gets a string that identifies the type of server-side encryption (e.g., <c>SSE-C</c>, <c>SSE-S3</c>, <c>SSE-KMS</c>).
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Writes the appropriate server-side encryption HTTP headers to the provided
    /// <see cref="HttpHeaders"/> collection for inclusion in an S3 request.
    /// </summary>
    /// <param name="headers">The HTTP headers collection to which the encryption headers are added.</param>
    void WriteHeaders(HttpHeaders headers);
}
