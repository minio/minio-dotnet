using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

/// <summary>
/// Contains metadata about the S3 bucket involved in a notification event.
/// </summary>
public class BucketMeta
{
    /// <summary>
    /// Gets or sets the Amazon Resource Name (ARN) of the bucket.
    /// </summary>
    [JsonPropertyName("arn")]
    public string Arn { get; set; }

    /// <summary>
    /// Gets or sets the name of the bucket.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the identity of the bucket owner.
    /// </summary>
    [JsonPropertyName("ownerIdentity")]
    public Identity OwnerIdentity { get; set; }
}
