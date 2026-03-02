namespace Minio.Model;

/// <summary>
/// Represents the result returned by S3 when a multipart upload is initiated,
/// containing the upload ID and associated metadata needed to upload parts and
/// complete the upload.
/// </summary>
public class CreateMultipartUploadResult
{
    /// <summary>
    /// Gets the name of the bucket in which the multipart upload was initiated.
    /// </summary>
    public required string Bucket { get; init; }

    /// <summary>
    /// Gets the object key for which the multipart upload was initiated.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the unique identifier for the multipart upload session.
    /// This value must be passed to subsequent UploadPart and CompleteMultipartUpload calls.
    /// </summary>
    public required string UploadId { get; init; }

    /// <summary>
    /// Gets the date and time at which the multipart upload will be aborted if it has not been completed,
    /// as dictated by the bucket's lifecycle configuration.
    /// </summary>
    public DateTimeOffset? AbortDate { get; init; }

    /// <summary>
    /// Gets the identifier of the lifecycle rule that caused the abort date to be set.
    /// </summary>
    public string? AbortRuleId { get; init; }

    /// <summary>
    /// Gets the options that were specified when the multipart upload was created.
    /// </summary>
    public CreateMultipartUploadOptions? CreateOptions { get; init; }
}
