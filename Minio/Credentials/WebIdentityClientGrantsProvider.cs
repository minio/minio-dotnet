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
using System.Net;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;


using Minio.DataModel;

namespace Minio.Credentials
{
    public abstract class WebIdentityClientGrantsProvider<T> : AssumeRoleBaseProvider<T>
                                    where T : WebIdentityClientGrantsProvider<T>
    {
        public readonly uint MIN_DURATION_SECONDS = 15;
        public readonly uint MAX_DURATION_SECONDS = (uint)(new TimeSpan(7,0,0,0)).TotalSeconds;
        internal Uri STSEndpoint { get; set; }
        internal Func<JsonWebToken> JWTSupplier { get; set; }

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

        internal T WithSTSEndpoint(Uri endpoint)
        {
            this.STSEndpoint = endpoint;
            return (T)this;
        }
        internal async override Task<HttpRequestMessageBuilder> BuildRequest()
        {
            this.Validate();
            JsonWebToken jwt = this.JWTSupplier();
            HttpRequestMessageBuilder requestMessageBuilder = await base.BuildRequest();
            requestMessageBuilder = utils.GetEmptyRestRequest(requestMessageBuilder);
            requestMessageBuilder.AddQueryParameter("WebIdentityToken", jwt.AccessToken);
            await Task.Yield();
            return requestMessageBuilder;
        }

        internal override AccessCredentials ParseResponse(HttpResponseMessage response)
        {
            this.Validate();
            // Stream receiveStream = response.Content.ReadAsStreamAsync();
            // StreamReader readStream = new StreamReader (receiveStream, Encoding.UTF8);
            // txtBlock.Text = readStream.ReadToEnd();
            if (string.IsNullOrWhiteSpace(Convert.ToString(response.Content)) ||
                !HttpStatusCode.OK.Equals(response.StatusCode))
            {
                throw new ArgumentNullException("Unable to get credentials. Response error.");
            }
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Convert.ToString(response.Content))))
            {
                return (AccessCredentials)new XmlSerializer(typeof(AccessCredentials)).Deserialize(stream);
            }
        }

        protected void Validate()
        {
            if (this.JWTSupplier == null)
            {
                throw new ArgumentNullException(nameof(JWTSupplier) + " JWT Token supplier cannot be null.");
            }
            if (this.STSEndpoint == null || string.IsNullOrWhiteSpace(this.STSEndpoint.AbsoluteUri))
            {
                throw new InvalidOperationException(nameof(this.STSEndpoint) + " value is invalid.");
            }
        }
    }
}