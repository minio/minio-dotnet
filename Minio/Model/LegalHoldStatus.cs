namespace Minio.Model;

/// <summary>
/// Specifies whether an S3 object legal hold is active.
/// A legal hold prevents an object version from being overwritten or deleted
/// regardless of the configured retention period.
/// </summary>
public enum LegalHoldStatus
{
    /// <summary>
    /// The legal hold is active. The object cannot be deleted or overwritten.
    /// </summary>
    On,

    /// <summary>
    /// The legal hold is inactive. Normal retention rules apply.
    /// </summary>
    Off,
}
