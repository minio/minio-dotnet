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

using System.Net;

namespace Minio.Functional.Tests;

internal static class Program
{
    public static async Task Main(string[] args)
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
                isSecure = Environment.GetEnvironmentVariable("ENABLE_HTTPS")
                    .Equals("1", StringComparison.OrdinalIgnoreCase);
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

#pragma warning disable MA0039 // Do not write your own certificate validation method
        ServicePointManager.ServerCertificateValidationCallback +=
            (sender, certificate, chain, sslPolicyErrors) => true;
#pragma warning restore MA0039 // Do not write your own certificate validation method

        using var minioClient = new MinioClient()
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
        await FunctionalTest.ListenBucketNotificationsAsync_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.ListenBucketNotificationsAsync_Test2(minioClient).ConfigureAwait(false);
        await FunctionalTest.ListenBucketNotificationsAsync_Test3(minioClient).ConfigureAwait(false);

        // Check if bucket exists
        await FunctionalTest.BucketExists_Test(minioClient).ConfigureAwait(false);
        await FunctionalTest.MakeBucket_Test5(minioClient).ConfigureAwait(false);

        if (useAWS)
        {
            await FunctionalTest.MakeBucket_Test2(minioClient, useAWS).ConfigureAwait(false);
            await FunctionalTest.MakeBucket_Test3(minioClient, useAWS).ConfigureAwait(false);
            await FunctionalTest.MakeBucket_Test4(minioClient, useAWS).ConfigureAwait(false);
        }

        // Test removal of bucket
        await FunctionalTest.RemoveBucket_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.RemoveBucket_Test2(minioClient).ConfigureAwait(false);

        // Test ListBuckets function
        await FunctionalTest.ListBuckets_Test(minioClient).ConfigureAwait(false);

        // Test Putobject function
        await FunctionalTest.PutObject_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.PutObject_Test2(minioClient).ConfigureAwait(false);
        await FunctionalTest.PutObject_Test3(minioClient).ConfigureAwait(false);
        await FunctionalTest.PutObject_Test4(minioClient).ConfigureAwait(false);
        await FunctionalTest.PutObject_Test5(minioClient).ConfigureAwait(false);
        await FunctionalTest.PutObject_Test7(minioClient).ConfigureAwait(false);
        await FunctionalTest.PutObject_Test8(minioClient).ConfigureAwait(false);

        // Test StatObject function
        await FunctionalTest.StatObject_Test1(minioClient).ConfigureAwait(false);

        // Test GetObjectAsync function
        await FunctionalTest.GetObject_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.GetObject_Test2(minioClient).ConfigureAwait(false);
        // 3 tests will run to check different values of offset and length parameters
        // when GetObject api returns part of the object as defined by the offset
        // and length parameters. Tests will be reported as GetObject_Test3,
        // GetObject_Test4 and GetObject_Test5.
        await FunctionalTest.GetObject_3_OffsetLength_Tests(minioClient).ConfigureAwait(false);

#if NET6_0_OR_GREATER
        // Test async callback function to download an object
        await FunctionalTest.GetObject_AsyncCallback_Test1(minioClient).ConfigureAwait(false);
#endif

        // Test File GetObject and PutObject functions
        await FunctionalTest.FGetObject_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.FPutObject_Test2(minioClient).ConfigureAwait(false);

        // Test SelectObjectContentAsync function
        await FunctionalTest.SelectObjectContent_Test(minioClient).ConfigureAwait(false);

        // Test ListObjectAsync function
        await FunctionalTest.ListObjects_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.ListObjects_Test2(minioClient).ConfigureAwait(false);
        await FunctionalTest.ListObjects_Test3(minioClient).ConfigureAwait(false);
        await FunctionalTest.ListObjects_Test4(minioClient).ConfigureAwait(false);
        await FunctionalTest.ListObjects_Test5(minioClient).ConfigureAwait(false);
        await FunctionalTest.ListObjects_Test6(minioClient).ConfigureAwait(false);

        // Test RemoveObjectAsync function
        await FunctionalTest.RemoveObject_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.RemoveObjects_Test2(minioClient).ConfigureAwait(false);
        await FunctionalTest.RemoveObjects_Test3(minioClient).ConfigureAwait(false);

        // Test CopyObjectAsync function
        await FunctionalTest.CopyObject_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.CopyObject_Test2(minioClient).ConfigureAwait(false);
        await FunctionalTest.CopyObject_Test3(minioClient).ConfigureAwait(false);
        await FunctionalTest.CopyObject_Test4(minioClient).ConfigureAwait(false);
        await FunctionalTest.CopyObject_Test5(minioClient).ConfigureAwait(false);
        await FunctionalTest.CopyObject_Test6(minioClient).ConfigureAwait(false);
        await FunctionalTest.CopyObject_Test7(minioClient).ConfigureAwait(false);
        await FunctionalTest.CopyObject_Test8(minioClient).ConfigureAwait(false);

        // Test SetPolicyAsync function
        await FunctionalTest.SetBucketPolicy_Test1(minioClient).ConfigureAwait(false);

        // Test S3Zip function
        await FunctionalTest.GetObjectS3Zip_Test1(minioClient).ConfigureAwait(false);

        // Test Presigned Get/Put operations
        await FunctionalTest.PresignedGetObject_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.PresignedGetObject_Test2(minioClient).ConfigureAwait(false);
        await FunctionalTest.PresignedGetObject_Test3(minioClient).ConfigureAwait(false);
        await FunctionalTest.PresignedPutObject_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.PresignedPutObject_Test2(minioClient).ConfigureAwait(false);
        // FunctionalTest.PresignedPostPolicy_Test1(minioClient).Wait();

        // Test incomplete uploads
        await FunctionalTest.ListIncompleteUpload_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.ListIncompleteUpload_Test2(minioClient).ConfigureAwait(false);
        await FunctionalTest.ListIncompleteUpload_Test3(minioClient).ConfigureAwait(false);
        await FunctionalTest.RemoveIncompleteUpload_Test(minioClient).ConfigureAwait(false);

        // Test GetBucket policy
        await FunctionalTest.GetBucketPolicy_Test1(minioClient).ConfigureAwait(false);

        // Test object versioning
        await FunctionalTest.ObjectVersioningAsync_Test1(minioClient).ConfigureAwait(false);

        // Test Object Lock Configuration
        await FunctionalTest.ObjectLockConfigurationAsync_Test1(minioClient).ConfigureAwait(false);

        // Test Bucket, Object Tags
        await FunctionalTest.BucketTagsAsync_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.ObjectTagsAsync_Test1(minioClient).ConfigureAwait(false);

        // Test Bucket Lifecycle configuration
        await FunctionalTest.BucketLifecycleAsync_Test1(minioClient).ConfigureAwait(false);
        await FunctionalTest.BucketLifecycleAsync_Test2(minioClient).ConfigureAwait(false);

        // Test encryption
        if (isSecure)
        {
#pragma warning disable MA0039 // Do not write your own certificate validation method
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, sslPolicyErrors) => true;
#pragma warning restore MA0039 // Do not write your own certificate validation method

            await FunctionalTest.PutGetStatEncryptedObject_Test1(minioClient).ConfigureAwait(false);
            await FunctionalTest.PutGetStatEncryptedObject_Test2(minioClient).ConfigureAwait(false);

            await FunctionalTest.EncryptedCopyObject_Test1(minioClient).ConfigureAwait(false);
            await FunctionalTest.EncryptedCopyObject_Test2(minioClient).ConfigureAwait(false);
        }

        if (kmsEnabled != null && kmsEnabled == "1")
        {
            await FunctionalTest.PutGetStatEncryptedObject_Test3(minioClient).ConfigureAwait(false);
            await FunctionalTest.EncryptedCopyObject_Test3(minioClient).ConfigureAwait(false);
            await FunctionalTest.EncryptedCopyObject_Test4(minioClient).ConfigureAwait(false);
        }
    }
}