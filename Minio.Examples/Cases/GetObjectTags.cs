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
    public class GetObjectTags
    {
        // Get Tags set for the object
        public async static Task Run(MinioClient minio,
                                    string bucketName = "my-bucket-name",
                                    string objectName = "my-object-name",
                                    string versionId = null)
        {
            try
            {
                Console.WriteLine("Running example for API: GetObjectTags");
                var tags = await minio.GetObjectTagsAsync(
                    new GetObjectTagsArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithVersionId(versionId)
                );
                if (tags != null && tags.GetTags() != null && tags.GetTags().Count > 0)
                {
                    Console.WriteLine($"Got tags set for object {bucketName}/{objectName}.");
                    foreach(var tag in tags.GetTags())
                    {
                        Console.WriteLine(tag.Key + " : " + tag.Value);
                    }
                    Console.WriteLine();
                    return;
                }
                Console.WriteLine($" Tags not set for object {bucketName}/{objectName}.");
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Object]  Exception: {e}");
            }
        }
    }
}