/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
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

public class GetBucketTags
{
    // Get Tags assigned to the bucket
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name")
    {
        try
        {
            Console.WriteLine("Running example for API: GetBucketTags");
            var tags = await minio.GetBucketTagsAsync(
                new GetBucketTagsArgs()
                    .WithBucket(bucketName)
            );
            if (tags != null && tags.GetTags() != null && tags.GetTags().Count > 0)
            {
                Console.WriteLine($"Got Bucket Tags set for bucket {bucketName}.");
                foreach (var tag in tags.GetTags()) Console.WriteLine(tag.Key + " : " + tag.Value);
                Console.WriteLine();
                return;
            }

            Console.WriteLine($"Bucket Tags not set for bucket {bucketName}.");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}