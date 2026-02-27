using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

/// <summary>
/// Represents a bucket notification event message received from MinIO via a
/// notification stream (e.g., over a server-sent events or WebSocket connection).
/// A single message may contain multiple <see cref="NotificationEvent"/> records.
/// </summary>
public class BucketNotificationEvent
{
    /// <summary>
    /// Gets or sets the name of the S3 event that triggered the notification
    /// (e.g., <c>s3:ObjectCreated:Put</c>).
    /// </summary>
    [JsonPropertyName("EventName")]
    public string EventName { get; set; }

    /// <summary>
    /// Gets or sets the object key associated with the notification event.
    /// </summary>
    [JsonPropertyName("Key")]
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets the list of individual notification event records included in this message.
    /// </summary>
    [JsonPropertyName("Records")]
    public IList<NotificationEvent> Records { get; set; } = new List<NotificationEvent>();
}
