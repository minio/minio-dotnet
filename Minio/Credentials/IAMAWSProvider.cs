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
using System.Linq;
using System.Net;

using Minio.DataModel;
using Minio.Exceptions;
using Newtonsoft.Json;

namespace Minio.Credentials
{
    public class IAMAWSProvider : EnvironmentProvider
    {
        internal Uri CustomEndPoint { get; set; }
        internal AccessCredentials Credentials { get; set; }
        internal HttpClient Http_Client { get; set; }
        internal MinioClient Minio_Client { get; set; }

        public override AccessCredentials GetCredentials()
        {
            Uri url = this.CustomEndPoint;
            if (this.CustomEndPoint == null)
            {
                string region = Environment.GetEnvironmentVariable("AWS_REGION");
                if (string.IsNullOrEmpty(region) || string.IsNullOrWhiteSpace(region))
                {
                    url = RequestUtil.MakeTargetURL("sts.amazonaws.com", true);
                }
                else
                {
                    url = RequestUtil.MakeTargetURL("sts." + region + ".amazonaws.com", true);
                }
            }
            ClientProvider provider = new WebIdentityProvider()
                                                    .WithSTSEndpoint(url)
                                                    .WithRoleAction("AssumeRoleWithWebIdentity")
                                                    .WithDurationInSeconds(null)
                                                    .WithPolicy(null)
                                                    .WithRoleARN(Environment.GetEnvironmentVariable("AWS_ROLE_ARN"))
                                                    .WithRoleSessionName(Environment.GetEnvironmentVariable("AWS_ROLE_SESSION_NAME"));
            return provider.GetCredentials();
        }

        internal AccessCredentials GetAccessCredentials(string tokenFile)
        {
            Uri url = this.CustomEndPoint;
            string urlStr = url.Authority;
            if (url == null)
            {
                string region = Environment.GetEnvironmentVariable("AWS_REGION");
                urlStr = (region == null)?"https://sts.amazonaws.com":"https://sts." + region + ".amazonaws.com";
                url = new Uri(urlStr);
            }
            ClientProvider provider = new WebIdentityProvider()
                                                .WithJWTSupplier((tokenContents) =>
                                                            {
                                                                return new JsonWebToken(tokenContents, 0);
                                                            })
                                                .WithSTSEndpoint(url)
                                                .WithDurationInSeconds(null)
                                                .WithPolicy(null)
                                                .WithRoleARN(Environment.GetEnvironmentVariable("AWS_ROLE_ARN"))
                                                .WithRoleSessionName(Environment.GetEnvironmentVariable("AWS_ROLE_SESSION_NAME"))
                                                .WithHttpClient(this.Http_Client);
            return provider.GetCredentials();
        }

        public async Task<AccessCredentials> GetAccessCredentials(Uri url)
        {
            RestRequest request = new RestRequest(url.ToString(), Method.GET);
            var response = await this.Minio_Client.ExecuteAsync(Enumerable.Empty<ApiResponseErrorHandlingDelegate>(), request);
            if (string.IsNullOrEmpty(response.Content) ||
                    !HttpStatusCode.OK.Equals(response.StatusCode))
            {
                throw new CredentialsProviderException("IAMAWSProvider", "Credential Get operation failed with HTTP Status code: " + response.StatusCode);
            }
            ECSCredentials credentials = JsonConvert.DeserializeObject<ECSCredentials>(response.Content);
            if (credentials.Code != null && !credentials.Code.ToLower().Equals("success"))
            {
                throw new CredentialsProviderException("IAMAWSProvider", "Credential Get operation failed with code: " + credentials.Code + " and message " + credentials.Message);
            }
            return credentials.GetAccessCredentials();
        }

        public override async Task<AccessCredentials> GetCredentialsAsync()
        {
            var creds = this.GetCredentials();
            await Task.Yield();
            return creds;
        }

        public async Task<string> GetIamRoleNameAsync(Uri url)
        {
            string[] roleNames = null;
            RestRequest request = new RestRequest(url.ToString(), Method.GET);
            var response = await this.Minio_Client.ExecuteAsync(Enumerable.Empty<ApiResponseErrorHandlingDelegate>(), request);
            if (string.IsNullOrEmpty(response.Content) ||
                    !HttpStatusCode.OK.Equals(response.StatusCode))
            {
                throw new CredentialsProviderException("IAMAWSProvider", "Credential Get operation failed with HTTP Status code: " + response.StatusCode);
            }
            roleNames = response.Content.Split('\n');
            if (roleNames.Length <= 0)
            {
                throw new CredentialsProviderException("IAMAWSProvider", "No IAM roles are attached to AWS service at "+ url.ToString());
            }
            int index = 0;
            foreach (var item in roleNames)
            {
                roleNames[index++] = item.Trim();
            }
            return roleNames[0];
        }
        public async Task<Uri> GetIamRoleNamedURL()
        {
            Uri url = this.CustomEndPoint;
            if (url == null || string.IsNullOrEmpty(url.Authority))
            {
                url = new Uri("http://169.254.169.254/latest/meta-data/iam/security-credentials/");
            }
            else
            {
                var urlStr = url.Scheme + "://" + url.Authority + "/latest/meta-data/iam/security-credentials/";
                url = new Uri(urlStr);
            }
            string roleName = await this.GetIamRoleNameAsync(url);
            var newUrlStr = url.Scheme + "://" + url.Authority + "/" + roleName;
            return new Uri(newUrlStr);
        }

        public IAMAWSProvider(string endpoint, HttpClient client)
        {
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException("Endpoint field " + nameof(CustomEndPoint) + " cannot be null or empty.");
            }
            if (client == null)
            {
                throw new ArgumentException("Http Client field " + nameof(this.Http_Client) + " cannot be null or empty.");
            }
            this.Http_Client = client;
            this.CustomEndPoint = new Uri(endpoint);
        }

        public IAMAWSProvider(string endpoint, MinioClient client)
        {
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException("Endpoint field " + nameof(CustomEndPoint) + " cannot be null or empty.");
            }
            if (client == null)
            {
                throw new ArgumentException("Http Client field " + nameof(this.Http_Client) + " cannot be null or empty.");
            }
            this.Minio_Client = client;
            this.CustomEndPoint = new Uri(endpoint);
        }
    }
}