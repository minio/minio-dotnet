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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Minio.DataModel;
using Minio.DataModel.ObjectLock;
using Minio.Examples.Cases;

namespace Minio.Examples;

public class Program
{
    private const int UNIT_MB = 1024 * 1024;
    private static readonly Random rnd = new();

    // Create a file of given size from random byte array
    private static string CreateFile(int size)
    {
        var fileName = GetRandomName();
        var data = new byte[size];
        rnd.NextBytes(data);

        File.WriteAllBytes(fileName, data);

        return fileName;
    }

    // Generate a random string
    public static string GetRandomName()
    {
        var characters = "0123456789abcdefghijklmnopqrstuvwxyz";
        var result = new StringBuilder(5);
        for (var i = 0; i < 5; i++) result.Append(characters[rnd.Next(characters.Length)]);
        return "minio-dotnet-example-" + result;
    }

    public static void Main(string[] args)
    {
        string endPoint = null;
        string accessKey = null;
        string secretKey = null;
        var enableHTTPS = false;
        var port = 80;

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
                enableHTTPS = Environment.GetEnvironmentVariable("ENABLE_HTTPS").Equals("1");
                if (enableHTTPS && port == 80) port = 443;
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
            minioClient = new MinioClient()
                .WithEndpoint(endPoint, port)
                .WithCredentials(accessKey, secretKey)
                .WithSSL()
                .Build();
        else
            minioClient = new MinioClient()
                .WithEndpoint(endPoint, port)
                .WithCredentials(accessKey, secretKey)
                .Build();
        // Assign parameters before starting the test 
        var bucketName = GetRandomName();
        var smallFileName = CreateFile(1 * UNIT_MB);
        var bigFileName = CreateFile(6 * UNIT_MB);
        var objectName = GetRandomName();
        var destBucketName = GetRandomName();
        var destObjectName = GetRandomName();
        var lockBucketName = GetRandomName();
        var objectsList = new List<string>();
        for (var i = 0; i < 10; i++) objectsList.Add(objectName + i);
        // Set app Info 
        minioClient.SetAppInfo("app-name", "app-version");

        // Set HTTP Tracing On
        // minioClient.SetTraceOn();

        // Set HTTP Tracing Off
        // minioClient.SetTraceOff();
        // Check if bucket exists
        BucketExists.Run(minioClient, bucketName).Wait();

        // Create a new bucket
        MakeBucket.Run(minioClient, bucketName).Wait();
        MakeBucket.Run(minioClient, destBucketName).Wait();

        // Bucket with Lock tests
        MakeBucketWithLock.Run(minioClient, lockBucketName).Wait();
        BucketExists.Run(minioClient, lockBucketName).Wait();
        RemoveBucket.Run(minioClient, lockBucketName).Wait();

        // Versioning tests
        GetVersioning.Run(minioClient, bucketName).Wait();
        EnableSuspendVersioning.Run(minioClient, bucketName).Wait();
        GetVersioning.Run(minioClient, bucketName).Wait();
        // List all the buckets on the server
        ListBuckets.Run(minioClient).Wait();

        // Start listening for bucket notifications
        ListenBucketNotifications.Run(minioClient, bucketName, new List<EventType> { EventType.ObjectCreatedAll });

        // Put an object to the new bucket
        PutObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

        // Get object metadata
        StatObject.Run(minioClient, bucketName, objectName).Wait();

        // List the objects in the new bucket
        ListObjects.Run(minioClient, bucketName);

        // Get the file and Download the object as file
        GetObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();
        // Select content from object
        SelectObjectContent.Run(minioClient, bucketName, objectName).Wait();
        // Delete the file and Download partial object as file
        GetPartialObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

        // Server side copyObject
        CopyObject.Run(minioClient, bucketName, objectName, destBucketName, objectName).Wait();

        // Server side copyObject with metadata replacement
        CopyObjectMetadata.Run(minioClient, bucketName, objectName, destBucketName, objectName).Wait();

        // Upload a File with PutObject
        FPutObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

        // Delete the file and Download the object as file
        FGetObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();

        // Automatic Multipart Upload with object more than 5Mb
        PutObject.Run(minioClient, bucketName, objectName, bigFileName).Wait();

        // Specify SSE-C encryption options
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
        var putFileName1 = CreateFile(1 * UNIT_MB);
        PutObject.Run(minioClient, bucketName, objectName, putFileName1, ssec).Wait();
        // Copy SSE-C encrypted object to unencrypted object
        CopyObject.Run(minioClient, bucketName, objectName, destBucketName, objectName, sseCpy, ssec).Wait();
        // Download SSE-C encrypted object
        FGetObject.Run(minioClient, destBucketName, objectName, bigFileName, ssec).Wait();

        // List the incomplete uploads
        ListIncompleteUploads.Run(minioClient, bucketName);

        // Remove all the incomplete uploads
        RemoveIncompleteUpload.Run(minioClient, bucketName, objectName).Wait();

        // Set a policy for given bucket
        SetBucketPolicy.Run(minioClient, bucketName).Wait();
        // Get the policy for given bucket
        GetBucketPolicy.Run(minioClient, bucketName).Wait();

        // Set bucket notifications
        SetBucketNotification.Run(minioClient, bucketName).Wait();

        // Get bucket notifications
        GetBucketNotification.Run(minioClient, bucketName).Wait();

        // Remove all bucket notifications
        RemoveAllBucketNotifications.Run(minioClient, bucketName).Wait();

        // Object Lock Configuration operations
        lockBucketName = GetRandomName();
        MakeBucketWithLock.Run(minioClient, lockBucketName).Wait();
        var configuration = new ObjectLockConfiguration(RetentionMode.GOVERNANCE, 35);
        SetObjectLockConfiguration.Run(minioClient, lockBucketName, configuration).Wait();
        GetObjectLockConfiguration.Run(minioClient, lockBucketName).Wait();
        RemoveObjectLockConfiguration.Run(minioClient, lockBucketName).Wait();
        RemoveBucket.Run(minioClient, lockBucketName).Wait();

        // Bucket Replication operations
        var replicationRuleID = "myreplicationID-3333";
        SetBucketReplication.Run(minioClient, bucketName,
            destBucketName, replicationRuleID).Wait();
        GetBucketReplication.Run(minioClient, bucketName,
            replicationRuleID).Wait();
        // TODO: we can verify that the replication happens by checking
        // the content in the destination matches the source content.
        //     We also cannot remove the replication config immediately
        //     after running GetBucketReplication command, as
        //     replicating the source in the destination takes some time.
        RemoveBucketReplication.Run(minioClient, bucketName).Wait();

        // Get the presigned url for a GET object request
        PresignedGetObject.Run(minioClient, bucketName, objectName).Wait();

        // Get the presigned POST policy curl url
        PresignedPostPolicy.Run(minioClient, bucketName, objectName).Wait();

        // Get the presigned url for a PUT object request
        PresignedPutObject.Run(minioClient, bucketName, objectName).Wait();

        // Delete the list of objects
        RemoveObjects.Run(minioClient, bucketName, objectsList).Wait();

        // Delete the object
        RemoveObject.Run(minioClient, bucketName, objectName).Wait();

        // Delete the object
        RemoveObject.Run(minioClient, destBucketName, objectName).Wait();

        // Retry on failure
        RetryPolicyObject.Run(minioClient, destBucketName, objectName).Wait();

        // Tracing request with custom logger
        CustomRequestLogger.Run(minioClient).Wait();

        // Remove the buckets
        RemoveBucket.Run(minioClient, bucketName).Wait();
        RemoveBucket.Run(minioClient, destBucketName).Wait();

        // Remove the binary files created for test
        File.Delete(smallFileName);
        File.Delete(bigFileName);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Console.ReadLine();
    }
}