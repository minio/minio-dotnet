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
using Minio.DataModel.ObjectLock;

namespace Minio.Examples.Cases;

public class SetObjectRetention
{
    // Put Object Retention Configuration for the bucket
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name",
        string versionId = null,
        RetentionMode mode = RetentionMode.GOVERNANCE,
        DateTime retentionValidDate = default)
    {
        try
        {
            if (retentionValidDate.Equals(default))
                retentionValidDate = DateTime.Now.AddDays(1);
            Console.WriteLine("Running example for API: SetObjectRetention");
            await minio.SetObjectRetentionAsync(
                new SetObjectRetentionArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithVersionId(versionId)
                    .WithRetentionMode(mode)
                    .WithRetentionUntilDate(retentionValidDate)
            );
            var versionInfo = string.IsNullOrEmpty(versionId) ? "" : " Version ID: " + versionId;
            Console.WriteLine($"Assigned retention configuration to object {bucketName}/{objectName} " +
                              versionInfo +
                              " till date: " + retentionValidDate.ToShortDateString());
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Object]  Exception: {e}");
        }
    }
}