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

using System.Text;
using System.Xml;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using Minio.DataModel;

namespace Minio.Credentials;

[Serializable]
[XmlRoot(ElementName = "AssumeRoleResponse", Namespace = "https://sts.amazonaws.com/doc/2011-06-15/")]
public class AssumeRoleResponse
{
    [XmlElement(ElementName = "AssumeRoleResult")]
    public AssumeRoleResult arr { get; set; }

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
[XmlRoot(ElementName = "AssumeRoleResult")]
public class AssumeRoleResult
{
    [XmlElement(ElementName = "Credentials")]
    public AccessCredentials Credentials { get; set; }
}

public class AssumeRoleProvider : AssumeRoleBaseProvider<AssumeRoleProvider>
{
    private readonly string AssumeRole = "AssumeRole";
    private readonly uint DefaultDurationInSeconds = 3600;

    public AssumeRoleProvider()
    {
    }

    public AssumeRoleProvider(MinioClient client) : base(client)
    {
    }

    internal string STSEndPoint { get; set; }
    internal AccessCredentials credentials { get; set; }
    internal string Url { get; set; }

    public AssumeRoleProvider WithSTSEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentNullException(nameof(endpoint), "The STS endpoint cannot be null or empty.");

        STSEndPoint = endpoint;
        var stsUri = Utils.GetBaseUrl(endpoint);
        if ((stsUri.Scheme == "http" && stsUri.Port == 80) ||
            (stsUri.Scheme == "https" && stsUri.Port == 443) ||
            stsUri.Port <= 0)
            Url = stsUri.Scheme + "://" + stsUri.Authority;
        else if (stsUri.Port > 0) Url = stsUri.Scheme + "://" + stsUri.Host + ":" + stsUri.Port;

        Url = stsUri.Authority;

        return this;
    }

    public override async Task<AccessCredentials> GetCredentialsAsync()
    {
        if (credentials?.AreExpired() == false) return credentials;

        var requestBuilder = await BuildRequest().ConfigureAwait(false);
        if (Client != null)
        {
            ResponseResult responseResult = null;
            try
            {
                responseResult = await Client.ExecuteTaskAsync(NoErrorHandlers, requestBuilder, true)
                    .ConfigureAwait(false);

                AssumeRoleResponse assumeRoleResp = null;
                if (responseResult.Response.IsSuccessStatusCode)
                    assumeRoleResp =
                        (AssumeRoleResponse)new XmlSerializer(typeof(AssumeRoleResponse)).Deserialize(
                            Encoding.UTF8.GetBytes(responseResult.Content).AsMemory().AsStream());

                if (credentials == null &&
                    assumeRoleResp?.arr != null)
                    credentials = assumeRoleResp.arr.Credentials;

                return credentials;
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
        Action = AssumeRole;
        if (DurationInSeconds == null || DurationInSeconds.Value == 0)
            DurationInSeconds = DefaultDurationInSeconds;

        var requestMessageBuilder = await Client.CreateRequest(HttpMethod.Post).ConfigureAwait(false);

        using var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Action", "AssumeRole"),
            new KeyValuePair<string, string>("DurationSeconds", DurationInSeconds.ToString()),
            new KeyValuePair<string, string>("Version", "2011-06-15")
        });
        ReadOnlyMemory<byte> byteArrContent = await formContent.ReadAsByteArrayAsync().ConfigureAwait(false);
        requestMessageBuilder.SetBody(byteArrContent);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Type",
            "application/x-www-form-urlencoded; charset=utf-8");
        requestMessageBuilder.AddOrUpdateHeaderParameter("Accept-Encoding", "identity");
        await Task.Yield();

        return requestMessageBuilder;
    }
}