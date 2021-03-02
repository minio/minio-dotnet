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
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using RestSharp;

using Minio.DataModel;

namespace Minio.Credentials
{
    [Serializable]
    [XmlRoot(ElementName = "AssumeRoleWithWebIdentityResponse")]
    public class WebIdentityResponse
    {
        [XmlElement("Credentials")]
        public AccessCredentials Credentials { get; set; }
        public AccessCredentials GetAccessCredentials()
        {
            return this.Credentials;
        }
    }

    public class WebIdentityProvider : WebIdentityClientGrantsProvider<WebIdentityProvider>
    {
        internal int ExpiryInSeconds { get; set; }
        internal Func<string, JsonWebToken> Supplier { get; set; }
        internal JsonWebToken CurrentJsonWebToken { get; set; }


        public WebIdentityProvider()
        {
        }

        public override AccessCredentials GetCredentials()
        {
            return base.GetCredentials();
        }

        public override async Task<AccessCredentials> GetCredentialsAsync()
        {
            return await base.GetCredentialsAsync();
        }

        internal override Uri GetJWTUri(JwtSecurityToken jwtSecurityToken)
        {
            throw new NotImplementedException();
        }

        internal WebIdentityProvider WithJWTSupplier(Func<string, JsonWebToken> f)
        {
            this.Supplier = (Func<string, JsonWebToken>)f.Clone();
            return this;
        }

        internal async override Task<IRestRequest> BuildRequest()
        {
            // Policy, RoleArn to be set already.
            if (string.IsNullOrEmpty(this.Policy) || string.IsNullOrWhiteSpace(this.Policy) ||
                string.IsNullOrEmpty(this.RoleARN) || string.IsNullOrWhiteSpace(this.RoleARN))
            {
                throw new InvalidOperationException(nameof(this.Policy) + " and " + nameof(this.RoleARN) + " needs to be initialized for the " + nameof(this.BuildRequest) + " operation to work.");
            }
            this.WithRoleAction("AssumeRoleWithWebIdentity");
            this.WithDurationInSeconds(this.CurrentJsonWebToken.Expiry);
            if (this.RoleSessionName == null)
            {
                this.RoleSessionName = utils.To8601String(DateTime.Now);
            }
            IRestRequest restRequest = await base.BuildRequest();
            restRequest.AddQueryParameter("WebIdentityToken", this.CurrentJsonWebToken.AccessToken);
            return restRequest;
        }

        public override AccessCredentials ParseResponse(IRestResponse response)
        {
            AccessCredentials credentials = base.ParseResponse(response);
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response.Content)))
            {
                return (AccessCredentials)new XmlSerializer(typeof(AccessCredentials)).Deserialize(stream);
            }
        }
    }
}