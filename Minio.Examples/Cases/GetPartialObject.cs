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

using System;
using System.Threading.Tasks;
using System.IO;

namespace Minio.Examples.Cases
{
    class GetPartialObject
    {
        // Get object in a bucket for a particular offset range. Dotnet SDK currently
        // requires both start offset and end 
        public async static Task Run(MinioClient minio,
                                     string bucketName="my-bucket-name",
                                     string objectName="my-object-name",
                                     string fileName="my-file-name")
        {
            try
            {
                Console.Out.WriteLine("Running example for API: GetObjectAsync");
                // Check whether the object exists using StatObjectAsync(). If the object is not found,
                // StatObjectAsync() will throw an exception.
                await minio.StatObjectAsync(bucketName, objectName);

                // Get object content starting at byte position 1024 and length of 4096
                await minio.GetObjectAsync(bucketName, objectName, 1024L, 4096L,
                (stream) =>
                {
                    var fileStream = File.Create(fileName);
                    stream.CopyTo(fileStream);
                    fileStream.Dispose();
                    FileInfo writtenInfo = new FileInfo(fileName);
                    long file_read_size = writtenInfo.Length;
                    // Uncomment to print the file on output console
                    // stream.CopyTo(Console.OpenStandardOutput());
                    Console.WriteLine("Successfully downloaded object with requested offset and length {0} into file", writtenInfo.Length);
                    stream.Dispose();
                });
                Console.Out.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
