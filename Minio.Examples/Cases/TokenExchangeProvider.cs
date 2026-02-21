// -*- coding: utf-8 -*-
// MinIO Python Library for Amazon S3 Compatible Cloud Storage,
// (C) 2022 MinIO, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System.Text.Json;
using Minio.DataModel;
using Minio.Credentials;
using Minio.Helper;

namespace Minio.Examples.Cases;

public class TokenExchangeProvider
{
    private IMinioClient minioClient;
    private readonly string tokenEndpoint = "http://192.168.86.151:8080/realms/master/protocol/openid-connect/token";
    private readonly string clientId = "minio-client";
    private readonly string clientSecret;
    internal Uri CustomEndPoint { get; set; }
    internal AccessCredentials Credentials { get; set; }
    public TokenExchangeProvider(IMinioClient minioCli, string token_end_point, string client_id, string client_secret, Uri endpoint)
    {
        minioClient = minioCli;
        tokenEndpoint = token_end_point;
        clientId = client_id;
        clientSecret = client_secret;
        CustomEndPoint = endpoint;
    }

    internal AccessCredentials GetAccessCredentials(string tokenFile)
    {
        var url = CustomEndPoint;
        if (url is null || string.IsNullOrWhiteSpace(url.Authority))
        {
            var region = Environment.GetEnvironmentVariable("MINIO_REGION");
            var urlStr = region is null ? "https://sts.amazonaws.com" : "https://sts." + region + ".amazonaws.com";
            url = new Uri(urlStr);
        }

        var provider = new WebIdentityProvider()
            .WithJWTSupplier(() => new JsonWebToken(tokenFile, 0))
            .WithDurationInSeconds(3600)
            .WithPolicy("consoleAdmin");
            // .WithRoleARN(Environment.GetEnvironmentVariable("MINIO_ROLE_ARN"))
            // .WithRoleSessionName(Environment.GetEnvironmentVariable("MINIO_ROLE_SESSION_NAME"));
        Credentials = provider.GetCredentials();
        return Credentials;
    }

    public async Task<string> TokenExchangeAsync(string currentAccessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                throw new Exception("Keycloak credentials missing in environment variables.");

            // Standard Token Exchange (RFC 8693) Parameters
            var requestData = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:token-exchange" },
                { "subject_token", currentAccessToken },
                { "subject_token_type", "urn:ietf:params:oauth:token-type:access_token" },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "requested_token_type", "urn:ietf:params:oauth:token-type:access_token" },
            };

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(requestData)
            };

            var response = await minioClient.Config.HttpClient.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Exception($"Token exchange failed: {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(jsonResponse);

            return doc.RootElement.GetProperty("access_token").GetString().Trim();
        }
        catch (Exception e)
        {
            Console.WriteLine($"TokenExchangeToken exception: {e}\n");
            throw;
        }
    }
}
