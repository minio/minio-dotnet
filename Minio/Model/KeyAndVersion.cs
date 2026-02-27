namespace Minio.Model;

/// <summary>
/// Represents an S3 object key together with an optional version ID, used to
/// identify a specific object or version of an object.
/// </summary>
public readonly struct KeyAndVersion
{
    /// <summary>
    /// Gets the object key.
    /// </summary>
    public readonly string Key { get; init; }

    /// <summary>
    /// Gets the version ID of the object, or <c>null</c> if no specific version is targeted.
    /// </summary>
    public string? VersionId { get; init; }
}
