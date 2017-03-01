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
using System.Net;
using Minio.Exceptions;
namespace Minio.Examples
{
    class Program
    {
        public static void Main(string[] args)
        {
            // * Uncomment to use Play minio server
            /*
            var minioClient = new Minio.MinioClient("play.minio.io:9000",
              "Q3AM3UQ867SPQQA43P2F",
              "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
              ).WithSSL();
             
            */
            /*
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                     | SecurityProtocolType.Tls11
                                     | SecurityProtocolType.Tls12;
            */
            
            var endPoint = Environment.GetEnvironmentVariable("AWS_ENDPOINT");
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

            
            var minioClient = new Minio.MinioClient(endPoint,
                                    accessKey: accessKey,
                                    secretKey: secretKey).WithSSL();
                                    
            try
            {
                // Change these parameters before running examples 
                string bucketName = "mount.shasta";
                string objectName = "goldengate.jpg";
                string objectPrefix = "gold";
                string smallFilePath = "C:\\Users\\vagrant\\Downloads\\hellotext";
                string uploadFilePath = "C:\\Users\\vagrant\\Downloads\\go1.7.4.windows-amd64.msi";
                string downloadFilePath = "C:\\Users\\vagrant\\Downloads\\downloaded-object";
                string destBucketName = "testminiopoli";
                string destObjectName = "goldengate_copy.jpg";
                string removeObject = "goldengate_pic";

                // Set app Info 
                minioClient.SetAppInfo("app-name", "app-version");

                // Set HTTP Tracing On
                //minioClient.SetTraceOn();

                // Set HTTP Tracing Off
                // minioClient.SetTraceOff();

                //* UNCOMMENT CASE TO RUN A TEST 

                Cases.BucketExists.Run(minioClient, bucketName).Wait();

                Cases.MakeBucket.Run(minioClient, bucketName).Wait();

                Cases.ListBuckets.Run(minioClient).Wait();
                //Cases.ListObjects.Run(minioClient, bucketName);
                Cases.PutObject.Run(minioClient, bucketName, objectName, smallFilePath).Wait();
                Cases.GetObject.Run(minioClient, bucketName, objectName).Wait();
                //Cases.FPutObject.Run(minioClient, bucketName, objectName, uploadFilePath).Wait();

                //Cases.FGetObject.Run(minioClient, bucketName, objectName, downloadFilePath).Wait();

                //Cases.RemoveObject.Run(minioClient, bucketName, objectName).Wait();
                //Cases.RemoveBucket.Run(minioClient, bucketName).Wait();
                //Cases.ListIncompleteUploads.Run(minioClient, bucketName, prefix: objectPrefix);
                //Cases.RemoveIncompleteUpload.Run(minioClient, bucketName, removeObject).Wait();

                //Cases.GetBucketPolicy.Run(minioClient, bucketName).Wait();

                Cases.SetBucketPolicy.Run(minioClient, bucketName).Wait();
                Cases.StatObject.Run(minioClient, bucketName, objectName).Wait();
                Cases.CopyObject.Run(minioClient, bucketName, objectName, destBucketName, destObjectName).Wait();

                //Cases.PresignedGetObject.Run(minioClient);
                //Cases.PresignedPostPolicy.Run(minioClient);
                //Cases.PresignedPutObject.Run(minioClient);
                Console.ReadLine();
            }
            catch (MinioException ex)
            {
                Console.Out.WriteLine(ex.Message);
            }

        }

    }
}
