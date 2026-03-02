using System.Net.Http.Headers;

namespace Minio;

/// <summary>
/// Represents a single part of an in-progress multipart upload, as returned by
/// the ListParts S3 API.
/// </summary>
public class PartItem
{
    /// <summary>
    /// Gets the entity tag (ETag) of the uploaded part, used to identify and
    /// verify the part when completing the multipart upload.
    /// </summary>
    public required string ETag { get; init; }

    /// <summary>
    /// Gets the date and time at which this part was last modified.
    /// </summary>
    public required DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// Gets the 1-based part number that identifies this part within the multipart upload.
    /// </summary>
    public required int PartNumber { get; init; }

    /// <summary>
    /// Gets the size of this part in bytes.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    /// Gets the CRC-32 checksum of this part, if one was provided at upload time.
    /// </summary>
    public string? ChecksumCRC32 { get; init; }

    /// <summary>
    /// Gets the CRC-32C checksum of this part, if one was provided at upload time.
    /// </summary>
    public string? ChecksumCRC32C { get; init; }

    /// <summary>
    /// Gets the SHA-1 checksum of this part, if one was provided at upload time.
    /// </summary>
    public string? ChecksumSHA1 { get; init; }

    /// <summary>
    /// Gets the SHA-256 checksum of this part, if one was provided at upload time.
    /// </summary>
    public string? ChecksumSHA256 { get; init; }
}

/// <summary>
/// Represents a single object returned by the ListObjects S3 API, including
/// its key, size, storage class, and optional metadata.
/// </summary>
public class ObjectItem
{
    /// <summary>
    /// Gets the object key (path) within the bucket.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the entity tag (ETag) of the object, typically the MD5 hash of its content.
    /// </summary>
    public required string ETag { get; init; }

    /// <summary>
    /// Gets the size of the object in bytes.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    /// Gets the storage class of the object (e.g., <c>STANDARD</c>, <c>REDUCED_REDUNDANCY</c>).
    /// </summary>
    public required string StorageClass { get; init; }

    /// <summary>
    /// Gets the date and time at which the object was last modified.
    /// </summary>
    public required DateTimeOffset LastModified { get; init; }

    // The following properties are only present when metadata
    // is requested during listing (MinIO specific feature)

    /// <summary>
    /// Gets the content type of the object. Only populated when metadata is requested
    /// during listing (MinIO-specific feature).
    /// </summary>
    public MediaTypeHeaderValue? ContentType { get; init; }

    /// <summary>
    /// Gets the expiry date and time of the object. Only populated when metadata is
    /// requested during listing (MinIO-specific feature).
    /// </summary>
    public DateTimeOffset? Expires { get; init; }

    /// <summary>
    /// Gets the user-defined metadata associated with the object. Only populated when
    /// metadata is requested during listing (MinIO-specific feature).
    /// </summary>
    public IReadOnlyDictionary<string, string> UserMetadata { get; init; }
}

/// <summary>
/// Represents an in-progress multipart upload as returned by the ListMultipartUploads S3 API.
/// </summary>
public class UploadItem
{
    /// <summary>
    /// Gets the object key (path) within the bucket that this multipart upload targets.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the unique identifier for this multipart upload session.
    /// </summary>
    public required string UploadId { get; init; }

    /// <summary>
    /// Gets the storage class associated with this multipart upload.
    /// </summary>
    public required string StorageClass { get; init; }

    /// <summary>
    /// Gets the date and time at which this multipart upload was initiated.
    /// </summary>
    public required DateTimeOffset Initiated { get; init; }
}
