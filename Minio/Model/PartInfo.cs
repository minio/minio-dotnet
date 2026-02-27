namespace Minio.Model;

/// <summary>
/// Contains checksum and ETag information for a single part in a multipart upload,
/// used when constructing the complete-multipart-upload request.
/// </summary>
public class PartInfo
{
    /// <summary>
    /// Gets or sets the checksum algorithm used to compute the checksum for this part.
    /// </summary>
    public ChecksumAlgorithm? ChecksumAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the raw checksum bytes for this part, computed using the algorithm
    /// specified by <see cref="ChecksumAlgorithm"/>.
    /// </summary>
    public byte[]? Checksum { get; set; }

    /// <summary>
    /// Gets or sets the ETag returned by S3 when this part was uploaded.
    /// </summary>
    public string Etag { get; set; }
}
