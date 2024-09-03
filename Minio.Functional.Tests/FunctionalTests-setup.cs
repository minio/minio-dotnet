/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017-2021 MinIO, Inc.
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Encryption;
using Minio.DataModel.ILM;
using Minio.DataModel.Notification;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Select;
using Minio.DataModel.Tags;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio.Functional.Tests;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Keep private const lowercase")]
[SuppressMessage("Design", "MA0048:File name must match type name")]
public static partial class FunctionalTest
{
    private const int KB = 1024;
    private const int MB = 1024 * 1024;
    private const int GB = 1024 * 1024 * 1024;

    private const string dataFile1B = "datafile-1-b";

    private const string dataFile10KB = "datafile-10-kB";
    private const string dataFile6MB = "datafile-6-MB";

    private const string makeBucketSignature =
        "Task MakeBucketAsync(string bucketName, string location = 'us-east-1', CancellationToken cancellationToken = default(CancellationToken))";

    private const string listBucketsSignature =
        "Task<ListAllMyBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default(CancellationToken))";

    private const string bucketExistsSignature =
        "Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";

    private const string removeBucketSignature =
        "Task RemoveBucketAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";

    private const string listObjectsSignature =
        "IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = false, CancellationToken cancellationToken = default(CancellationToken))";

    private const string putObjectSignature =
        "Task PutObjectAsync(PutObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getObjectSignature =
        "Task GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string listIncompleteUploadsSignature =
        "IObservable<Upload> ListIncompleteUploads(ListIncompleteUploads args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string listenBucketNotificationsSignature =
        "IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(ListenBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string listenNotificationsSignature =
        "IObservable<MinioNotificationRaw> ListenNotifications(ListenBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string copyObjectSignature =
        "Task<CopyObjectResult> CopyObjectAsync(CopyObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string statObjectSignature =
        "Task<ObjectStat> StatObjectAsync(StatObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string removeObjectSignature1 =
        "Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string removeObjectSignature2 =
        "Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs, CancellationToken cancellationToken = default(CancellationToken))";

    private const string removeIncompleteUploadSignature =
        "Task RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string presignedPutObjectSignature =
        "Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args)";

    private const string presignedGetObjectSignature =
        "Task<string> PresignedGetObjectAsync(PresignedGetObjectArgs args)";

    private const string presignedPostPolicySignature =
        "Task<Dictionary<string, string>> PresignedPostPolicyAsync(PresignedPostPolicyArgs args)";

    private const string getBucketPolicySignature =
        "Task<string> GetPolicyAsync(GetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string setBucketPolicySignature =
        "Task SetPolicyAsync(SetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getBucketNotificationSignature =
        "Task<BucketNotification> GetBucketNotificationAsync(GetBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string setBucketNotificationSignature =
        "Task SetBucketNotificationAsync(SetBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string removeAllBucketsNotificationSignature =
        "Task RemoveAllBucketNotificationsAsync(RemoveAllBucketNotifications args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string setBucketEncryptionSignature =
        "Task SetBucketEncryptionAsync(SetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getBucketEncryptionSignature =
        "Task<ServerSideEncryptionConfiguration> GetBucketEncryptionAsync(GetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string removeBucketEncryptionSignature =
        "Task RemoveBucketEncryptionAsync(RemoveBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string selectObjectSignature =
        "Task<SelectResponseStream> SelectObjectContentAsync(SelectObjectContentArgs args,CancellationToken cancellationToken = default(CancellationToken))";

    private const string setObjectLegalHoldSignature =
        "Task SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getObjectLegalHoldSignature =
        "Task<bool> GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string setObjectLockConfigurationSignature =
        "Task SetObjectLockConfigurationAsync(SetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getObjectLockConfigurationSignature =
        "Task<ObjectLockConfiguration> GetObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string deleteObjectLockConfigurationSignature =
        "Task RemoveObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getBucketTagsSignature =
        "Task<Tagging> GetBucketTagsAsync(GetBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string setBucketTagsSignature =
        "Task SetBucketTagsAsync(SetBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string deleteBucketTagsSignature =
        "Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string setVersioningSignature =
        "Task SetVersioningAsync(SetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getVersioningSignature =
        "Task<VersioningConfiguration> GetVersioningAsync(GetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string removeVersioningSignature =
        "Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getObjectTagsSignature =
        "Task<Tagging> GetObjectTagsAsync(GetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string setObjectTagsSignature =
        "Task SetObjectTagsAsync(SetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string deleteObjectTagsSignature =
        "Task RemoveObjectTagsAsync(RemoveObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string setObjectRetentionSignature =
        "Task SetObjectRetentionAsync(SetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getObjectRetentionSignature =
        "Task<ObjectRetentionConfiguration> GetObjectRetentionAsync(GetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string clearObjectRetentionSignature =
        "Task ClearObjectRetentionAsync(ClearObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string getBucketLifecycleSignature =
        "Task<LifecycleConfiguration> GetBucketLifecycleAsync(GetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string setBucketLifecycleSignature =
        "Task SetBucketLifecycleAsync(SetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private const string deleteBucketLifecycleSignature =
        "Task RemoveBucketLifecycleAsync(RemoveBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))";

    private static readonly Random rnd = new();

    private static readonly RandomStreamGenerator rsg = new(100 * MB);

    private static string Bash(string cmd)
    {
        var Replacements = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "$", "\\$" },
                { "(", "\\(" },
                { ")", "\\)" },
                { "{", "\\{" },
                { "}", "\\}" },
                { "[", "\\[" },
                { "]", "\\]" },
                { "@", "\\@" },
                { "%", "\\%" },
                { "&", "\\&" },
                { "#", "\\#" },
                { "+", "\\+" }
            };

        foreach (var toReplace in Replacements.Keys)
            cmd = cmd.Replace(toReplace, Replacements[toReplace], StringComparison.Ordinal);
        var cmdNoReturn = cmd + " >/dev/null 2>&1";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{cmdNoReturn}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        _ = process.Start();
        var result = process.StandardOutput.ReadLine();
        process.WaitForExit();

        return result;
    }

    // Create a file of given size from random byte array or optionally create a symbolic link
    // to the dataFileName residing in MINT_DATA_DIR
    private static string CreateFile(int size, string dataFileName = null)
    {
        var fileName = GetRandomName();

        if (!IsMintEnv())
        {
            var data = new byte[size];
            rnd.NextBytes(data);

            File.WriteAllBytes(fileName, data);
            return GetFilePath(fileName);
        }

        return GetFilePath(dataFileName);
    }

    public static string GetRandomObjectName(int length = 5)
    {
        // Server side does not allow the following characters in object names
        // '-', '_', '.', '/', '*'
#if NET6_0_OR_GREATER
        var characters = "abcd+%$#@&{}[]()";
#else
        var characters = "abcdefgh+%$#@&";
#endif
        var result = new StringBuilder(length);

        for (var i = 0; i < length; i++) result.Append(characters[rnd.Next(characters.Length)]);
        return result.ToString();
    }

    // Generate a random string
    public static string GetRandomName(int length = 5)
    {
        var characters = "0123456789abcdefghijklmnopqrstuvwxyz";
        if (length > 50) length = 50;

        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++) _ = result.Append(characters[rnd.Next(characters.Length)]);

        return "minio-dotnet-example-" + result;
    }

    internal static void GenerateRandom500MB_File(string fileName)
    {
        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        var fileSize = 500L * 1024 * 1024;
        var segments = fileSize / 10000;
        var last_seg = fileSize % 10000;
        using var br = new BinaryWriter(fs);

        for (long i = 0; i < segments; i++)
            br.Write(new byte[10000]);

        br.Write(new byte[last_seg]);
        br.Close();
    }

    // Return true if running in Mint mode
    public static bool IsMintEnv()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MINT_DATA_DIR"));
    }

    // Get full path of file
    public static string GetFilePath(string fileName)
    {
        var dataDir = Environment.GetEnvironmentVariable("MINT_DATA_DIR");
        if (!string.IsNullOrEmpty(dataDir)) return $"{dataDir}/{fileName}";

        var path = Directory.GetCurrentDirectory();
        return $"{path}/{fileName}";
    }
}
