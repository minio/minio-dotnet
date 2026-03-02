using Minio.Helpers;
using Xunit;

namespace Minio.UnitTests.Tests;

public class ProgressReadStreamTests
{
    [Fact]
    public async Task TestAllReadMethods()
    {
        var calls = 0;
        long lastPosition = -1;

        // We explicitly want to call both sync and async methods
#pragma warning disable CA1835, CA1849
        using var ms = new MemoryStream(new byte[1024 * 1024], false);
        var prs = new ProgressReadStream(ms, (p, l) =>
        {
            calls++;
            lastPosition = p;
            // Progress won't be reported after the stream is disposed
            // ReSharper disable once AccessToDisposedClosure
            Assert.Equal(ms.Length, l);
        });
        await using (prs.ConfigureAwait(true))
        {
            Assert.Equal(0, lastPosition);
            Assert.Equal(1, calls);
            _ = prs.ReadByte();
            Assert.Equal(1, lastPosition);
            Assert.Equal(2, calls);
            var buffer = new byte[8192];
            var bytesRead1 = prs.Read(buffer, 2048, 4096);
            Assert.Equal(4096, bytesRead1);
            Assert.Equal(4097, lastPosition);
            Assert.Equal(3, calls);
            var bytesRead2 = prs.Read(new Span<byte>(buffer));
            Assert.Equal(8192, bytesRead2);
            Assert.Equal(12289, lastPosition);
            Assert.Equal(4, calls);
            var bytesRead3 = await prs.ReadAsync(buffer, 2048, 4096).ConfigureAwait(true);
            Assert.Equal(4096, bytesRead3);
            Assert.Equal(16385, lastPosition);
            Assert.Equal(5, calls);
            var bytesRead4 = prs.Read(new Span<byte>(buffer));
            Assert.Equal(8192, bytesRead4);
            Assert.Equal(24577, lastPosition);
            Assert.Equal(6, calls);
            await prs.CopyToAsync(Stream.Null).ConfigureAwait(true);
            Assert.Equal(1024*1024, lastPosition);
            Assert.True(calls >= 7);
        }
#pragma warning restore CA1835, CA1849
        
        // Check if the underlying stream was disposed
        Assert.Throws<ObjectDisposedException>(() => _ = ms.Position);
    }
    
    [Fact]
    public async Task TestSeeking()
    {
        var calls = 0;
        long lastPosition = -1;

        // We explicitly want to call both sync and async methods
#pragma warning disable CA1835, CA1849
        using var ms = new MemoryStream(new byte[1024 * 1024], false);
        var prs = new ProgressReadStream(ms, (p, l) =>
        {
            calls++;
            lastPosition = p;
            // Progress won't be reported after the stream is disposed
            // ReSharper disable once AccessToDisposedClosure
            Assert.Equal(ms.Length, l);
        });
        await using (prs.ConfigureAwait(true))
        {
            Assert.Equal(0, lastPosition);
            Assert.Equal(1, calls);
            var buffer = new Memory<byte>(new byte[8192]);
            var bytesRead1 = prs.Read(buffer.Span);
            Assert.Equal(8192, bytesRead1);
            Assert.Equal(8192, lastPosition);
            Assert.Equal(2, calls);
            var bytesRead2 = prs.Read(buffer.Span);
            Assert.Equal(8192, bytesRead2);
            Assert.Equal(16384, lastPosition);
            Assert.Equal(3, calls);
            prs.Position = 2048;
            Assert.Equal(2048, lastPosition);
            Assert.Equal(4, calls);
            prs.Seek(2048, SeekOrigin.Begin);
            Assert.Equal(2048, lastPosition);
            Assert.Equal(4, calls); // position not changed, so we shouldn't have gotten an update
            prs.Seek(4096, SeekOrigin.Begin);
            Assert.Equal(4096, lastPosition);
            Assert.Equal(5, calls);
            prs.Position = 4096;
            Assert.Equal(4096, lastPosition);
            Assert.Equal(5, calls); // position not changed, so we shouldn't have gotten an update
        }
#pragma warning restore CA1835, CA1849
        
        // Check if the underlying stream was disposed
        Assert.Throws<ObjectDisposedException>(() => _ = ms.Position);
    }
}