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

namespace Minio;

public class MinioClientFactory : IMinioClientFactory
{
    private readonly Action<IMinioClient> defaultConfigureClient;

    public MinioClientFactory(Action<IMinioClient> defaultConfigureClient)
    {
        this.defaultConfigureClient =
            defaultConfigureClient ?? throw new ArgumentNullException(nameof(defaultConfigureClient));
    }

    public IMinioClient CreateClient()
    {
        return CreateClient(defaultConfigureClient);
    }

    public IMinioClient CreateClient(Action<IMinioClient> configureClient)
    {
        if (configureClient == null) throw new ArgumentNullException(nameof(configureClient));

        var client = new MinioClient()
            .WithSSL();
        configureClient(client);
        _ = client.Build();


        return client;
    }
}
