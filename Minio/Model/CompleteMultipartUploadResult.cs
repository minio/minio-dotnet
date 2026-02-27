namespace Minio.Model;

/// <summary>
/// Represents the result returned by S3 after a multipart upload is successfully completed.
/// </summary>
public class CompleteMultipartUploadResult
{
    /// <summary>
    /// Gets the URL of the assembled object in the bucket.
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Gets the name of the bucket in which the assembled object is stored.
    /// </summary>
    public required string Bucket { get; init; }

    /// <summary>
    /// Gets the object key of the assembled object.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the ETag of the assembled object.
    /// </summary>
    public required string Etag { get; init; }

    /// <summary>
    /// Gets the Base64-encoded CRC-32 checksum of the assembled object, if one was requested.
    /// </summary>
    public string? ChecksumCRC32 { get; init; }

    /// <summary>
    /// Gets the Base64-encoded CRC-32C checksum of the assembled object, if one was requested.
    /// </summary>
    public string? ChecksumCRC32C { get; init; }

    /// <summary>
    /// Gets the Base64-encoded SHA-1 checksum of the assembled object, if one was requested.
    /// </summary>
    public string? ChecksumSHA1 { get; init; }

    /// <summary>
    /// Gets the Base64-encoded SHA-256 checksum of the assembled object, if one was requested.
    /// </summary>
    public string? ChecksumSHA256 { get; init; }
}
