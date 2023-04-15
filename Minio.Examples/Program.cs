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

using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Minio.DataModel;
using Minio.DataModel.ObjectLock;
using Minio.Examples.Cases;

namespace Minio.Examples;

public static class Program
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

    public static async Task Main(string[] args)
    {
        string endPoint = null;
        string accessKey = null;
        string secretKey = null;
        var isSecure = false;
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
                isSecure = Environment.GetEnvironmentVariable("ENABLE_HTTPS").Equals("1", StringComparison.OrdinalIgnoreCase);
                if (isSecure && port == 80) port = 443;
            }
        }
        else
        {
            endPoint = "play.min.io";
            accessKey = "Q3AM3UQ867SPQQA43P2F";
            secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            isSecure = true;
            port = 443;
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
        await BucketExists.Run(minioClient, bucketName).ConfigureAwait(false);

        // Create a new bucket
        await MakeBucket.Run(minioClient, bucketName).ConfigureAwait(false);
        await MakeBucket.Run(minioClient, destBucketName).ConfigureAwait(false);

        // Bucket with Lock tests
        await MakeBucketWithLock.Run(minioClient, lockBucketName).ConfigureAwait(false);
        await BucketExists.Run(minioClient, lockBucketName).ConfigureAwait(false);
        await RemoveBucket.Run(minioClient, lockBucketName).ConfigureAwait(false);

        // Versioning tests
        await GetVersioning.Run(minioClient, bucketName).ConfigureAwait(false);
        await EnableSuspendVersioning.Run(minioClient, bucketName).ConfigureAwait(false);
        await GetVersioning.Run(minioClient, bucketName).ConfigureAwait(false);
        // List all the buckets on the server
        await ListBuckets.Run(minioClient).ConfigureAwait(false);

        // Start listening for bucket notifications
        ListenBucketNotifications.Run(minioClient, bucketName, new List<EventType> { EventType.ObjectCreatedAll });

        // Put an object to the new bucket
        await PutObject.Run(minioClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Get object metadata
        await StatObject.Run(minioClient, bucketName, objectName).ConfigureAwait(false);

        // List the objects in the new bucket
        ListObjects.Run(minioClient, bucketName);

        // Get the file and Download the object as file
        await GetObject.Run(minioClient, bucketName, objectName, smallFileName).ConfigureAwait(false);
        // Select content from object
        await SelectObjectContent.Run(minioClient, bucketName, objectName).ConfigureAwait(false);
        // Delete the file and Download partial object as file
        await GetPartialObject.Run(minioClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Server side copyObject
        await CopyObject.Run(minioClient, bucketName, objectName, destBucketName, objectName).ConfigureAwait(false);

        // Server side copyObject with metadata replacement
        await CopyObjectMetadata.Run(minioClient, bucketName, objectName, destBucketName, objectName)
            .ConfigureAwait(false);

        // Upload a File with PutObject
        await FPutObject.Run(minioClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Delete the file and Download the object as file
        await FGetObject.Run(minioClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Automatic Multipart Upload with object more than 5Mb
        await PutObject.Run(minioClient, bucketName, objectName, bigFileName).ConfigureAwait(false);

        // Specify SSE-C encryption options
        using var aesEncryption = Aes.Create();
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
        await PutObject.Run(minioClient, bucketName, objectName, putFileName1, ssec).ConfigureAwait(false);
        // Copy SSE-C encrypted object to unencrypted object
        await CopyObject.Run(minioClient, bucketName, objectName, destBucketName, objectName, sseCpy, ssec)
            .ConfigureAwait(false);
        // Download SSE-C encrypted object
        await FGetObject.Run(minioClient, destBucketName, objectName, bigFileName, ssec).ConfigureAwait(false);

        // List the incomplete uploads
        ListIncompleteUploads.Run(minioClient, bucketName);

        // Remove all the incomplete uploads
        await RemoveIncompleteUpload.Run(minioClient, bucketName, objectName).ConfigureAwait(false);

        // Set a policy for given bucket
        await SetBucketPolicy.Run(minioClient, bucketName).ConfigureAwait(false);
        // Get the policy for given bucket
        await GetBucketPolicy.Run(minioClient, bucketName).ConfigureAwait(false);

        // Set bucket notifications
        await SetBucketNotification.Run(minioClient, bucketName).ConfigureAwait(false);

        // Get bucket notifications
        await GetBucketNotification.Run(minioClient, bucketName).ConfigureAwait(false);

        // Remove all bucket notifications
        await RemoveAllBucketNotifications.Run(minioClient, bucketName).ConfigureAwait(false);

        // Object Lock Configuration operations
        lockBucketName = GetRandomName();
        await MakeBucketWithLock.Run(minioClient, lockBucketName).ConfigureAwait(false);
        var configuration = new ObjectLockConfiguration(RetentionMode.GOVERNANCE, 35);
        await SetObjectLockConfiguration.Run(minioClient, lockBucketName, configuration).ConfigureAwait(false);
        await GetObjectLockConfiguration.Run(minioClient, lockBucketName).ConfigureAwait(false);
        await RemoveObjectLockConfiguration.Run(minioClient, lockBucketName).ConfigureAwait(false);
        await RemoveBucket.Run(minioClient, lockBucketName).ConfigureAwait(false);

        // Bucket Replication operations
        var replicationRuleID = "myreplicationID-3333";
        await SetBucketReplication.Run(minioClient, bucketName, destBucketName, replicationRuleID)
            .ConfigureAwait(false);
        await GetBucketReplication.Run(minioClient, bucketName, replicationRuleID).ConfigureAwait(false);
        // TODO: we can verify that the replication happens by checking
        // the content in the destination matches the source content.
        //     We also cannot remove the replication config immediately
        //     after running GetBucketReplication command, as
        //     replicating the source in the destination takes some time.
        await RemoveBucketReplication.Run(minioClient, bucketName).ConfigureAwait(false);

        // Get the presigned url for a GET object request
        await PresignedGetObject.Run(minioClient, bucketName, objectName).ConfigureAwait(false);

        // Get the presigned POST policy curl url
        await PresignedPostPolicy.Run(minioClient, bucketName, objectName).ConfigureAwait(false);

        // Get the presigned url for a PUT object request
        await PresignedPutObject.Run(minioClient, bucketName, objectName).ConfigureAwait(false);

        // Delete the list of objects
        await RemoveObjects.Run(minioClient, bucketName, objectsList).ConfigureAwait(false);

        // Delete the object
        await RemoveObject.Run(minioClient, bucketName, objectName).ConfigureAwait(false);

        // Delete the object
        await RemoveObject.Run(minioClient, destBucketName, objectName).ConfigureAwait(false);

        // Retry on failure
        await RetryPolicyObject.Run(minioClient, destBucketName, objectName).ConfigureAwait(false);

        // Tracing request with custom logger
        await CustomRequestLogger.Run(minioClient).ConfigureAwait(false);

        // Remove the buckets
        await RemoveBucket.Run(minioClient, bucketName).ConfigureAwait(false);
        await RemoveBucket.Run(minioClient, destBucketName).ConfigureAwait(false);

        // Remove the binary files created for test
        File.Delete(smallFileName);
        File.Delete(bigFileName);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Console.ReadLine();
    }
}