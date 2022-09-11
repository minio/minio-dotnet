/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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

using System;
using System.Threading.Tasks;
using Minio.Exceptions;

namespace Minio.Examples.Cases;

internal class DeleteBucketPolicy
{
    // Set bucket policy
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name")
    {
        try
        {
            Console.WriteLine("Running example for API: DeletePolicyAsync");
            var args = new RemovePolicyArgs()
                .WithBucket(bucketName);
            await minio.RemovePolicyAsync(args);
            Console.WriteLine($"Policy previously set for the bucket {bucketName} removed.");
            try
            {
                var getArgs = new GetPolicyArgs()
                    .WithBucket(bucketName);
                var policy = await minio.GetPolicyAsync(getArgs);
            }
            catch (UnexpectedMinioException e)
            {
                Console.WriteLine($"GetPolicy operation for {bucketName} result: {e.ServerMessage}");
            }

            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}