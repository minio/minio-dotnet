/*
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

using System;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio.Examples.Cases
{
    class GetObjectWithStat
    {
        // Get object in a bucket
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket-name",
                                     string objectName = "my-object-name",
                                     string fileName = "my-file-name")
        {
            try
            {
                Console.WriteLine("Running example for API: GetObjectAsync");
                GetObjectArgs args = new GetObjectArgs()
                                                .WithBucket(bucketName)
                                                .WithObject(objectName)
                                                .WithFile(fileName);
                ObjectStat stat = await minio.GetObjectWithStatAsync(args);
                Console.WriteLine($"Downloaded the file {fileName} in bucket {bucketName}");
                Console.WriteLine($"Details of object {objectName} in bucket {bucketName}\n" + stat);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}
