namespace Minio.Model;

/// <summary>
/// Represents a byte range used to retrieve a partial object from S3,
/// corresponding to the HTTP <c>Range</c> header (e.g., <c>bytes=0-1023</c>).
/// </summary>
/// <param name="Start">The zero-based byte offset at which the range begins (inclusive).</param>
/// <param name="End">The zero-based byte offset at which the range ends (inclusive).</param>
public readonly record struct S3Range(long Start, long End);
