namespace Minio.Model;

/// <summary>
/// Options for retrieving an object from an S3-compatible bucket, including
/// conditional request headers, byte range retrieval, versioning, and server-side
/// encryption settings.
/// </summary>
public class GetObjectOptions
{
    /// <summary>
    /// Gets or sets the server-side encryption configuration used to decrypt the object.
    /// Required when the object was uploaded with SSE-C encryption.
    /// </summary>
    public IServerSideEncryption? ServerSideEncryption { get; set; }

    /// <summary>
    /// Gets or sets an ETag value for a conditional GET request. The object is returned
    /// only if its ETag matches this value (<c>If-Match</c> header).
    /// </summary>
    public string? IfMatchETag { get; set; }

    /// <summary>
    /// Gets or sets an ETag value for a conditional GET request. The object is returned
    /// only if its ETag does not match this value (<c>If-None-Match</c> header).
    /// </summary>
    public string? IfMatchETagExcept { get; set; }

    /// <summary>
    /// Gets or sets a timestamp for a conditional GET request. The object is returned
    /// only if it has not been modified since this date and time (<c>If-Unmodified-Since</c> header).
    /// </summary>
    public DateTimeOffset? IfUnmodifiedSince { get; set; }

    /// <summary>
    /// Gets or sets a timestamp for a conditional GET request. The object is returned
    /// only if it has been modified since this date and time (<c>If-Modified-Since</c> header).
    /// </summary>
    public DateTimeOffset? IfModifiedSince { get; set; }

    /// <summary>
    /// Gets or sets a byte range to retrieve only a portion of the object content
    /// (<c>Range</c> header). When <c>null</c>, the entire object is returned.
    /// </summary>
    public S3Range? Range { get; set; }

    /// <summary>
    /// Gets or sets the version ID of the specific object version to retrieve.
    /// When <c>null</c>, the latest version is returned.
    /// </summary>
    public string? VersionId { get; set; }

    /// <summary>
    /// Gets or sets the part number to retrieve when accessing a specific part of a
    /// multipart-uploaded object.
    /// </summary>
    public int? PartNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the server should return checksum information
    /// for the retrieved object.
    /// </summary>
    public bool? CheckSum { get; set; }
}
