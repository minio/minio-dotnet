/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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
using System.IO;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class PutObject
    {
        //Put an object from a local stream into bucket
        public async static Task Run(Minio.MinioClient minio,
                                     string bucketName = "my-bucket-name", 
                                     string objectName = "my-object-name",
                                     string fileName="location-of-file")
        {
            try
            {
                byte[] bs = File.ReadAllBytes(fileName);
                System.IO.MemoryStream filestream = new System.IO.MemoryStream(bs);

                await minio.PutObjectAsync(bucketName,
                                           objectName,
                                           filestream,
                                           filestream.Length,
                                           "application/octet-stream");

                Console.Out.WriteLine("done uploading");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
      
    }
}
