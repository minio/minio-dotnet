/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2021 MinIO, Inc.
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

using Minio;

namespace Minio.Examples.Cases
{
    public class GetBucketLifecycle
    {
        // Get Lifecycle configuration assigned to the bucket
        public async static Task Run(MinioClient minio,
                                    string bucketName = "my-bucket-name")
        {
            try
            {
                Console.WriteLine("Running example for API: GetBucketLifecycle");
                var lfc = await minio.GetBucketLifecycleAsync(
                    new GetBucketLifecycleArgs()
                        .WithBucket(bucketName)
                );
                if (lfc != null && lfc.Rules != null && lfc.Rules.Count > 0)
                {
                    Console.WriteLine($"Got Bucket Lifecycle set for bucket {bucketName}.");
                    Console.WriteLine(lfc.MarshalXML());
                    Console.WriteLine();
                    return;
                }
                Console.WriteLine($"Bucket Lifecycle not set for bucket {bucketName}.");
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }

}