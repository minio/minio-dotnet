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
using System.Linq;
using System.Net;

using Minio.DataModel;
using Minio.Exceptions;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Serialization;

/*
 * IAM roles for Amazon EC2
 * http://docs.aws.amazon.com/AWSEC2/latest/UserGuide/iam-roles-for-amazon-ec2.html
 * The Credential provider for attaching an IAM rule.
 */

namespace Minio.Credentials
{
    public class IAMAWSProvider : EnvironmentProvider
    {
        internal Uri CustomEndPoint { get; set; }
        internal AccessCredentials Credentials { get; set; }
        internal MinioClient Minio_Client { get; set; }

        public override AccessCredentials GetCredentials()
        {
            this.Validate();
            Uri url = this.CustomEndPoint;
            if (this.CustomEndPoint == null)
            {
                string region = Environment.GetEnvironmentVariable("AWS_REGION");
                if (string.IsNullOrWhiteSpace(region))
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
            this.Credentials = provider.GetCredentials();
            return this.Credentials;
        }

        internal AccessCredentials GetAccessCredentials(string tokenFile)
        {
            this.Validate();
            Uri url = this.CustomEndPoint;
            string urlStr = url.Authority;
            if (url == null || string.IsNullOrWhiteSpace(urlStr))
            {
                string region = Environment.GetEnvironmentVariable("AWS_REGION");
                urlStr = (region == null) ? "https://sts.amazonaws.com" : "https://sts." + region + ".amazonaws.com";
                url = new Uri(urlStr);
            }
            ClientProvider provider = new WebIdentityProvider()
                                                .WithJWTSupplier(() =>
                                                            {
                                                                string tokenContents = File.ReadAllText(tokenFile);
                                                                return new JsonWebToken(tokenContents, 0);
                                                            })
                                                .WithSTSEndpoint(url)
                                                .WithDurationInSeconds(null)
                                                .WithPolicy(null)
                                                .WithRoleARN(Environment.GetEnvironmentVariable("AWS_ROLE_ARN"))
                                                .WithRoleSessionName(Environment.GetEnvironmentVariable("AWS_ROLE_SESSION_NAME"));
            this.Credentials = provider.GetCredentials();
            return this.Credentials;
        }

        public async Task<AccessCredentials> GetAccessCredentials(Uri url)
        {
            this.Validate();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url.ToString());

            var requestBuilder = new HttpRequestMessageBuilder(HttpMethod.Get, url);
            requestBuilder.AddQueryParameter("location", "");

            using var response = await this.Minio_Client.ExecuteTaskAsync(Enumerable.Empty<ApiResponseErrorHandlingDelegate>(), requestBuilder);
            if (string.IsNullOrWhiteSpace(response.Content) ||
                    !HttpStatusCode.OK.Equals(response.StatusCode))
            {
                throw new CredentialsProviderException("IAMAWSProvider", "Credential Get operation failed with HTTP Status code: " + response.StatusCode);
            }
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Error = null,
            };
            ECSCredentials credentials = JsonConvert.DeserializeObject<ECSCredentials>(response.Content);
            if (credentials.Code != null && !credentials.Code.ToLower().Equals("success"))
            {
                throw new CredentialsProviderException("IAMAWSProvider", "Credential Get operation failed with code: " + credentials.Code + " and message " + credentials.Message);
            }
            this.Credentials = credentials.GetAccessCredentials();
            return this.Credentials;
        }

        public override async Task<AccessCredentials> GetCredentialsAsync()
        {
            if (this.Credentials != null && !this.Credentials.AreExpired())
            {
                this.Credentials = this.Credentials;
                return this.Credentials;
            }
            Uri url = this.CustomEndPoint;
            string awsTokenFile = Environment.GetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE");
            if (!string.IsNullOrWhiteSpace(awsTokenFile))
            {
                this.Credentials = this.GetAccessCredentials(awsTokenFile);
                return this.Credentials;
            }
            string containerRelativeUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI");
            string containerFullUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_FULL_URI");
            bool isURLEmpty = (url == null);
            if (!string.IsNullOrWhiteSpace(containerRelativeUri) && isURLEmpty)
            {
                url = RequestUtil.MakeTargetURL("169.254.170.2" + "/" + containerRelativeUri, false);
            }
            else if (!string.IsNullOrWhiteSpace(containerFullUri) && isURLEmpty)
            {
                var fullUri = new Uri(containerFullUri);
                url = RequestUtil.MakeTargetURL(fullUri.AbsolutePath, (fullUri.Scheme == "https"));
            }
            else
            {
                url = await GetIamRoleNamedURL();
            }
            this.Credentials = await GetAccessCredentials(url);
            return this.Credentials;
        }
        public async Task<string> GetIamRoleNameAsync(Uri url)
        {
            this.Validate();
            string[] roleNames = null;

            var requestBuilder = new HttpRequestMessageBuilder(HttpMethod.Get, url);
            requestBuilder.AddQueryParameter("location", "");

            using var response = await this.Minio_Client.ExecuteTaskAsync(Enumerable.Empty<ApiResponseErrorHandlingDelegate>(), requestBuilder);


            if (string.IsNullOrWhiteSpace(response.Content) ||
                    !HttpStatusCode.OK.Equals(response.StatusCode))
            {
                throw new CredentialsProviderException("IAMAWSProvider", "Credential Get operation failed with HTTP Status code: " + response.StatusCode);
            }
            roleNames = response.Content.Split('\n');
            if (roleNames.Length <= 0)
            {
                throw new CredentialsProviderException("IAMAWSProvider", "No IAM roles are attached to AWS service at " + url.ToString());
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
            this.Validate();
            Uri url = this.CustomEndPoint;
            string newUrlStr = null;
            if (url == null || string.IsNullOrWhiteSpace(url.Authority))
            {
                url = new Uri("http://169.254.169.254/latest/meta-data/iam/security-credentials/");
                newUrlStr = "http://169.254.169.254/latest/meta-data/iam/security-credentials/";
            }
            else
            {
                var urlStr = url.Scheme + "://" + url.Authority + "/latest/meta-data/iam/security-credentials/";
                url = new Uri(urlStr);
                newUrlStr = urlStr;
            }
            string roleName = await this.GetIamRoleNameAsync(url);
            newUrlStr += roleName;
            return new Uri(newUrlStr);
        }

        public IAMAWSProvider()
        {
            this.Minio_Client = null;
        }

        public IAMAWSProvider WithMinioClient(MinioClient minio)
        {
            this.Minio_Client = minio;
            if (this.Credentials == null ||
                string.IsNullOrWhiteSpace(this.Credentials.AccessKey) || string.IsNullOrWhiteSpace(this.Credentials.SecretKey))
            {
                this.Credentials = this.GetCredentialsAsync().GetAwaiter().GetResult();
            }
            return this;
        }

        public IAMAWSProvider WithEndpoint(string endpoint)
        {
            if (endpoint.Contains("https") || endpoint.Contains("http"))
            {
                this.CustomEndPoint = new Uri(endpoint);
            }
            else
            {
                this.CustomEndPoint = RequestUtil.MakeTargetURL(endpoint, true);
            }
            return this;
        }
        public IAMAWSProvider(string endpoint, MinioClient client)
        {
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                this.CustomEndPoint = new Uri(endpoint);
                if (string.IsNullOrWhiteSpace(this.CustomEndPoint.Authority))
                {
                    throw new ArgumentNullException("Endpoint field " + nameof(CustomEndPoint) + " is invalid.");
                }
            }
            if (client == null)
            {
                throw new ArgumentException("MinioClient reference field " + nameof(this.Minio_Client) + " cannot be null.");
            }
            this.Minio_Client = client;
            this.CustomEndPoint = new Uri(endpoint);
        }

        public void Validate()
        {
            if (this.Minio_Client == null)
            {
                throw new ArgumentNullException(nameof(Minio_Client) + " should be assigned for the operation to continue.");
            }
        }
    }
}