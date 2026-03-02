using System.Net;
using Minio.UnitTests.Helpers;
using Xunit;

namespace Minio.UnitTests.Tests;

public class S3HeadBucketUnitTests : MinioUnitTests
{
    [Fact]
    public async Task CheckHeadBucketExist()
    {
        await RunWithMinioClientAsync(
            (req, resp) =>
            {
                // Check request
                Assert.Equal("HEAD", req.Method.Method);
                Assert.Equal("http://localhost:9000/testbucket", req.RequestUri?.ToString());
                req.AssertHeaders(
                    "host: localhost:9000",
                    "authorization: AWS4-HMAC-SHA256 Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature=86f1a6fdd504b8e3246e6bfa3936d8f172d6656932eab1598da1bf88021da95a",
                    "x-amz-content-sha256: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    "x-amz-date: 20240411T153713Z"
                );

                // Set response
                resp.StatusCode = HttpStatusCode.OK;
            },
            async minioClient =>
            {
                var bucketExists = await minioClient.BucketExistsAsync("testbucket").ConfigureAwait(true);
                Assert.True(bucketExists);
            });
    }
    
    [Fact]
    public async Task CheckHeadBucketNotExist()
    {
        await RunWithMinioClientAsync(
            (req, resp) =>
            {
            // Check request
            Assert.Equal("HEAD", req.Method.Method);
            Assert.Equal("http://localhost:9000/anotherbucket", req.RequestUri?.ToString());
            req.AssertHeaders(
                "host: localhost:9000",
                "authorization: AWS4-HMAC-SHA256 Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature=0d28bf4ea9b48fb5f615afc21f6dd9fbf8c108364259690a8848733b282756b6",
                "x-amz-content-sha256: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                "x-amz-date: 20240411T153713Z"
            );
            
            // Set response
            resp.StatusCode = HttpStatusCode.NotFound;
        },
            async minioClient =>
            {
                var bucketExists = await minioClient.BucketExistsAsync("anotherbucket").ConfigureAwait(true);
                Assert.False(bucketExists);
            });
    }
}