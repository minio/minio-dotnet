namespace Minio.Helpers;

internal class ProgressReadStream : Stream
{
    private readonly Stream _baseStream;
    private readonly ProgressHandler _progress;
    private long _position;

    public ProgressReadStream(Stream baseStream, ProgressHandler progress) : this(baseStream, baseStream.Length, progress)
    {
    }

    public ProgressReadStream(Stream baseStream, long length, ProgressHandler progress)
    {
        _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));

        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), length, "Length should be non-negative");
        if (!_baseStream.CanRead) throw new ArgumentException("Stream should be readable", nameof(baseStream));

        Length = length;

        _progress(0, length);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _baseStream.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _baseStream.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    public override void Close()
    {
        _baseStream.Close();
    }

    public override int ReadByte()
    {
        var result = _baseStream.ReadByte();
        if (result >= 0)
            InternalPosition++;
        return result;
    }

    public override int Read(Span<byte> buffer)
    {
        var bytesRead = _baseStream.Read(buffer);
        InternalPosition += bytesRead;
        return bytesRead;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _baseStream.Read(buffer, offset, count);
        InternalPosition += bytesRead;
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
#pragma warning disable CA1835
        var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1835
        InternalPosition += bytesRead;
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        var bytesRead = await _baseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        InternalPosition += bytesRead;
        return bytesRead;
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _baseStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        var bytesRead = _baseStream.EndRead(asyncResult);
        InternalPosition += bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var result = _baseStream.Seek(offset, origin);
        InternalPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => InternalPosition + offset,
            SeekOrigin.End => Length - offset,
            _ => throw new ArgumentException("Invalid origin", nameof(origin))
        };
        return result;
    }

    public override bool CanRead => true;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length { get; }

    public override long Position
    {
        get => InternalPosition;
        set
        {
            _baseStream.Position = value;
            InternalPosition = value;
        }
    }

    // Unsupported operations
    public override void WriteByte(byte value) => throw new NotSupportedException();
    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();
    public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();

    public override int WriteTimeout
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => throw new NotSupportedException();
    public override Task FlushAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    private long InternalPosition
    {
        get => _position;
        set {
            // Don't send updates anymore when the stream has been
            // completed, because some implementations reset the
            // stream back to the beginning after they used it.
            if (_position != value && _position < Length)
            {
                _position = value;
                _progress.Invoke(_position, Length);
            }
        }
    }
}