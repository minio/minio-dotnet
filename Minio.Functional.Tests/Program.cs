/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
* (C) 2019, 2020 MinIO, Inc.
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

namespace Minio.Functional.Tests
{
    class Program
    {
        public static void Main(string[] args)
        {
            string endPoint = null;
            string accessKey = null;
            string secretKey = null;
            string enableHttps = "0";
            string kmsEnabled = "0";

            if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") != null)
            {
                endPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
                accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
                secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
                enableHttps = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
                kmsEnabled = Environment.GetEnvironmentVariable("ENABLE_KMS");
            }
            else
            {
                endPoint = "play.min.io";
                accessKey = "Q3AM3UQ867SPQQA43P2F";
                secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
                enableHttps = "1";
                kmsEnabled = "1";
            }

            MinioClient minioClient = null;
            if (enableHttps == "1")
                // WithSSL() enables SSL support in MinIO client
                minioClient = new MinioClient()
                                        .WithSSL()
                                        .WithCredentials(accessKey, secretKey)
                                        .WithEndpoint(endPoint)
                                        .Build();
            else
                minioClient = new MinioClient()
                                        .WithCredentials(accessKey, secretKey)
                                        .WithEndpoint(endPoint)
                                        .Build();

            // Assign parameters before starting the test
            string bucketName = FunctionalTest.GetRandomName();
            string objectName = FunctionalTest.GetRandomName();
            string destBucketName = FunctionalTest.GetRandomName();
            string destObjectName = FunctionalTest.GetRandomName();

            // Set app Info
            minioClient.SetAppInfo("app-name", "app-version");
            // Set HTTP Tracing On
            // minioClient.SetTraceOn(new JsonNetLogger());

            // Set HTTP Tracing Off
            // minioClient.SetTraceOff();

            string runMode = Environment.GetEnvironmentVariable("MINT_MODE");

            if (!string.IsNullOrEmpty(runMode) && runMode == "core")
            {
                FunctionalTest.RunCoreTests(minioClient);
                Environment.Exit(0);
            }

            // Try catch as 'finally' section needs to run in the Functional Tests
            try
            {
                // FunctionalTest.ListenBucketNotificationsAsync_Test1(minioClient).Wait();

                // // Check if bucket exists
                // FunctionalTest.BucketExists_Test(minioClient).Wait();

                // // Create a new bucket
                // FunctionalTest.MakeBucket_Test1(minioClient).Wait();
                // FunctionalTest.MakeBucket_Test5(minioClient).Wait();

                // if (useAWS)
                // {
                //     FunctionalTest.MakeBucket_Test2(minioClient, useAWS).Wait();
                //     FunctionalTest.MakeBucket_Test3(minioClient, useAWS).Wait();
                //     FunctionalTest.MakeBucket_Test4(minioClient, useAWS).Wait();
                // }

                // // Test removal of bucket
                // FunctionalTest.RemoveBucket_Test1(minioClient).Wait();

                // // Test ListBuckets function
                // FunctionalTest.ListBuckets_Test(minioClient).Wait();

                // // Test Putobject function
                // FunctionalTest.PutObject_Test1(minioClient).Wait();
                // FunctionalTest.PutObject_Test2(minioClient).Wait();
                // FunctionalTest.PutObject_Test3(minioClient).Wait();
                // FunctionalTest.PutObject_Test4(minioClient).Wait();
                // FunctionalTest.PutObject_Test5(minioClient).Wait();
                // FunctionalTest.PutObject_Test7(minioClient).Wait();
                // FunctionalTest.PutObject_Test8(minioClient).Wait();

                // // Test StatObject function
                // FunctionalTest.StatObject_Test1(minioClient).Wait();

                // // Test GetObjectAsync function
                // FunctionalTest.GetObject_Test1(minioClient).Wait();
                // FunctionalTest.GetObject_Test2(minioClient).Wait();
                // FunctionalTest.GetObject_Test3(minioClient).Wait();

                // // Test File GetObject and PutObject functions
                // FunctionalTest.FGetObject_Test1(minioClient).Wait();
                // // FIX => FPutObject_Test1(minioClient).Wait();
                // FunctionalTest.FPutObject_Test2(minioClient).Wait();

                // // Test SelectObjectContentAsync function
                // FunctionalTest.SelectObjectContent_Test(minioClient).Wait();

                // // Test ListObjectAsync function
                // FunctionalTest.ListObjects_Test1(minioClient).Wait();
                // FunctionalTest.ListObjects_Test2(minioClient).Wait();
                // FunctionalTest.ListObjects_Test3(minioClient).Wait();
                // FunctionalTest.ListObjects_Test4(minioClient).Wait();
                // FunctionalTest.ListObjects_Test5(minioClient).Wait();
                // FunctionalTest.ListObjects_Test6(minioClient).Wait();

                // // Test RemoveObjectAsync function
                // FunctionalTest.RemoveObject_Test1(minioClient).Wait();
                // FunctionalTest.RemoveObjects_Test2(minioClient).Wait();
                // FunctionalTest.RemoveObjects_Test3(minioClient).Wait();

                // // Test CopyObjectAsync function
                // FunctionalTest.CopyObject_Test1(minioClient).Wait();
                // FunctionalTest.CopyObject_Test2(minioClient).Wait();
                // FunctionalTest.CopyObject_Test3(minioClient).Wait();
                // FunctionalTest.CopyObject_Test4(minioClient).Wait();
                // FunctionalTest.CopyObject_Test5(minioClient).Wait();
                // FunctionalTest.CopyObject_Test6(minioClient).Wait();
                // FunctionalTest.CopyObject_Test7(minioClient).Wait();
                FunctionalTest.CopyObject_Test8(minioClient).Wait();

                // // Test SetPolicyAsync function
                // FunctionalTest.SetBucketPolicy_Test1(minioClient).Wait();

                // // Test Presigned Get/Put operations
                // FunctionalTest.PresignedGetObject_Test1(minioClient).Wait();
                // FunctionalTest.PresignedGetObject_Test2(minioClient).Wait();
                // FunctionalTest.PresignedGetObject_Test3(minioClient).Wait();
                // FunctionalTest.PresignedPutObject_Test1(minioClient).Wait();
                // FunctionalTest.PresignedPutObject_Test2(minioClient).Wait();

                // // Test incomplete uploads
                // FunctionalTest.ListIncompleteUpload_Test1(minioClient).Wait();
                // FunctionalTest.ListIncompleteUpload_Test2(minioClient).Wait();
                // FunctionalTest.ListIncompleteUpload_Test3(minioClient).Wait();
                // FunctionalTest.RemoveIncompleteUpload_Test(minioClient).Wait();

                // // Test GetBucket policy
                // FunctionalTest.GetBucketPolicy_Test1(minioClient).Wait();

                // // Test Object Lock Configuration
                // FunctionalTest.ObjectLockConfigurationAsync_Test1(minioClient).Wait();

                // // Test Bucket, Object Tags
                // FunctionalTest.BucketTagsAsync_Test1(minioClient).Wait();
                // FunctionalTest.ObjectTagsAsync_Test1(minioClient).Wait();

                // // Test Bucket Lifecycle configuration
                // FunctionalTest.BucketLifecycleAsync_Test1(minioClient).Wait();

                // Test encryption
                // if (enableHttps == "1")
                // {
                //     ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                //     FunctionalTest.PutGetStatEncryptedObject_Test1(minioClient).Wait();
                //     FunctionalTest.PutGetStatEncryptedObject_Test2(minioClient).Wait();

                //     FunctionalTest.EncryptedCopyObject_Test1(minioClient).Wait();
                //     FunctionalTest.EncryptedCopyObject_Test2(minioClient).Wait();
                // }

                // if (kmsEnabled != null && kmsEnabled == "1")
                // {
                //     FunctionalTest.PutGetStatEncryptedObject_Test3(minioClient).Wait();
                //     FunctionalTest.EncryptedCopyObject_Test3(minioClient).Wait();
                //     FunctionalTest.EncryptedCopyObject_Test4(minioClient).Wait();
                // }
            }
            catch (System.Exception)
            {
                throw;
            }
        }
    }
}
