﻿/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2018 MinIO, Inc.
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

using Minio.DataModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class CopyObjectMetadata
    {
        // Copy object from one bucket to another
        public async static Task Run(MinioClient minio,
                                     string fromBucketName = "from-bucket-name",
                                     string fromObjectName = "from-object-name",
                                     string destBucketName = "dest-bucket",
                                     string destObjectName = "to-object-name")
        {
            try
            {
                Console.WriteLine("Running example for API: CopyObjectAsync");

                // Optionally pass copy conditions to replace metadata on destination object with custom metadata
                var copyCond = new CopyConditions();
                copyCond.SetReplaceMetadataDirective();

                // set custom metadata
                var metadata = new Dictionary<string, string>
                {
                    { "Content-Type", "application/css" },
                    { "Mynewkey", "my-new-value" }
                };

                await minio.CopyObjectAsync(fromBucketName, 
                                                fromObjectName, 
                                                destBucketName, 
                                                destObjectName, 
                                                copyConditions:copyCond,
                                                metadata: metadata);

                Console.WriteLine($"Copied object {fromObjectName} from bucket {fromBucketName} to bucket {destBucketName}");
                Console.WriteLine();    
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}
