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

namespace Minio.Functional.Tests;

internal class Program
{
    public static void Main(string[] args)
    {
        string endPoint = null;
        string accessKey = null;
        string secretKey = null;
        var isSecure = false;
        var kmsEnabled = "0";
        var port = 80;

        var useAWS = Environment.GetEnvironmentVariable("AWS_ENDPOINT") != null;
        if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") != null)
        {
            endPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
            var posColon = endPoint.LastIndexOf(':');
            if (posColon != -1)
            {
                port = int.Parse(endPoint.Substring(posColon + 1, endPoint.Length - posColon - 1));
                endPoint = endPoint.Substring(0, posColon);
            }

            accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
            secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
            if (Environment.GetEnvironmentVariable("ENABLE_HTTPS") != null)
            {
                isSecure = Environment.GetEnvironmentVariable("ENABLE_HTTPS").Equals("1");
                if (isSecure && port == 80) port = 443;
            }

            kmsEnabled = Environment.GetEnvironmentVariable("ENABLE_KMS");
        }
        else
        {
            endPoint = "play.min.io";
            accessKey = "Q3AM3UQ867SPQQA43P2F";
            secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            isSecure = true;
            port = 443;
            kmsEnabled = "1";
        }

        ServicePointManager.ServerCertificateValidationCallback +=
            (sender, certificate, chain, sslPolicyErrors) => true;

        MinioClient minioClient = null;
        minioClient = new MinioClient()
            .WithEndpoint(endPoint, port)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(isSecure)
            .Build();

        // Assign parameters before starting the test
        var bucketName = FunctionalTest.GetRandomName();
        var objectName = FunctionalTest.GetRandomName();
        var destBucketName = FunctionalTest.GetRandomName();
        var destObjectName = FunctionalTest.GetRandomName();

        // Set app Info
        minioClient.SetAppInfo("app-name", "app-version");
        // Set HTTP Tracing On
        // minioClient.SetTraceOn(new JsonNetLogger());

        // Set HTTP Tracing Off
        // minioClient.SetTraceOff();

        var runMode = Environment.GetEnvironmentVariable("MINT_MODE");

        if (!string.IsNullOrEmpty(runMode) && runMode == "core")
        {
            FunctionalTest.RunCoreTests(minioClient);
            Environment.Exit(0);
        }

        // Try catch as 'finally' section needs to run in the Functional Tests
        // Bucket notification is a minio specific feature.
        // If the following test is run against AWS, then the SDK throws
        // "Listening for bucket notification is specific only to `minio`
        // server endpoints".
        FunctionalTest.ListenBucketNotificationsAsync_Test1(minioClient).Wait();
        FunctionalTest.ListenBucketNotificationsAsync_Test2(minioClient).Wait();
        FunctionalTest.ListenBucketNotificationsAsync_Test3(minioClient).Wait();

        // Check if bucket exists
        FunctionalTest.BucketExists_Test(minioClient).Wait();
        FunctionalTest.MakeBucket_Test5(minioClient).Wait();

        if (useAWS)
        {
            FunctionalTest.MakeBucket_Test2(minioClient, useAWS).Wait();
            FunctionalTest.MakeBucket_Test3(minioClient, useAWS).Wait();
            FunctionalTest.MakeBucket_Test4(minioClient, useAWS).Wait();
        }

        // Test removal of bucket
        FunctionalTest.RemoveBucket_Test1(minioClient).Wait();
        FunctionalTest.RemoveBucket_Test2(minioClient).Wait();

        // Test ListBuckets function
        FunctionalTest.ListBuckets_Test(minioClient).Wait();

        // Test Putobject function
        FunctionalTest.PutObject_Test1(minioClient).Wait();
        FunctionalTest.PutObject_Test2(minioClient).Wait();
        FunctionalTest.PutObject_Test3(minioClient).Wait();
        FunctionalTest.PutObject_Test4(minioClient).Wait();
        FunctionalTest.PutObject_Test5(minioClient).Wait();
        FunctionalTest.PutObject_Test7(minioClient).Wait();
        FunctionalTest.PutObject_Test8(minioClient).Wait();

        // Test StatObject function
        FunctionalTest.StatObject_Test1(minioClient).Wait();

        // Test GetObjectAsync function
        FunctionalTest.GetObject_Test1(minioClient).Wait();
        FunctionalTest.GetObject_Test2(minioClient).Wait();
        // 3 tests will run to check different values of offset and length parameters
        // when GetObject api returns part of the object as defined by the offset
        // and length parameters. Tests will be reported as GetObject_Test3,
        // GetObject_Test4 and GetObject_Test5.
        FunctionalTest.GetObject_3_OffsetLength_Tests(minioClient).Wait();
        // Test async callback function to download an object
        FunctionalTest.GetObject_AsyncCallback_Test1(minioClient).Wait();

        // Test File GetObject and PutObject functions
        FunctionalTest.FGetObject_Test1(minioClient).Wait();
        FunctionalTest.FPutObject_Test2(minioClient).Wait();

        // Test SelectObjectContentAsync function
        FunctionalTest.SelectObjectContent_Test(minioClient).Wait();

        // Test ListObjectAsync function
        FunctionalTest.ListObjects_Test1(minioClient).Wait();
        FunctionalTest.ListObjects_Test2(minioClient).Wait();
        FunctionalTest.ListObjects_Test3(minioClient).Wait();
        FunctionalTest.ListObjects_Test4(minioClient).Wait();
        FunctionalTest.ListObjects_Test5(minioClient).Wait();
        FunctionalTest.ListObjects_Test6(minioClient).Wait();

        // Test RemoveObjectAsync function
        FunctionalTest.RemoveObject_Test1(minioClient).Wait();
        FunctionalTest.RemoveObjects_Test2(minioClient).Wait();
        FunctionalTest.RemoveObjects_Test3(minioClient).Wait();

        // Test CopyObjectAsync function
        FunctionalTest.CopyObject_Test1(minioClient).Wait();
        FunctionalTest.CopyObject_Test2(minioClient).Wait();
        FunctionalTest.CopyObject_Test3(minioClient).Wait();
        FunctionalTest.CopyObject_Test4(minioClient).Wait();
        FunctionalTest.CopyObject_Test5(minioClient).Wait();
        FunctionalTest.CopyObject_Test6(minioClient).Wait();
        FunctionalTest.CopyObject_Test7(minioClient).Wait();
        FunctionalTest.CopyObject_Test8(minioClient).Wait();

        // Test SetPolicyAsync function
        FunctionalTest.SetBucketPolicy_Test1(minioClient).Wait();

        // Test S3Zip function
        FunctionalTest.GetObjectS3Zip_Test1(minioClient).Wait();

        // Test Presigned Get/Put operations
        FunctionalTest.PresignedGetObject_Test1(minioClient).Wait();
        FunctionalTest.PresignedGetObject_Test2(minioClient).Wait();
        FunctionalTest.PresignedGetObject_Test3(minioClient).Wait();
        FunctionalTest.PresignedPutObject_Test1(minioClient).Wait();
        FunctionalTest.PresignedPutObject_Test2(minioClient).Wait();
        // FunctionalTest.PresignedPostPolicy_Test1(minioClient).Wait();

        // Test incomplete uploads
        FunctionalTest.ListIncompleteUpload_Test1(minioClient).Wait();
        FunctionalTest.ListIncompleteUpload_Test2(minioClient).Wait();
        FunctionalTest.ListIncompleteUpload_Test3(minioClient).Wait();
        FunctionalTest.RemoveIncompleteUpload_Test(minioClient).Wait();

        // Test GetBucket policy
        FunctionalTest.GetBucketPolicy_Test1(minioClient).Wait();

        // Test object versioning
        FunctionalTest.ObjectVersioningAsync_Test1(minioClient).Wait();

        // Test Object Lock Configuration
        FunctionalTest.ObjectLockConfigurationAsync_Test1(minioClient).Wait();

        // Test Bucket, Object Tags
        FunctionalTest.BucketTagsAsync_Test1(minioClient).Wait();
        FunctionalTest.ObjectTagsAsync_Test1(minioClient).Wait();

        // Test Bucket Lifecycle configuration
        FunctionalTest.BucketLifecycleAsync_Test1(minioClient).Wait();
        FunctionalTest.BucketLifecycleAsync_Test2(minioClient).Wait();

        // Test encryption
        if (isSecure)
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, sslPolicyErrors) => true;
            FunctionalTest.PutGetStatEncryptedObject_Test1(minioClient).Wait();
            FunctionalTest.PutGetStatEncryptedObject_Test2(minioClient).Wait();

            FunctionalTest.EncryptedCopyObject_Test1(minioClient).Wait();
            FunctionalTest.EncryptedCopyObject_Test2(minioClient).Wait();
        }

        if (kmsEnabled != null && kmsEnabled == "1")
        {
            FunctionalTest.PutGetStatEncryptedObject_Test3(minioClient).Wait();
            FunctionalTest.EncryptedCopyObject_Test3(minioClient).Wait();
            FunctionalTest.EncryptedCopyObject_Test4(minioClient).Wait();
        }
    }
}