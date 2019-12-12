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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class PutObject
    {
        private const int MB = 1024 * 1024;

        // Put an object from a local stream into bucket
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket-name", 
                                     string objectName = "my-object-name",
                                     string fileName = "location-of-file",
                                     ServerSideEncryption sse = null)
        {
            try
            {
                byte[] bs = File.ReadAllBytes(fileName);
                using (MemoryStream filestream = new MemoryStream(bs))
                {
                    if (filestream.Length < (5 * MB))
                    {
                        Console.WriteLine("Running example for API: PutObjectAsync with Stream");
                    }
                    else
                    {
                        Console.WriteLine("Running example for API: PutObjectAsync with Stream and MultiPartUpload");
                    }

                    var metaData = new Dictionary<string, string>
                    {
                        { "Test-Metadata", "Test  Test" }
                    };

                    await minio.PutObjectAsync(bucketName,
                                               objectName,
                                               filestream,
                                               filestream.Length,
                                               "application/octet-stream",
                                               metaData: metaData,
                                               sse: sse);
                }
            
                Console.WriteLine($"Uploaded object {objectName} to bucket {bucketName}");
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}
