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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

using Minio.DataModel;

namespace Minio.Credentials
{
    [Serializable]
    [XmlRoot(ElementName = "AssumeRoleResponse", Namespace = "https://sts.amazonaws.com/doc/2011-06-15/")]
    public class AssumeRoleResponse
    {
        [XmlElement(ElementName = "AssumeRoleResult")]
        public AssumeRoleResult arr { get; set; }
        public string ToXML()
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true
            };
            using (MemoryStream ms = new MemoryStream())
            {
                var xmlWriter = XmlWriter.Create(ms, settings);
                XmlSerializerNamespaces names = new XmlSerializerNamespaces();
                names.Add(string.Empty, "https://sts.amazonaws.com/doc/2011-06-15/");

                XmlSerializer cs = new XmlSerializer(typeof(CertificateResponse));
                cs.Serialize(xmlWriter, this, names);

                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                var streamReader = new StreamReader(ms);
                var xml = streamReader.ReadToEnd();
                return xml;
            }
        }

        [Serializable]
        [XmlRoot(ElementName = "AssumeRoleResult")]
        public class AssumeRoleResult
        {
            public AssumeRoleResult() { }

            [XmlElement(ElementName = "Credentials")]
            public AccessCredentials Credentials { get; set; }
            public AccessCredentials GetAccessCredentials()
            {
                return this.Credentials;
            }
        }
    }

    public class AssumeRoleProvider : AssumeRoleBaseProvider<AssumeRoleProvider>
    {
        internal string STSEndPoint { get; set; }
        internal AccessCredentials credentials { get; set; }
        internal string Url { get; set; }
        private readonly uint DefaultDurationInSeconds = 3600;
        private readonly string AssumeRole = "AssumeRole";

        public AssumeRoleProvider()
        {
        }

        public AssumeRoleProvider(MinioClient client) : base(client)
        {
        }

        public AssumeRoleProvider WithSTSEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException("The STS endpoint cannot be null or empty.");
            }
            this.STSEndPoint = endpoint;
            Uri stsUri = utils.GetBaseUrl(endpoint);
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

            return this;
        }

        public async override Task<AccessCredentials> GetCredentialsAsync()
        {
            if (this.credentials != null && !this.credentials.AreExpired())
            {
                return this.credentials;
            }

            var requestBuilder = await this.BuildRequest();
            if (Client != null)
            {
                ResponseResult responseResult = null;
                try
                {
                    responseResult = await Client.ExecuteTaskAsync(this.NoErrorHandlers, requestBuilder, assumeRole: true);

                    AssumeRoleResponse assumeRoleResp = null;
                    if (responseResult.Response.IsSuccessStatusCode)
                    {
                        var contentBytes = Encoding.UTF8.GetBytes(responseResult.Content);

                        using (var stream = new MemoryStream(contentBytes))
                        {
                            assumeRoleResp = (AssumeRoleResponse)new XmlSerializer(typeof(AssumeRoleResponse)).Deserialize(stream);
                        }

                    }
                    if (this.credentials == null &&
                             assumeRoleResp != null &&
                             assumeRoleResp.arr != null)
                    {
                        this.credentials = assumeRoleResp.arr.Credentials;
                    }
                    return this.credentials;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    responseResult?.Dispose();
                }
            }
            throw new ArgumentNullException(nameof(Client) + " should have been assigned for the operation to continue.");
        }

        internal override async Task<HttpRequestMessageBuilder> BuildRequest()
        {
            this.Action = this.AssumeRole;
            if (this.DurationInSeconds == null || this.DurationInSeconds.Value == 0)
                this.DurationInSeconds = DefaultDurationInSeconds;

            var requestMessageBuilder = await Client.CreateRequest(HttpMethod.Post);

            FormUrlEncodedContent formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Action", "AssumeRole"),
                new KeyValuePair<string, string>("DurationSeconds", this.DurationInSeconds.ToString()),
                new KeyValuePair<string, string>("Version", "2011-06-15"),
            });
            var byteArrContent = await formContent.ReadAsByteArrayAsync();
            requestMessageBuilder.SetBody(byteArrContent);
            requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Type", "application/x-www-form-urlencoded");
            requestMessageBuilder.AddOrUpdateHeaderParameter("Accept-Encoding", "identity");
            await Task.Yield();

            return requestMessageBuilder;
        }
    }
}