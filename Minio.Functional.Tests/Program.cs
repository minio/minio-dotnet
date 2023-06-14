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

using System.Collections.Concurrent;
using System.Globalization;
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

        var useAWS = Environment.GetEnvironmentVariable("AWS_ENDPOINT") is not null;
        if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") is not null)
        {
            endPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
            var posColon = endPoint.LastIndexOf(':');
            if (posColon != -1)
            {
                port = int.Parse(endPoint.Substring(posColon + 1, endPoint.Length - posColon - 1), NumberStyles.Integer, CultureInfo.InvariantCulture);
                endPoint = endPoint.Substring(0, posColon);
            }

            accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
            secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
            if (Environment.GetEnvironmentVariable("ENABLE_HTTPS") is not null)
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

        // Print Minio version in use
        // var version = typeof(MinioClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        // Console.WriteLine($"\n  Minio package version is {version.Substring(0, version.IndexOf('+'))}\n");

        var runMode = Environment.GetEnvironmentVariable("MINT_MODE");

        if (!string.IsNullOrEmpty(runMode) && string.Equals(runMode, "core", StringComparison.OrdinalIgnoreCase))
        {
            await FunctionalTest.RunCoreTests(minioClient).ConfigureAwait(false);
            Environment.Exit(0);
        }

        ConcurrentBag<Task> functionalTestTasks = new();

        // Try catch as 'finally' section needs to run in the Functional Tests
        // Bucket notification is a minio specific feature.
        // If the following test is run against AWS, then the SDK throws
        // "Listening for bucket notification is specific only to `minio`
        // server endpoints".
        functionalTestTasks.Add(FunctionalTest.ListenBucketNotificationsAsync_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.ListenBucketNotificationsAsync_Test2(minioClient));
        functionalTestTasks.Add(FunctionalTest.ListenBucketNotificationsAsync_Test3(minioClient));

        // Check if bucket exists
        functionalTestTasks.Add(FunctionalTest.BucketExists_Test(minioClient));
        functionalTestTasks.Add(FunctionalTest.MakeBucket_Test5(minioClient));

        if (useAWS)
        {
            functionalTestTasks.Add(FunctionalTest.MakeBucket_Test2(minioClient, useAWS));
            functionalTestTasks.Add(FunctionalTest.MakeBucket_Test3(minioClient, useAWS));
            functionalTestTasks.Add(FunctionalTest.MakeBucket_Test4(minioClient, useAWS));
        }

        // Test removal of bucket
        functionalTestTasks.Add(FunctionalTest.RemoveBucket_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.RemoveBucket_Test2(minioClient));

        // Test ListBuckets function
        functionalTestTasks.Add(FunctionalTest.ListBuckets_Test(minioClient));

        // Test Putobject function
        functionalTestTasks.Add(FunctionalTest.PutObject_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.PutObject_Test2(minioClient));
        functionalTestTasks.Add(FunctionalTest.PutObject_Test3(minioClient));
        functionalTestTasks.Add(FunctionalTest.PutObject_Test4(minioClient));
        functionalTestTasks.Add(FunctionalTest.PutObject_Test5(minioClient));
        functionalTestTasks.Add(FunctionalTest.PutObject_Test7(minioClient));
        functionalTestTasks.Add(FunctionalTest.PutObject_Test8(minioClient));
        functionalTestTasks.Add(FunctionalTest.PutObject_Test9(minioClient));
        functionalTestTasks.Add(FunctionalTest.PutObject_Test10(minioClient));

        // Test StatObject function
        functionalTestTasks.Add(FunctionalTest.StatObject_Test1(minioClient));

        // Test GetObjectAsync function
        functionalTestTasks.Add(FunctionalTest.GetObject_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.GetObject_Test2(minioClient));
        // 3 tests will run to check different values of offset and length parameters
        // when GetObject api returns part of the object as defined by the offset
        // and length parameters. Tests will be reported as GetObject_Test3,
        // GetObject_Test4 and GetObject_Test5.
        functionalTestTasks.Add(FunctionalTest.GetObject_3_OffsetLength_Tests(minioClient));

#if NET6_0_OR_GREATER
        // Test async callback function to download an object
        functionalTestTasks.Add(FunctionalTest.GetObject_AsyncCallback_Test1(minioClient));
#endif

        // Test File GetObject and PutObject functions
        functionalTestTasks.Add(FunctionalTest.FGetObject_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.FPutObject_Test2(minioClient));

        // Test SelectObjectContentAsync function
        functionalTestTasks.Add(FunctionalTest.SelectObjectContent_Test(minioClient));

        // Test ListObjectAsync function
        functionalTestTasks.Add(FunctionalTest.ListObjects_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.ListObjects_Test2(minioClient));
        functionalTestTasks.Add(FunctionalTest.ListObjects_Test3(minioClient));
        functionalTestTasks.Add(FunctionalTest.ListObjects_Test4(minioClient));
        functionalTestTasks.Add(FunctionalTest.ListObjects_Test5(minioClient));
        functionalTestTasks.Add(FunctionalTest.ListObjects_Test6(minioClient));

        // Test RemoveObjectAsync function
        functionalTestTasks.Add(FunctionalTest.RemoveObject_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.RemoveObjects_Test2(minioClient));
        functionalTestTasks.Add(FunctionalTest.RemoveObjects_Test3(minioClient));

        // Test CopyObjectAsync function
        functionalTestTasks.Add(FunctionalTest.CopyObject_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.CopyObject_Test2(minioClient));
        functionalTestTasks.Add(FunctionalTest.CopyObject_Test3(minioClient));
        functionalTestTasks.Add(FunctionalTest.CopyObject_Test4(minioClient));
        functionalTestTasks.Add(FunctionalTest.CopyObject_Test5(minioClient));
        functionalTestTasks.Add(FunctionalTest.CopyObject_Test6(minioClient));
        functionalTestTasks.Add(FunctionalTest.CopyObject_Test7(minioClient));
        functionalTestTasks.Add(FunctionalTest.CopyObject_Test8(minioClient));

        // Test SetPolicyAsync function
        functionalTestTasks.Add(FunctionalTest.SetBucketPolicy_Test1(minioClient));

        // Test S3Zip function
        functionalTestTasks.Add(FunctionalTest.GetObjectS3Zip_Test1(minioClient));

        // Test Presigned Get/Put operations
        functionalTestTasks.Add(FunctionalTest.PresignedGetObject_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.PresignedGetObject_Test2(minioClient));
        functionalTestTasks.Add(FunctionalTest.PresignedGetObject_Test3(minioClient));
        functionalTestTasks.Add(FunctionalTest.PresignedPutObject_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.PresignedPutObject_Test2(minioClient));
        // FunctionalTest.PresignedPostPolicy_Test1(minioClient).Wait();

        // Test incomplete uploads
        functionalTestTasks.Add(FunctionalTest.ListIncompleteUpload_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.ListIncompleteUpload_Test2(minioClient));
        functionalTestTasks.Add(FunctionalTest.ListIncompleteUpload_Test3(minioClient));
        functionalTestTasks.Add(FunctionalTest.RemoveIncompleteUpload_Test(minioClient));

        // Test GetBucket policy
        functionalTestTasks.Add(FunctionalTest.GetBucketPolicy_Test1(minioClient));

        // Test object versioning
        functionalTestTasks.Add(FunctionalTest.ObjectVersioningAsync_Test1(minioClient));

        // Test Object Lock Configuration
        functionalTestTasks.Add(FunctionalTest.ObjectLockConfigurationAsync_Test1(minioClient));

        // Test Bucket, Object Tags
        functionalTestTasks.Add(FunctionalTest.BucketTagsAsync_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.ObjectTagsAsync_Test1(minioClient));

        // Test Bucket Lifecycle configuration
        functionalTestTasks.Add(FunctionalTest.BucketLifecycleAsync_Test1(minioClient));
        functionalTestTasks.Add(FunctionalTest.BucketLifecycleAsync_Test2(minioClient));

        // Test encryption
        if (isSecure)
        {
#pragma warning disable MA0039 // Do not write your own certificate validation method
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, sslPolicyErrors) => true;
#pragma warning restore MA0039 // Do not write your own certificate validation method

            functionalTestTasks.Add(FunctionalTest.PutGetStatEncryptedObject_Test1(minioClient));
            functionalTestTasks.Add(FunctionalTest.PutGetStatEncryptedObject_Test2(minioClient));

            functionalTestTasks.Add(FunctionalTest.EncryptedCopyObject_Test1(minioClient));
            functionalTestTasks.Add(FunctionalTest.EncryptedCopyObject_Test2(minioClient));
        }

        if (kmsEnabled is not null && string.Equals(kmsEnabled, "1", StringComparison.OrdinalIgnoreCase))
        {
            functionalTestTasks.Add(FunctionalTest.PutGetStatEncryptedObject_Test3(minioClient));
            functionalTestTasks.Add(FunctionalTest.EncryptedCopyObject_Test3(minioClient));
            functionalTestTasks.Add(FunctionalTest.EncryptedCopyObject_Test4(minioClient));
        }

        await Utils.RunInParallel(functionalTestTasks, async (task, _) => { await task.ConfigureAwait(false); })
            .ConfigureAwait(false);
    }
}