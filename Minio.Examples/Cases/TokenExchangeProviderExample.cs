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

using Minio.Exceptions;
using Minio.DataModel.Args;

namespace Minio.Examples.Cases;

public static class TokenExchangeProviderExample
{
    public static async Task Run(IMinioClient minioClient)
    {
        var currentAccessTokenPath = Environment.GetEnvironmentVariable("MINIO_WEB_IDENTITY_TOKEN_FILE");
        var currentAccessToken = (await File.ReadAllTextAsync(currentAccessTokenPath).ConfigureAwait(false)).Trim();

        var tokenEndpoint = "http://192.168.86.151:8080/realms/master/protocol/openid-connect/token";
        var clientId = "minio-client";
        var clientSecret = "bM6Lxy1QkaYjhk7iT1ZSs7qfuFN2VYt5";
        var endPoint = new Uri("https://192.168.86.151:9005");

        try
        {
            Console.WriteLine("\n\n**************** ERSAN TEST BEGINS HERE ****************\n\n");
#pragma warning disable MA0039 // Suppress Meziantou.Analyzer
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (
                    message,
                    cert,
                    chain,
                    sslPolicyErrors
                ) =>
                {
                    // If trusted, we're good
                    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                        return true;

                    // Check if our MinIO server
                    // uses the thumbprint from my console output
                    return string.Equals(
                        cert?.GetCertHashString(),
                        "ACE26B985DD4DC88FF369B4938E95C6EEF48B12F",
                        StringComparison.Ordinal
                    );
                },
            };
#pragma warning restore MA0039
            var httpClient = new HttpClient(handler);

            minioClient = minioClient
                    .WithHttpClient(httpClient)
                    .Build();

            var provider = new TokenExchangeProvider(minioClient, tokenEndpoint, clientId, clientSecret, endPoint);
            var newJwt = await provider.TokenExchangeAsync(currentAccessToken).ConfigureAwait(false);

            var creds = provider.GetAccessCredentials(newJwt);

            minioClient = minioClient
                .WithCredentials(creds.AccessKey, creds.SecretKey)
                .WithSessionToken(creds.SessionToken)
                .Build();

            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket("my-bucket-name")
                    .WithObject("my-object-name");
                var result = await minioClient.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
                Console.WriteLine("Object Stat: \n" + result);
            }
            catch (MinioException me)
            {
                Console.WriteLine($"[Bucket] IAMAWSProviderExample example case encountered MinioException: {me}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket] IAMAWSProviderExample example case encountered Exception: {e}");
            }

        }
        catch (Exception e)
        {
            Console.WriteLine($"AssumeRoleWithWebIdentity test exception: {e}\n");
            throw;
        }
    }
}
