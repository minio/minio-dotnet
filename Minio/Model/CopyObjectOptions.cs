using System.Net.Http.Headers;

namespace Minio.Model;

/// <summary>Specifies whether metadata is copied from the source or replaced.</summary>
public enum MetadataDirective
{
    /// <summary>Copy the metadata from the source object (default).</summary>
    Copy,
    /// <summary>Replace the metadata with values provided in the request.</summary>
    Replace,
}

/// <summary>Specifies whether tags are copied from the source or replaced.</summary>
public enum TaggingDirective
{
    /// <summary>Copy the tags from the source object (default).</summary>
    Copy,
    /// <summary>Replace the tags with values provided in the request.</summary>
    Replace,
}

/// <summary>Options for a server-side object copy operation.</summary>
public class CopyObjectOptions
{
    /// <summary>Version ID of the source object to copy. When <see langword="null"/>, copies the latest version.</summary>
    public string? SourceVersionId { get; set; }

    /// <summary>Controls whether metadata is copied from the source or supplied by the caller.</summary>
    public MetadataDirective? MetadataDirective { get; set; }

    /// <summary>Controls whether tags are copied from the source or supplied by the caller.</summary>
    public TaggingDirective? TaggingDirective { get; set; }

    // Content headers (effective when MetadataDirective = Replace)

    /// <summary>MIME content type of the destination object.</summary>
    public MediaTypeHeaderValue? ContentType { get; set; }

    /// <summary>Content encodings of the destination object.</summary>
    public IEnumerable<string>? ContentEncoding { get; set; }

    /// <summary>Content disposition of the destination object.</summary>
    public ContentDispositionHeaderValue? ContentDisposition { get; set; }

    /// <summary>Content languages of the destination object.</summary>
    public IEnumerable<string>? ContentLanguage { get; set; }

    /// <summary>Cache-control directives for the destination object.</summary>
    public CacheControlHeaderValue? CacheControl { get; set; }

    /// <summary>Expiry date for the destination object.</summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>User-defined metadata for the destination object. Requires <see cref="MetadataDirective"/> = <see cref="Model.MetadataDirective.Replace"/>.</summary>
    public IDictionary<string, string>? UserMetadata { get; set; }

    /// <summary>User-defined tags for the destination object. Requires <see cref="TaggingDirective"/> = <see cref="Model.TaggingDirective.Replace"/>.</summary>
    public IEnumerable<KeyValuePair<string, string>>? UserTags { get; set; }

    // Conditional copy headers

    /// <summary>Copies only if the source object ETag matches this value.</summary>
    public string? IfMatch { get; set; }

    /// <summary>Copies only if the source object ETag does not match this value.</summary>
    public string? IfNoneMatch { get; set; }

    /// <summary>Copies only if the source object was modified after this date.</summary>
    public DateTimeOffset? IfModifiedSince { get; set; }

    /// <summary>Copies only if the source object was not modified after this date.</summary>
    public DateTimeOffset? IfUnmodifiedSince { get; set; }

    // Destination object settings

    /// <summary>Server-side encryption to apply to the destination object.</summary>
    public IServerSideEncryption? ServerSideEncryption { get; set; }

    /// <summary>Storage class for the destination object.</summary>
    public string? StorageClass { get; set; }

    /// <summary>Website redirect location for the destination object.</summary>
    public string? WebsiteRedirectLocation { get; set; }

    /// <summary>Checksum algorithm to use for the destination object.</summary>
    public ChecksumAlgorithm? ChecksumAlgorithm { get; set; }

    // Object lock settings for the destination

    /// <summary>Retention mode for the destination object.</summary>
    public RetentionMode? Mode { get; set; }

    /// <summary>Retention until date for the destination object.</summary>
    public DateTimeOffset? RetainUntilDate { get; set; }

    /// <summary>Legal hold status for the destination object.</summary>
    public LegalHoldStatus? LegalHold { get; set; }
}
