namespace Minio.Model;

/// <summary>
/// Specifies the versioning state of an S3 bucket.
/// </summary>
public enum VersioningStatus
{
    /// <summary>
    /// Versioning has never been enabled on the bucket. Objects are not versioned.
    /// </summary>
    Off = 0,

    /// <summary>
    /// Versioning is currently enabled on the bucket. Every write creates a new object version.
    /// </summary>
    Enabled = 1,

    /// <summary>
    /// Versioning has been suspended on the bucket. Existing versions are preserved, but
    /// new writes overwrite the null version rather than creating additional versions.
    /// </summary>
    Suspended = 2
}

internal static class VersioningStatusExtensions
{
    public static string Serialize(VersioningStatus versioningStatus)
    {
        return versioningStatus switch
        {
            VersioningStatus.Enabled => "Enabled",
            VersioningStatus.Suspended => "Suspended",
            _ => throw new ArgumentException("Invalid versioning status", nameof(versioningStatus))
        };
    }

    public static VersioningStatus Deserialize(string versioningStatus)
    {
        return versioningStatus switch
        {
            "Enabled" => VersioningStatus.Enabled,
            "Suspended" => VersioningStatus.Suspended,
            _ => throw new ArgumentException("Invalid versioning status", nameof(versioningStatus))
        };
    }
}
