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
using System.Text;
using System.IO;
#if NET452
using System.Configuration;
#endif

namespace Minio.Examples
{
    class Program
    {
        private static Random rnd = new Random();
        private static int MB = 1024 * 1024;

        // Create a file of given size from random byte array
        private static String CreateFile(int size) 
        {
            String fileName = GetRandomName();
            byte[] data = new byte[size];
            rnd.NextBytes(data);

            File.WriteAllBytes(fileName, data);

            return fileName;
        }

        // Generate a random string
        public static String GetRandomName()
        {
            string characters = "0123456789abcdefghijklmnopqrstuvwxyz";
            StringBuilder result = new StringBuilder(5);
            for (int i = 0; i < 5; i++)
            {
                result.Append(characters[rnd.Next(characters.Length)]);
            }
            return "minio-dotnet-example-" + result.ToString();
        }

        public static void Main(string[] args)
        {
            String endPoint = null;
            String accessKey = null;
            String secretKey = null;

#if NET452
            endPoint = ConfigurationManager.AppSettings["Endpoint"];
            accessKey = ConfigurationManager.AppSettings["AccessKey"];
            secretKey = ConfigurationManager.AppSettings["SecretKey"];

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                     | SecurityProtocolType.Tls11
                                     | SecurityProtocolType.Tls12;
#endif
#if NETCOREAPP1_0
            endPoint = "play.minio.io:9000";
            accessKey = "Q3AM3UQ867SPQQA43P2F";
            secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
#endif
            var minioClient = new Minio.MinioClient(endPoint, accessKey, secretKey).WithSSL();

            try
            {
                // Assign parameters before starting the test 
                string bucketName = GetRandomName();
                string fileName = CreateFile(1 * MB);
                string objectName = GetRandomName();
                string destBucketName = GetRandomName();
                string destObjectName = GetRandomName();

                // Set app Info 
                minioClient.SetAppInfo("app-name", "app-version");

                // Set HTTP Tracing On
                //minioClient.SetTraceOn();

                // Set HTTP Tracing Off
                // minioClient.SetTraceOff();

                // Check if bucket exists
                Cases.BucketExists.Run(minioClient, bucketName).Wait();

                // Create a new bucket
                Cases.MakeBucket.Run(minioClient, bucketName).Wait();

                // List all the buckets on the server
                Cases.ListBuckets.Run(minioClient).Wait();

                // Put an object to the new bucket
                Cases.PutObject.Run(minioClient, bucketName, objectName, fileName).Wait();

                // List the objects in the new bucket
                Cases.ListObjects.Run(minioClient, bucketName);

                // Delete the file and Download the object as file
                File.Delete(fileName);
                Cases.GetObject.Run(minioClient, bucketName, objectName, fileName).Wait();
                
                //Cases.FPutObject.Run(minioClient, bucketName, objectName, uploadFilePath).Wait();

                //Cases.FGetObject.Run(minioClient, bucketName, objectName, downloadFilePath).Wait();

                //Cases.RemoveObject.Run(minioClient, bucketName, objectName).Wait();
                //Cases.RemoveBucket.Run(minioClient, bucketName).Wait();
                //Cases.ListIncompleteUploads.Run(minioClient, bucketName, prefix: objectPrefix);
                //Cases.RemoveIncompleteUpload.Run(minioClient, bucketName, removeObject).Wait();

                //Cases.GetBucketPolicy.Run(minioClient, bucketName).Wait();

                //Cases.SetBucketPolicy.Run(minioClient, bucketName).Wait();
                //Cases.StatObject.Run(minioClient, bucketName, objectName).Wait();
                //Cases.CopyObject.Run(minioClient, bucketName, objectName, destBucketName, destObjectName).Wait();

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
