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
using Minio.DataModel.ObjectLock;

namespace Minio.Examples.Cases;

public class SetObjectLockConfiguration
{
    // Set Object Lock Configuration on the bucket
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        ObjectLockConfiguration config = null)
    {
        try
        {
            Console.WriteLine("Running example for API: SetObjectLockConfiguration");
            await minio.SetObjectLockConfigurationAsync(
                new SetObjectLockConfigurationArgs()
                    .WithBucket(bucketName)
                    .WithLockConfiguration(config)
            );
            Console.WriteLine($"Set object lock configuration on bucket {bucketName}");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}