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
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using Minio.DataModel;

/*
 * Web Identity Credential provider
 * https://docs.aws.amazon.com/STS/latest/APIReference/API_AssumeRoleWithWebIdentity.html
 */

namespace Minio.Credentials;

[Serializable]
[XmlRoot(ElementName = "AssumeRoleWithWebIdentityResponse")]
public class WebIdentityResponse
{
    [XmlElement("Credentials")] public AccessCredentials Credentials { get; set; }
}

public class WebIdentityProvider : WebIdentityClientGrantsProvider<WebIdentityProvider>
{
    internal int ExpiryInSeconds { get; set; }
    internal JsonWebToken CurrentJsonWebToken { get; set; }

    public override AccessCredentials GetCredentials()
    {
        Validate();
        return base.GetCredentials();
    }

    public override Task<AccessCredentials> GetCredentialsAsync()
    {
        Validate();
        return base.GetCredentialsAsync();
    }

    internal WebIdentityProvider WithJWTSupplier(Func<JsonWebToken> f)
    {
        JWTSupplier = (Func<JsonWebToken>)f.Clone();
        Validate();
        return this;
    }

    internal override Task<HttpRequestMessageBuilder> BuildRequest()
    {
        Validate();
        CurrentJsonWebToken = JWTSupplier();
        // RoleArn to be set already.
        WithRoleAction("AssumeRoleWithWebIdentity");
        WithDurationInSeconds(GetDurationInSeconds(CurrentJsonWebToken.Expiry));
        RoleSessionName ??= Utils.To8601String(DateTime.Now);
        return base.BuildRequest();
    }

    internal override AccessCredentials ParseResponse(HttpResponseMessage response)
    {
        Validate();
        var credentials = base.ParseResponse(response);
        return (AccessCredentials)new XmlSerializer(typeof(AccessCredentials)).Deserialize(Encoding.UTF8
            .GetBytes(Convert.ToString(response.Content)).AsMemory().AsStream());
    }
}