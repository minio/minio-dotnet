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
using Minio.DataModel;

namespace Minio.Examples.Cases
{
    class GetLegalHold
    {
        // Check Legal Hold status a object
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket-name",
                                     string objectName = "my-object-name",
                                     string versionId = null)
        {
            try
            {
                Console.WriteLine("Running example for API: GetLegalHoldInfo, ");
                var args = new GetObjectLegalHoldArgs()
                                        .WithBucket(bucketName)
                                        .WithObject(objectName)
                                        .WithVersionId(versionId);
                bool enabled = await minio.GetObjectLegalHoldAsync(args);
                Console.WriteLine("LegalHold Configuration STATUS for " + bucketName + "/" + objectName +
                                        (!string.IsNullOrEmpty(versionId)?" with Version ID " + versionId: " ") +
                                        " : " + (enabled?"ON":"OFF"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}