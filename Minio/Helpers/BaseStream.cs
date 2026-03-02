namespace Minio.Helpers;

/// <summary>
/// An abstract decorator base class for <see cref="Stream"/> that forwards all read, write,
/// seek, and lifecycle operations to an inner stream supplied at construction time.
/// Subclasses override only the members they need to change (e.g. <see cref="Stream.Length"/>)
/// and inherit correct delegation for everything else, including both synchronous and
/// asynchronous disposal.
/// </summary>
public abstract class BaseStream : Stream
{
    private readonly Stream _baseStream;

    /// <summary>
    /// Initializes a new instance of <see cref="BaseStream"/> wrapping the given inner stream.
    /// </summary>
    /// <param name="baseStream">The inner stream to which all operations are delegated.</param>
    internal BaseStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _baseStream.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>
    /// Asynchronously releases the resources used by the inner stream and this instance.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that completes when both disposals have finished.</returns>
    public override async ValueTask DisposeAsync()
    {
        await _baseStream.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously clears all buffers for the inner stream and causes any buffered data
    /// to be written to the underlying device.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the flush operation.</param>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    public override Task FlushAsync(CancellationToken cancellationToken)
        => _baseStream.FlushAsync(cancellationToken);

    /// <summary>
    /// Clears all buffers for the inner stream and causes any buffered data to be written
    /// to the underlying device.
    /// </summary>
    public override void Flush()
        => _baseStream.Flush();

    /// <summary>
    /// Asynchronously reads a sequence of bytes from the inner stream into <paramref name="buffer"/>
    /// and advances the position by the number of bytes read.
    /// </summary>
    /// <param name="buffer">The region of memory to write the data into.</param>
    /// <param name="cancellationToken">A token to cancel the read operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{T}"/> that completes with the total number of bytes read.
    /// This may be less than the size of <paramref name="buffer"/> if that many bytes are
    /// not currently available, or zero if the end of the stream has been reached.
    /// </returns>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _baseStream.ReadAsync(buffer, cancellationToken);

    /// <summary>
    /// Asynchronously reads a sequence of bytes from the inner stream into <paramref name="buffer"/>
    /// and advances the position by the number of bytes read.
    /// </summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing data.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">A token to cancel the read operation.</param>
    /// <returns>
    /// A task that completes with the total number of bytes read into <paramref name="buffer"/>.
    /// This may be less than <paramref name="count"/> if that many bytes are not currently
    /// available, or zero if the end of the stream has been reached.
    /// </returns>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _baseStream.ReadAsync(buffer, offset, count, cancellationToken);

    /// <summary>
    /// Reads a sequence of bytes from the inner stream into <paramref name="buffer"/> and
    /// advances the position by the number of bytes read.
    /// </summary>
    /// <param name="buffer">A region of memory to write the data into.</param>
    /// <returns>
    /// The total number of bytes read. This may be less than the size of <paramref name="buffer"/>
    /// if that many bytes are not currently available, or zero if the end of the stream has been reached.
    /// </returns>
    public override int Read(Span<byte> buffer)
        => _baseStream.Read(buffer);

    /// <summary>
    /// Reads a sequence of bytes from the inner stream into <paramref name="buffer"/> and
    /// advances the position by the number of bytes read.
    /// </summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing data.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>
    /// The total number of bytes read into <paramref name="buffer"/>. This may be less than
    /// <paramref name="count"/> if that many bytes are not currently available, or zero if the
    /// end of the stream has been reached.
    /// </returns>
    public override int Read(byte[] buffer, int offset, int count)
        => _baseStream.Read(buffer, offset, count);

    /// <summary>
    /// Reads a single byte from the inner stream and advances the position by one byte,
    /// or returns <c>-1</c> if the end of the stream has been reached.
    /// </summary>
    /// <returns>The unsigned value of the byte cast to <see cref="int"/>, or <c>-1</c> at end of stream.</returns>
    public override int ReadByte()
        => _baseStream.ReadByte();

    /// <summary>
    /// Begins an asynchronous read operation on the inner stream.
    /// </summary>
    /// <param name="buffer">The buffer to read data into.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing data.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="callback">An optional callback to invoke when the read is complete.</param>
    /// <param name="state">A user-provided object that distinguishes this request from other requests.</param>
    /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous read.</returns>
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => _baseStream.BeginRead(buffer, offset, count, callback, state);

    /// <summary>
    /// Waits for the pending asynchronous read initiated by <see cref="BeginRead"/> to complete.
    /// </summary>
    /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
    /// <returns>The number of bytes read, or zero if the end of the stream has been reached.</returns>
    public override int EndRead(IAsyncResult asyncResult)
        => _baseStream.EndRead(asyncResult);

    /// <summary>
    /// Asynchronously writes a sequence of bytes from <paramref name="buffer"/> to the inner stream
    /// and advances the position by the number of bytes written.
    /// </summary>
    /// <param name="buffer">The region of memory to read data from.</param>
    /// <param name="cancellationToken">A token to cancel the write operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous write operation.</returns>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        => _baseStream.WriteAsync(buffer, cancellationToken);

    /// <summary>
    /// Asynchronously writes a sequence of bytes from <paramref name="buffer"/> to the inner stream
    /// and advances the position by the number of bytes written.
    /// </summary>
    /// <param name="buffer">The buffer to read data from.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> from which to begin reading data.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <param name="cancellationToken">A token to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _baseStream.WriteAsync(buffer, offset, count, cancellationToken);

    /// <summary>
    /// Writes a sequence of bytes from <paramref name="buffer"/> to the inner stream and
    /// advances the position by the number of bytes written.
    /// </summary>
    /// <param name="buffer">A region of memory containing the data to write.</param>
    public override void Write(ReadOnlySpan<byte> buffer)
        => _baseStream.Write(buffer);

    /// <summary>
    /// Writes a sequence of bytes from <paramref name="buffer"/> to the inner stream and
    /// advances the position by the number of bytes written.
    /// </summary>
    /// <param name="buffer">The buffer to read data from.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> from which to begin reading data.</param>
    /// <param name="count">The number of bytes to write.</param>
    public override void Write(byte[] buffer, int offset, int count)
        => _baseStream.Write(buffer, offset, count);

    /// <summary>
    /// Writes a single byte to the inner stream and advances the position by one byte.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    public override void WriteByte(byte value)
        => _baseStream.WriteByte(value);

    /// <summary>
    /// Begins an asynchronous write operation on the inner stream.
    /// </summary>
    /// <param name="buffer">The buffer containing data to write.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> from which to begin reading data.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <param name="callback">An optional callback to invoke when the write is complete.</param>
    /// <param name="state">A user-provided object that distinguishes this request from other requests.</param>
    /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous write.</returns>
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => _baseStream.BeginWrite(buffer, offset, count, callback, state);

    /// <summary>
    /// Ends an asynchronous write operation initiated by <see cref="BeginWrite"/>.
    /// </summary>
    /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
    public override void EndWrite(IAsyncResult asyncResult)
        => _baseStream.EndWrite(asyncResult);

    /// <summary>
    /// Closes the inner stream and releases any resources associated with it.
    /// </summary>
    public override void Close()
        => _baseStream.Close();

    /// <summary>
    /// Gets a value indicating whether the inner stream supports reading.
    /// </summary>
    public override bool CanRead => _baseStream.CanRead;

    /// <summary>
    /// Gets a value indicating whether the inner stream supports seeking.
    /// </summary>
    public override bool CanSeek => _baseStream.CanSeek;

    /// <summary>
    /// Gets a value indicating whether the inner stream supports writing.
    /// </summary>
    public override bool CanWrite => _baseStream.CanWrite;

    /// <summary>
    /// Gets a value indicating whether the inner stream can time out.
    /// </summary>
    public override bool CanTimeout => _baseStream.CanTimeout;

    /// <summary>
    /// Gets the length of the inner stream in bytes.
    /// </summary>
    public override long Length => _baseStream.Length;

    /// <summary>
    /// Gets or sets the current position within the inner stream.
    /// </summary>
    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    /// <summary>
    /// Sets the length of the inner stream.
    /// </summary>
    /// <param name="value">The desired length of the stream in bytes.</param>
    public override void SetLength(long value)
        => _baseStream.SetLength(value);

    /// <summary>
    /// Sets the position within the inner stream.
    /// </summary>
    /// <param name="offset">A byte offset relative to <paramref name="origin"/>.</param>
    /// <param name="origin">
    /// A <see cref="SeekOrigin"/> value indicating the reference point used to obtain the new position.
    /// </param>
    /// <returns>The new position within the inner stream.</returns>
    public override long Seek(long offset, SeekOrigin origin)
        => _baseStream.Seek(offset, origin);

    /// <summary>
    /// Gets or sets a value, in milliseconds, that determines how long the inner stream
    /// will attempt to read before timing out.
    /// </summary>
    public override int ReadTimeout
    {
        get => _baseStream.ReadTimeout;
        set => _baseStream.ReadTimeout = value;
    }

    /// <summary>
    /// Gets or sets a value, in milliseconds, that determines how long the inner stream
    /// will attempt to write before timing out.
    /// </summary>
    public override int WriteTimeout
    {
        get => _baseStream.WriteTimeout;
        set => _baseStream.WriteTimeout = value;
    }

    /// <summary>
    /// Reads all bytes from the inner stream and writes them to <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The stream to which the contents of the inner stream will be copied.</param>
    /// <param name="bufferSize">The size of the buffer used during copying, in bytes.</param>
    public override void CopyTo(Stream destination, int bufferSize)
        => _baseStream.CopyTo(destination, bufferSize);

    /// <summary>
    /// Asynchronously reads all bytes from the inner stream and writes them to
    /// <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The stream to which the contents of the inner stream will be copied.</param>
    /// <param name="bufferSize">The size of the buffer used during copying, in bytes.</param>
    /// <param name="cancellationToken">A token to cancel the copy operation.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        => _baseStream.CopyToAsync(destination, bufferSize, cancellationToken);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => _baseStream.Equals(obj);

    /// <inheritdoc/>
    public override string ToString() => _baseStream.ToString()!;

    /// <inheritdoc/>
    public override int GetHashCode() => _baseStream.GetHashCode();
}
