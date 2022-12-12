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

internal class SetLegalHold
{
    // Enable Legal Hold
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string objectName = "my-object-name",
        string versionId = null)
    {
        try
        {
            Console.WriteLine("Running example for API: SetLegalHold, enable legal hold");
            // Setting WithLegalHold true, sets Legal hold status to ON.
            // Setting WithLegalHold false will set Legal hold status to OFF.
            var args = new SetObjectLegalHoldArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithVersionId(versionId)
                .WithLegalHold(true);
            await minio.SetObjectLegalHoldAsync(args);
            Console.WriteLine("Legal Hold status for " + bucketName + "/" + objectName +
                              (string.IsNullOrEmpty(versionId) ? " " : " with version id " + versionId + " ") +
                              " set to ON.");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Object]  Exception: {e}");
        }
    }
}