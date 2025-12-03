/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2021 MinIO, Inc.
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

using Minio.DataModel;

namespace Minio.Credentials;

public class ChainedProvider : IClientProvider
{
    public ChainedProvider()
    {
        Providers = [];
    }

    internal List<IClientProvider> Providers { get; set; }
    internal IClientProvider CurrentProvider { get; set; }
    internal AccessCredentials Credentials { get; set; }

    public AccessCredentials GetCredentials()
    {
        if (Credentials?.AreExpired() == false)
            return Credentials;
        if (CurrentProvider is not null && !Credentials.AreExpired())
        {
            Credentials = CurrentProvider.GetCredentials();
            return CurrentProvider.GetCredentials();
        }

        foreach (var provider in Providers)
        {
            var credentials = provider.GetCredentials();
            if (credentials?.AreExpired() == false)
            {
                CurrentProvider = provider;
                Credentials = credentials;
                return credentials;
            }
        }

        throw new InvalidOperationException(
            "None of the assigned providers were able to provide valid credentials."
        );
    }

    public ValueTask<AccessCredentials> GetCredentialsAsync()
    {
        return new ValueTask<AccessCredentials>(GetCredentials());
    }

    public ChainedProvider AddProvider(IClientProvider provider)
    {
        Providers.Add(provider);
        return this;
    }

    public ChainedProvider AddProviders(IClientProvider[] providers)
    {
        Providers.AddRange(providers.ToList());
        return this;
    }
}
