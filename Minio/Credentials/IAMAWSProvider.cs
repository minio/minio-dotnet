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
using System.Text.Json;
using Minio.DataModel;
using Minio.Exceptions;

/*
 * IAM roles for Amazon EC2
 * http://docs.aws.amazon.com/AWSEC2/latest/UserGuide/iam-roles-for-amazon-ec2.html
 * The Credential provider for attaching an IAM rule.
 */

namespace Minio.Credentials;

public class IAMAWSProvider : EnvironmentProvider
{
    public IAMAWSProvider()
    {
        Minio_Client = null;
    }

    public IAMAWSProvider(string endpoint, MinioClient client)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            CustomEndPoint = new Uri(endpoint);
            if (string.IsNullOrWhiteSpace(CustomEndPoint.Authority))
                throw new ArgumentNullException("Endpoint field " + nameof(CustomEndPoint) + " is invalid.");
        }

        Minio_Client = client ?? throw new ArgumentException("MinioClient reference field " + nameof(Minio_Client) + " cannot be null.");
        CustomEndPoint = new Uri(endpoint);
    }

    internal Uri CustomEndPoint { get; set; }
    internal AccessCredentials Credentials { get; set; }
    internal MinioClient Minio_Client { get; set; }

    public override AccessCredentials GetCredentials()
    {
        Validate();
        var url = CustomEndPoint;
        if (CustomEndPoint == null)
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION");
            if (string.IsNullOrWhiteSpace(region))
                url = RequestUtil.MakeTargetURL("sts.amazonaws.com", true);
            else
                url = RequestUtil.MakeTargetURL("sts." + region + ".amazonaws.com", true);
        }

        ClientProvider provider = new WebIdentityProvider()
            .WithSTSEndpoint(url)
            .WithRoleAction("AssumeRoleWithWebIdentity")
            .WithDurationInSeconds(null)
            .WithPolicy(null)
            .WithRoleARN(Environment.GetEnvironmentVariable("AWS_ROLE_ARN"))
            .WithRoleSessionName(Environment.GetEnvironmentVariable("AWS_ROLE_SESSION_NAME"));
        Credentials = provider.GetCredentials();
        return Credentials;
    }

    internal AccessCredentials GetAccessCredentials(string tokenFile)
    {
        Validate();
        var url = CustomEndPoint;
        var urlStr = url.Authority;
        if (url == null || string.IsNullOrWhiteSpace(urlStr))
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION");
            urlStr = region == null ? "https://sts.amazonaws.com" : "https://sts." + region + ".amazonaws.com";
            url = new Uri(urlStr);
        }

        ClientProvider provider = new WebIdentityProvider()
            .WithJWTSupplier(() =>
            {
                var tokenContents = File.ReadAllText(tokenFile);
                return new JsonWebToken(tokenContents, 0);
            })
            .WithSTSEndpoint(url)
            .WithDurationInSeconds(null)
            .WithPolicy(null)
            .WithRoleARN(Environment.GetEnvironmentVariable("AWS_ROLE_ARN"))
            .WithRoleSessionName(Environment.GetEnvironmentVariable("AWS_ROLE_SESSION_NAME"));
        Credentials = provider.GetCredentials();
        return Credentials;
    }

    public async Task<AccessCredentials> GetAccessCredentials(Uri url)
    {
        Validate();
        using var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());

        var requestBuilder = new HttpRequestMessageBuilder(HttpMethod.Get, url);
        requestBuilder.AddQueryParameter("location", "");

        using var response =
            await Minio_Client.ExecuteTaskAsync(Enumerable.Empty<ApiResponseErrorHandlingDelegate>(), requestBuilder);
        if (string.IsNullOrWhiteSpace(response.Content) ||
            !HttpStatusCode.OK.Equals(response.StatusCode))
            throw new CredentialsProviderException("IAMAWSProvider",
                "Credential Get operation failed with HTTP Status code: " + response.StatusCode);
        /*
JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
   MissingMemberHandling = MissingMemberHandling.Error,
   ContractResolver = new CamelCasePropertyNamesContractResolver(),
   Error = null
};*/

        var credentials = JsonSerializer.Deserialize<ECSCredentials>(response.Content);
        if (credentials.Code?.Equals("success", StringComparison.OrdinalIgnoreCase) == false)
            throw new CredentialsProviderException("IAMAWSProvider",
                "Credential Get operation failed with code: " + credentials.Code + " and message " +
                credentials.Message);

        Credentials = credentials.GetAccessCredentials();
        return Credentials;
    }

    public override async Task<AccessCredentials> GetCredentialsAsync()
    {
        if (Credentials?.AreExpired() == false)
        {
            return Credentials;
        }

        var url = CustomEndPoint;
        var awsTokenFile = Environment.GetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE");
        if (!string.IsNullOrWhiteSpace(awsTokenFile))
        {
            Credentials = GetAccessCredentials(awsTokenFile);
            return Credentials;
        }

        var containerRelativeUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI");
        var containerFullUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_FULL_URI");
        var isURLEmpty = url == null;
        if (!string.IsNullOrWhiteSpace(containerRelativeUri) && isURLEmpty)
        {
            url = RequestUtil.MakeTargetURL("169.254.170.2" + "/" + containerRelativeUri, false);
        }
        else if (!string.IsNullOrWhiteSpace(containerFullUri) && isURLEmpty)
        {
            var fullUri = new Uri(containerFullUri);
            url = RequestUtil.MakeTargetURL(fullUri.AbsolutePath, fullUri.Scheme == "https");
        }
        else
        {
            url = await GetIamRoleNamedURL();
        }

        Credentials = await GetAccessCredentials(url);
        return Credentials;
    }

    public async Task<string> GetIamRoleNameAsync(Uri url)
    {
        Validate();
        var requestBuilder = new HttpRequestMessageBuilder(HttpMethod.Get, url);
        requestBuilder.AddQueryParameter("location", "");

        using var response =
            await Minio_Client.ExecuteTaskAsync(Enumerable.Empty<ApiResponseErrorHandlingDelegate>(), requestBuilder);

        if (string.IsNullOrWhiteSpace(response.Content) ||
            !HttpStatusCode.OK.Equals(response.StatusCode))
            throw new CredentialsProviderException("IAMAWSProvider",
                "Credential Get operation failed with HTTP Status code: " + response.StatusCode);

        string[] roleNames = response.Content.Split('\n');
        if (roleNames.Length <= 0)
            throw new CredentialsProviderException("IAMAWSProvider",
                "No IAM roles are attached to AWS service at " + url);

        var index = 0;
        foreach (var item in roleNames) roleNames[index++] = item.Trim();
        return roleNames[0];
    }

    public async Task<Uri> GetIamRoleNamedURL()
    {
        Validate();
        var url = CustomEndPoint;
        string newUrlStr;
        if (url == null || string.IsNullOrWhiteSpace(url.Authority))
        {
            url = new Uri("http://169.254.169.254/latest/meta-data/iam/security-credentials/");
            newUrlStr = "http://169.254.169.254/latest/meta-data/iam/security-credentials/";
        }
        else
        {
            var urlStr = url.Scheme + "://" + url.Authority + "/latest/meta-data/iam/security-credentials/";
            url = new Uri(urlStr);
            newUrlStr = urlStr;
        }

        var roleName = await GetIamRoleNameAsync(url);
        newUrlStr += roleName;
        return new Uri(newUrlStr);
    }

    public IAMAWSProvider WithMinioClient(MinioClient minio)
    {
        Minio_Client = minio;
        if (Credentials == null ||
            string.IsNullOrWhiteSpace(Credentials.AccessKey) || string.IsNullOrWhiteSpace(Credentials.SecretKey))
            Credentials = GetCredentialsAsync().GetAwaiter().GetResult();

        return this;
    }

    public IAMAWSProvider WithEndpoint(string endpoint)
    {
        if (endpoint.Contains("https") || endpoint.Contains("http"))
            CustomEndPoint = new Uri(endpoint);
        else
            CustomEndPoint = RequestUtil.MakeTargetURL(endpoint, true);
        return this;
    }

    public void Validate()
    {
        if (Minio_Client == null)
            throw new ArgumentNullException(nameof(Minio_Client) +
                                            " should be assigned for the operation to continue.");
    }
}