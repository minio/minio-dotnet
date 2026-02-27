namespace Minio.Model;

/// <summary>
/// Specifies the object lock retention mode applied to an S3 object.
/// Retention modes control whether a locked object can be overwritten or deleted
/// before the retention period expires.
/// </summary>
public enum RetentionMode
{
    /// <summary>
    /// Governance mode. Users with the appropriate IAM permissions can overwrite or
    /// delete a protected object version or alter its retention settings before the
    /// retention period expires.
    /// </summary>
    Governance,

    /// <summary>
    /// Compliance mode. A protected object version cannot be overwritten or deleted
    /// by any user, including the root account. The retention mode itself cannot be
    /// changed and the retention period cannot be shortened.
    /// </summary>
    Compliance,
}

internal static class RetentionModeExtensions
{
    public static string Serialize(RetentionMode retentionMode)
    {
        return retentionMode switch
        {
            RetentionMode.Compliance => "COMPLIANCE",
            RetentionMode.Governance => "GOVERNANCE",
            _ => throw new ArgumentException("Invalid object lock mode", nameof(retentionMode))
        };
    }

    public static RetentionMode Deserialize(string retentionMode)
    {
        return retentionMode switch
        {
            "COMPLIANCE" => RetentionMode.Compliance,
            "GOVERNANCE" => RetentionMode.Governance,
            _ => throw new ArgumentException("Invalid object lock mode", nameof(retentionMode))
        };
    }
}
