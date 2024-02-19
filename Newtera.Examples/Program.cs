/*
 * Newtera .NET Library for Newtera TDM, (C) 2017, 2020 Newtera, Inc.
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
using Newtera.DataModel;
using Newtera.Examples.Cases;

namespace Newtera.Examples;

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
        return "newtera-tdm-" + result;
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
            endPoint = "localhost";
            accessKey = "demo1";
            secretKey = "888";
            isSecure = false;
            port = 8080;
        }

        using var newteraClient = new NewteraClient()
            .WithEndpoint(endPoint, port)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(isSecure)
            .Build();

        // Assign parameters before starting the test 
        var bucketName = "tdm";
        var smallFileName = CreateFile(1 * UNIT_MB);
        var bigFileName = CreateFile(6 * UNIT_MB);
        var objectName = GetRandomName();
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

        // Set HTTP Tracing On
        //newteraClient.SetTraceOn();

        // Set HTTP Tracing Off
        // newteraClient.SetTraceOff();
        // Check if bucket exists
        await BucketExists.Run(newteraClient, bucketName).ConfigureAwait(false);

        // List the objects in the new bucket
        ListObjects.Run(newteraClient, bucketName);

        // Put an object to the new bucket
        await PutObject.Run(newteraClient, bucketName, objectName, smallFileName, progress).ConfigureAwait(false);

        // Get the file and Download the object as file
        await GetObject.Run(newteraClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Upload a File with PutObject
        await FPutObject.Run(newteraClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Delete the file and Download the object as file
        await FGetObject.Run(newteraClient, bucketName, objectName, smallFileName).ConfigureAwait(false);

        // Automatic Multipart Upload with object more than 5Mb
        await PutObject.Run(newteraClient, bucketName, objectName, bigFileName, progress).ConfigureAwait(false);

        // Upload encrypted object
        var putFileName1 = CreateFile(1 * UNIT_MB);
        await PutObject.Run(newteraClient, bucketName, objectName, putFileName1, progress).ConfigureAwait(false);
  
        // Delete the list of objects
        await RemoveObjects.Run(newteraClient, bucketName, objectsList).ConfigureAwait(false);

        // Delete the object
        await RemoveObject.Run(newteraClient, bucketName, objectName).ConfigureAwait(false);

        // Delete the object
        await RemoveObject.Run(newteraClient, bucketName, objectName).ConfigureAwait(false);

        // Tracing request with custom logger
        await CustomRequestLogger.Run(newteraClient).ConfigureAwait(false);

        // Remove the binary files created for test
        File.Delete(smallFileName);
        File.Delete(bigFileName);

        if (OperatingSystem.IsWindows()) _ = Console.ReadLine();
    }
}
