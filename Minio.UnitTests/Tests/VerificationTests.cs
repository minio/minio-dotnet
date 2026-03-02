using Minio.Helpers;
using Xunit;

namespace Minio.UnitTests.Tests;

public class VerificationTests
{
    public static readonly IEnumerable<object[]> BucketNames =
    [
        ["docexamplebucket1", true],
        ["log-delivery-march-2020", true],
        ["my-hosted-content", true],
        ["docexamplewebsite.com", true],
        ["www.docexamplewebsite.com", true],
        ["my.example.s3.bucket", true],

        ["doc_example_bucket", false],
        ["DocExampleBucket", false],
        ["doc-example-bucket-", false]
    ];

    [Theory]
    [MemberData(nameof(BucketNames))]
    public void CheckBucketNameValidation(string bucketName, bool valid)
    {
        if (valid)
            Assert.True(VerificationHelpers.VerifyBucketName(bucketName));
        else
            Assert.False(VerificationHelpers.VerifyBucketName(bucketName));
    }

    [Fact]
    public async Task CheckBucketException()
    {
        var minioClient = new MinioClientBuilder("http://localhost:9000")
            .WithStaticCredentials("minioadmin", "minioadmin")
            .Build();
        var exc = await Assert.ThrowsAsync<ArgumentException>(() => minioClient.CreateBucketAsync("-invalid-")).ConfigureAwait(true);
        Assert.Equal("bucketName", exc.ParamName);
    }
}