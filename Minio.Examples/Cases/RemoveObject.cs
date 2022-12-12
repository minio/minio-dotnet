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

namespace Minio.Examples.Cases;

internal class RemoveObject
{
    // Remove an object from a bucket
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name",
        string versionId = null)
    {
        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var versions = "";
            if (!string.IsNullOrEmpty(versionId))
            {
                args = args.WithVersionId(versionId);
                versions = ", with version ID " + versionId + " ";
            }

            Console.WriteLine("Running example for API: RemoveObjectAsync");
            await minio.RemoveObjectAsync(args);
            Console.WriteLine($"Removed object {objectName} from bucket {bucketName}{versions} successfully");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket-Object]  Exception: {e}");
        }
    }
}