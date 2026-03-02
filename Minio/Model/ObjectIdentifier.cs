namespace Minio.Model;

/// <summary>
/// Identifies an S3 object, optionally including version, ETag, last-modified time,
/// and size information. Used when enumerating or targeting specific objects.
/// </summary>
public readonly struct ObjectIdentifier
{
    /// <summary>
    /// Gets the object key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the version ID of the object, or <c>null</c> if the object is not versioned
    /// or no specific version is targeted.
    /// </summary>
    public string? VersionId { get; init; }

    /// <summary>
    /// Gets the ETag of the object, or <c>null</c> if not available.
    /// </summary>
    public string? ETag { get; init; }

    /// <summary>
    /// Gets the date and time at which the object was last modified, or <c>null</c> if not available.
    /// </summary>
    public DateTime? LastModifiedTime { get; init; }

    /// <summary>
    /// Gets the size of the object in bytes, or <c>null</c> if not available.
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// Initializes a new <see cref="ObjectIdentifier"/> with the specified object key.
    /// </summary>
    /// <param name="key">The object key.</param>
    public ObjectIdentifier(string key)
    {
        Key = key;
    }
}
