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

using CommunityToolkit.HighPerformance;
using Minio.DataModel;

namespace Minio.Examples.Cases;

internal static class PutObject
{
    private const int MB = 1024 * 1024;

    // Put an object from a local stream into bucket
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name",
        string fileName = "location-of-file",
        IProgress<ProgressReport> progress = null,
        IServerSideEncryption sse = null)
    {
        try
        {
            ReadOnlyMemory<byte> bs = await File.ReadAllBytesAsync(fileName).ConfigureAwait(false);
            Console.WriteLine("Running example for API: PutObjectAsync");
            using var filestream = bs.AsStream();

            var fileInfo = new FileInfo(fileName);
            var metaData = new Dictionary<string, string>
                (StringComparer.Ordinal)
                {
                    { "Test-Metadata", "Test  Test" }
                };
            var args = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(filestream)
                .WithObjectSize(filestream.Length)
                .WithContentType("application/octet-stream")
                .WithHeaders(metaData)
                .WithProgress(progress)
                .WithServerSideEncryption(sse);
            await minio.PutObjectAsync(args).ConfigureAwait(false);

            Console.WriteLine($"Uploaded object {objectName} to bucket {bucketName}");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}