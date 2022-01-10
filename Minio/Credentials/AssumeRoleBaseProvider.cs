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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections.Generic;
using RestSharp;

using Minio.DataModel;

namespace Minio.Credentials
{
    // Assume-role credential provider
    public abstract class AssumeRoleBaseProvider<T> : ClientProvider
                                where T: AssumeRoleBaseProvider<T>
    {
        internal AccessCredentials Credentials { get; set; }
        internal MinioClient Client { get; set; }
        internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers = Enumerable.Empty<ApiResponseErrorHandlingDelegate>();
        internal string Action { get; set; }
        internal uint? DurationInSeconds { get; set; }
        internal string Region { get; set; }
        internal string RoleSessionName { get; set; }
        internal string Policy{ get; set; }
        internal string RoleARN { get; set; }
        internal string ExternalID { get; set; }

        public AssumeRoleBaseProvider(MinioClient client)
        {
            this.Client = client;
        }

        public AssumeRoleBaseProvider()
        {
            this.Client = null;
        }

        public T WithDurationInSeconds(uint? durationInSeconds)
        {
            this.DurationInSeconds = durationInSeconds;
            return (T)this;
        }

        public T WithRegion(string region)
        {
            this.Region = (!string.IsNullOrWhiteSpace(region))? region : "";
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
            if (string.IsNullOrWhiteSpace(externalId))
            {
                throw new ArgumentNullException("The External ID cannot be null or empty.");
            }
            if (externalId.Length < 2 || externalId.Length > 1224)
            {
                throw new ArgumentOutOfRangeException("The External Id needs to be between 2 to 1224 characters in length");
            }
            this.ExternalID = externalId;
            return (T)this;
        }

        public T WithRoleAction(string action)
        {
            this.Action = action;
            return (T)this;
        }

        internal async virtual Task<IRestRequest> BuildRequest()
        {
            IRestRequest restRequest = null;
            if (Client != null)
            {
                restRequest = await Client.CreateRequest(Method.POST);
            }
            else
            {
                throw new InvalidOperationException("MinioClient is not set in AssumeRoleBaseProvider");
            }
            restRequest = restRequest.AddQueryParameter("Action", this.Action)
                                     .AddQueryParameter("Version", "2011-06-15");
            if (!string.IsNullOrWhiteSpace(this.Policy))
            {
                restRequest = restRequest.AddQueryParameter("Policy", this.Policy);
            }
            if (!string.IsNullOrWhiteSpace(this.RoleARN))
            {
                restRequest = restRequest.AddQueryParameter("RoleArn", this.RoleARN);
            }
            if (!string.IsNullOrWhiteSpace(this.RoleSessionName))
            {
                restRequest = restRequest.AddQueryParameter("RoleSessionName", this.RoleARN);
            }

            return restRequest;
        }

        public async override Task<AccessCredentials> GetCredentialsAsync()
        {
            if (this.Credentials != null && !this.Credentials.AreExpired())
            {
                return this.Credentials;
            }

            var request = await this.BuildRequest();
            if (this.Client != null)
            {
                IRestResponse restResponse = null;
                try
                {
                    restResponse = await Client.ExecuteAsync(this.NoErrorHandlers, request);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return null;
        }

        internal virtual AccessCredentials ParseResponse(IRestResponse response)
        {
            if (string.IsNullOrEmpty(response.Content) || !HttpStatusCode.OK.Equals(response.StatusCode))
            {
                throw new ArgumentNullException("Unable to generate credentials. Response error.");
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response.Content)))
            {
                return (AccessCredentials)new XmlSerializer(typeof(AccessCredentials)).Deserialize(stream);
            }
        }

        public override AccessCredentials GetCredentials()
        {
            throw new InvalidOperationException("Please use the GetCredentialsAsync method.");
        }
    }
}