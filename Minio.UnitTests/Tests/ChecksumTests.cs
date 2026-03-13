// Suppress CA5350: SHA1 is required for S3 checksum verification compatibility
#pragma warning disable CA5350
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Minio.Helpers;
using Minio.Model;
using Xunit;

namespace Minio.UnitTests.Tests;

public class ChecksumAlgorithmTests
{
    [Fact] public void Crc32AlgorithmWorks() => Assert.Equal("Crc32", ChecksumAlgorithm.Crc32.ToString());
    [Fact] public void Crc32CAlgorithmWorks() => Assert.Equal("Crc32c", ChecksumAlgorithm.Crc32c.ToString());
    [Fact] public void Crc64NvmAlgorithmWorks() => Assert.Equal("Crc64nvme", ChecksumAlgorithm.Crc64nvme.ToString());
    [Fact] public void Sha1AlgorithmWorks() => Assert.Equal("Sha1", ChecksumAlgorithm.Sha1.ToString());
    [Fact] public void Sha256AlgorithmWorks() => Assert.Equal("Sha256", ChecksumAlgorithm.Sha256.ToString());
}

public class ChecksumVerifyingStreamTests
{
    [Fact]
    public async Task VerifyCrc32ChecksumMatches()
    {
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        var stream = new MemoryStream(data);
        var crc32 = ComputeCrc32(data);
        var verifyingStream = new ChecksumVerifyingStream(stream, ChecksumAlgorithm.Crc32, Convert.ToBase64String(BitConverter.GetBytes(crc32)));
        while (await verifyingStream.ReadAsync(new byte[1024], 0, 1024) > 0) { }
        await verifyingStream.DisposeAsync();
        Assert.True(verifyingStream.IsChecksumVerified);
    }

    [Fact]
    public async Task VerifyCrc32ChecksumMismatchThrows()
    {
        var stream = new MemoryStream(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f });
        var verifyingStream = new ChecksumVerifyingStream(stream, ChecksumAlgorithm.Crc32, "wrong==");
        while (await verifyingStream.ReadAsync(new byte[1024], 0, 1024) > 0) { }
        var ex = await Assert.ThrowsAsync<ChecksumVerificationException>(() => verifyingStream.DisposeAsync().AsTask());
        Assert.Contains("Crc32", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerifyCrc32CChecksumMatches()
    {
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        var stream = new MemoryStream(data);
        var crc32c = ComputeCrc32C(data);
        var verifyingStream = new ChecksumVerifyingStream(stream, ChecksumAlgorithm.Crc32c, Convert.ToBase64String(BitConverter.GetBytes(crc32c)));
        while (await verifyingStream.ReadAsync(new byte[1024], 0, 1024) > 0) { }
        await verifyingStream.DisposeAsync();
        Assert.True(verifyingStream.IsChecksumVerified);
    }

    [Fact]
    public async Task VerifyCrc64NvmChecksumMatches()
    {
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        var stream = new MemoryStream(data);
        var crc64 = ComputeCrc64Nvm(data);
        var verifyingStream = new ChecksumVerifyingStream(stream, ChecksumAlgorithm.Crc64nvme, Convert.ToBase64String(BitConverter.GetBytes(crc64)));
        while (await verifyingStream.ReadAsync(new byte[1024], 0, 1024) > 0) { }
        await verifyingStream.DisposeAsync();
        Assert.True(verifyingStream.IsChecksumVerified);
    }

    [Fact]
    public async Task VerifySha1ChecksumMatches()
    {
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        var stream = new MemoryStream(data);
        var checksum = Convert.ToBase64String(SHA1.HashData(data));
        var verifyingStream = new ChecksumVerifyingStream(stream, ChecksumAlgorithm.Sha1, checksum);
        while (await verifyingStream.ReadAsync(new byte[1024], 0, 1024) > 0) { }
        await verifyingStream.DisposeAsync();
        Assert.True(verifyingStream.IsChecksumVerified);
    }

    [Fact]
    public async Task VerifySha256ChecksumMatches()
    {
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        var stream = new MemoryStream(data);
        var checksum = Convert.ToBase64String(SHA256.HashData(data));
        var verifyingStream = new ChecksumVerifyingStream(stream, ChecksumAlgorithm.Sha256, checksum);
        while (await verifyingStream.ReadAsync(new byte[1024], 0, 1024) > 0) { }
        await verifyingStream.DisposeAsync();
        Assert.True(verifyingStream.IsChecksumVerified);
    }

    private static uint ComputeCrc32(byte[] data)
    {
        const uint polynomial = 0xEDB88320;
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc >> 1) ^ ((crc & 1) != 0 ? polynomial : 0);
        }
        return ~crc;
    }

    private static uint ComputeCrc32C(byte[] data)
    {
        const uint polynomial = 0x82F63B78;
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc >> 1) ^ ((crc & 1) != 0 ? polynomial : 0);
        }
        return ~crc;
    }

    private static ulong ComputeCrc64Nvm(byte[] data)
    {
        const ulong polynomial = 0x9A6C000000000000UL;
        ulong crc = 0xFFFFFFFFFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= (ulong)b << 56;
            for (var i = 0; i < 8; i++)
                crc = (crc << 1) ^ ((crc & (1UL << 63)) != 0 ? polynomial : 0);
        }
        return ~crc;
    }
}

public class HttpHeadersExtensionsChecksumTests
{
    [Fact]
    public void SetChecksumAddsHeaderForCrc32()
    {
        var req = new HttpRequestMessage();
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        var checksum = BitConverter.GetBytes(ComputeCrc32(data));
        req.SetChecksum(ChecksumAlgorithm.Crc32, checksum);
        Assert.True(req.Headers.Contains("x-amz-checksum-crc32"));
    }

    [Fact]
    public void SetChecksumAddsHeaderForCrc32C()
    {
        var req = new HttpRequestMessage();
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        var checksum = BitConverter.GetBytes(ComputeCrc32C(data));
        req.SetChecksum(ChecksumAlgorithm.Crc32c, checksum);
        Assert.True(req.Headers.Contains("x-amz-checksum-crc32c"));
    }

    [Fact]
    public void SetChecksumAddsHeaderForCrc64Nvm()
    {
        var req = new HttpRequestMessage();
        var checksum = BitConverter.GetBytes(0x123456789ABCDEF0UL);
        req.SetChecksum(ChecksumAlgorithm.Crc64nvme, checksum);
        Assert.True(req.Headers.Contains("x-amz-checksum-crc64nvme"));
    }

    [Fact]
    public void SetChecksumAddsHeaderForSha1()
    {
        var req = new HttpRequestMessage();
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        var checksum = SHA1.HashData(data);
        Assert.Equal(20, checksum.Length);
        req.SetChecksum(ChecksumAlgorithm.Sha1, checksum);
        Assert.True(req.Headers.Contains("x-amz-checksum-sha1"));
    }

    [Fact]
    public void SetChecksumAddsHeaderForSha256()
    {
        var req = new HttpRequestMessage();
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        var checksum = SHA256.HashData(data);
        Assert.Equal(32, checksum.Length);
        req.SetChecksum(ChecksumAlgorithm.Sha256, checksum);
        Assert.True(req.Headers.Contains("x-amz-checksum-sha256"));
    }

    [Fact]
    public void SetChecksumThrowsForInvalidLength()
    {
        var req = new HttpRequestMessage();
        Assert.Throws<ArgumentException>(() => req.SetChecksum(ChecksumAlgorithm.Crc32, new byte[] { 0x01, 0x02 }));
    }

    [Fact]
    public void SetChecksumReturnsRequestWhenNullAlgorithm()
    {
        var req = new HttpRequestMessage();
        var result = req.SetChecksum(null, new byte[] { 0x01, 0x02, 0x03, 0x04 });
        Assert.Same(req, result);
        Assert.False(req.Headers.Contains("x-amz-checksum-crc32"));
    }

    [Fact]
    public void SetChecksumReturnsRequestWhenNullChecksum()
    {
        var req = new HttpRequestMessage();
        var result = req.SetChecksum(ChecksumAlgorithm.Crc32, null);
        Assert.Same(req, result);
        Assert.False(req.Headers.Contains("x-amz-checksum-crc32"));
    }

    private static uint ComputeCrc32(byte[] data)
    {
        const uint polynomial = 0xEDB88320;
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc >> 1) ^ ((crc & 1) != 0 ? polynomial : 0);
        }
        return ~crc;
    }

    private static uint ComputeCrc32C(byte[] data)
    {
        const uint polynomial = 0x82F63B78;
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc >> 1) ^ ((crc & 1) != 0 ? polynomial : 0);
        }
        return ~crc;
    }
}
#pragma warning restore CA5350