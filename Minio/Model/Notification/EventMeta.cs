using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

/// <summary>
/// Contains S3-specific metadata embedded in a notification event record,
/// describing the bucket, object, and schema version involved in the event.
/// </summary>
public class EventMeta
{
    /// <summary>
    /// Gets or sets the metadata for the bucket involved in the event.
    /// </summary>
    [JsonPropertyName("bucket")]
    public BucketMeta Bucket { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the notification configuration that triggered this event.
    /// </summary>
    [JsonPropertyName("configurationId")]
    public string ConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the metadata for the object involved in the event.
    /// </summary>
    [JsonPropertyName("object")]
    public ObjectMeta Object { get; set; }

    /// <summary>
    /// Gets or sets the S3 event notification schema version (e.g., <c>2.0</c>).
    /// </summary>
    [JsonPropertyName("s3SchemaVersion")]
    public string SchemaVersion { get; set; }
}
