﻿/*
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

using Minio.DataModel.Args;

namespace Minio.Examples.Cases;

internal static class ListObjects
{
    // List objects matching optional prefix in a specified bucket.
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string prefix = null,
        bool recursive = true,
        bool versions = false)
    {
        try
        {
            Console.WriteLine("Running example for API: ListObjectsAsync");
            var listArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithPrefix(prefix)
                .WithRecursive(recursive)
                .WithVersions(versions);
            await foreach (var item in minio.ListObjectsEnumAsync(listArgs).ConfigureAwait(false))
                Console.WriteLine($"Object: {item.Key}");
            Console.WriteLine($"Listed all objects in bucket {bucketName}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}
