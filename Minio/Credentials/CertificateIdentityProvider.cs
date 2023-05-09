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

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using Minio.DataModel;
using Minio.Exceptions;

/*
 * Certificate Identity Credential provider.
 * This is a MinIO Extension to AssumeRole STS APIs on
 * AWS, purely based on client certificates mTLS authentication.
 */

namespace Minio.Credentials;

[Serializable]
[XmlRoot(ElementName = "AssumeRoleWithCertificateResponse", Namespace = "https://sts.amazonaws.com/doc/2011-06-15/")]
public class CertificateResponse
{
    [XmlElement(ElementName = "AssumeRoleWithCertificateResult")]
    public CertificateResult Cr { get; set; }

    public string ToXML()
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true
        };
        using var ms = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(ms, settings);
        var names = new XmlSerializerNamespaces();
        names.Add(string.Empty, "https://sts.amazonaws.com/doc/2011-06-15/");

        var cs = new XmlSerializer(typeof(CertificateResponse));
        cs.Serialize(xmlWriter, this, names);

        ms.Flush();
        ms.Seek(0, SeekOrigin.Begin);
        using var streamReader = new StreamReader(ms);
        return streamReader.ReadToEnd();
    }
}

[Serializable]
[XmlRoot(ElementName = "AssumeRoleWithCertificateResult")]
public class CertificateResult
{
    [XmlElement(ElementName = "Credentials")]
    public AccessCredentials Credentials { get; set; }
}

public class CertificateIdentityProvider : IClientProvider
{
    private readonly int DEFAULT_DURATION_IN_SECONDS = 3600;

    public CertificateIdentityProvider()
    {
        DurationInSeconds = DEFAULT_DURATION_IN_SECONDS;
    }

    internal string StsEndpoint { get; set; }
    internal int DurationInSeconds { get; set; }
    internal X509Certificate2 ClientCertificate { get; set; }
    internal string PostEndpoint { get; set; }
    internal HttpClient HttpClient { get; set; }
    internal AccessCredentials Credentials { get; set; }

    public CertificateIdentityProvider WithStsEndpoint(string stsEndpoint)
    {
        if (string.IsNullOrEmpty(stsEndpoint))
            throw new InvalidEndpointException("Missing mandatory argument: stsEndpoint");
        if (!stsEndpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            throw new InvalidEndpointException($"stsEndpoint {stsEndpoint} is invalid." + " The scheme must be https");

        StsEndpoint = stsEndpoint;
        return this;
    }

    public CertificateIdentityProvider WithHttpClient(HttpClient httpClient = null)
    {
        HttpClient = httpClient;
        return this;
    }

    public CertificateIdentityProvider WithCertificate(X509Certificate2 cert = null)
    {
        ClientCertificate = cert;
        return this;
    }

    public AccessCredentials GetCredentials()
    {
        return GetCredentialsAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async ValueTask<AccessCredentials> GetCredentialsAsync()
    {
        if (Credentials?.AreExpired() == false)
            return Credentials;

        if (HttpClient is null)
            throw new ArgumentException("httpClient cannot be null or empty");

        if (ClientCertificate is null) throw new ArgumentException("clientCertificate cannot be null or empty");

        using var response = await HttpClient.PostAsync(PostEndpoint, null).ConfigureAwait(false);

        var certResponse = new CertificateResponse();
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var stream = Encoding.UTF8.GetBytes(content).AsMemory().AsStream();
            certResponse =
                Utils.DeserializeXml<CertificateResponse>(stream);
        }

        if (Credentials is null && certResponse?.Cr is not null)
            Credentials = certResponse.Cr.Credentials;

        return Credentials;
    }

    public CertificateIdentityProvider Build()
    {
        if (string.IsNullOrEmpty(DurationInSeconds.ToString()))
            DurationInSeconds = DEFAULT_DURATION_IN_SECONDS;

        var builder = new UriBuilder(StsEndpoint);
        var query = HttpUtility.ParseQueryString(builder.Query);
        query["Action"] = "AssumeRoleWithCertificate";
        query["Version"] = "2011-06-15";
        query["DurationInSeconds"] = DurationInSeconds.ToString();
        builder.Query = query.ToString();
        PostEndpoint = builder.ToString();

        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = SslProtocols.Tls12
        };
        handler.ClientCertificates.Add(ClientCertificate);
        HttpClient ??= new HttpClient(handler)
        {
            BaseAddress = new Uri(StsEndpoint)
        };

        Credentials = GetCredentials();
        return this;
    }
}