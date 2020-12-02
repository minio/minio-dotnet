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

namespace Minio.Examples.Cases
{
    public class SetObjectRetention
    {
        // Put Encryption Configuration for the bucket
        public async static Task Run(MinioClient minio,
                                    string bucketName = "my-bucket-name",
                                    string objectName = "my-object-name",
                                    string versionId = null,
                                    int numOfDays = 1)
        {
            try
            {
                Console.WriteLine("Running example for API: SetObjectRetentionAsync");
                await minio.SetObjectRetentionAsync(
                    new SetObjectRetentionArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithVersionId(versionId)
                        .WithRetentionValidDays(numOfDays)
                );
                string versionInfo = (string.IsNullOrEmpty(versionId))?"":(" Version ID: " + versionId);
                Console.WriteLine($"Assigned retention configuration to object {bucketName}/{objectName} "  +
                        versionInfo +
                        " Number of days: " + numOfDays);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}