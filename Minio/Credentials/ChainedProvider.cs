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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio.Credentials
{
    public class ChainedProvider : ClientProvider
    {
        internal List<ClientProvider> Providers { get; set; }
        internal ClientProvider CurrentProvider { get; set; }
        internal AccessCredentials Credentials { get; set; }

        public ChainedProvider()
        {
            this.Providers = new List<ClientProvider>();
        }

        public ChainedProvider AddProvider(ClientProvider provider)
        {
            this.Providers.Add(provider);
            return this;
        }

        public ChainedProvider AddProviders(ClientProvider[] providers)
        {
            this.Providers.AddRange(providers.ToList());
            return this;
        }

        public override AccessCredentials GetCredentials()
        {
            if (this.Credentials != null && !this.Credentials.AreExpired())
            {
                return this.Credentials;
            }
            if (this.CurrentProvider != null && !this.Credentials.AreExpired())
            {
                this.Credentials = this.CurrentProvider.GetCredentials();
                return this.CurrentProvider.GetCredentials();
            }
            foreach (var provider in this.Providers)
            {
                var credentials = provider.GetCredentials();
                if (credentials != null && !credentials.AreExpired())
                {
                    this.CurrentProvider = provider;
                    this.Credentials = credentials;
                    return credentials;
                }
            }
            throw new InvalidOperationException("None of the assigned providers were able to provide valid credentials.");
        }

        public override async Task<AccessCredentials> GetCredentialsAsync()
        {
            AccessCredentials credentials = this.GetCredentials();
            await Task.Yield();
            return credentials;
        }
    }
}