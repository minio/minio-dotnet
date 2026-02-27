namespace Minio.Implementation;

/// <summary>
/// The default implementation of <see cref="ITimeProvider"/> that returns the current UTC date and time.
/// </summary>
public sealed class DefaultTimeProvider : ITimeProvider
{
    /// <summary>
    /// Gets the current date and time expressed as Coordinated Universal Time (UTC).
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}