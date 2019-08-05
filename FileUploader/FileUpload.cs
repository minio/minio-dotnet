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

using Minio;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FileUploader
{
    /// <summary>
    /// This example creates a new bucket if it does not already exist, and uploads a file
    /// to the bucket.
    /// </summary>
    public class FileUpload
    {
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                                 | SecurityProtocolType.Tls11
                                                 | SecurityProtocolType.Tls12;
            var endpoint = "play.min.io";
            var accessKey = "Q3AM3UQ867SPQQA43P2F";
            var secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            try
            {
                var minio = new MinioClient(endpoint, accessKey, secretKey).WithSSL();
                Run(minio).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }

        /// <summary>
        /// Task that uploads a file to a bucket
        /// </summary>
        /// <param name="minio"></param>
        /// <returns></returns>
        private static async Task Run(MinioClient minio)
        {
            // Make a new bucket called mymusic.
            var bucketName = "mymusic-folder"; //<==== change this
            var location = "us-east-1";
            // Upload the zip file
            var objectName = "my-golden-oldies.mp3";
            var filePath = "C:\\Users\\vagrant\\Downloads\\golden_oldies.mp3";
            var contentType = "application/zip";

            try
            {
                bool found = await minio.BucketExistsAsync(bucketName);
                if (!found)
                {
                    await minio.MakeBucketAsync(bucketName, location);
                }
                await minio.PutObjectAsync(bucketName, objectName, filePath, contentType);
                Console.WriteLine("Successfully uploaded " + objectName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
