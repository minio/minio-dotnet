using Minio.Model;

namespace Minio.Helpers;

/// <summary>
/// A <see cref="Stream"/> returned by <see cref="IMinioClient.GetObjectAsync"/> that carries
/// both the object's content stream and its associated <see cref="ObjectInfo"/> metadata.
/// Disposing this stream also disposes the underlying HTTP response, releasing the network
/// connection back to the pool.
/// </summary>
public sealed class ObjectInfoStream : BaseStream
{
    private readonly IDisposable _dispose;

    /// <summary>
    /// Gets the metadata describing the downloaded object (size, content type, ETag, etc.).
    /// </summary>
    public ObjectInfo Info { get; }

    /// <inheritdoc/>
    public override long Length => Info.ContentLength ?? throw new NotSupportedException("Object info stream does not support length");

    internal ObjectInfoStream(Stream stream, ObjectInfo objectInfo, IDisposable dispose) : base(stream)
    {
        _dispose = dispose;
        Info = objectInfo;
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _dispose.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        _dispose.Dispose();
    }
}
