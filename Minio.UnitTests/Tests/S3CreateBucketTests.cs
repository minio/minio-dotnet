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
                    "authorization: AWS4-HMAC-SHA256 Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-bucket-object-lock-enabled;x-amz-content-sha256;x-amz-date, Signature=c4e96cffcb8110a014c3afbd13eeec7ed1912da87de309147326a948412c9426",
                    "x-amz-bucket-object-lock-enabled: true",
                    "x-amz-content-sha256: 6c77aa8df1e0bba57b10126d4e030c71a2828053b184fab6cdd55629b1dffcd7",
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