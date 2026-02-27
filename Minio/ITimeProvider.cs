namespace Minio;

/// <summary>
/// Abstraction over the system clock, allowing the current UTC time to be injected
/// for request signing and testing purposes.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current date and time expressed as Coordinated Universal Time (UTC).
    /// </summary>
    public DateTime UtcNow { get; }
}
