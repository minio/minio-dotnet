/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Minio;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMinio(
        this IServiceCollection services,
        string accessKey,
        string secretKey,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        _ = services.AddMinio(configureClient => configureClient.WithCredentials(accessKey, secretKey), lifetime);
        return services;
    }

    public static IServiceCollection AddMinio(
        this IServiceCollection services,
        Action<IMinioClient> configureClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configureClient == null) throw new ArgumentNullException(nameof(configureClient));

        services.TryAddSingleton<IMinioClientFactory>(_ => new MinioClientFactory(configureClient));

        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.TryAddSingleton(CreateClient);
                break;
            case ServiceLifetime.Scoped:
                services.TryAddScoped(CreateClient);
                break;
            case ServiceLifetime.Transient:
                services.TryAddTransient(CreateClient);
                break;
        }

        return services;

        static IMinioClient CreateClient(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetRequiredService<IMinioClientFactory>();
            var client = factory.CreateClient();
            client.Config.ServiceProvider = serviceProvider;
            return client;
        }
    }
}
