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

using Minio.DataModel;
using Minio.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Minio.Examples
{
    public class Program
    {
        private static Random rnd = new Random();
        private static int UNIT_MB = 1024 * 1024;

        // Create a file of given size from random byte array
        private static string CreateFile(int size)
        {
            string fileName = GetRandomName();
            byte[] data = new byte[size];
            rnd.NextBytes(data);

            File.WriteAllBytes(fileName, data);

            return fileName;
        }

        // Generate a random string
        public static string GetRandomName()
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
            string endPoint = null;
            string accessKey = null;
            string secretKey = null;
            bool enableHTTPS = false;
            if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") != null)
            {
                endPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
                accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
                secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
                if (Environment.GetEnvironmentVariable("ENABLE_HTTPS") != null)
                    enableHTTPS = Environment.GetEnvironmentVariable("ENABLE_HTTPS").Equals("1");
            }
            else
            {
                endPoint = "play.min.io:9000";
                accessKey = "Q3AM3UQ867SPQQA43P2F";
                secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
                enableHTTPS = true;
            }
            ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, certificate, chain, sslPolicyErrors) => true;

            // WithSSL() enables SSL support in MinIO client
            MinioClient minioClient = null;
            if (enableHTTPS)
            {
                minioClient = new MinioClient(endPoint, accessKey, secretKey).WithSSL();
            }
            else
            {
                minioClient = new MinioClient(endPoint, accessKey, secretKey);
            }

            try
            {
                // Assign parameters before starting the test 
                string bucketName = GetRandomName();
                string smallFileName = CreateFile(1 * UNIT_MB);
                string bigFileName = CreateFile(6 * UNIT_MB);
                string objectName = GetRandomName();
                string destBucketName = GetRandomName();
                string destObjectName = GetRandomName();
                List<string> objectsList = new List<string>();
                for (int i = 0; i < 10; i++)
                {
                    objectsList.Add(objectName + i.ToString());
                }
                // Set app Info 
                minioClient.SetAppInfo("app-name", "app-version");

                // Set HTTP Tracing On
                // minioClient.SetTraceOn();

                // Set HTTP Tracing Off
                // minioClient.SetTraceOff();
                // Check if bucket exists
                Cases.BucketExists.Run(minioClient, bucketName).Wait();

                // Create a new bucket
                Cases.MakeBucket.Run(minioClient, bucketName).Wait();

                Cases.MakeBucket.Run(minioClient, destBucketName).Wait();


                // List all the buckets on the server
                Cases.ListBuckets.Run(minioClient).Wait();

                // Put an object to the new bucket
                Cases.PutObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

                // Get object metadata
                Cases.StatObject.Run(minioClient, bucketName, objectName).Wait();

                // List the objects in the new bucket
                Cases.ListObjects.Run(minioClient, bucketName);

                // Delete the file and Download the object as file
                Cases.GetObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

                // Delete the file and Download partial object as file
                Cases.GetPartialObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

                // Server side copyObject
                Cases.CopyObject.Run(minioClient, bucketName, objectName, destBucketName, objectName).Wait();

                // Server side copyObject with metadata replacement
                Cases.CopyObjectMetadata.Run(minioClient, bucketName, objectName, destBucketName, objectName).Wait();

                // Upload a File with PutObject
                Cases.FPutObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

                // Delete the file and Download the object as file
                Cases.FGetObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

                // Automatic Multipart Upload with object more than 5Mb
                Cases.PutObject.Run(minioClient, bucketName, objectName, bigFileName).Wait();

                // Specify SSE-C encryption options
                Aes aesEncryption = Aes.Create();
                aesEncryption.KeySize = 256;
                aesEncryption.GenerateKey();
                var ssec = new SSEC(aesEncryption.Key);
                // Specify SSE-C source side encryption for Copy operations
                var sseCpy = new SSECopy(aesEncryption.Key);

                // Uncommment to specify SSE-S3 encryption option
                // var sses3 = new SSES3();

                // Uncommment to specify SSE-KMS encryption option
                // var sseKms = new SSEKMS("kms-key",new Dictionary<string,string>{{ "kms-context", "somevalue"}});

                // Upload encrypted object
                Cases.PutObject.Run(minioClient, bucketName, objectName, smallFileName, sse: ssec).Wait();
                // Copy SSE-C encrypted object to unencrypted object
                Cases.CopyObject.Run(minioClient, bucketName, objectName, destBucketName, objectName, sseSrc: sseCpy, sseDest: ssec).Wait();
                // Download SSE-C encrypted object
                Cases.FGetObject.Run(minioClient, destBucketName, objectName, bigFileName, sse: ssec).Wait();

                // List the incomplete uploads
                Cases.ListIncompleteUploads.Run(minioClient, bucketName);

                // Remove all the incomplete uploads
                Cases.RemoveIncompleteUpload.Run(minioClient, bucketName, objectName).Wait();

                // Set a policy for given bucket
                Cases.SetBucketPolicy.Run(minioClient, bucketName).Wait();
                // Get the policy for given bucket
                Cases.GetBucketPolicy.Run(minioClient, bucketName).Wait();

                // Set bucket notifications
                Cases.SetBucketNotification.Run(minioClient, bucketName).Wait();

                // Get bucket notifications
                Cases.GetBucketNotification.Run(minioClient, bucketName).Wait();

                // Remove all bucket notifications
                Cases.RemoveAllBucketNotifications.Run(minioClient, bucketName).Wait();

                // Get the presigned url for a GET object request
                Cases.PresignedGetObject.Run(minioClient, bucketName, objectName).Wait();

                // Get the presigned POST policy curl url
                Cases.PresignedPostPolicy.Run(minioClient).Wait();

                // Get the presigned url for a PUT object request
                Cases.PresignedPutObject.Run(minioClient, bucketName, objectName).Wait();

                // Delete the list of objects
                Cases.RemoveObjects.Run(minioClient, bucketName, objectsList).Wait();

                // Delete the object
                Cases.RemoveObject.Run(minioClient, bucketName, objectName).Wait();

                // Delete the object
                Cases.RemoveObject.Run(minioClient, destBucketName, objectName).Wait();

                // Tacing request with custom logger
                Cases.CustomRequestLogger.Run(minioClient).Wait();

                // Remove the buckets
                Cases.RemoveBucket.Run(minioClient, bucketName).Wait();
                Cases.RemoveBucket.Run(minioClient, destBucketName).Wait();

                // Remove the binary files created for test
                File.Delete(smallFileName);
                File.Delete(bigFileName);

                Console.ReadLine();
            }
            catch (MinioException ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
        }
    }
}

