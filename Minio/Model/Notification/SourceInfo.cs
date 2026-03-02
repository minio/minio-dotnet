using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

/// <summary>
/// Contains information about the client that originated the request which triggered an S3 notification event.
/// </summary>
public class SourceInfo
{
    /// <summary>
    /// Gets or sets the hostname or IP address of the client that made the request.
    /// </summary>
    [JsonPropertyName("host")]
    public string Host { get; set; }

    /// <summary>
    /// Gets or sets the port number used by the client for the request.
    /// </summary>
    [JsonPropertyName("port")]
    public string Port { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent string of the client that made the request.
    /// </summary>
    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; }
}
