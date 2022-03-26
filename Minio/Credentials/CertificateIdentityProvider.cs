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
 * Certificate Identity Credential provider.
 * This is a MinIO Extension to AssumeRole STS APIs on
 * AWS, purely based on client certificates mTLS authentication.
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
        int DEFAULT_DURATION_IN_SECONDS = 3600;
        internal string stsEndpoint { get; set; }
        internal int durationInSeconds { get; set; }
        internal X509Certificate2 clientCertificate { get; set; }
        internal string postEndpoint { get; set; }
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

        public CertificateIdentityProvider WithHttpClient(HttpClient httpClient = null)
        {
            this.httpClient = httpClient;
            return this;
        }

        public CertificateIdentityProvider WithCertificate(X509Certificate2 cert = null)
        {
            this.clientCertificate = cert;
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
                throw new ArgumentException("httpClient cannot be null or empty");
            }

            if (this.clientCertificate == null)
            {
                throw new ArgumentException("clientCertificate cannot be null or empty");
            }

            Task<HttpResponseMessage> t = Task.Run(async () => await this.httpClient.PostAsync(this.postEndpoint, null));
            t.Wait();
            HttpResponseMessage response = t.Result;

            CertificateResponse certResponse = new CertificateResponse();
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                var contentBytes = Encoding.UTF8.GetBytes(content);

                using (var stream = new MemoryStream(contentBytes))
                {
                    certResponse =
                    (CertificateResponse)new XmlSerializer(typeof(CertificateResponse)).Deserialize(stream);
                }

            }
            if (this.credentials == null &&
                     certResponse != null &&
                     certResponse.cr != null)
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
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["Action"] = "AssumeRoleWithCertificate";
            query["Version"] = "2011-06-15";
            query["DurationInSeconds"] = this.durationInSeconds.ToString();
            builder.Query = query.ToString();
            this.postEndpoint = builder.ToString();

            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            handler.ClientCertificates.Add(this.clientCertificate);
            this.httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(stsEndpoint)
            };

            this.credentials = GetCredentials();
            return this;
        }
    }
}
