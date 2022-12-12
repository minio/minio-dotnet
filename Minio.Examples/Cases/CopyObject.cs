/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017-2021 MinIO, Inc.
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio.Examples.Cases;

internal class CopyObject
{
    // Copy object from one bucket to another
    public static async Task Run(IMinioClient minio,
        string fromBucketName = "from-bucket-name",
        string fromObjectName = "from-object-name",
        string destBucketName = "dest-bucket",
        string destObjectName = " to-object-name",
        ServerSideEncryption sseSrc = null,
        ServerSideEncryption sseDest = null)
    {
        try
        {
            Console.WriteLine("Running example for API: CopyObjectAsync");
            var metaData = new Dictionary<string, string>
            {
                { "Test-Metadata", "Test  Test" }
            };
            // Optionally pass copy conditions
            var cpSrcArgs = new CopySourceObjectArgs()
                .WithBucket(fromBucketName)
                .WithObject(fromObjectName)
                .WithServerSideEncryption(sseSrc);
            var args = new CopyObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithCopyObjectSource(cpSrcArgs)
                .WithServerSideEncryption(sseDest);
            await minio.CopyObjectAsync(args);
            Console.WriteLine("Copied object {0} from bucket {1} to bucket {2}", fromObjectName, fromBucketName,
                destBucketName);
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine("[Bucket]  Exception: {0}", e);
        }
    }
}