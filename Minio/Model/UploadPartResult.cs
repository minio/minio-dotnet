namespace Minio.Model;

/// <summary>
/// Represents the result returned by S3 after a single part of a multipart upload
/// is successfully uploaded. The ETag and any returned checksum must be recorded and
/// submitted when completing the multipart upload.
/// </summary>
public class UploadPartResult
{
    /// <summary>
    /// Gets the ETag of the uploaded part, returned by S3 in the response.
    /// This value must be included in the complete-multipart-upload request.
    /// </summary>
    public string? Etag { get; init; }

    /// <summary>
    /// Gets the Base64-encoded CRC-32 checksum of the uploaded part, if one was requested.
    /// </summary>
    public string? ChecksumCRC32 { get; init; }

    /// <summary>
    /// Gets the Base64-encoded CRC-32C checksum of the uploaded part, if one was requested.
    /// </summary>
    public string? ChecksumCRC32C { get; init; }

    /// <summary>
    /// Gets the Base64-encoded SHA-1 checksum of the uploaded part, if one was requested.
    /// </summary>
    public string? ChecksumSHA1 { get; init; }

    /// <summary>
    /// Gets the Base64-encoded SHA-256 checksum of the uploaded part, if one was requested.
    /// </summary>
    public string? ChecksumSHA256 { get; init; }
}
