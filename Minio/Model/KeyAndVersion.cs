namespace Minio.Model;

/// <summary>
/// Represents an S3 object key together with an optional version ID, used to
/// identify a specific object or version of an object.
/// </summary>
public readonly struct KeyAndVersion : IEquatable<KeyAndVersion>
{
    /// <summary>
    /// Gets the object key.
    /// </summary>
    public readonly string Key { get; init; }

    /// <summary>
    /// Gets the version ID of the object, or <c>null</c> if no specific version is targeted.
    /// </summary>
    public string? VersionId { get; init; }

    /// <summary>
    /// Determines whether this instance is equal to another <see cref="KeyAndVersion"/>.
    /// </summary>
    /// <param name="other">The other <see cref="KeyAndVersion"/> to compare with.</param>
    /// <returns><c>true</c> if both instances have the same key and version ID; otherwise <c>false</c>.</returns>
    public bool Equals(KeyAndVersion other) =>
        Key.Equals(other.Key, StringComparison.Ordinal) &&
        string.Equals(VersionId, other.VersionId, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether this instance is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="KeyAndVersion"/> with the same values; otherwise <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is KeyAndVersion other && Equals(other);

    /// <summary>
    /// Returns the hash code for this instance, based on the key and version ID.
    /// </summary>
    /// <returns>An integer hash code.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(Key, VersionId);

    /// <summary>
    /// Determines whether two <see cref="KeyAndVersion"/> instances are equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise <c>false</c>.</returns>
    public static bool operator ==(KeyAndVersion left, KeyAndVersion right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="KeyAndVersion"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><c>true</c> if the instances are not equal; otherwise <c>false</c>.</returns>
    public static bool operator !=(KeyAndVersion left, KeyAndVersion right) => !(left == right);
}
