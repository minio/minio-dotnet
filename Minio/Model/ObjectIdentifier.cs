namespace Minio.Model;

/// <summary>
/// Identifies an S3 object, optionally including version, ETag, last-modified time,
/// and size information. Used when enumerating or targeting specific objects.
/// </summary>
public readonly struct ObjectIdentifier : IEquatable<ObjectIdentifier>
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

    /// <summary>
    /// Determines whether this instance is equal to another <see cref="ObjectIdentifier"/>.
    /// </summary>
    /// <param name="other">The other <see cref="ObjectIdentifier"/> to compare with.</param>
    /// <returns><c>true</c> if both instances have the same key, version ID, and other metadata; otherwise <c>false</c>.</returns>
    public bool Equals(ObjectIdentifier other) =>
        Key.Equals(other.Key, StringComparison.Ordinal) &&
        string.Equals(VersionId, other.VersionId, StringComparison.Ordinal) &&
        string.Equals(ETag, other.ETag, StringComparison.Ordinal) &&
        LastModifiedTime.Equals(other.LastModifiedTime) &&
        Size.Equals(other.Size);

    /// <summary>
    /// Determines whether this instance is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an <see cref="ObjectIdentifier"/> with the same values; otherwise <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is ObjectIdentifier other && Equals(other);

    /// <summary>
    /// Returns the hash code for this instance, based on the object key and metadata.
    /// </summary>
    /// <returns>An integer hash code.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(Key, VersionId, ETag, LastModifiedTime, Size);

    /// <summary>
    /// Determines whether two <see cref="ObjectIdentifier"/> instances are equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise <c>false</c>.</returns>
    public static bool operator ==(ObjectIdentifier left, ObjectIdentifier right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="ObjectIdentifier"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><c>true</c> if the instances are not equal; otherwise <c>false</c>.</returns>
    public static bool operator !=(ObjectIdentifier left, ObjectIdentifier right) => !(left == right);
}
