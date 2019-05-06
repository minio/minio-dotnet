/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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
    class CopyObject
    {
        // Copy object from one bucket to another
        public async static Task Run(Minio.MinioClient minio,
                                     string fromBucketName = "from-bucket-name",
                                     string fromObjectName = "from-object-name",
                                     string destBucketName = "dest-bucket",
                                     string destObjectName =" to-object-name",
                                     ServerSideEncryption sseSrc = null,
                                     ServerSideEncryption sseDest = null)
        {
            try
            {
                Console.Out.WriteLine("Running example for API: CopyObjectAsync");
                // Optionally pass copy conditions
                await minio.CopyObjectAsync(fromBucketName, 
                                                fromObjectName, 
                                                destBucketName, 
                                                destObjectName, 
                                                copyConditions:null,
                                                sseSrc: sseSrc,
                                                sseDest: sseDest);
                Console.Out.WriteLine("Copied object {0} from bucket {1} to bucket {2}", fromObjectName, fromBucketName, destBucketName);
                Console.Out.WriteLine();    
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }

    }
}