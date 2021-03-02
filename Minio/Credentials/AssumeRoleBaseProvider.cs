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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Minio.DataModel;
using RestSharp;

namespace Minio.Credentials
{
    // Assume-role credential provider
    public abstract class AssumeRoleBaseProvider<T> : ClientProvider
                                where T: AssumeRoleBaseProvider<T>
    {
        internal AccessCredentials Credentials { get; set; }
        internal HttpClient Http_Client { get; set; }
        internal MinioClient Minio_Client { get; set; }
        internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers = Enumerable.Empty<ApiResponseErrorHandlingDelegate>();
        internal string Action { get; set; }
        internal uint? DurationInSeconds { get; set; }
        internal string Region { get; set; }
        internal string RoleSessionName { get; set; }
        internal string Policy{ get; set; }
        internal string RoleARN { get; set; }
        internal string ExternalID { get; set; }

        public AssumeRoleBaseProvider(HttpClient httpClient)
        {
            this.Http_Client = httpClient;
        }

        public AssumeRoleBaseProvider(MinioClient client)
        {
            this.Minio_Client = Minio_Client;
        }

        public AssumeRoleBaseProvider()
        {
            this.Minio_Client = null;
            this.Http_Client = null;
        }

        public T WithDurationInSeconds(uint? durationInSeconds)
        {
            this.DurationInSeconds = durationInSeconds;
            return (T)this;
        }

        public T WithRegion(string region)
        {
            if (string.IsNullOrEmpty(region) || string.IsNullOrWhiteSpace(region))
            {
                this.Region = "";
                return (T)this;
            }

            this.Region = region;
            return (T)this;
        }

        public T WithRoleARN(string roleArn)
        {
            this.RoleARN = roleArn;
            return (T)this;
        }

        public T WithPolicy(string policy)
        {
            this.Policy = policy;
            return (T)this;
        }

        public T WithRoleSessionName(string sessionName)
        {
            this.RoleSessionName = sessionName;
            return (T)this;
        }

        public T WithExternalID(string externalId)
        {
            if (string.IsNullOrEmpty(externalId) || string.IsNullOrWhiteSpace(externalId))
            {
                throw new ArgumentNullException("The External ID cannot be null or empty.");
            }
            if (externalId.Length < 2 || externalId.Length > 1224)
            {
                throw new ArgumentOutOfRangeException("The External Id needs to be between 2 to 1224 in length");
            }
            this.ExternalID = externalId;
            return (T)this;
        }

        public T WithRoleAction(string action)
        {
            this.Action = action;
            return (T)this;
        }

        public T WithHttpClient(HttpClient client)
        {
            this.Http_Client = client;
            return (T)this;
        }

        internal async virtual Task<IRestRequest> BuildRequest()
        {
            IRestRequest restRequest = null;
            if (Minio_Client != null)
            {
                restRequest = await Minio_Client.CreateRequest(Method.POST);
            }
            else
            {
                throw new InvalidOperationException("MinioClient not initialized in AssumeRoleBaseProvider");
            }
            restRequest = restRequest.AddQueryParameter("Action", this.Action)
                                     .AddQueryParameter("Version", "2011-06-15");
            if (!string.IsNullOrEmpty(this.Policy))
            {
                restRequest = restRequest.AddQueryParameter("Policy", this.Policy);
            }
            if (!string.IsNullOrEmpty(this.RoleARN))
            {
                restRequest = restRequest.AddQueryParameter("RoleArn", this.RoleARN);
            }
            if (!string.IsNullOrEmpty(this.RoleSessionName))
            {
                restRequest = restRequest.AddQueryParameter("RoleSessionName", this.RoleARN);
            }

            return restRequest;
        }

        public async override Task<AccessCredentials> GetCredentialsAsync()
        {
            if (this.Credentials != null && this.Credentials.AreExpired())
            {
                return this.Credentials;
            }

            var request = await this.BuildRequest();
            if (this.Minio_Client != null)
            {
                IRestResponse restResponse = null;
                try
                {
                    restResponse = await Minio_Client.ExecuteAsync(this.NoErrorHandlers, request);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return null;
        }

        public virtual AccessCredentials ParseResponse(IRestResponse response)
        {
            if (string.IsNullOrEmpty(response.Content) || !HttpStatusCode.OK.Equals(response.StatusCode))
            {
                throw new ArgumentNullException("Cannot generate credentials because of erroneous response");
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response.Content)))
            {
                return (AccessCredentials)new XmlSerializer(typeof(AccessCredentials)).Deserialize(stream);
            }
        }

        public override AccessCredentials GetCredentials()
        {
            throw new InvalidOperationException("Please use the Async method.");
        }
    }
}