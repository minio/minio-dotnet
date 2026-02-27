using Testcontainers.Minio;
using Xunit;

namespace Minio.IntegrationTests.Tests;

public abstract class MinioTest : IAsyncLifetime
{
    private readonly MinioContainer _minioContainer = new MinioBuilder(Images.AIStor)
        .WithEnvironment(new Dictionary<string, string>
        {
            ["MINIO_LICENSE"] =  License.Minio,
        })
        .Build();

    public Task InitializeAsync() => _minioContainer.StartAsync();
    public Task DisposeAsync() => _minioContainer.StopAsync();

    protected IMinioClient CreateClient()
    {
        return new MinioClientBuilder(_minioContainer.GetConnectionString())
            .WithStaticCredentials(_minioContainer.GetAccessKey(), _minioContainer.GetSecretKey())
            .Build();        
    }
}