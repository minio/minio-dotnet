namespace Minio.Model;

/// <summary>
/// Represents basic metadata about an S3 bucket.
/// </summary>
public record struct BucketInfo
{
    /// <summary>
    /// Gets the date and time at which the bucket was created.
    /// </summary>
    public DateTimeOffset CreationDate { get; init; }

    /// <summary>
    /// Gets the name of the bucket.
    /// </summary>
    public string Name { get; init; }
}
