/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2022 MinIO, Inc.
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
using System.Text;
using System.Web;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;

using Minio.DataModel;
using Minio.Exceptions;

/*
 * Certificate Identity Credential provider
 * https://docs.aws.amazon.com/STS/latest/APIReference/API_AssumeRoleWithertificateIdentity.html
 */

namespace Minio.Credentials
{
    [Serializable]
    [XmlRoot(ElementName = "AssumeRoleWithCertificateResponse", Namespace = "https://sts.amazonaws.com/doc/2011-06-15/")]
    public class CertificateResponse
    {
        [XmlElement(ElementName = "AssumeRoleWithCertificateResult")]
        public CertificateResult cr { get; set; }
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
        [XmlRoot(ElementName = "AssumeRoleWithCertificateResult")]
        public class CertificateResult
        {
            public CertificateResult() { }

            [XmlElement(ElementName = "Credentials")]
            public AccessCredentials Credentials { get; set; }
            public AccessCredentials GetAccessCredentials()
            {
                return this.Credentials;
            }
        }
    }
    public class CertificateIdentityProvider : ClientProvider
    {
        int DEFAULT_DURATION_IN_SECONDS = 300;
        internal string stsEndpoint { get; set; }
        internal string clientPublicCrt { get; set; }
        internal string clientPrivateKey { get; set; }
        internal string clientKeyPassword { get; set; }
        internal string serverKeyPassword { get; set; }
        internal string serverPublicCrt { get; set; }
        // internal string serverPfxCert { get; set; }
        // internal string clientPfxCert { get; set; }
        internal int durationInSeconds { get; set; }
        internal HttpClient httpClient { get; set; }

        internal AccessCredentials credentials { get; set; }

        public CertificateIdentityProvider()
        {
            this.durationInSeconds = DEFAULT_DURATION_IN_SECONDS;
        }

        public CertificateIdentityProvider WithStsEndpoint(string stsEndpoint)
        {
            if (string.IsNullOrEmpty(stsEndpoint))
            {
                throw new InvalidEndpointException("Missing mandatory argument: stsEndpoint");
            }
            if (!stsEndpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidEndpointException(
                                    $"stsEndpoint {stsEndpoint} is invalid." +
                                    " The scheme must be https");
            }

            this.stsEndpoint = stsEndpoint;
            return this;
        }
        public CertificateIdentityProvider WithClientPrivateKey(string clientPrivateKey = null)
        {
            this.clientPrivateKey = clientPrivateKey;
            return this;
        }

        public CertificateIdentityProvider WithClientPublicCrt(string clientPublicCrt = null)
        {
            this.clientPublicCrt = clientPublicCrt;
            return this;
        }

        public CertificateIdentityProvider WithClientKeyPassword(string clientKeyPassword = null)
        {
            this.clientKeyPassword = clientKeyPassword;
            return this;
        }

        public CertificateIdentityProvider WithServerKeyPassword(string serverKeyPassword = null)
        {
            this.serverKeyPassword = serverKeyPassword;
            return this;
        }

        public CertificateIdentityProvider WithServerPublicCrt(string serverPublicCrt = null)
        {
            this.serverPublicCrt = serverPublicCrt;
            return this;
        }

        public CertificateIdentityProvider WithDurationInSeconds(int durationInSeconds)
        {
            if (!string.IsNullOrEmpty(durationInSeconds.ToString()))
            {
                this.durationInSeconds = durationInSeconds;
            }
            return this;
        }

        public CertificateIdentityProvider WithHttpClient(HttpClient httpClient = null)
        {
            // if (HttpClient.ReferenceEquals(httpClient, null) ==
            //     string.IsNullOrWhiteSpace(this.clientPfxCert))
            // {
            //     throw new ArgumentException("2 - Either PFX Certificate or customized http_client must be provided");
            // }
            this.httpClient = httpClient;
            return this;
        }

        public override AccessCredentials GetCredentials()
        {
            if (this.credentials != null && !this.credentials.AreExpired())
            {
                return this.credentials;
            }

            if (this.httpClient == null)
            {
                throw new ArgumentException("Appname cannot be null or empty", nameof(httpClient));
            }

            Task<HttpResponseMessage> t = Task.Run(async () => await this.httpClient.PostAsync(this.stsEndpoint, null));
            t.Wait();
            HttpResponseMessage response = t.Result;

            CertificateResponse certResponse = new CertificateResponse();
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                var contentBytes = Encoding.UTF8.GetBytes(content);

                using (var stream = new MemoryStream(contentBytes))
                    certResponse =
                        (CertificateResponse)new XmlSerializer(typeof(CertificateResponse)).Deserialize(stream);

            }
            if (this.credentials == null && certResponse != null)
            {
                this.credentials = certResponse.cr.Credentials;
            }
            return this.credentials;
        }

        public override async Task<AccessCredentials> GetCredentialsAsync()
        {
            AccessCredentials credentials = this.GetCredentials();
            await Task.Yield();
            return credentials;
        }

        public CertificateIdentityProvider Build()
        {
            if (string.IsNullOrEmpty(this.durationInSeconds.ToString()))
            {
                this.durationInSeconds = DEFAULT_DURATION_IN_SECONDS;
            }

            var builder = new UriBuilder(stsEndpoint);
            builder.Port = 9000;
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["Action"] = "AssumeRoleWithCertificate";
            query["Version"] = "2011-06-15";
            query["DurationInSeconds"] = this.durationInSeconds.ToString();
            builder.Query = query.ToString();
            this.stsEndpoint = builder.ToString();

            if (HttpClient.ReferenceEquals(this.httpClient, null))
            {
                var handler = new HttpClientHandler();

                X509Certificate2 serverCrt = new X509Certificate2(this.serverPublicCrt, this.serverKeyPassword);
                handler.ClientCertificates.Add(serverCrt);

                X509Certificate2 clientCrt = new X509Certificate2(this.clientPublicCrt, this.clientKeyPassword);
                handler.ClientCertificates.Add(clientCrt);

                X509Certificate2 clientKey = new X509Certificate2(this.clientPrivateKey, this.clientKeyPassword);
                handler.ClientCertificates.Add(clientKey);

                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                this.httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(stsEndpoint)
                };
            }
            else
            {

            }
            this.credentials = GetCredentials();
            return this;
        }
    }
}