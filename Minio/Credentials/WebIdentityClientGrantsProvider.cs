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
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using RestSharp;
using Minio.DataModel;
using System.Net;
using System.Xml.Serialization;
using System.IO;

namespace Minio.Credentials
{
    public abstract class WebIdentityClientGrantsProvider<T> : AssumeRoleBaseProvider<T>
                                    where T : WebIdentityClientGrantsProvider<T>
    {
        public readonly uint MIN_DURATION_SECONDS = 15;
        public readonly uint MAX_DURATION_SECONDS = (uint)(new TimeSpan(7,0,0,0)).TotalSeconds;
        internal JwtSecurityTokenHandler TokenHandler { get; set; } // JWT supplier.
        internal JwtSecurityToken SecurityToken { get; set; }
        internal Uri STSEndpoint { get; set; }
        internal Func<string, JsonWebToken> JWTSupplier { get; set; }

        public WebIdentityClientGrantsProvider()
        {
        }

        internal uint GetDurationInSeconds(uint expiry)
        {
            if (this.DurationInSeconds != null && this.DurationInSeconds.Value > 0)
            {
                expiry = this.DurationInSeconds.Value;
            }
            if (expiry > MAX_DURATION_SECONDS)
            {
                return MAX_DURATION_SECONDS;
            }
            return (expiry < MIN_DURATION_SECONDS) ? MIN_DURATION_SECONDS : expiry;
        }

        internal abstract Uri GetJWTUri(JwtSecurityToken jwtSecurityToken);

        internal T WithSTSEndpoint(Uri endpoint)
        {
            this.STSEndpoint = endpoint;
            return (T)this;
        }
        internal async override Task<IRestRequest> BuildRequest()
        {
            IRestRequest restRequest = new RestRequest(GetJWTUri(this.SecurityToken), Method.POST);
            // Add utils.GetHTTPEmptyBody()
            await Task.Yield();
            return restRequest;
        }

        public override AccessCredentials ParseResponse(IRestResponse response)
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

    }
}