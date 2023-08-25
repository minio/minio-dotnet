using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Minio;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMinio(
    this IServiceCollection services,
    Action<IMinioClient> configureClient)
    {
        if (configureClient == null)
        {
            throw new ArgumentNullException(nameof(configureClient));
        }

        var client = new MinioClient();
        configureClient(client); // Apply user configuration
        _ = client.Build();

        services.TryAddSingleton<IMinioClient>(_ => client);

        return services;
    }
}
