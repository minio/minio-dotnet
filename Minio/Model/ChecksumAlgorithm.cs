namespace Minio.Model;

/// <summary>
/// Specifies the checksum algorithm to use when uploading or verifying an S3 object.
/// </summary>
public enum ChecksumAlgorithm
{
    /// <summary>
    /// CRC-32 checksum algorithm.
    /// </summary>
    Crc32,

    /// <summary>
    /// CRC-32C (Castagnoli) checksum algorithm.
    /// </summary>
    Crc32c,

    /// <summary>
    /// SHA-1 checksum algorithm.
    /// </summary>
    Sha1,

    /// <summary>
    /// SHA-256 checksum algorithm.
    /// </summary>
    Sha256
}
