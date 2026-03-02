namespace Minio.Model;

/// <summary>
/// Options for uploading a single part in a multipart upload, including optional
/// checksum and MD5 integrity verification.
/// </summary>
public class UploadPartOptions
{
    /// <summary>
    /// Gets or sets the checksum algorithm used to compute the checksum for this part.
    /// When set, a matching <see cref="Checksum"/> value must also be provided.
    /// </summary>
    public ChecksumAlgorithm? ChecksumAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the raw checksum bytes for this part, computed using the algorithm
    /// specified by <see cref="ChecksumAlgorithm"/>.
    /// </summary>
    public byte[]? Checksum { get; set; }

    /// <summary>
    /// Gets or sets the raw MD5 hash bytes of the part data, used for end-to-end integrity
    /// verification via the <c>Content-MD5</c> request header. When <c>null</c>, no MD5
    /// header is sent.
    /// </summary>
    public byte[]? ContentMD5 { get; set; }
}
