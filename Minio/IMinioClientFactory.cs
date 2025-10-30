namespace Minio;

public interface IMinioClientFactory
{
    IMinioClient CreateClient();
    IMinioClient CreateClient(Action<IMinioClient> configureClient);
}
