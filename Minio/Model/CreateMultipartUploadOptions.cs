using System.Net.Http.Headers;

namespace Minio.Model;

/// <summary>
/// Options for initiating a multipart upload, specifying metadata, caching,
/// server-side encryption, retention, and other object attributes.
/// </summary>
public class CreateMultipartUploadOptions
{
    /// <summary>
    /// Gets or sets the <c>Cache-Control</c> header value to associate with the object.
    /// </summary>
    public CacheControlHeaderValue? CacheControl { get; set; }

    /// <summary>
    /// Gets or sets the <c>Content-Disposition</c> header value to associate with the object.
    /// </summary>
    public ContentDispositionHeaderValue? ContentDisposition { get; set; }

    /// <summary>
    /// Gets or sets the collection of content encodings (e.g., <c>gzip</c>) to associate with the object.
    /// </summary>
    public ICollection<string>? ContentEncoding { get; set; }

    /// <summary>
    /// Gets or sets the collection of natural languages of the intended audience for the object.
    /// </summary>
    public ICollection<string>? ContentLanguage { get; set; }

    /// <summary>
    /// Gets or sets the MIME content type of the object.
    /// </summary>
    public MediaTypeHeaderValue? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the date and time at which the object is no longer cacheable.
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// Gets or sets the server-side encryption configuration to apply to the object.
    /// </summary>
    public IServerSideEncryption? ServerSideEncryption { get; set; }

    /// <summary>
    /// Gets or sets the storage class to use for the object (e.g., <c>STANDARD</c>, <c>REDUCED_REDUNDANCY</c>).
    /// When <c>null</c>, the bucket's default storage class is used.
    /// </summary>
    public string? StorageClass { get; set; }

    /// <summary>
    /// Gets or sets the URL to redirect requests to if this object is requested via S3 website hosting.
    /// </summary>
    public string? WebsiteRedirectLocation { get; set; }

    /// <summary>
    /// Gets a dictionary of user-defined tags to attach to the object.
    /// </summary>
    public IDictionary<string, string> UserTags { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the object lock retention mode to apply to the object.
    /// </summary>
    public RetentionMode? Mode { get; set; }

    /// <summary>
    /// Gets or sets the date until which the object is to be retained under the configured retention mode.
    /// </summary>
    public DateTimeOffset? RetainUntilDate { get; set; }

    /// <summary>
    /// Gets or sets the legal hold status to apply to the object.
    /// </summary>
    public LegalHoldStatus? LegalHold { get; set; }
}
