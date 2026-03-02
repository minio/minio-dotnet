using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

/// <summary>
/// Represents an identity (principal) associated with an S3 notification event,
/// such as the bucket owner or the user who initiated the request.
/// </summary>
public class Identity
{
    /// <summary>
    /// Gets or sets the principal ID of the identity, typically the AWS account or IAM entity identifier.
    /// </summary>
    [JsonPropertyName("principalId")] public string PrincipalId { get; set; }
}
