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
using System.Net.Http;
using System.Threading.Tasks;
using RestSharp;

namespace Minio.Credentials
{
    public class AssumeRoleProvider<T> : AssumeRoleBaseProvider<T>
                            where T: AssumeRoleProvider<T>
    {
        internal string STSEndPoint { get; set; }
        internal string AccessKey { get; set; }
        internal string SecretKey { get; set; }
        internal string ContentSHA256 { get; set; }
        internal RestRequest Request { get; set; }
        internal string Url { get; set; }
        private readonly uint DefaultDurationInSeconds = 1;
        private readonly string AssumeRole = "AssumeRole";

        public AssumeRoleProvider()
        {
        }

        public AssumeRoleProvider(MinioClient client) : base(client)
        {
        }

        public T WithAccessKey(string accessKey)
        {
            if (string.IsNullOrWhiteSpace(accessKey))
            {
                throw new ArgumentNullException("The Access Key cannot be null or empty.");
            }

            this.AccessKey = accessKey;
            return (T)this;
        }

        public T WithSecretKey(string secretKey)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentNullException("The Secret Key cannot be null or empty.");
            }
            this.SecretKey = secretKey;
            return (T)this;
        }

        public T WithSTSEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException("The STS endpoint cannot be null or empty.");
            }
            this.STSEndPoint = endpoint;
            var stsUri = utils.GetBaseUrl(endpoint);
            if ((stsUri.Scheme == "http" && stsUri.Port == 80) ||
                    (stsUri.Scheme == "https" && stsUri.Port == 443) ||
                    stsUri.Port <= 0)
            {
                this.Url = stsUri.Scheme + "://" + stsUri.Authority;
            }
            else if (stsUri.Port > 0)
            {
                this.Url = stsUri.Scheme + "://" + stsUri.Host + ":" + stsUri.Port;
            }
            this.Url = stsUri.Authority;

            return (T)this;
        }

        internal override async Task<IRestRequest> BuildRequest()
        {
            this.Action = this.AssumeRole;
            if (this.DurationInSeconds != null && this.DurationInSeconds.Value == 0)
                this.DurationInSeconds = DefaultDurationInSeconds;

            var restRequest = await base.BuildRequest();
            if (string.IsNullOrWhiteSpace(this.ExternalID))
            {
                restRequest.AddQueryParameter("ExternalId", this.ExternalID);
            }
            throw new System.NotImplementedException();
        }
    }
}