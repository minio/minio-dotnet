using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

/// <summary>
/// Represents a single S3 event notification record, containing full details about
/// the event including region, timing, source, request parameters, and S3-specific metadata.
/// </summary>
public class NotificationEvent
{
    /// <summary>
    /// Gets or sets the AWS region in which the event occurred (e.g., <c>us-east-1</c>).
    /// </summary>
    [JsonPropertyName("awsRegion")]
    public string AwsRegion { get; set; }

    /// <summary>
    /// Gets or sets the name of the S3 event (e.g., <c>s3:ObjectCreated:Put</c>).
    /// </summary>
    [JsonPropertyName("eventName")]
    public string EventName { get; set; }

    /// <summary>
    /// Gets or sets the source of the event, typically <c>aws:s3</c>.
    /// </summary>
    [JsonPropertyName("eventSource")]
    public string EventSource { get; set; }

    /// <summary>
    /// Gets or sets the date and time at which the event occurred, as an ISO 8601 string.
    /// </summary>
    [JsonPropertyName("eventTime")]
    public string EventTime { get; set; }

    /// <summary>
    /// Gets or sets the version of the event notification schema.
    /// </summary>
    [JsonPropertyName("eventVersion")]
    public string EventVersion { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of request parameters associated with the event
    /// (e.g., source IP address).
    /// </summary>
    [JsonPropertyName("requestParameters")]
    public IDictionary<string, string> RequestParameters { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets a dictionary of response elements associated with the event
    /// (e.g., the x-amz-request-id and x-amz-id-2 headers).
    /// </summary>
    [JsonPropertyName("responseElements")]
    public IDictionary<string, string> ResponseElements { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the S3-specific metadata for the event, including bucket and object details.
    /// </summary>
    [JsonPropertyName("s3")]
    public EventMeta S3 { get; set; }

    /// <summary>
    /// Gets or sets information about the client that originated the request that triggered the event.
    /// </summary>
    [JsonPropertyName("source")]
    public SourceInfo Source { get; set; }

    /// <summary>
    /// Gets or sets the identity of the user whose request triggered the event.
    /// </summary>
    [JsonPropertyName("userIdentity")]
    public Identity UserIdentity { get; set; }
}
