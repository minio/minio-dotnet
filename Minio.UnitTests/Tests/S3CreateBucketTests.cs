using System.Net;
using Minio.UnitTests.Helpers;
using Xunit;

namespace Minio.UnitTests.Tests;

public class S3CreateBucketUnitTests : MinioUnitTests
{
    [Fact]
    public async Task CheckCreateBucket()
    {
        await RunWithMinioClientAsync((req, resp) =>
            {
                // Check request
                Assert.Equal("PUT", req.Method.Method);
                Assert.Equal("http://localhost:9000/testbucket", req.RequestUri?.ToString());
                req.AssertHeaders(
                    "host: localhost:9000",
                    "authorization: AWS4-HMAC-SHA256 Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-bucket-object-lock-enabled;x-amz-content-sha256;x-amz-date, Signature=2b64311d5771462ef66090c11aea1e1bcaa6e84138f10041761f1a2b3acae993",
                    "x-amz-bucket-object-lock-enabled: true",
                    "x-amz-content-sha256: e7b658141fd6e456e8e79ce8bfc36ec6ffe1518122a2f38006a8b24e67a1f8cc",
                    "x-amz-date: 20240411T153713Z"
                );

                // Set response
                resp.Headers.Location = new Uri("/testbucket", UriKind.Relative);
                resp.StatusCode = HttpStatusCode.OK;
            },
            async minioClient =>
            {
                var location = await minioClient.CreateBucketAsync("testbucket", region: "us-east-2", objectLocking: true).ConfigureAwait(true);

                // Check result
                Assert.Equal("/testbucket", location);
            });
    }
}