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

using System.Globalization;
using System.Net;
using System.Text;
using CommunityToolkit.HighPerformance;
using Minio.DataModel;

namespace Minio.Credentials;

// Assume-role credential provider
public abstract class AssumeRoleBaseProvider<T> : IClientProvider
    where T : AssumeRoleBaseProvider<T>
{
    internal readonly IEnumerable<ApiResponseErrorHandler> NoErrorHandlers =
        Enumerable.Empty<ApiResponseErrorHandler>();

    protected AssumeRoleBaseProvider(MinioClient client)
    {
        Client = client;
    }

    protected AssumeRoleBaseProvider()
    {
        Client = null;
    }

    internal AccessCredentials Credentials { get; set; }
    internal MinioClient Client { get; set; }
    internal string Action { get; set; }
    internal uint? DurationInSeconds { get; set; }
    internal string Region { get; set; }
    internal string RoleSessionName { get; set; }
    internal string Policy { get; set; }
    internal string RoleARN { get; set; }
    internal string ExternalID { get; set; }

    public virtual async ValueTask<AccessCredentials> GetCredentialsAsync()
    {
        if (Credentials?.AreExpired() == false) return Credentials;

        var requestBuilder = await BuildRequest().ConfigureAwait(false);
        if (Client is not null)
        {
            ResponseResult responseMessage = null;
            try
            {
                responseMessage = await Client.ExecuteTaskAsync(NoErrorHandlers, requestBuilder).ConfigureAwait(false);
            }
            finally
            {
                responseMessage?.Dispose();
            }
        }

        return null;
    }

    public virtual AccessCredentials GetCredentials()
    {
        throw new InvalidOperationException("Please use the GetCredentialsAsync method.");
    }

    public T WithDurationInSeconds(uint? durationInSeconds)
    {
        DurationInSeconds = durationInSeconds;
        return (T)this;
    }

    public T WithRegion(string region)
    {
        Region = !string.IsNullOrWhiteSpace(region) ? region : "";
        return (T)this;
    }

    public T WithRoleARN(string roleArn)
    {
        RoleARN = roleArn;
        return (T)this;
    }

    public T WithPolicy(string policy)
    {
        Policy = policy;
        return (T)this;
    }

    public T WithRoleSessionName(string sessionName)
    {
        RoleSessionName = sessionName;
        return (T)this;
    }

    public T WithExternalID(string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentNullException("The External ID cannot be null or empty.");
        if (externalId.Length < 2 || externalId.Length > 1224)
            throw new ArgumentOutOfRangeException("The External Id needs to be between 2 to 1224 characters in length");
        ExternalID = externalId;
        return (T)this;
    }

    public T WithRoleAction(string action)
    {
        Action = action;
        return (T)this;
    }

    internal virtual async Task<HttpRequestMessageBuilder> BuildRequest()
    {
        if (Client is null) throw new InvalidOperationException("MinioClient is not set in AssumeRoleBaseProvider");
        var reqBuilder = await Client.CreateRequest(HttpMethod.Post).ConfigureAwait(false);
        reqBuilder.AddQueryParameter("Action", Action);
        reqBuilder.AddQueryParameter("Version", "2011-06-15");
        if (!string.IsNullOrWhiteSpace(Policy)) reqBuilder.AddQueryParameter("Policy", Policy);
        if (!string.IsNullOrWhiteSpace(RoleARN)) reqBuilder.AddQueryParameter("RoleArn", RoleARN);
        if (!string.IsNullOrWhiteSpace(RoleSessionName)) reqBuilder.AddQueryParameter("RoleSessionName", RoleARN);

        return reqBuilder;
    }

    internal virtual AccessCredentials ParseResponse(HttpResponseMessage response)
    {
        var content = Convert.ToString(response.Content, CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(content) || !HttpStatusCode.OK.Equals(response.StatusCode))
            throw new ArgumentNullException(nameof(response), "Unable to generate credentials. Response error.");

        using var stream = Encoding.UTF8.GetBytes(content).AsMemory().AsStream();
        return Utils.DeserializeXml<AccessCredentials>(stream);
    }
}