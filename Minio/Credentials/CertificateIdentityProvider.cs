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
using System.Text;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
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
    public class CertificateResponse : CertificateResult
    {
    }


    [Serializable]
    [XmlRoot(ElementName = "AssumeRoleWithCertificateResult")]
    public class CertificateResult : AccessCredentials
    {
    }

    // [Serializable]
    // public class CertificateResponse
    // {
    //     public string AssumeRoleWithCertificateResult;
    //     [XmlElement("Credentials")]
    //     public AccessCredentials Credentials { get; set; }
    //     public AccessCredentials GetAccessCredentials()
    //     {
    //         return this.Credentials;
    //     }
    // }

    public class CertificateIdentityProvider : ClientProvider
    {
        int DEFAULT_DURATION_IN_SECONDS = 300;
        internal string stsEndpoint { get; set; }
        internal string clientPublicCrt { get; set; }
        internal string clientPrivateKey { get; set; }
        internal string clientKeyPassword { get; set; }
        internal string serverKeyPassword { get; set; }
        internal string serverPublicCrt { get; set; }
        internal string serverPfxCert { get; set; }
        internal string clientPfxCert { get; set; }
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

        public CertificateIdentityProvider WithClientPfxCert(string clientPfxCert = null)
        {
            if (string.IsNullOrEmpty(clientPfxCert))
            {
                throw new InvalidEndpointException("PFX certificate a is mandatory argument.");
            }
            this.clientPfxCert = clientPfxCert;
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
            Console.WriteLine("Get Credential _ _ _ _ _\nNot Null = " + (this.credentials != null));
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
            var content = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Printing response content\n" + content);
            XmlDocument doc = new XmlDocument();
            // doc.LoadXml(content);
            // var credDoc = doc.GetElementsByTagName("Credentials");
            // // Console.WriteLine("node1.FirstChild = " + node1.LocalName);
            // // var node2 = doc.GetElementsByTagName("AssumeRoleWithCertificateResult");
            // // Console.WriteLine("node2.Count = " + node2.Count);
            // // Console.WriteLine("node2.Item = " + node2[0]);

            // XmlNodeList nodes = doc.GetElementsByTagName("Credentials");
            // XmlNodeList xnList = doc.SelectNodes("/AssumeRoleWithCertificateResponse[@*]/AssumeRoleWithCertificateResult");
            // string ak = "";
            // foreach (XmlNode node in nodes)
            // {
            //     Console.WriteLine("node.Name = " + node.Name);
            //     // Console.WriteLine("aaaaaaaaaaaaaaaaaaaaaaaa"); utils.Print(node);
            //     // foreach (XmlNode child in node.ChildNodes)
            //     foreach (XmlNode xn in xnList)
            //     {
            //         XmlNode creds = xn.SelectSingleNode("Credentials");
            //         // if (example != null)
            //         // {
            //         ak = creds["AccessKeyId"].InnerText;
            //         // string no = creds["NO"].InnerText;
            //         // Console.WriteLine(" name = " + child.Name);
            //         // Console.WriteLine(" value " + child.Value);
            //     }
            // }
            // Console.WriteLine("access key id = " + ak);
            string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.None, true);
            Console.WriteLine("\nJ S O N\n" + json + "\n");
            var certResponse = JsonConvert.DeserializeObject<CertificateResponse>(json);
            // Console.WriteLine("certResponse\n" + certResponse);
            Console.WriteLine("      ========== certResponse ========\n"); utils.Print(certResponse);

            if (this.credentials == null //&&
                                         // certResponse.AccessKey != null &&
                                         // certResponse.SecretKey != null &&
                                         // !certResponse.AreExpired()
                )
            {
                this.credentials.AccessKey = certResponse.AccessKey;
                this.credentials.SecretKey = certResponse.SecretKey;
                this.credentials.SessionToken = certResponse.SessionToken;
                this.credentials.Expiration = certResponse.Expiration;

                // DateTime.Parse(certResponse.Expiration));
            }
            return credentials;
        }

        public override async Task<AccessCredentials> GetCredentialsAsync()
        {
            AccessCredentials credentials = this.GetCredentials();
            await Task.Yield();
            return credentials;
        }

        internal AccessCredentials ParseResponse(string content)
        {
            Console.WriteLine("Entered ParseResponse");
            // Stream receiveStream = content.ReadAsStreamAsync();
            // StreamReader readStream = new StreamReader (content, Encoding.UTF8);
            // // txtBlock.Text = readStream.ReadToEnd();
            // Console.WriteLine("response\n"); utils.Print(response.Content);
            // if (string.IsNullOrWhiteSpace(Convert.ToString(response.Content)) ||
            //     !response.IsSuccessStatusCode)
            // {
            //     throw new ArgumentNullException("Unable to get credentials. Response error.");
            // }
            // Console.WriteLine("Parsing\n"); utils.Print(response.Content);


            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return (AccessCredentials)new XmlSerializer(typeof(AccessCredentials)).Deserialize(stream);
            }

            // using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            // {
            //     var szed = new XmlSerializer(typeof(AccessCredentials));
            //     var dszed = szed.Deserialize(stream);
            //     Console.WriteLine($"\n\n       ddddd\n{dszed}");
            //     return (AccessCredentials)dszed;
            // }
        }

        public CertificateIdentityProvider Build()
        {
            // this.Validate();
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

                // Add server public crt
                if (serverPublicCrt != null)
                {
                    X509Certificate2 origServerCrt = new X509Certificate2(this.serverPublicCrt, this.serverKeyPassword);
                    String b64ServerCrt = Convert.ToBase64String(origServerCrt.RawData);
                    X509Certificate2 serverCrt = new X509Certificate2(Convert.FromBase64String(b64ServerCrt), this.serverKeyPassword);
                    handler.ClientCertificates.Add(serverCrt);
                }

                // // Add client public crt
                // if (clientPublicCrt != null)
                // {
                //     X509Certificate2 origClientCrt = new X509Certificate2(this.clientPublicCrt, this.clientKeyPassword);
                //     String b64ClientCrt = Convert.ToBase64String(origClientCrt.RawData);
                //     X509Certificate2 clientCrt = new X509Certificate2(Convert.FromBase64String(b64ClientCrt), this.clientKeyPassword);
                //     handler.ClientCertificates.Add(clientCrt);
                // }

                // // Add client private key
                // using (var rsa = RSA.Create())
                // {
                //     X509Certificate2 origClientKey = new X509Certificate2(this.clientPrivateKey, this.clientKeyPassword);
                //     byte[] decryptedClientKey = rsa.Decrypt(origClientKey.RawData, RSAEncryptionPadding.Pkcs1);
                //     String b64ClientKey = Convert.ToBase64String(decryptedClientKey);
                //     X509Certificate2 clientKey = new X509Certificate2(decryptedClientKey, this.clientKeyPassword);
                // }

                // Add client certificate pfx package
                if (clientPfxCert != null)
                {
                    X509Certificate2 clientPfx = new X509Certificate2(this.clientPfxCert, this.clientKeyPassword);
                    handler.ClientCertificates.Add(clientPfx);
                }

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
            // Console.WriteLine($"credentials.AccessKey = {this.credentials.AccessKey}");
            return this;
        }
    }
}