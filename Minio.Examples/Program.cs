/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017, 2020 MinIO, Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

using Minio.DataModel;
using Minio.Exceptions;
using Minio.DataModel.ObjectLock;

namespace Minio.Examples
{
    public class Program
    {
        private static Random rnd = new Random();
        private const int UNIT_MB = 1024 * 1024;

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
            var characters = "0123456789abcdefghijklmnopqrstuvwxyz";
            var result = new StringBuilder(5);
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
            int port = 80;

            if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") != null)
            {
                endPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
                int posColon = endPoint.LastIndexOf(':');
                if (posColon != -1)
                {
                    port = Int32.Parse(endPoint.Substring(posColon + 1, (endPoint.Length - posColon - 1)));
                    endPoint = endPoint.Substring(0, posColon);
                }
                accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
                secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
                if (Environment.GetEnvironmentVariable("ENABLE_HTTPS") != null)
                {
                    enableHTTPS = Environment.GetEnvironmentVariable("ENABLE_HTTPS").Equals("1");
                    if (enableHTTPS && port == 80)
                    {
                        port = 443;
                    }
                }
            }
            else
            {
                endPoint = "play.min.io";
                accessKey = "Q3AM3UQ867SPQQA43P2F";
                secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
                enableHTTPS = true;
                port = 443;
            }

            ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, certificate, chain, sslPolicyErrors) => true;

            // WithSSL() enables SSL support in MinIO client
            MinioClient minioClient = null;
            if (enableHTTPS)
            {
                minioClient = new MinioClient()
                                        .WithEndpoint(endPoint, port)
                                        .WithCredentials(accessKey, secretKey)
                                        .WithSSL()
                                        .Build();
            }
            else
            {
                minioClient = new MinioClient()
                                        .WithEndpoint(endPoint, port)
                                        .WithCredentials(accessKey, secretKey)
                                        .Build();
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
                string lockBucketName = GetRandomName();
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

                // Bucket with Lock tests
                Cases.MakeBucketWithLock.Run(minioClient, lockBucketName).Wait();
                Cases.BucketExists.Run(minioClient, lockBucketName).Wait();
                Cases.RemoveBucket.Run(minioClient, lockBucketName).Wait();

                // Versioning tests
                Cases.GetVersioning.Run(minioClient, bucketName).Wait();
                Cases.EnableSuspendVersioning.Run(minioClient, bucketName).Wait();
                Cases.GetVersioning.Run(minioClient, bucketName).Wait();
                // List all the buckets on the server
                Cases.ListBuckets.Run(minioClient).Wait();

                // Start listening for bucket notifications
                Cases.ListenBucketNotifications.Run(minioClient, bucketName, new List<EventType> { EventType.ObjectCreatedAll });

                // Put an object to the new bucket
                Cases.PutObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

                // Get object metadata
                Cases.StatObject.Run(minioClient, bucketName, objectName).Wait();

                // List the objects in the new bucket
                Cases.ListObjects.Run(minioClient, bucketName);

                // Get the file and Download the object as file
                Cases.GetObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();
                // Select content from object
                Cases.SelectObjectContent.Run(minioClient, bucketName, objectName).Wait();
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
                Console.WriteLine("Specifying SSE-C encryption options");
                var aesEncryption = Aes.Create();
                aesEncryption.KeySize = 256;
                aesEncryption.GenerateKey();

                var ssec = new SSEC(aesEncryption.Key);
                // Specify SSE-C source side encryption for Copy operations
                var sseCpy = new SSECopy(aesEncryption.Key);

                // Uncomment to specify SSE-S3 encryption option
                var sses3 = new SSES3();

                // Uncomment to specify SSE-KMS encryption option
                var sseKms = new SSEKMS("kms-key", new Dictionary<string, string> { { "kms-context", "somevalue" } });

                // Upload encrypted object
                Console.WriteLine("Running PutObject");
                string putFileName1 = CreateFile(1 * UNIT_MB);
                Cases.PutObject.Run(minioClient, bucketName, objectName, putFileName1, sse: ssec).Wait();
                // Copy SSE-C encrypted object to unencrypted object
                Console.WriteLine("Running CopyObject");
                Cases.CopyObject.Run(minioClient, bucketName, objectName, destBucketName, objectName, sseSrc: sseCpy, sseDest: ssec).Wait();
                // Download SSE-C encrypted object
                Console.WriteLine("Running FGetObject");
                Cases.FGetObject.Run(minioClient, destBucketName, objectName, bigFileName, sse: ssec).Wait();

                // List the incomplete uploads
                Console.WriteLine("Running ListIncompleteUploads");
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

                // Object Lock Configuration operations
                lockBucketName = GetRandomName();
                Cases.MakeBucketWithLock.Run(minioClient, lockBucketName).Wait();
                ObjectLockConfiguration configuration = new ObjectLockConfiguration(RetentionMode.GOVERNANCE, 35);
                Cases.SetObjectLockConfiguration.Run(minioClient, lockBucketName, configuration).Wait();
                Cases.GetObjectLockConfiguration.Run(minioClient, lockBucketName).Wait();
                Cases.RemoveObjectLockConfiguration.Run(minioClient, lockBucketName).Wait();
                Cases.RemoveBucket.Run(minioClient, lockBucketName).Wait();

                // Bucket Replication operations
                Cases.RemoveBucketReplication.Run(minioClient, bucketName).Wait();
                Cases.GetBucketReplication.Run(minioClient, bucketName).Wait();

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

                // Retry on failure
                Cases.RetryPolicyObject.Run(minioClient, destBucketName, objectName).Wait();

                // Tracing request with custom logger
                Cases.CustomRequestLogger.Run(minioClient).Wait();

                // Remove the buckets
                Console.WriteLine();
                Cases.RemoveBucket.Run(minioClient, bucketName).Wait();
                Cases.RemoveBucket.Run(minioClient, destBucketName).Wait();

                // Remove the binary files created for test
                File.Delete(smallFileName);
                File.Delete(bigFileName);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.ReadLine();
                }
            }
            catch (MinioException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}