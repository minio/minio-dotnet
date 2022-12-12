/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2021 MinIO, Inc.
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
using Minio.DataModel.Tags;

namespace Minio.Examples.Cases;

internal class CopyObjectReplaceTags
{
    // Copy object from one bucket to another, replace tags in the copied object
    public static async Task Run(IMinioClient minio,
        string fromBucketName = "from-bucket-name",
        string fromObjectName = "from-object-name",
        string destBucketName = "dest-bucket",
        string destObjectName = " to-object-name")
    {
        try
        {
            Console.WriteLine("Running example for API: CopyObjectAsync with Tags");
            var tags = new Dictionary<string, string>
            {
                { "Test-TagKey", "Test-TagValue" }
            };
            var tagObj = Tagging.GetObjectTags(tags);
            var cpSrcArgs = new CopySourceObjectArgs()
                .WithBucket(fromBucketName)
                .WithObject(fromObjectName);
            var args = new CopyObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithTagging(tagObj)
                .WithReplaceTagsDirective(true)
                .WithCopyObjectSource(cpSrcArgs);
            await minio.CopyObjectAsync(args).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine("[Bucket]  Exception: {0}", e);
        }
    }
}