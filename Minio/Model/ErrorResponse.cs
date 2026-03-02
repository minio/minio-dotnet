namespace Minio.Model;

/// <summary>
/// Represents the error details returned by the S3-compatible API in response to a failed request.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets the S3 error code that identifies the type of error (e.g., <c>NoSuchBucket</c>, <c>AccessDenied</c>).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets a human-readable description of the error.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the name of the bucket involved in the failed request, if applicable.
    /// </summary>
    public required string BucketName { get; init; }

    /// <summary>
    /// Gets the object key involved in the failed request, if applicable.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the resource path that was the target of the failed request.
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// Gets the unique identifier assigned to the failed request by the server.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Gets the identifier of the host that processed the request.
    /// </summary>
    public required string HostId { get; init; }

    /// <summary>
    /// Gets the AWS region associated with the failed request.
    /// </summary>
    public required string Region { get; init; }

    /// <summary>
    /// Gets the name of the server that returned the error.
    /// </summary>
    public required string Server { get; init; }
}
