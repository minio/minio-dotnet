using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

/// <summary>
/// Contains metadata about the S3 object involved in a notification event.
/// </summary>
public class ObjectMeta
{
    /// <summary>
    /// Gets or sets the MIME content type of the object.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the ETag of the object at the time of the event.
    /// </summary>
    [JsonPropertyName("eTag")]
    public string Etag { get; set; }

    /// <summary>
    /// Gets or sets the object key (URL-encoded).
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets an opaque token used to determine event sequencing for a given key.
    /// Events with a higher sequencer value occurred after events with a lower value.
    /// </summary>
    [JsonPropertyName("sequencer")]
    public string Sequencer { get; set; }

    /// <summary>
    /// Gets or sets the size of the object in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public ulong Size { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of user-defined metadata associated with the object
    /// (key-value pairs from <c>x-amz-meta-*</c> headers, returned without the prefix).
    /// </summary>
    [JsonPropertyName("userMetadata")]
    public IDictionary<string, string> UserMetadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the version ID of the object, or <c>null</c> if the bucket is not versioned.
    /// </summary>
    [JsonPropertyName("versionId")]
    public string VersionId { get; set; }
}
