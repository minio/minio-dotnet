namespace Minio.Model;

/// <summary>
/// Represents the Glacier restore status of an S3 object that has been archived
/// and subsequently requested for restoration.
/// </summary>
/// <param name="OngoingRestore">
/// <c>true</c> if the restore request is still in progress; <c>false</c> if the restore
/// has completed and the object is temporarily available.
/// </param>
/// <param name="ExpiryTime">
/// The date and time at which the temporarily restored copy will be removed,
/// or <c>null</c> if the restore is still ongoing.
/// </param>
public readonly record struct Restore(bool OngoingRestore, DateTimeOffset? ExpiryTime = null);
