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

using Minio.DataModel.Args;

namespace Minio.Examples.Cases;

internal static class ListIncompleteUploads
{
    // List incomplete uploads on the bucket matching specified prefix
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string prefix = "my-object-name",
        bool recursive = true)
    {
        try
        {
            Console.WriteLine("Running example for API: ListIncompleteUploads");

            try
            {
                var args = new ListIncompleteUploadsArgs()
                    .WithBucket(bucketName)
                    .WithPrefix(prefix)
                    .WithRecursive(recursive);
                await foreach (var item in minio.ListIncompleteUploads(args).ConfigureAwait(false))
                    Console.WriteLine($"OnNext: {item.Key}");
            }
            catch (Exception exc)
            {
                Console.WriteLine($"OnError: {exc.Message}");
            }

            Console.WriteLine($"Listed the pending uploads to bucket {bucketName}");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e}");
        }
    }
}
