namespace Minio;

/// <summary>
/// The base exception type for all errors raised by the MinIO SDK.
/// Derive from this class to create SDK-specific exception types.
/// </summary>
public abstract class MinioException : Exception
{
    internal MinioException()
    {
    }

    internal MinioException(string message) : base(message)
    {
    }

    internal MinioException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
