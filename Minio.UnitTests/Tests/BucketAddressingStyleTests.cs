using System.Net;
using Minio.UnitTests.Helpers;
using Xunit;

namespace Minio.UnitTests.Tests;

public class BucketAddressingStyleTests : MinioUnitTests
{
    [Fact]
    public async Task UsesVirtualHostStyleForBucketOperations()
    {
        await RunWithMinioClientAsync(
            (req, resp) =>
            {
                Assert.Equal("http://testbucket.localhost:9000/", req.RequestUri?.ToString());
                Assert.Equal("testbucket.localhost:9000", req.Headers.Host);
                resp.StatusCode = HttpStatusCode.OK;
            },
            client => client.BucketExistsAsync("testbucket"),
            addressingStyle: BucketAddressingStyle.VirtualHost);
    }

    [Fact]
    public async Task KeepsPathStyleByDefault()
    {
        await RunWithMinioClientAsync(
            (req, resp) =>
            {
                Assert.Equal("http://localhost:9000/testbucket", req.RequestUri?.ToString());
                resp.StatusCode = HttpStatusCode.OK;
            },
            client => client.BucketExistsAsync("testbucket"));
    }
}
