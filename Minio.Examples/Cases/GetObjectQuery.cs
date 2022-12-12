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

internal class GetObjectQuery
{
    // Get object in a bucket
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name",
        string versionId = "my-version-id",
        string fileName = "my-file-name",
        string matchEtag = null,
        DateTime modifiedSince = default)
    {
        try
        {
            var withVersionId = string.IsNullOrEmpty(versionId) ? "" : " with Version ID";
            Console.WriteLine("Running example for API: GetObjectAsync" + withVersionId);
            var args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithVersionId(versionId)
                .WithFile(fileName)
                .WithMatchETag(matchEtag)
                .WithModifiedSince(modifiedSince);
            await minio.GetObjectAsync(args);
            Console.WriteLine(
                $"Downloaded the file {fileName} for object {objectName} with given query parameters in bucket {bucketName}");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}