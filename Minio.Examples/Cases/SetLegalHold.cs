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
using System.Threading;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio.Examples.Cases
{
    class SetLegalHold
    {
        // Enable Legal Hold and then Check if Legal Hold is Enabled on a bucket
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket-name",
                                     string objectName = "my-object-name",
                                     string versionId = "")
        {
            try
            {
                Console.WriteLine("Running example for API: SetLegalHold, enable legal hold");
                // Setting WithLegalHold true, sets Legal hold status to ON.
                SetObjectLegalHoldArgs args = new SetObjectLegalHoldArgs(bucketName, objectName)
                                                    .WithVersionID(versionId)
                                                    .WithLegalHold(true);
                await minio.SetObjectLegalHoldAsync(args);
                Console.WriteLine("Legal Hold status for bucket " + bucketName + " object " + objectName + 
                            (string.IsNullOrEmpty(versionId)?" " : " with version id " + versionId + " ") + 
                            " set to ON." );
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}
