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

using Minio.Credentials;
using Minio.DataModel.Args;

namespace Minio.Examples.Cases
{
    public static class AssumeRoleProviderExample
    {
        // Establish Authentication by assuming the role of an existing user
        public static async Task Run()
        {
            // endpoint usually point to MinIO server.
            var endpoint = "alias:port";

            // Access key to fetch credentials from STS endpoint.
            var accessKey = "access-key";

            // Secret key to fetch credentials from STS endpoint.
            var secretKey = "secret-key";

            using var minio = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL()
                .Build();
            try
            {
                var provider = new AssumeRoleProvider(minio);

                var token = await provider.GetCredentialsAsync().ConfigureAwait(false);
                // Console.WriteLine("\nToken = "); utils.Print(token);
                using var minioClient = new MinioClient()
                        .WithEndpoint(endpoint)
                        .WithCredentials(token.AccessKey, token.SecretKey)
                        .WithSessionToken(token.SessionToken)
                        .WithSSL()
                        .Build()
                    ;
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket("bucket-name")
                    .WithObject("object-name");
                var result = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
                // Console.WriteLine("Object Stat: \n"); utils.Print(result);
                Console.WriteLine("AssumeRoleProvider test PASSed\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"AssumeRoleProvider test exception: {e}\n");
            }
        }
    }
}