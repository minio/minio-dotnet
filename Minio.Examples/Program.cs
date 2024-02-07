﻿/*
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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Minio.DataModel;
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
        for (var i = 0; i < 5; i++) _ = result.Append(characters[rnd.Next(characters.Length)]);
        return "minio-dotnet-example-" + result;
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Needs to run all tests")]
    public static async Task Main()
    {
        string endPoint = null;
        string accessKey = null;
        string secretKey = null;
        var isSecure = false;
        var port = 80;

        if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") is not null)
        {
            endPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
            var posColon = endPoint.LastIndexOf(':');
            if (posColon != -1)
            {
                port = int.Parse(endPoint.Substring(posColon + 1, endPoint.Length - posColon - 1), NumberStyles.Integer,
                    CultureInfo.InvariantCulture);
                endPoint = endPoint[..posColon];
            }

            accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
            secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
            if (Environment.GetEnvironmentVariable("ENABLE_HTTPS") is not null)
            {
                isSecure = Environment.GetEnvironmentVariable("ENABLE_HTTPS")
                    .Equals("1", StringComparison.OrdinalIgnoreCase);
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
        var progress = new Progress<ProgressReport>(progressReport =>
        {
            Console.WriteLine(
                $"Percentage: {progressReport.Percentage}% TotalBytesTransferred: {progressReport.TotalBytesTransferred} bytes");
            if (progressReport.Percentage != 100)
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            else Console.WriteLine();
        });
        var objectsList = new List<string>();
        for (var i = 0; i < 10; i++) objectsList.Add(objectName + i);

        // Set app Info 
        //minioClient.SetAppInfo("app-name", "app-version");

        // Set HTTP Tracing On
        // minioClient.SetTraceOn();

        // Set HTTP Tracing Off
        // minioClient.SetTraceOff();
        // Check if bucket exists
        await BucketExists.Run(minioClient, bucketName).ConfigureAwait(false);

        // Create a new bucket
        await MakeBucket.Run(minioClient, bucketName).ConfigureAwait(false);
        await MakeBucket.Run(minioClient, destBucketName).ConfigureAwait(false);

        // Put an object to the new bucket
        await PutObject.Run(minioClient, bucketName, objectName, smallFileName, progress).ConfigureAwait(false);

        // List the objects in the new bucket
        ListObjects.Run(minioClient, bucketName);

        // Get the file and Download the object as file
        await GetObject.Run(minioClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Upload a File with PutObject
        await FPutObject.Run(minioClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Delete the file and Download the object as file
        await FGetObject.Run(minioClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Automatic Multipart Upload with object more than 5Mb
        await PutObject.Run(minioClient, bucketName, objectName, bigFileName, progress).ConfigureAwait(false);

        // Upload encrypted object
        var putFileName1 = CreateFile(1 * UNIT_MB);
        await PutObject.Run(minioClient, bucketName, objectName, putFileName1, progress).ConfigureAwait(false);
  
        // Delete the list of objects
        await RemoveObjects.Run(minioClient, bucketName, objectsList).ConfigureAwait(false);

        // Delete the object
        await RemoveObject.Run(minioClient, bucketName, objectName).ConfigureAwait(false);

        // Delete the object
        await RemoveObject.Run(minioClient, destBucketName, objectName).ConfigureAwait(false);

        // Tracing request with custom logger
        await CustomRequestLogger.Run(minioClient).ConfigureAwait(false);

        // Remove the buckets
        await RemoveBucket.Run(minioClient, bucketName).ConfigureAwait(false);
        await RemoveBucket.Run(minioClient, destBucketName).ConfigureAwait(false);

        // Remove the binary files created for test
        File.Delete(smallFileName);
        File.Delete(bigFileName);

        if (OperatingSystem.IsWindows()) _ = Console.ReadLine();
    }
}
