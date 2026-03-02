namespace Minio.Model;

/// <summary>
/// Represents the result of a single object deletion within a bulk delete operation.
/// A successful deletion populates <see cref="Key"/> and optionally <see cref="VersionId"/>,
/// <see cref="DeleteMarker"/>, and <see cref="DeleteMarkerVersionId"/>.
/// A failed deletion additionally populates <see cref="ErrorCode"/> and <see cref="ErrorMessage"/>.
/// </summary>
/// <param name="Key">The object key that was targeted for deletion.</param>
/// <param name="VersionId">The version ID of the object that was deleted, or <c>null</c> for non-versioned objects.</param>
/// <param name="DeleteMarker">
/// Indicates whether the deletion created a delete marker (<c>true</c>), removed one (<c>false</c>),
/// or is not applicable (<c>null</c>).
/// </param>
/// <param name="DeleteMarkerVersionId">The version ID of the delete marker that was created or removed, if applicable.</param>
/// <param name="ErrorCode">The S3 error code if the deletion failed; otherwise <c>null</c>.</param>
/// <param name="ErrorMessage">The human-readable error message if the deletion failed; otherwise <c>null</c>.</param>
public readonly record struct DeleteResult(string Key, string? VersionId = null, bool? DeleteMarker = null, string? DeleteMarkerVersionId = null, string? ErrorCode = null, string? ErrorMessage = null);
