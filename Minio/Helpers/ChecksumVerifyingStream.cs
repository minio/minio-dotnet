using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Minio.Model;

namespace Minio.Helpers;

/// <summary>
/// A <see cref="Stream"/> wrapper that verifies the checksum of downloaded data
/// against the checksum provided in the S3 response headers.
/// Automatically verifies checksum on dispose if the checksum hasn't already been verified.
/// </summary>
/// <remarks>
/// Checksum verification is skipped for objects where no checksum was provided in the response headers.
/// Multipart upload objects with composite checksums are also skipped as they cannot be verified incrementally.
/// </remarks>
public sealed class ChecksumVerifyingStream : Stream
{
    private readonly Stream _inner;
    private readonly HashAlgorithm _hasher;
    private readonly string _expectedChecksum;
    private readonly ChecksumAlgorithm _algorithm;
    private bool _finalized;

    /// <summary>
    /// Gets a value indicating whether the checksum was successfully verified.
    /// </summary>
    public bool IsChecksumVerified { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChecksumVerifyingStream"/> class.
    /// </summary>
    /// <param name="inner">The inner stream containing the object data.</param>
    /// <param name="algorithm">The checksum algorithm used to compute the checksum.</param>
    /// <param name="expectedChecksum">The Base64-encoded checksum value expected.</param>
    internal ChecksumVerifyingStream(Stream inner, ChecksumAlgorithm algorithm, string expectedChecksum)
    {
        Debug.Assert(!string.IsNullOrEmpty(expectedChecksum), "Expected checksum should not be null or empty");

        _inner = inner;
        _algorithm = algorithm;
        _expectedChecksum = expectedChecksum;
        _hasher = CreateHasher(algorithm);
    }

    [SuppressMessage("Security", "CA5350:Do not use weak cryptographic algorithms", Justification = "SHA1 is required for S3 checksum verification compatibility")]
    private static HashAlgorithm CreateHasher(ChecksumAlgorithm algorithm)
    {
        return algorithm switch
        {
            ChecksumAlgorithm.Crc32 => new Crc32Helper(),
            ChecksumAlgorithm.Crc32c => new Crc32CHelper(),
            ChecksumAlgorithm.Crc64nvme => new Crc64NvmHelper(),
            ChecksumAlgorithm.Sha1 => SHA1.Create()!,
            ChecksumAlgorithm.Sha256 => SHA256.Create()!,
            _ => throw new ArgumentException($"Unsupported checksum algorithm: {algorithm}", nameof(algorithm))
        };
    }

    private class Crc32Helper : HashAlgorithm
    {
        private const uint DefaultPolynomial = 0xEDB88320;
        private uint _hash;

        public Crc32Helper()
        {
            _hash = 0xFFFFFFFF;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _hash = CalculateCrc32(_hash, array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            _hash = ~_hash;
            return BitConverter.GetBytes(_hash);
        }

        private static uint CalculateCrc32(uint hash, byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                hash ^= buffer[offset + i];
                for (var j = 0; j < 8; j++)
                {
                    hash = (hash >> 1) ^ ((hash & 1) != 0 ? DefaultPolynomial : 0);
                }
            }
            return hash;
        }

        public override void Initialize()
        {
            _hash = 0xFFFFFFFF;
        }
    }

    private class Crc32CHelper : HashAlgorithm
    {
        private const uint DefaultPolynomial = 0x82F63B78;
        private uint _hash;

        public Crc32CHelper()
        {
            _hash = 0xFFFFFFFF;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _hash = CalculateCrc32C(_hash, array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            _hash = ~_hash;
            return BitConverter.GetBytes(_hash);
        }

        private static uint CalculateCrc32C(uint hash, byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                hash ^= buffer[offset + i];
                for (var j = 0; j < 8; j++)
                {
                    hash = (hash >> 1) ^ ((hash & 1) != 0 ? DefaultPolynomial : 0);
                }
            }
            return hash;
        }

        public override void Initialize()
        {
            _hash = 0xFFFFFFFF;
        }
    }

    private class Crc64NvmHelper : HashAlgorithm
    {
        private const ulong DefaultPolynomial = 0x9A6C000000000000UL;
        private ulong _hash;

        public Crc64NvmHelper()
        {
            _hash = 0xFFFFFFFFFFFFFFFF;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _hash = CalculateCrc64Nvm(_hash, array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            _hash = ~_hash;
            return BitConverter.GetBytes(_hash);
        }

        private static ulong CalculateCrc64Nvm(ulong hash, byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                hash ^= (ulong)buffer[offset + i] << 56;
                for (var j = 0; j < 8; j++)
                {
                    hash = (hash << 1) ^ ((hash & (1UL << 63)) != 0 ? DefaultPolynomial : 0);
                }
            }
            return hash;
        }

        public override void Initialize()
        {
            _hash = 0xFFFFFFFFFFFFFFFF;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _inner.Read(buffer, offset, count);
        if (bytesRead > 0)
            _hasher.TransformBlock(buffer, offset, bytesRead, buffer, offset);
        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        var bytesRead = _inner.Read(buffer);
        if (bytesRead > 0)
        {
            var tempBuffer = buffer.ToArray();
            _hasher.TransformBlock(tempBuffer, 0, bytesRead, tempBuffer, 0);
        }
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        if (bytesRead > 0)
            _hasher.TransformBlock(buffer, offset, bytesRead, buffer, offset);
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var bytesRead = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (bytesRead > 0)
        {
            var tempBuffer = buffer.ToArray();
            _hasher.TransformBlock(tempBuffer, 0, bytesRead, tempBuffer, 0);
        }
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _inner.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _inner.Write(buffer, offset, count);
    }

    public override bool CanSeek => _inner.CanSeek;

    public override bool CanRead => _inner.CanRead;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush()
    {
        _inner.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _inner.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public override bool CanTimeout => _inner.CanTimeout;

    public override int ReadTimeout
    {
        get => _inner.ReadTimeout;
        set => _inner.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => _inner.WriteTimeout;
        set => _inner.WriteTimeout = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_finalized)
            VerifyChecksumInternal();
        _hasher.Dispose();
        _inner.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_finalized)
            await VerifyChecksumAsync().ConfigureAwait(false);
        try
        {
            _hasher.Dispose();
        }
        finally
        {
            _inner.Dispose();
            GC.SuppressFinalize(this);
        }
        await base.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the computed checksum value before verification.
    /// Only valid after the stream has been fully read and disposed.
    /// </summary>
    public string? ComputedChecksum { get; private set; }

    /// <summary>
    /// Explicitly verify the checksum of the downloaded data.
    /// </summary>
    /// <exception cref="ChecksumVerificationException">Thrown when the computed checksum doesn't match the expected value.</exception>
    public async Task VerifyChecksumAsync()
    {
        await VerifyChecksumInternalAsync().ConfigureAwait(false);
    }

    private void VerifyChecksumInternal()
    {
        _hasher.TransformFinalBlock([], 0, 0);
        _finalized = true;
        ComputedChecksum = Convert.ToBase64String(_hasher.Hash!);
        IsChecksumVerified = true;

        if (!string.Equals(ComputedChecksum, _expectedChecksum, StringComparison.Ordinal))
        {
            throw new ChecksumVerificationException(_algorithm, ComputedChecksum, _expectedChecksum);
        }
    }

    private async Task VerifyChecksumInternalAsync()
    {
        _hasher.TransformFinalBlock([], 0, 0);
        _finalized = true;
        ComputedChecksum = Convert.ToBase64String(_hasher.Hash!);
        IsChecksumVerified = true;

        if (!string.Equals(ComputedChecksum, _expectedChecksum, StringComparison.Ordinal))
        {
            throw new ChecksumVerificationException(_algorithm, ComputedChecksum, _expectedChecksum);
        }
    }
}

/// <summary>
/// Represents a checksum verification failure during object download.
/// </summary>
public sealed class ChecksumVerificationException : Exception
{
    /// <summary>
    /// Gets the checksum algorithm that was used for verification.
    /// </summary>
    public ChecksumAlgorithm Algorithm { get; }

    /// <summary>
    /// Gets the computed checksum value that was calculated during download.
    /// </summary>
    public string? Computed { get; }

    /// <summary>
    /// Gets the expected checksum value that was provided in the S3 response.
    /// </summary>
    public string? Expected { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChecksumVerificationException"/> class.
    /// </summary>
    /// <param name="algorithm">The checksum algorithm that failed verification.</param>
    /// <param name="computed">The computed checksum value.</param>
    /// <param name="expected">The expected checksum value.</param>
    public ChecksumVerificationException(ChecksumAlgorithm algorithm, string computed, string expected)
        : base($"Checksum verification failed for {algorithm}: expected '{expected}', computed '{computed}'")
    {
        Algorithm = algorithm;
        Computed = computed;
        Expected = expected;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChecksumVerificationException"/> class.
    /// </summary>
    public ChecksumVerificationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChecksumVerificationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ChecksumVerificationException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChecksumVerificationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ChecksumVerificationException(string? message, Exception? innerException) : base(message, innerException) { }
}
