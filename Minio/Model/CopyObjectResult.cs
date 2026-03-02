namespace Minio.Model;

/// <summary>Result returned by a server-side object copy operation.</summary>
public class CopyObjectResult
{
    /// <summary>ETag of the newly created destination object.</summary>
    public required string ETag { get; init; }

    /// <summary>Last-modified timestamp of the newly created destination object.</summary>
    public required DateTimeOffset LastModified { get; init; }

    /// <summary>Version ID of the newly created destination object, if versioning is enabled.</summary>
    public string? VersionId { get; init; }

    /// <summary>CRC-32 checksum of the destination object, if requested.</summary>
    public string? ChecksumCRC32 { get; init; }

    /// <summary>CRC-32C checksum of the destination object, if requested.</summary>
    public string? ChecksumCRC32C { get; init; }

    /// <summary>SHA-1 checksum of the destination object, if requested.</summary>
    public string? ChecksumSHA1 { get; init; }

    /// <summary>SHA-256 checksum of the destination object, if requested.</summary>
    public string? ChecksumSHA256 { get; init; }
}
