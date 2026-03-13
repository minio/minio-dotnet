namespace Minio.Model;

/// <summary>
/// Options for completing a multipart upload, including an optional checksum
/// that covers the entire assembled object.
/// </summary>
public class CompleteMultipartUploadOptions
{
    /// <summary>
    /// Gets or sets the checksum algorithm used to verify the integrity of the completed object.
    /// When set, a matching <see cref="Checksum"/> value must also be provided.
    /// </summary>
    public ChecksumAlgorithm? ChecksumAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the raw checksum bytes for the completed object, computed using
    /// the algorithm specified by <see cref="ChecksumAlgorithm"/>.
    /// </summary>
    public byte[]? Checksum { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable checksum mode for the upload.
    /// When enabled, the server will calculate and return checksums for the object.
    /// </summary>
    public bool? ChecksumMode { get; set; }
}
