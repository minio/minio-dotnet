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
using CommunityToolkit.HighPerformance;
using Minio.DataModel;
using Minio.DataModel.Result;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio.Credentials;

public class AssumeRoleProvider : AssumeRoleBaseProvider<AssumeRoleProvider>
{
    private readonly string assumeRole = "AssumeRole";
    private readonly uint defaultDurationInSeconds = 3600;

    public AssumeRoleProvider()
    {
    }

    public AssumeRoleProvider(IMinioClient client) : base(client)
    {
    }

    internal string STSEndPoint { get; set; }
    internal string Url { get; set; }

    public AssumeRoleProvider WithSTSEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentNullException(nameof(endpoint), "The STS endpoint cannot be null or empty.");

        STSEndPoint = endpoint;
        var stsUri = Utils.GetBaseUrl(endpoint);
        if ((string.Equals(stsUri.Scheme, "http", StringComparison.OrdinalIgnoreCase) && stsUri.Port == 80) ||
            (string.Equals(stsUri.Scheme, "https", StringComparison.OrdinalIgnoreCase) && stsUri.Port == 443) ||
            stsUri.Port <= 0)
            Url = stsUri.Scheme + "://" + stsUri.Authority;
        else if (stsUri.Port > 0) Url = stsUri.Scheme + "://" + stsUri.Host + ":" + stsUri.Port;

        Url = stsUri.Authority;

        return this;
    }

    public override async ValueTask<AccessCredentials> GetCredentialsAsync()
    {
        if (Credentials?.AreExpired() == false) return Credentials;

        var requestBuilder = await BuildRequest().ConfigureAwait(false);
        if (Client is not null)
        {
            ResponseResult responseResult = null;
            try
            {
                responseResult = await Client.ExecuteTaskAsync(requestBuilder, isSts: true)
                    .ConfigureAwait(false);

                AssumeRoleResponse assumeRoleResp = null;
                if (responseResult.Response.IsSuccessStatusCode)
                {
                    using var stream = Encoding.UTF8.GetBytes(responseResult.Content).AsMemory().AsStream();
                    assumeRoleResp = Utils.DeserializeXml<AssumeRoleResponse>(stream);
                }

                if (Credentials is null &&
                    assumeRoleResp?.AssumeRole is not null)
                    Credentials = assumeRoleResp.AssumeRole.Credentials;

                return Credentials;
            }
            finally
            {
                responseResult?.Dispose();
            }
        }

        throw new InternalClientException("Client should have been assigned for the operation to continue.");
    }

    internal override async Task<HttpRequestMessageBuilder> BuildRequest()
    {
        Action = assumeRole;
        if (DurationInSeconds is null || DurationInSeconds.Value == 0)
            DurationInSeconds = defaultDurationInSeconds;

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
