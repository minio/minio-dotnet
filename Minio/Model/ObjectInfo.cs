using System.Net.Http.Headers;

namespace Minio.Model;

/// <summary>
/// Contains detailed metadata about an S3 object returned by a HEAD or GET object request,
/// including content properties, versioning information, retention settings, and checksums.
/// </summary>
public class ObjectInfo
{
    /// <summary>
    /// Gets the ETag of the object, which uniquely identifies a specific version of its content.
    /// </summary>
    public required EntityTagHeaderValue Etag { get; init; }

    /// <summary>
    /// Gets the object key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the size of the object content in bytes, or <c>null</c> if not reported.
    /// </summary>
    public long? ContentLength { get; init; }

    /// <summary>
    /// Gets the date and time at which the object was last modified, or <c>null</c> if not reported.
    /// </summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>
    /// Gets the MIME content type of the object.
    /// </summary>
    public required MediaTypeHeaderValue ContentType { get; init; }

    /// <summary>
    /// Gets the date and time at which the object's cached representation expires,
    /// as specified by the <c>Expires</c> response header, or <c>null</c> if not set.
    /// </summary>
    public DateTimeOffset? Expires { get; init; }

    /// <summary>
    /// Gets the version ID of the object, or <c>null</c> if versioning is not enabled on the bucket.
    /// </summary>
    public string? VersionId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this object metadata represents a delete marker.
    /// </summary>
    public required bool IsDeleteMarker { get; init; }

    /// <summary>
    /// Gets the cross-region replication status of the object (e.g., <c>COMPLETED</c>, <c>PENDING</c>, <c>FAILED</c>),
    /// or <c>null</c> if replication is not configured.
    /// </summary>
    public string? ReplicationStatus { get; init; }

    /// <summary>
    /// Gets the date and time at which this object version will be permanently deleted
    /// due to a lifecycle expiration rule, or <c>null</c> if no expiration is scheduled.
    /// </summary>
    public DateTimeOffset? Expiration { get; init; }

    /// <summary>
    /// Gets the identifier of the lifecycle rule that governs the expiration of this object,
    /// or <c>null</c> if no expiration rule applies.
    /// </summary>
    public string? ExpirationRuleId { get; init; }

    /// <summary>
    /// Gets a read-only dictionary of all raw response headers (both system and user metadata)
    /// associated with the object.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }

    /// <summary>
    /// Gets a read-only dictionary of user-defined metadata stored with the object
    /// (headers with the <c>x-amz-meta-</c> prefix, returned without the prefix).
    /// </summary>
    public IReadOnlyDictionary<string, string> UserMetadata { get; init; }

    /// <summary>
    /// Gets a read-only dictionary of user-defined tags attached to the object.
    /// </summary>
    public IReadOnlyDictionary<string, string> UserTags { get; init; }

    /// <summary>
    /// Gets the number of user-defined tags attached to the object.
    /// </summary>
    public int UserTagCount { get; init; }

    /// <summary>
    /// Gets the restore status of the object if it was archived to Glacier, or <c>null</c>
    /// if the object has not been archived or restored.
    /// </summary>
    public Restore? Restore { get; init; }

    /// <summary>
    /// Gets the Base64-encoded CRC-32 checksum of the object, if one was stored at upload time.
    /// </summary>
    public string? ChecksumCRC32 { get; init; }

    /// <summary>
    /// Gets the Base64-encoded CRC-32C checksum of the object, if one was stored at upload time.
    /// </summary>
    public string? ChecksumCRC32C { get; init; }

    /// <summary>
    /// Gets the Base64-encoded SHA-1 checksum of the object, if one was stored at upload time.
    /// </summary>
    public string? ChecksumSHA1 { get; init; }

    /// <summary>
    /// Gets the Base64-encoded SHA-256 checksum of the object, if one was stored at upload time.
    /// </summary>
    public string? ChecksumSHA256 { get; init; }
}
