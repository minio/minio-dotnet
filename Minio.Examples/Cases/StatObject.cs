﻿/*
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

using Minio.DataModel;
using System;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class StatObject
    {
        // Get stats on a object
        public async static Task Run(MinioClient minio, 
                                     string bucketName = "my-bucket-name",
                                     string bucketObject = "my-object-name")
        {
            try
            {
                Console.WriteLine("Running example for API: StatObjectAsync");
                ObjectStat statObject = await minio.StatObjectAsync(bucketName, bucketObject);
                Console.WriteLine($"Details of the object {bucketObject} are {statObject}");
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[StatObject] {bucketName}-{bucketObject} Exception: {e}");
            }
        }
    }
}
