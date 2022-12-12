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

namespace Minio.Examples.Cases;

public class RemoveObjectTags
{
    // Remove Tags set for the object
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name",
        string versionId = null)
    {
        try
        {
            Console.WriteLine("Running example for API: RemoveObjectTags");
            await minio.RemoveObjectTagsAsync(
                new RemoveObjectTagsArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithVersionId(versionId)
            );
            Console.WriteLine($"Tags removed for object {bucketName}/{objectName}.");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Object]  Exception: {e}");
        }
    }
}