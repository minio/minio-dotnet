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

using System.Net;
using System.Text;
using CommunityToolkit.HighPerformance;
using Minio.DataModel;

namespace Minio.Credentials;

public abstract class WebIdentityClientGrantsProvider<T> : AssumeRoleBaseProvider<T>
    where T : WebIdentityClientGrantsProvider<T>
{
    public readonly uint MAX_DURATION_SECONDS = (uint)new TimeSpan(7, 0, 0, 0).TotalSeconds;
    public readonly uint MIN_DURATION_SECONDS = 15;

    internal Uri STSEndpoint { get; set; }
    internal Func<JsonWebToken> JWTSupplier { get; set; }

    internal uint GetDurationInSeconds(uint expiry)
    {
        if (DurationInSeconds != null && DurationInSeconds.Value > 0) expiry = DurationInSeconds.Value;
        if (expiry > MAX_DURATION_SECONDS) return MAX_DURATION_SECONDS;
        return expiry < MIN_DURATION_SECONDS ? MIN_DURATION_SECONDS : expiry;
    }

    internal T WithSTSEndpoint(Uri endpoint)
    {
        STSEndpoint = endpoint;
        return (T)this;
    }

    internal override async Task<HttpRequestMessageBuilder> BuildRequest()
    {
        Validate();
        var jwt = JWTSupplier();
        var requestMessageBuilder = await base.BuildRequest().ConfigureAwait(false);
        requestMessageBuilder = Utils.GetEmptyRestRequest(requestMessageBuilder);
        requestMessageBuilder.AddQueryParameter("WebIdentityToken", jwt.AccessToken);
        await Task.Yield();
        return requestMessageBuilder;
    }

    internal override AccessCredentials ParseResponse(HttpResponseMessage response)
    {
        Validate();
        // Stream receiveStream = response.Content.ReadAsStreamAsync();
        // StreamReader readStream = new StreamReader (receiveStream, Encoding.UTF8);
        // txtBlock.Text = readStream.ReadToEnd();
        var content = Convert.ToString(response.Content);
        if (string.IsNullOrWhiteSpace(content) ||
            !HttpStatusCode.OK.Equals(response.StatusCode))
            throw new ArgumentNullException(nameof(response), "Unable to get credentials. Response error.");

        using var stream = Encoding.UTF8.GetBytes(content).AsMemory().AsStream();
        return Utils.DeserializeXml<AccessCredentials>(stream);
    }

    protected void Validate()
    {
        if (JWTSupplier == null)
            throw new ArgumentNullException(nameof(JWTSupplier) + " JWT Token supplier cannot be null.");
        if (STSEndpoint == null || string.IsNullOrWhiteSpace(STSEndpoint.AbsoluteUri))
            throw new InvalidOperationException(nameof(STSEndpoint) + " value is invalid.");
    }
}