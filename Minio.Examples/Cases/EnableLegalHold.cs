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
    class EnableLegalHold
    {
        // Enable Legal Hold and then Check if Legal Hold is Enabled on a bucket
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket",
                                     string objectName = "my-object",
                                     string versionId = "versionId")
        {
            try
            {
                Console.WriteLine("Running example for API: EnableLegalHold, ");
                await minio.EnableObjectLegalHoldAsync(bucketName, objectName, versionId);
                bool enabled = await minio.IsObjectLegalHoldEnabledAsync(bucketName, objectName, versionId);
                Console.WriteLine("Legal Hold " + (enabled ? "ON " : "OFF ") + " for bucket " + bucketName + "after calling Enable Legal Hold.");
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}
