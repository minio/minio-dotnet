using Xunit;

namespace Minio.UnitTests.Tests;

public class MinioClientBuilderTests
{
    [Fact]
    public void EnsureMinioClient()
    {
        var minioClient = new MinioClientBuilder("http://localhost:9000")
            .WithStaticCredentials("minio", "minio123")
            .Build();
        Assert.NotNull(minioClient);
    }

    [Fact]
    public void EnsureExceptionWithoutCredentialsProvider()
    {
        Assert.Throws<InvalidOperationException>(() => new MinioClientBuilder("http://localhost:9000").Build());
    }
}