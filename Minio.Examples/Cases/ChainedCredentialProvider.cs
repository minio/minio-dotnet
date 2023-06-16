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

using Minio.Credentials;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Minio.Examples.Cases;

public static class ChainedCredentialProvider
{
    // Establish Credentials with AWS Session token
    public static async Task Run()
    {
        var provider = new ChainedProvider()
            .AddProviders(new IClientProvider[] { new AWSEnvironmentProvider(), new MinioEnvironmentProvider() });
        //Chained provider definition here.
        using var minioClient = new MinioClient()
            .WithEndpoint("s3.amazonaws.com")
            .WithSSL()
            .WithCredentialsProvider(provider)
            .Build();
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket("my-bucket-name")
                .WithObject("my-object-name");
            var result = await minioClient.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
        }
        catch (MinioException me)
        {
            Console.WriteLine($"[Bucket] ChainedCredentialProvider example case encountered Exception: {me}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket] ChainedCredentialProvider example case encountered Exception: {e}");
        }
    }
}