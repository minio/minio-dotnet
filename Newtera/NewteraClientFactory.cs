/*
 * Newtera .NET Library for Newtera TDM, (C) 2017 Newtera, Inc.
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

namespace Newtera;

public class NewteraClientFactory : INewteraClientFactory
{
    private const string DefaultEndpoint = "play.min.io";
    private readonly Action<INewteraClient> defaultConfigureClient;

    public NewteraClientFactory(Action<INewteraClient> defaultConfigureClient)
    {
        this.defaultConfigureClient =
            defaultConfigureClient ?? throw new ArgumentNullException(nameof(defaultConfigureClient));
    }

    public INewteraClient CreateClient()
    {
        return CreateClient(defaultConfigureClient);
    }

    public INewteraClient CreateClient(Action<INewteraClient> configureClient)
    {
        if (configureClient == null) throw new ArgumentNullException(nameof(configureClient));

        var client = new NewteraClient()
            .WithSSL();

        configureClient(client);

        if (string.IsNullOrEmpty(client.Config.Endpoint))
            _ = client.WithEndpoint(DefaultEndpoint);

        _ = client.Build();


        return client;
    }
}
