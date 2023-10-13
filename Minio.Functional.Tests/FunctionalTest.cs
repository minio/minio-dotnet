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
using System.Runtime.InteropServices;
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
public static class FunctionalTest
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

    internal static Task RunCoreTests(IMinioClient minioClient)
    {
        ConcurrentBag<Task> coreTestsTasks = new()
        {
            // Check if bucket exists
            BucketExists_Test(minioClient),

            // Create a new bucket
            MakeBucket_Test1(minioClient),
            PutObject_Test1(minioClient),
            PutObject_Test2(minioClient),
            ListObjects_Test1(minioClient),
            RemoveObject_Test1(minioClient),
            CopyObject_Test1(minioClient),

            // Test SetPolicyAsync function
            SetBucketPolicy_Test1(minioClient),

            // Test Presigned Get/Put operations
            PresignedGetObject_Test1(minioClient),
            PresignedPutObject_Test1(minioClient),

            // Test incomplete uploads
            ListIncompleteUpload_Test1(minioClient),
            RemoveIncompleteUpload_Test(minioClient),

            // Test GetBucket policy
            GetBucketPolicy_Test1(minioClient)
        };

        return coreTestsTasks.ForEachAsync();
    }

    internal static async Task BucketExists_Test(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName();
        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName);
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var rbArgs = new RemoveBucketArgs()
            .WithBucket(bucketName);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName } };

        try
        {
            await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            var found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsTrue(found);
            new MintLogger(nameof(BucketExists_Test), bucketExistsSignature, "Tests whether BucketExists passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketExists_Test), bucketExistsSignature, "Tests whether BucketExists passes",
                TestStatus.NA, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(BucketExists_Test), bucketExistsSignature, "Tests whether BucketExists passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
        }
    }

    internal static async Task RemoveBucket_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(20);
        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName);
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var rbArgs = new RemoveBucketArgs()
            .WithBucket(bucketName);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName } };

        var found = false;
        try
        {
            await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsTrue(found);
            await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
            found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsFalse(found);
            new MintLogger(nameof(RemoveBucket_Test1), removeBucketSignature, "Tests whether RemoveBucket passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(RemoveBucket_Test1), removeBucketSignature, "Tests whether RemoveBucket passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            if (found)
                await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
        }
    }

    internal static async Task RemoveBucket_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(20);
        var objectName = GetRandomName(20);
        var forceFlagHeader = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "x-minio-force-delete", "true" } };

        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var rbArgs = new RemoveBucketArgs()
            .WithBucket(bucketName)
            .WithHeaders(forceFlagHeader);

        // Create and populate a bucket
        var count = 50;
        var tasks = new Task[count];
        await Setup_Test(minio, bucketName).ConfigureAwait(false);
        for (var i = 0; i < count; i++)
            tasks[i] = PutObject_Task(minio, bucketName, objectName + i, null, null, 0, null,
                rsg.GenerateStreamFromSeed(5));

        await Task.WhenAll(tasks).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "x-minio-force-delete", "true" } };

        var found = false;

        try
        {
            found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsTrue(found);
            await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
            found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsFalse(found);
            new MintLogger(nameof(RemoveBucket_Test2), removeBucketSignature, "Tests whether RemoveBucket passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(RemoveBucket_Test2), removeBucketSignature, "Tests whether RemoveBucket passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            if (found)
                await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
        }
    }

    internal static async Task ListBuckets_Test(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        IList<Bucket> bucketList = new List<Bucket>();
        var bucketNameSuffix = "listbucketstest";
        var noOfBuckets = 5;

        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketNameSuffix", bucketNameSuffix },
                { "noOfBuckets", noOfBuckets.ToString(CultureInfo.InvariantCulture) }
            };

        try
        {
            foreach (var indx in Enumerable.Range(1, noOfBuckets))
                await Setup_Test(minio, "bucket" + indx + bucketNameSuffix).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex.Message.StartsWith("Bucket already owned by you", StringComparison.OrdinalIgnoreCase))
            {
                // You have your bucket already created, continue
            }
            else
            {
                throw;
            }
        }

        try
        {
            var list = await minio.ListBucketsAsync().ConfigureAwait(false);
            bucketList = list.Buckets;
            bucketList = bucketList.Where(x => x.Name.EndsWith(bucketNameSuffix, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.AreEqual(noOfBuckets, bucketList.Count);
            bucketList.ToList().Sort((x, y) =>
            {
                if (string.Equals(x.Name, y.Name, StringComparison.Ordinal)) return 0;
                if (x.Name is null) return -1;
                if (y.Name is null) return 1;
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });
            var indx = 0;
            foreach (var bucket in bucketList)
            {
                indx++;
                Assert.AreEqual("bucket" + indx + bucketNameSuffix, bucket.Name);
            }

            new MintLogger(nameof(ListBuckets_Test), listBucketsSignature, "Tests whether ListBucket passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(ListBuckets_Test), listBucketsSignature, "Tests whether ListBucket passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            foreach (var bucket in bucketList)
            {
                var rbArgs = new RemoveBucketArgs()
                    .WithBucket(bucket.Name);
                await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
            }
        }
    }

    internal static async Task Setup_Test(IMinioClient minio, string bucketName)
    {
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        if (await minio.BucketExistsAsync(beArgs).ConfigureAwait(false))
            return;
        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName);
        await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
        var found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
        Assert.IsTrue(found);
    }

    internal static async Task Setup_WithLock_Test(IMinioClient minio, string bucketName)
    {
        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName)
            .WithObjectLock();
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
        var found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
        Assert.IsTrue(found);
    }

    internal static async Task TearDown(IMinioClient minio, string bucketName)
    {
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var bktExists = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
        if (!bktExists)
            return;
        var taskList = new List<Task>();
        var getVersions = false;
        // Get Versioning/Retention Info.
        var lockConfigurationArgs =
            new GetObjectLockConfigurationArgs()
                .WithBucket(bucketName);
        ObjectLockConfiguration lockConfig = null;
        try
        {
            var versioningConfig = await minio.GetVersioningAsync(new GetVersioningArgs()
                .WithBucket(bucketName)).ConfigureAwait(false);
            if (versioningConfig is not null &&
                (versioningConfig.Status.Contains("Enabled", StringComparison.Ordinal) ||
                 versioningConfig.Status.Contains("Suspended", StringComparison.Ordinal)))
                getVersions = true;

            lockConfig = await minio.GetObjectLockConfigurationAsync(lockConfigurationArgs).ConfigureAwait(false);
        }
        catch (MissingObjectLockConfigurationException)
        {
            // This exception is expected for those buckets created without a lock.
        }
        catch (NotImplementedException)
        {
            // No throw. Move to the next step without versions.
        }

        var tasks = new List<Task>();
        var listObjectsArgs = new ListObjectsArgs()
            .WithBucket(bucketName)
            .WithRecursive(true)
            .WithVersions(getVersions);
        var objectNamesVersions =
            new List<Tuple<string, string>>();
        var objectNames = new List<string>();
        var observable = minio.ListObjectsAsync(listObjectsArgs);

        var exceptionList = new List<Exception>();
        var subscription = observable.Subscribe(
            item =>
            {
                if (getVersions)
                    objectNamesVersions.Add(new Tuple<string, string>(item.Key, item.VersionId));
                else
                    objectNames.Add(item.Key);
            },
            exceptionList.Add,
            () => { });

        await Task.Delay(9000).ConfigureAwait(false);
        if (lockConfig?.ObjectLockEnabled.Equals(ObjectLockConfiguration.LockEnabled,
                StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var item in objectNamesVersions)
            {
                var objectRetentionArgs = new GetObjectRetentionArgs()
                    .WithBucket(bucketName)
                    .WithObject(item.Item1)
                    .WithVersionId(item.Item2);
                var retentionConfig = await minio.GetObjectRetentionAsync(objectRetentionArgs).ConfigureAwait(false);
                var bypassGovMode = retentionConfig.Mode == ObjectRetentionMode.GOVERNANCE;
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(item.Item1)
                    .WithVersionId(item.Item2);
                if (bypassGovMode)
                    removeObjectArgs = removeObjectArgs.WithBypassGovernanceMode(bypassGovMode);
                var t = minio.RemoveObjectAsync(removeObjectArgs);
                tasks.Add(t);
            }
        }
        else
        {
            if (objectNamesVersions.Count > 0)
            {
                var removeObjectArgs = new RemoveObjectsArgs()
                    .WithBucket(bucketName)
                    .WithObjectsVersions(objectNamesVersions);
                Task t = minio.RemoveObjectsAsync(removeObjectArgs);
                tasks.Add(t);
            }

            if (objectNames.Count > 0)
            {
                var removeObjectArgs = new RemoveObjectsArgs()
                    .WithBucket(bucketName)
                    .WithObjects(objectNames);

                Task t = minio.RemoveObjectsAsync(removeObjectArgs);
                tasks.Add(t);
            }
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        var rbArgs = new RemoveBucketArgs()
            .WithBucket(bucketName);
        await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
    }

    internal static string XmlStrToJsonStr(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var xmlSerializer = new XmlSerializer(typeof(string));
        using var stringReader = new StringReader(xml);

        var obj = (string)xmlSerializer.Deserialize(stringReader)!;
        return JsonSerializer.Serialize(obj);
    }

    internal static async Task PutGetStatEncryptedObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "application/octet-stream";
        var tempFileName = "tempFile-" + GetRandomName();
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            // Putobject with SSE-C encryption.
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using var aesEncryption = Aes.Create();
            aesEncryption.KeySize = 256;
            aesEncryption.GenerateKey();
            var ssec = new SSEC(aesEncryption.Key);

            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var file_write_size = filestream.Length;

                long file_read_size = 0;
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithServerSideEncryption(ssec)
                    .WithContentType(contentType);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithServerSideEncryption(ssec)
                    .WithCallbackStream(async (stream, cancellationToken) =>
                    {
                        using var fileStream = File.Create(tempFileName);

                        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                        await fileStream.DisposeAsync().ConfigureAwait(false);

                        var writtenInfo = new FileInfo(tempFileName);
                        file_read_size = writtenInfo.Length;

                        Assert.AreEqual(file_write_size, file_read_size);
                        File.Delete(tempFileName);
                    });
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithServerSideEncryption(ssec);
                _ = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
                _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            }

            new MintLogger("PutGetStatEncryptedObject_Test1", putObjectSignature,
                "Tests whether Put/Get/Stat Object with encryption passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger("PutGetStatEncryptedObject_Test1", putObjectSignature,
                "Tests whether Put/Get/Stat Object with encryption passes", TestStatus.NA, DateTime.Now - startTime, "",
                ex.Message, ex.ToString(), args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("PutGetStatEncryptedObject_Test1", putObjectSignature,
                "Tests whether Put/Get/Stat Object with encryption passes", TestStatus.FAIL, DateTime.Now - startTime,
                "", ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutGetStatEncryptedObject_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "application/octet-stream";
        var tempFileName = "tempFile-" + GetRandomName();
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "6MB" },
                { "size", "6MB" }
            };
        try
        {
            // Test multipart Put with SSE-C encryption
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using var aesEncryption = Aes.Create();
            aesEncryption.KeySize = 256;
            aesEncryption.GenerateKey();
            var ssec = new SSEC(aesEncryption.Key);

            using (var filestream = rsg.GenerateStreamFromSeed(6 * MB))
            {
                var file_write_size = filestream.Length;

                long file_read_size = 0;
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithContentType(contentType)
                    .WithServerSideEncryption(ssec);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithServerSideEncryption(ssec)
                    .WithCallbackStream(async (stream, cancellationToken) =>
                    {
                        using var fileStream = File.Create(tempFileName);

                        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                        await fileStream.DisposeAsync().ConfigureAwait(false);

                        var writtenInfo = new FileInfo(tempFileName);
                        file_read_size = writtenInfo.Length;

                        Assert.AreEqual(file_write_size, file_read_size);
                        File.Delete(tempFileName);
                    });
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithServerSideEncryption(ssec);
                _ = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
                _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            }

            new MintLogger("PutGetStatEncryptedObject_Test2", putObjectSignature,
                "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger("PutGetStatEncryptedObject_Test2", putObjectSignature,
                "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.NA,
                DateTime.Now - startTime, "", ex.Message, ex.ToString(), args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("PutGetStatEncryptedObject_Test2", putObjectSignature,
                "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.FAIL,
                DateTime.Now - startTime, "", ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            File.Delete(tempFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutGetStatEncryptedObject_Test3(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "application/octet-stream";
        var tempFileName = "tempFile-" + GetRandomName();
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "6MB" },
                { "size", "6MB" }
            };
        try
        {
            // Test multipart Put/Get/Stat with SSE-S3 encryption
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using var aesEncryption = Aes.Create();
            var sses3 = new SSES3();

            using (var filestream = rsg.GenerateStreamFromSeed(6 * MB))
            {
                var file_write_size = filestream.Length;
                long file_read_size = 0;
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithServerSideEncryption(sses3)
                    .WithContentType(contentType);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream(async (stream, cancellationToken) =>
                    {
                        using var fileStream = File.Create(tempFileName);

                        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                        await fileStream.DisposeAsync().ConfigureAwait(false);

                        var writtenInfo = new FileInfo(tempFileName);
                        file_read_size = writtenInfo.Length;

                        Assert.AreEqual(file_write_size, file_read_size);
                        File.Delete(tempFileName);
                    });
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);
                _ = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
                _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            }

            new MintLogger("PutGetStatEncryptedObject_Test3", putObjectSignature,
                "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("PutGetStatEncryptedObject_Test3", putObjectSignature,
                "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.FAIL,
                DateTime.Now - startTime, "", ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutObject_Task(IMinioClient minio, string bucketName, string objectName,
        string fileName = null, string contentType = "application/octet-stream", long size = 0,
        Dictionary<string, string> metaData = null, Stream mstream = null)
    {
        var startTime = DateTime.Now;

        var filestream = mstream;
        if (filestream is null)
        {
#if NETFRAMEWORK
            ReadOnlyMemory<byte> bs = File.ReadAllBytes(fileName);
#else
            ReadOnlyMemory<byte> bs = await File.ReadAllBytesAsync(fileName).ConfigureAwait(false);
#endif
            filestream = bs.AsStream();
        }

        using (filestream)
        {
            var file_write_size = filestream.Length;
            var tempFileName = "tempfile-" + GetRandomName();
            if (size == 0)
                size = filestream.Length;

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(filestream)
                .WithObjectSize(size)
                .WithContentType(contentType)
                .WithHeaders(metaData);
            await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            File.Delete(tempFileName);
        }
    }

    internal static async Task<ObjectStat> PutObject_Tester(IMinioClient minio,
        string bucketName, string objectName, string fileName = null,
        string contentType = "application/octet-stream", long size = 0,
        Dictionary<string, string> metaData = null, Stream mstream = null,
        IProgress<ProgressReport> progress = null)
    {
        ObjectStat statObject = null;
        var startTime = DateTime.Now;

        var filestream = mstream;
        if (filestream is null)
        {
#if NETFRAMEWORK
            Memory<byte> bs = File.ReadAllBytes(fileName);
#else
            Memory<byte> bs = await File.ReadAllBytesAsync(fileName).ConfigureAwait(false);
#endif
            filestream = bs.AsStream();
        }

        using (filestream)
        {
            var file_write_size = filestream.Length;
            var tempFileName = "tempfile-" + GetRandomName();
            if (size == 0) size = filestream.Length;
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(filestream)
                .WithObjectSize(size)
                .WithProgress(progress)
                .WithContentType(contentType)
                .WithHeaders(metaData);
            await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            statObject = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            Assert.IsNotNull(statObject);
            Assert.IsTrue(statObject.ObjectName.Equals(objectName, StringComparison.Ordinal));
            Assert.AreEqual(statObject.Size, size);

            if (contentType is not null)
            {
                Assert.IsNotNull(statObject.ContentType);
                Assert.IsTrue(statObject.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase));
            }

            var rmArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            await minio.RemoveObjectAsync(rmArgs).ConfigureAwait(false);
        }

        return statObject;
    }

    internal static async Task StatObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "gzip";

        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "1KB" },
                { "size", "1KB" }
            };

        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            _ = await PutObject_Tester(minio, bucketName, objectName, null, null, 0, null,
                rsg.GenerateStreamFromSeed(1 * KB)).ConfigureAwait(false);
            new MintLogger(nameof(StatObject_Test1), statObjectSignature, "Tests whether StatObject passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(StatObject_Test1), statObjectSignature, "Tests whether statObjectSignature passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task FPutObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var fileName = CreateFile(6 * MB, dataFile6MB);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectName }, { "fileName", fileName }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFileName(fileName);
            _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            new MintLogger("FPutObject_Test1", putObjectSignature,
                "Tests whether FPutObject for multipart upload passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("FPutObject_Test1", putObjectSignature,
                "Tests whether FPutObject for multipart upload passes", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            if (!IsMintEnv()) File.Delete(fileName);
        }
    }

    internal static async Task FPutObject_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var fileName = CreateFile(10 * KB, dataFile10KB);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectName }, { "fileName", fileName }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFileName(fileName);
            _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            new MintLogger("FPutObject_Test2", putObjectSignature, "Tests whether FPutObject for small upload passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("FPutObject_Test2", putObjectSignature, "Tests whether FPutObject for small upload passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            if (!IsMintEnv())
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                File.Delete(fileName);
            }
        }
    }

    internal static async Task RemoveObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "objectName", objectName } };
        try
        {
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                await Setup_Test(minio, bucketName).ConfigureAwait(false);
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);

                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            new MintLogger("RemoveObject_Test1", removeObjectSignature1,
                "Tests whether RemoveObjectAsync for existing object passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("RemoveObject_Test1", removeObjectSignature1,
                "Tests whether RemoveObjectAsync for existing object passes", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task RemoveObjects_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(6);
        var objectsList = new List<string>();
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectNames", "[" + objectName + "0..." + objectName + "50]" }
            };
        try
        {
            var count = 50;
            var tasks = new Task[count];
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            for (var i = 0; i < count; i++)
            {
                tasks[i] = PutObject_Task(minio, bucketName, objectName + i, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(5));
                objectsList.Add(objectName + i);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            new MintLogger("RemoveObjects_Test2", removeObjectSignature2,
                "Tests whether RemoveObjectAsync for multi objects delete passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("RemoveObjects_Test2", removeObjectSignature2,
                "Tests whether RemoveObjectAsync for multi objects delete passes", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task RemoveObjects_Test3(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(6);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectNames", "[" + objectName + "0..." + objectName + "50]" }
            };
        try
        {
            var count = 50;
            var tasks = new Task[count * 2];
            var objectsList = new List<string>();
            await Setup_WithLock_Test(minio, bucketName).ConfigureAwait(false);
            for (var i = 0; i < count * 2;)
            {
                tasks[i++] = PutObject_Task(minio, bucketName, objectName + i, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(5));
                tasks[i++] = PutObject_Task(minio, bucketName, objectName + i, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(5));
                objectsList.Add(objectName + i);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithRecursive(true)
                .WithVersions(true);
            var observable = minio.ListObjectsAsync(listObjectsArgs);
            var objVersions = new List<Tuple<string, string>>();
#pragma warning disable AsyncFixer03 // Fire-and-forget async-void methods or delegates
            var subscription = observable.Subscribe(
                item => objVersions.Add(new Tuple<string, string>(item.Key, item.VersionId)),
                ex => throw ex,
                async () =>
                {
                    var removeObjectsArgs = new RemoveObjectsArgs()
                        .WithBucket(bucketName)
                        .WithObjectsVersions(objVersions);

                    var rmObservable = await minio.RemoveObjectsAsync(removeObjectsArgs).ConfigureAwait(false);

                    var deList = new List<DeleteError>();
                    using var rmSub = rmObservable.Subscribe(
                        err => deList.Add(err),
                        ex => throw ex,
                        async () => await TearDown(minio, bucketName).ConfigureAwait(false));
                });
#pragma warning restore AsyncFixer03 // Fire-and-forget async-void methods or delegates

            await Task.Delay(2 * 1000).ConfigureAwait(false);
            new MintLogger("RemoveObjects_Test3", removeObjectSignature2,
                "Tests whether RemoveObjectsAsync for multi objects/versions delete passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger("RemoveObjects_Test3", removeObjectSignature2,
                "Tests whether RemoveObjectsAsync for multi objects/versions delete passes", TestStatus.NA,
                DateTime.Now - startTime, "", ex.Message, ex.ToString(), args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("RemoveObjects_Test3", removeObjectSignature2,
                "Tests whether RemoveObjectsAsync for multi objects/versions delete passes", TestStatus.FAIL,
                DateTime.Now - startTime, "", ex.Message, ex.ToString(), args).Log();
            throw;
        }
    }

    internal static async Task DownloadObjectAsync(IMinioClient minio, string url, string filePath,
        CancellationToken cancellationToken = default)
    {
        using var response = await minio.WrapperGetAsync(url).ConfigureAwait(false);
        if (string.IsNullOrEmpty(Convert.ToString(response.Content)) || !HttpStatusCode.OK.Equals(response.StatusCode))
            throw new InvalidOperationException("Unable to download via presigned URL" + nameof(response.Content));

        using var fs = new FileStream(filePath, FileMode.CreateNew);
        await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task UploadObjectAsync(IMinioClient minio, string url, string filePath)
    {
        using var filestream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var stream = new StreamContent(filestream);
        await minio.WrapperPutAsync(url, stream).ConfigureAwait(false);
    }

    internal static async Task PresignedPostPolicy_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var metadataKey = GetRandomName(10);
        var metadataValue = GetRandomName(10);

        // Generate presigned post policy url
        var formPolicy = new PostPolicy();
        var expiresOn = DateTime.UtcNow.AddMinutes(15);
        formPolicy.SetExpires(expiresOn);
        formPolicy.SetBucket(bucketName);
        formPolicy.SetKey(objectName);
        formPolicy.SetUserMetadata(metadataKey, metadataValue);

        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresOn", expiresOn.ToString(CultureInfo.InvariantCulture) }
            };

        // File to be uploaded
        var size = 10 * KB;
        var sizeExpected = 10240;
        var contentType = "application/octet-stream";
        var fileName = CreateFile(size, dataFile10KB);

        try
        {
            // Creates the bucket
            await Setup_Test(minio, bucketName).ConfigureAwait(false);

            var polArgs = new PresignedPostPolicyArgs().WithBucket(bucketName)
                .WithObject(objectName)
                .WithPolicy(formPolicy);

            var policyTuple = await minio.PresignedPostPolicyAsync(polArgs).ConfigureAwait(false);
            var uri = policyTuple.Item1.AbsoluteUri;

            var curlCommand = "curl --insecure";
            foreach (var pair in policyTuple.Item2) curlCommand += $" -F {pair.Key}=\"{pair.Value}\"";
            curlCommand += $" -F file=\"@{fileName}\" {uri}";

            _ = Bash(curlCommand);

            // Validate
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            var statObject = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            Assert.IsNotNull(statObject);
            Assert.IsTrue(statObject.ObjectName.Equals(objectName, StringComparison.Ordinal));
            Assert.AreEqual(statObject.Size, sizeExpected);
            Assert.IsTrue(statObject.MetaData["Content-Type"] is not null);
            Assert.IsTrue(statObject.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(statObject.MetaData[metadataKey].Equals(metadataValue, StringComparison.Ordinal));

            new MintLogger("PresignedPostPolicy_Test1", presignedPostPolicySignature,
                "Tests whether PresignedPostPolicy url applies policy on server", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("PresignedPostPolicy_Test1", presignedPostPolicySignature,
                "Tests whether PresignedPostPolicy url applies policy on server", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }

        if (!IsMintEnv()) File.Delete(fileName);
    }

    internal static async Task RemoveIncompleteUpload_Test(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "csv";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "objectName", objectName } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(2));
            try
            {
                using var filestream = rsg.GenerateStreamFromSeed(30 * MB);
                var file_write_size = filestream.Length;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithContentType(contentType);
                _ = await minio.PutObjectAsync(putObjectArgs, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                var rmArgs = new RemoveIncompleteUploadArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);

                await minio.RemoveIncompleteUploadAsync(rmArgs).ConfigureAwait(false);

                var listArgs = new ListIncompleteUploadsArgs()
                    .WithBucket(bucketName);
                var observable = minio.ListIncompleteUploads(listArgs);

                var subscription = observable.Subscribe(
                    item => Assert.Fail(),
                    ex => Assert.Fail());
            }

            new MintLogger("RemoveIncompleteUpload_Test", removeIncompleteUploadSignature,
                    "Tests whether RemoveIncompleteUpload passes.", TestStatus.PASS, DateTime.Now - startTime,
                    args: args)
                .Log();
        }
        catch (Exception ex)
        {
            new MintLogger("RemoveIncompleteUpload_Test", removeIncompleteUploadSignature,
                "Tests whether RemoveIncompleteUpload passes.", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #region Select Object Content

    internal static async Task SelectObjectContent_Test(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var outFileName = "outFileName-SelectObjectContent_Test";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectName }, { "fileName", outFileName }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var csvString = new StringBuilder();
            csvString.AppendLine("Employee,Manager,Group");
            csvString.AppendLine("Employee4,Employee2,500");
            csvString.AppendLine("Employee3,Employee1,500");
            csvString.AppendLine("Employee1,,1000");
            csvString.AppendLine("Employee5,Employee1,500");
            csvString.AppendLine("Employee2,Employee1,800");
            ReadOnlyMemory<byte> csvBytes = Encoding.UTF8.GetBytes(csvString.ToString());
            using var stream = csvBytes.AsStream();

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length);
            await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

            var inputSerialization = new SelectObjectInputSerialization
            {
                CompressionType = SelectCompressionType.NONE,
                CSV = new CSVInputOptions
                {
                    FileHeaderInfo = CSVFileHeaderInfo.None, RecordDelimiter = "\n", FieldDelimiter = ","
                }
            };
            var outputSerialization = new SelectObjectOutputSerialization
            {
                CSV = new CSVOutputOptions { RecordDelimiter = "\n", FieldDelimiter = "," }
            };
            var selArgs = new SelectObjectContentArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpressionType(QueryExpressionType.SQL)
                .WithQueryExpression("select * from s3object")
                .WithInputSerialization(inputSerialization)
                .WithOutputSerialization(outputSerialization);

            var resp = await minio.SelectObjectContentAsync(selArgs).ConfigureAwait(false);
            using var streamReader = new StreamReader(resp.Payload);
            var output = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            var csvStringNoWS =
                Regex.Replace(csvString.ToString(), @"\s+", "", RegexOptions.None, TimeSpan.FromHours(1));
            var outputNoWS = Regex.Replace(output, @"\s+", "", RegexOptions.None, TimeSpan.FromHours(1));

#if NETFRAMEWORK
            using var md5 = MD5.Create();
            var hashedOutputBytes
 = md5.ComputeHash(Encoding.UTF8.GetBytes(outputNoWS));
#else
            // Compute MD5 for a better result.
            var hashedOutputBytes = MD5.HashData(Encoding.UTF8.GetBytes(outputNoWS));
#endif

            var outputMd5 = Convert.ToBase64String(hashedOutputBytes);

#if NETFRAMEWORK
            using var md5CSV = MD5.Create();
            var hashedCSVBytes
 = md5CSV.ComputeHash(Encoding.UTF8.GetBytes(csvStringNoWS));
#else
            var hashedCSVBytes = MD5.HashData(Encoding.UTF8.GetBytes(csvStringNoWS));
#endif
            var csvMd5 = Convert.ToBase64String(hashedCSVBytes);

            Assert.IsTrue(csvMd5.Contains(outputMd5, StringComparison.Ordinal));
            new MintLogger("SelectObjectContent_Test", selectObjectSignature,
                "Tests whether SelectObjectContent passes for a select query", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("SelectObjectContent_Test", selectObjectSignature,
                "Tests whether SelectObjectContent passes for a select query", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            File.Delete(outFileName);
        }
    }

    #endregion

    #region Bucket Encryption

    internal static async Task BucketEncryptionsAsync_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(BucketEncryptionsAsync_Test1), setBucketEncryptionSignature,
                "Tests whether SetBucketEncryptionAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var encryptionArgs = new SetBucketEncryptionArgs()
                .WithBucket(bucketName);
            await minio.SetBucketEncryptionAsync(encryptionArgs).ConfigureAwait(false);
            new MintLogger(nameof(BucketEncryptionsAsync_Test1), setBucketEncryptionSignature,
                    "Tests whether SetBucketEncryptionAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketEncryptionsAsync_Test1), setBucketEncryptionSignature,
                "Tests whether SetBucketEncryptionAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(BucketEncryptionsAsync_Test1), setBucketEncryptionSignature,
                "Tests whether SetBucketEncryptionAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var encryptionArgs = new GetBucketEncryptionArgs()
                .WithBucket(bucketName);
            var config = await minio.GetBucketEncryptionAsync(encryptionArgs).ConfigureAwait(false);
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.Rule);
            Assert.IsNotNull(config.Rule.Apply);
            Assert.IsTrue(config.Rule.Apply.SSEAlgorithm.Contains("AES256", StringComparison.OrdinalIgnoreCase));
            new MintLogger(nameof(BucketEncryptionsAsync_Test1), getBucketEncryptionSignature,
                    "Tests whether GetBucketEncryptionAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketEncryptionsAsync_Test1), getBucketEncryptionSignature,
                "Tests whether GetBucketEncryptionAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(BucketEncryptionsAsync_Test1), getBucketEncryptionSignature,
                "Tests whether GetBucketEncryptionAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var rmEncryptionArgs = new RemoveBucketEncryptionArgs()
                .WithBucket(bucketName);
            await minio.RemoveBucketEncryptionAsync(rmEncryptionArgs).ConfigureAwait(false);
            var encryptionArgs = new GetBucketEncryptionArgs()
                .WithBucket(bucketName);
            var config = await minio.GetBucketEncryptionAsync(encryptionArgs).ConfigureAwait(false);
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketEncryptionsAsync_Test1), removeBucketEncryptionSignature,
                "Tests whether RemoveBucketEncryptionAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("The server side encryption configuration was not found",
                    StringComparison.OrdinalIgnoreCase))
            {
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), removeBucketEncryptionSignature,
                    "Tests whether RemoveBucketEncryptionAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args).Log();
            }
            else
            {
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), removeBucketEncryptionSignature,
                    "Tests whether RemoveBucketEncryptionAsync passes", TestStatus.FAIL, DateTime.Now - startTime,
                    ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Legal Hold Status

    internal static async Task LegalHoldStatusAsync_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "objectName", objectName } };
        try
        {
            await Setup_WithLock_Test(minio, bucketName).ConfigureAwait(false);
        }
        catch (NotImplementedException ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature,
                "Tests whether SetObjectLegalHoldAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            return;
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature,
                "Tests whether SetObjectLegalHoldAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithContentType(null);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var legalHoldArgs = new SetObjectLegalHoldArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithLegalHold(true);
            await minio.SetObjectLegalHoldAsync(legalHoldArgs).ConfigureAwait(false);
            new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature,
                    "Tests whether SetObjectLegalHoldAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature,
                "Tests whether SetObjectLegalHoldAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature,
                "Tests whether SetObjectLegalHoldAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var getLegalHoldArgs = new GetObjectLegalHoldArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var enabled = await minio.GetObjectLegalHoldAsync(getLegalHoldArgs).ConfigureAwait(false);
            Assert.IsTrue(enabled);
            var rmArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await minio.RemoveObjectAsync(rmArgs).ConfigureAwait(false);
            new MintLogger(nameof(LegalHoldStatusAsync_Test1), getObjectLegalHoldSignature,
                    "Tests whether GetObjectLegalHoldAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(LegalHoldStatusAsync_Test1), getObjectLegalHoldSignature,
                "Tests whether GetObjectLegalHoldAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(LegalHoldStatusAsync_Test1), getObjectLegalHoldSignature,
                "Tests whether GetObjectLegalHoldAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Bucket Tagging

    internal static async Task BucketTagsAsync_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName } };
        var tags = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "key1", "value1" }, { "key2", "value2" }, { "key3", "value3" } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(BucketTagsAsync_Test1), setBucketTagsSignature,
                "Tests whether SetBucketTagsAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var tagsArgs = new SetBucketTagsArgs()
                .WithBucket(bucketName)
                .WithTagging(Tagging.GetBucketTags(tags));
            await minio.SetBucketTagsAsync(tagsArgs).ConfigureAwait(false);
            new MintLogger(nameof(BucketTagsAsync_Test1), setBucketTagsSignature,
                "Tests whether SetBucketTagsAsync passes", TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketTagsAsync_Test1), setBucketTagsSignature,
                "Tests whether SetBucketTagsAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(BucketTagsAsync_Test1), setBucketTagsSignature,
                "Tests whether SetBucketTagsAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var tagsArgs = new GetBucketTagsArgs()
                .WithBucket(bucketName);
            var tagObj = await minio.GetBucketTagsAsync(tagsArgs).ConfigureAwait(false);
            Assert.IsNotNull(tagObj);
            Assert.IsNotNull(tagObj.Tags);
            var tagsRes = tagObj.Tags;
            Assert.AreEqual(tagsRes.Count, tags.Count);

            new MintLogger(nameof(BucketTagsAsync_Test1), getBucketTagsSignature,
                "Tests whether GetBucketTagsAsync passes", TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketTagsAsync_Test1), getBucketTagsSignature,
                "Tests whether GetBucketTagsAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(BucketTagsAsync_Test1), getBucketTagsSignature,
                "Tests whether GetBucketTagsAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            await TearDown(minio, bucketName).ConfigureAwait(false);
            throw;
        }

        try
        {
            var tagsArgs = new RemoveBucketTagsArgs()
                .WithBucket(bucketName);
            await minio.RemoveBucketTagsAsync(tagsArgs).ConfigureAwait(false);
            var getTagsArgs = new GetBucketTagsArgs()
                .WithBucket(bucketName);
            var tagObj = await minio.GetBucketTagsAsync(getTagsArgs).ConfigureAwait(false);
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketTagsAsync_Test1), deleteBucketTagsSignature,
                "Tests whether RemoveBucketTagsAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("The TagSet does not exist", StringComparison.OrdinalIgnoreCase))
            {
                new MintLogger(nameof(BucketTagsAsync_Test1), deleteBucketTagsSignature,
                        "Tests whether RemoveBucketTagsAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                        args: args)
                    .Log();
            }
            else
            {
                new MintLogger(nameof(BucketTagsAsync_Test1), deleteBucketTagsSignature,
                    "Tests whether RemoveBucketTagsAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
                throw;
            }
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Object Tagging

    internal static async Task ObjectTagsAsync_Test1(IMinioClient minio)
    {
        // Test will run twice once for file size 1KB amd once
        // for 6MB to cover single and multipart upload functions
        var sizesList = new List<int> { 1 * KB, 6 * MB };
        foreach (var size in sizesList)
        {
            var startTime = DateTime.Now;
            var bucketName = GetRandomName(15);
            var objectName = GetRandomName(10);
            var args = new Dictionary<string, string>
                (StringComparer.Ordinal)
                {
                    { "bucketName", bucketName },
                    { "objectName", objectName },
                    { "fileSize", size.ToString(CultureInfo.InvariantCulture) }
                };
            var tags = new Dictionary<string, string>
                (StringComparer.Ordinal) { { "key1", "value1" }, { "key2", "value2" }, { "key3", "value3" } };
            try
            {
                await Setup_Test(minio, bucketName).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectTagsAsync_Test1), setObjectTagsSignature,
                    "Tests whether SetObjectTagsAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
                throw;
            }

            var exceptionThrown = false;
            try
            {
                using (var filestream = rsg.GenerateStreamFromSeed(size))
                {
                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithStreamData(filestream)
                        .WithObjectSize(filestream.Length)
                        .WithContentType(null);
                    _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                }

                var tagsArgs = new SetObjectTagsArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithTagging(Tagging.GetObjectTags(tags));
                await minio.SetObjectTagsAsync(tagsArgs).ConfigureAwait(false);
                new MintLogger(nameof(ObjectTagsAsync_Test1), setObjectTagsSignature,
                        "Tests whether SetObjectTagsAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                        args: args)
                    .Log();
            }
            catch (NotImplementedException ex)
            {
                exceptionThrown = true;
                new MintLogger(nameof(ObjectTagsAsync_Test1), setObjectTagsSignature,
                    "Tests whether SetObjectTagsAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                new MintLogger(nameof(ObjectTagsAsync_Test1), setObjectTagsSignature,
                    "Tests whether SetObjectTagsAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
                throw;
            }

            try
            {
                exceptionThrown = false;
                var tagsArgs = new GetObjectTagsArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);
                var tagObj = await minio.GetObjectTagsAsync(tagsArgs).ConfigureAwait(false);
                Assert.IsNotNull(tagObj);
                Assert.IsNotNull(tagObj.Tags);
                var tagsRes = tagObj.Tags;
                Assert.AreEqual(tagsRes.Count, tags.Count);
                new MintLogger(nameof(ObjectTagsAsync_Test1), getObjectTagsSignature,
                        "Tests whether GetObjectTagsAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                        args: args)
                    .Log();
            }
            catch (NotImplementedException ex)
            {
                exceptionThrown = true;
                new MintLogger(nameof(ObjectTagsAsync_Test1), getObjectTagsSignature,
                    "Tests whether GetObjectTagsAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                new MintLogger(nameof(ObjectTagsAsync_Test1), getObjectTagsSignature,
                    "Tests whether GetObjectTagsAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
                throw;
            }

            if (exceptionThrown)
            {
                await TearDown(minio, bucketName).ConfigureAwait(false);
                return;
            }

            try
            {
                var tagsArgs = new RemoveObjectTagsArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);
                await minio.RemoveObjectTagsAsync(tagsArgs).ConfigureAwait(false);
                var getTagsArgs = new GetObjectTagsArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);
                var tagObj = await minio.GetObjectTagsAsync(getTagsArgs).ConfigureAwait(false);
                Assert.IsNotNull(tagObj);
                var tagsRes = tagObj.Tags;
                Assert.IsNull(tagsRes);
                new MintLogger(nameof(ObjectTagsAsync_Test1), deleteObjectTagsSignature,
                        "Tests whether RemoveObjectTagsAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                        args: args)
                    .Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ObjectTagsAsync_Test1), deleteObjectTagsSignature,
                    "Tests whether RemoveObjectTagsAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectTagsAsync_Test1), deleteObjectTagsSignature,
                    "Tests whether RemoveObjectTagsAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName).ConfigureAwait(false);
            }
        }
    }

    #endregion

    #region Object Versioning

    internal static async Task ObjectVersioningAsync_Test1(IMinioClient minio)
    {
        // Test will run twice once for file size 1KB amd once
        // for 6MB to cover single and multipart upload functions
        var sizesList = new List<int> { 1 * KB, 6 * MB };
        foreach (var size in sizesList)
        {
            var loopIndex = 1;
            var startTime = DateTime.Now;
            var bucketName = GetRandomName(15);
            var objectName = GetRandomName(10);
            var args = new Dictionary<string, string>
                (StringComparer.Ordinal)
                {
                    { "bucketName", bucketName },
                    { "objectName", objectName },
                    { "fileSize", size.ToString(CultureInfo.InvariantCulture) }
                };
            try
            {
                await Setup_Test(minio, bucketName).ConfigureAwait(false);
                {
                    // Set versioning enabled test
                    var setVersioningArgs = new SetVersioningArgs()
                        .WithBucket(bucketName)
                        .WithVersioningEnabled();
                    await minio.SetVersioningAsync(setVersioningArgs).ConfigureAwait(false);

                    // Put the same object twice to have 2 versions of it
                    using (var filestream = rsg.GenerateStreamFromSeed(size))
                    {
                        var putObjectArgs = new PutObjectArgs()
                            .WithBucket(bucketName)
                            .WithObject(objectName)
                            .WithStreamData(filestream)
                            .WithObjectSize(filestream.Length)
                            .WithContentType(null);
                        _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                    }

                    using (var filestream = rsg.GenerateStreamFromSeed(size))
                    {
                        var putObjectArgs = new PutObjectArgs()
                            .WithBucket(bucketName)
                            .WithObject(objectName)
                            .WithStreamData(filestream)
                            .WithObjectSize(filestream.Length)
                            .WithContentType(null);
                        _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                    }

                    // In each run, there will be 2 more versions of the object
                    var objectVersionCount = loopIndex * 2;
                    await ListObjects_Test(minio, bucketName, "", objectVersionCount, true, true).ConfigureAwait(false);

                    new MintLogger(nameof(ObjectVersioningAsync_Test1), setVersioningSignature,
                        "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes",
                        TestStatus.PASS,
                        DateTime.Now - startTime, args: args).Log();

                    // Get Versioning Test
                    var getVersioningArgs = new GetVersioningArgs()
                        .WithBucket(bucketName);
                    var versioningConfig = await minio.GetVersioningAsync(getVersioningArgs).ConfigureAwait(false);
                    Assert.IsNotNull(versioningConfig);
                    Assert.IsNotNull(versioningConfig.Status);
                    Assert.IsTrue(versioningConfig.Status.Equals("enabled", StringComparison.OrdinalIgnoreCase));

                    new MintLogger(nameof(ObjectVersioningAsync_Test1), getVersioningSignature,
                        "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes",
                        TestStatus.PASS,
                        DateTime.Now - startTime, args: args).Log();

                    // Suspend Versioning test
                    setVersioningArgs = new SetVersioningArgs()
                        .WithBucket(bucketName)
                        .WithVersioningSuspended();
                    await minio.SetVersioningAsync(setVersioningArgs).ConfigureAwait(false);

                    var objectCount = 1;
                    await ListObjects_Test(minio, bucketName, "", objectCount, false).ConfigureAwait(false);

                    new MintLogger(nameof(ObjectVersioningAsync_Test1), removeVersioningSignature,
                        "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes",
                        TestStatus.PASS,
                        DateTime.Now - startTime, args: args).Log();
                }
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ObjectVersioningAsync_Test1), setVersioningSignature,
                    "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes", TestStatus.NA,
                    DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectVersioningAsync_Test1), setVersioningSignature,
                    "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes", TestStatus.FAIL,
                    DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName).ConfigureAwait(false);
            }
        }
    }

    #endregion

    #region Object Lock Configuration

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "TODO")]
    internal static async Task ObjectLockConfigurationAsync_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName } };
        var setLockNotImplemented = false;
        var getLockNotImplemented = false;

        try
        {
            await Setup_WithLock_Test(minio, bucketName).ConfigureAwait(false);
            //TODO: Use it for testing and remove
            {
                var objectRetention = new ObjectRetentionConfiguration(DateTime.Today.AddDays(3));
                using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    // Twice, for 2 versions.
                    var putObjectArgs1 = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithStreamData(filestream)
                        .WithObjectSize(filestream.Length)
                        .WithRetentionConfiguration(objectRetention)
                        .WithContentType(null);
                    _ = await minio.PutObjectAsync(putObjectArgs1).ConfigureAwait(false);
                }

                using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    var putObjectArgs2 = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithStreamData(filestream)
                        .WithObjectSize(filestream.Length)
                        .WithRetentionConfiguration(objectRetention)
                        .WithContentType(null);
                    _ = await minio.PutObjectAsync(putObjectArgs2).ConfigureAwait(false);
                }
            }
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature,
                "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.NA, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            await TearDown(minio, bucketName).ConfigureAwait(false);
            return;
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature,
                "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            await TearDown(minio, bucketName).ConfigureAwait(false);
            throw;
        }

        try
        {
            var objectLockArgs = new SetObjectLockConfigurationArgs()
                .WithBucket(bucketName)
                .WithLockConfiguration(
                    new ObjectLockConfiguration(ObjectRetentionMode.GOVERNANCE, 33)
                );
            await minio.SetObjectLockConfigurationAsync(objectLockArgs).ConfigureAwait(false);
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature,
                "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            setLockNotImplemented = true;
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature,
                "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.NA, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature,
                "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            await TearDown(minio, bucketName).ConfigureAwait(false);
            throw;
        }

        try
        {
            var objectLockArgs = new GetObjectLockConfigurationArgs()
                .WithBucket(bucketName);
            var config = await minio.GetObjectLockConfigurationAsync(objectLockArgs).ConfigureAwait(false);
            Assert.IsNotNull(config);
            Assert.IsTrue(config.ObjectLockEnabled.Contains(ObjectLockConfiguration.LockEnabled,
                StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(config.Rule);
            Assert.IsNotNull(config.Rule.DefaultRetention);
            Assert.AreEqual(config.Rule.DefaultRetention.Days, 33);
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), getObjectLockConfigurationSignature,
                "Tests whether GetObjectLockConfigurationAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            getLockNotImplemented = true;
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), getObjectLockConfigurationSignature,
                "Tests whether GetObjectLockConfigurationAsync passes", TestStatus.NA, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), getObjectLockConfigurationSignature,
                "Tests whether GetObjectLockConfigurationAsync passes", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            if (setLockNotImplemented || getLockNotImplemented)
            {
                // Cannot test Remove Object Lock with Set & Get Object Lock implemented.
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), deleteObjectLockConfigurationSignature,
                    "Tests whether RemoveObjectLockConfigurationAsync passes", TestStatus.NA, DateTime.Now - startTime,
                    "Functionality that is not implemented", "", args: args).Log();
                await TearDown(minio, bucketName).ConfigureAwait(false);
                return;
            }

            var objectLockArgs = new RemoveObjectLockConfigurationArgs()
                .WithBucket(bucketName);
            await minio.RemoveObjectLockConfigurationAsync(objectLockArgs).ConfigureAwait(false);
            var getObjectLockArgs = new GetObjectLockConfigurationArgs()
                .WithBucket(bucketName);
            var config = await minio.GetObjectLockConfigurationAsync(getObjectLockArgs).ConfigureAwait(false);
            Assert.IsNotNull(config);
            Assert.IsNull(config.Rule);
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), deleteObjectLockConfigurationSignature,
                "Tests whether RemoveObjectLockConfigurationAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), deleteObjectLockConfigurationSignature,
                "Tests whether RemoveObjectLockConfigurationAsync passes", TestStatus.NA, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), deleteObjectLockConfigurationSignature,
                "Tests whether RemoveObjectLockConfigurationAsync passes", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await Task.Delay(1500).ConfigureAwait(false);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Object Retention

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Refactor later")]
    internal static async Task ObjectRetentionAsync_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var args = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "bucketName", bucketName }, { "objectName", objectName }
        };

        try
        {
            await Setup_WithLock_Test(minio, bucketName).ConfigureAwait(false);
        }
        catch (NotImplementedException ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature,
                "Tests whether SetObjectRetentionAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            return;
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature,
                "Tests whether SetObjectRetentionAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var plusDays = 10;
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithContentType(null);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var untilDate = DateTime.Now.AddDays(plusDays);
            var setRetentionArgs = new SetObjectRetentionArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithRetentionMode(ObjectRetentionMode.GOVERNANCE)
                .WithRetentionUntilDate(untilDate);
            await minio.SetObjectRetentionAsync(setRetentionArgs).ConfigureAwait(false);
            new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature,
                "Tests whether SetObjectRetentionAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature,
                "Tests whether SetObjectRetentionAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature,
                "Tests whether SetObjectRetentionAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var getRetentionArgs = new GetObjectRetentionArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var config = await minio.GetObjectRetentionAsync(getRetentionArgs).ConfigureAwait(false);
            var plusDays = 10.0;
            Assert.IsNotNull(config);
            Assert.AreEqual(config.Mode, ObjectRetentionMode.GOVERNANCE);
            var untilDate = DateTime.Parse(config.RetainUntilDate, null, DateTimeStyles.RoundtripKind);
            Assert.AreEqual(Math.Ceiling((untilDate - DateTime.Now).TotalDays), plusDays);
            new MintLogger(nameof(ObjectRetentionAsync_Test1), getObjectRetentionSignature,
                "Tests whether GetObjectRetentionAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(ObjectRetentionAsync_Test1), getObjectRetentionSignature,
                "Tests whether GetObjectRetentionAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(ObjectRetentionAsync_Test1), getObjectRetentionSignature,
                "Tests whether GetObjectRetentionAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var clearRetentionArgs = new ClearObjectRetentionArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            await minio.ClearObjectRetentionAsync(clearRetentionArgs).ConfigureAwait(false);
            var getRetentionArgs = new GetObjectRetentionArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var config = await minio.GetObjectRetentionAsync(getRetentionArgs).ConfigureAwait(false);
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(ObjectRetentionAsync_Test1), clearObjectRetentionSignature,
                "Tests whether ClearObjectRetentionAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            var errMsgLock = ex.Message.Contains("The specified object does not have a ObjectLock configuration",
                StringComparison.OrdinalIgnoreCase);
            if (errMsgLock)
            {
                new MintLogger(nameof(ObjectRetentionAsync_Test1), clearObjectRetentionSignature,
                    "Tests whether ClearObjectRetentionAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args).Log();
            }
            else
            {
                new MintLogger(nameof(ObjectRetentionAsync_Test1), clearObjectRetentionSignature,
                    "Tests whether ClearObjectRetentionAsync passes", TestStatus.FAIL, DateTime.Now - startTime,
                    ex.Message, ex.ToString(), args: args).Log();
                await TearDown(minio, bucketName).ConfigureAwait(false);
                throw;
            }
        }

        try
        {
            var rmArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await minio.RemoveObjectAsync(rmArgs).ConfigureAwait(false);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(ObjectRetentionAsync_Test1), clearObjectRetentionSignature,
                "TearDown operation ClearObjectRetentionAsync", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }
    }

    #endregion

    internal static MemoryStream CreateZipFile(string prefix, int nFiles)
    {
        // CreateZipFile creates a zip file, populates it with <nFiles> many
        // small files, each prefixed with <prefix> and in bytes size plus a single
        // 1MB file. It generates and returns a memory stream of the zip file.
        // The names of these files are arranged in "<file-size>.bin" format,
        // like "127.bin" is created as a small binary file in 127 bytes size.
        var outputMemStream = new MemoryStream();
        using var zipStream = new ZipOutputStream(outputMemStream);

        zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

        _ = Directory.CreateDirectory(prefix);
        for (var i = 1; i <= nFiles; i++)
        {
            // Make a single 1Mb file
            if (i == nFiles) i = 1000000;

            var fileName = prefix + i + ".bin";
            var newEntry = new ZipEntry(fileName) { DateTime = DateTime.Now };
            zipStream.PutNextEntry(newEntry);

            using var stream = rsg.GenerateStreamFromSeed(i);
            StreamUtils.Copy(stream, zipStream, new byte[i * 128]);
            zipStream.CloseEntry();
        }

        // Setting ownership to False keeps the underlying stream open
        zipStream.IsStreamOwner = false;
        // Must finish the ZipOutputStream before using outputMemStream
        zipStream.Close();

        outputMemStream.Position = 0;
        _ = outputMemStream.Seek(0, SeekOrigin.Begin);

        return outputMemStream;
    }

    internal static async Task GetObjectS3Zip_Test1(IMinioClient minio)
    {
        var path = "test/small/";
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var randomFileName = GetRandomName(15) + ".zip";
        var objectName = GetRandomObjectName(15) + ".zip";

        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "objectName", objectName } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            const int nFiles = 500;
            using var memStream = CreateZipFile(path, nFiles);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(memStream)
                .WithObjectSize(memStream.Length);
            _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

            var extractHeader = new Dictionary<string, string>
                (StringComparer.Ordinal) { { "x-minio-extract", "true" } };

            // GeObject api test
            var r = new Random();
            var singleFileName = r.Next(1, nFiles - 1) + ".bin";
            var singleObjectName = objectName + "/" + path + singleFileName;
            // File names in the zip file also show the sizes of the files
            // For example file "35.bin" has a size of 35Bytes
            var expectedFileSize = Path.GetFileNameWithoutExtension(singleFileName);
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithFile(randomFileName)
                .WithObject(singleObjectName)
                .WithHeaders(extractHeader);

            var resp = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            // Verify the size of the file from the returned info
            Assert.AreEqual(expectedFileSize, resp.Size.ToString(CultureInfo.InvariantCulture));

            // HeadObject api test
            var statArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(singleObjectName)
                .WithHeaders(extractHeader);
            var stat = await minio.StatObjectAsync(statArgs).ConfigureAwait(false);
            // Verify the size of the file from the returned info
            Assert.AreEqual(expectedFileSize, resp.Size.ToString(CultureInfo.InvariantCulture));

            // ListObject api test with different prefix values
            // prefix value="", expected number of files listed=1
            var prefix = "";
            await ListObjects_Test(minio, bucketName, prefix, 1, headers: extractHeader).ConfigureAwait(false);

            // prefix value="/", expected number of files listed=nFiles
            prefix = objectName + "/";
            await ListObjects_Test(minio, bucketName, prefix, nFiles, headers: extractHeader)
                .ConfigureAwait(false);

            // prefix value="/test", expected number of files listed=nFiles
            prefix = objectName + "/test";
            await ListObjects_Test(minio, bucketName, prefix, nFiles, headers: extractHeader)
                .ConfigureAwait(false);

            // prefix value="/test/", expected number of files listed=nFiles
            prefix = objectName + "/test/";
            await ListObjects_Test(minio, bucketName, prefix, nFiles, headers: extractHeader)
                .ConfigureAwait(false);

            // prefix value="/test", expected number of files listed=nFiles
            prefix = objectName + "/test/small";
            await ListObjects_Test(minio, bucketName, prefix, nFiles, headers: extractHeader)
                .ConfigureAwait(false);

            // prefix value="/test", expected number of files listed=nFiles
            prefix = objectName + "/test/small/";
            await ListObjects_Test(minio, bucketName, prefix, nFiles, headers: extractHeader)
                .ConfigureAwait(false);

            // prefix value="/test", expected number of files listed=1
            await ListObjects_Test(minio, bucketName, singleObjectName, 1, headers: extractHeader)
                .ConfigureAwait(false);

            new MintLogger("GetObjectS3Zip_Test1", getObjectSignature, "Tests s3Zip files", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("GetObjectS3Zip_Test1", getObjectSignature, "Tests s3Zip files", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(randomFileName);
            Directory.Delete(path.Split('/')[0], true);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #region Bucket Notifications

    internal static async Task ListenBucketNotificationsAsync_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomName(10);
        var contentType = "application/octet-stream";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "1KB" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);

            var received = new List<MinioNotificationRaw>();

            var eventsList = new List<EventType> { EventType.ObjectCreatedAll };

            var listenArgs = new ListenBucketNotificationsArgs()
                .WithBucket(bucketName)
                .WithEvents(eventsList);
            var events = minio.ListenBucketNotificationsAsync(listenArgs);
            var subscription = events.Subscribe(
                received.Add,
                ex => { },
                () => { }
            );

            _ = await PutObject_Tester(minio, bucketName, objectName, null, contentType,
                0, null, rsg.GenerateStreamFromSeed(1 * KB)).ConfigureAwait(false);

            // wait for notifications
            var eventDetected = false;
            for (var attempt = 0; attempt < 10; attempt++)
                if (received.Count > 0)
                {
                    // Check if there is any unexpected error returned
                    // and captured in the receivedJson list, like
                    // "NotImplemented" api error. If so, we throw an exception
                    // and skip running this test
                    if (received.Count > 1 &&
                        received[1].Json.StartsWith("<Error><Code>", StringComparison.OrdinalIgnoreCase))
                    {
                        // Although the attribute is called "json",
                        // returned data in list "received" is in xml
                        // format and it is an error.Here, we convert xml
                        // into json format.
                        var receivedJson = XmlStrToJsonStr(received[1].Json);

                        // Cleanup the "Error" key encapsulating "receivedJson"
                        // data. This is required to match and convert json data
                        // "receivedJson" into class "ErrorResponse"
                        var len = "{'Error':".Length;
                        var trimmedFront = receivedJson[len..];
                        var trimmedFull = trimmedFront[..^1];

                        var err = JsonSerializer.Deserialize<ErrorResponse>(trimmedFull);

                        Exception ex = new UnexpectedMinioException(err.Message);
                        if (string.Equals(err.Code, "NotImplemented", StringComparison.OrdinalIgnoreCase))
                            ex = new NotImplementedException(err.Message);
                        await TearDown(minio, bucketName).ConfigureAwait(false);
                        throw ex;
                    }

                    var notification = JsonSerializer.Deserialize<MinioNotification>(received[0].Json);

                    if (notification.Records is not null)
                    {
                        Assert.AreEqual(1, notification.Records.Count);
                        Assert.IsTrue(notification.Records[0].EventName
                            .Contains("s3:ObjectCreated:Put", StringComparison.OrdinalIgnoreCase));
                        Assert.IsTrue(
                            objectName.Contains(HttpUtility.UrlDecode(notification.Records[0].S3.ObjectMeta.Key),
                                StringComparison.OrdinalIgnoreCase));
                        Assert.IsTrue(contentType.Contains(notification.Records[0].S3.ObjectMeta.ContentType,
                            StringComparison.OrdinalIgnoreCase));
                        eventDetected = true;
                        break;
                    }
                }

            // subscription.Dispose();
            if (!eventDetected)
                throw new UnexpectedMinioException("Failed to detect the expected bucket notification event.");

            new MintLogger(nameof(ListenBucketNotificationsAsync_Test1),
                listenBucketNotificationsSignature,
                "Tests whether ListenBucketNotifications passes for small object",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(ListenBucketNotificationsAsync_Test1),
                listenBucketNotificationsSignature,
                "Tests whether ListenBucketNotifications passes for small object",
                TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (string.Equals(ex.Message, "Listening for bucket notification is specific" +
                                          " only to `minio` server endpoints", StringComparison.OrdinalIgnoreCase))
            {
                // This is expected when bucket notification
                // is requested against AWS.
                // Check if endPoint is AWS
                static bool isAWS(string endPoint)
                {
                    var rgx = new Regex("^s3\\.?.*\\.amazonaws\\.com", RegexOptions.IgnoreCase, TimeSpan.FromHours(1));
                    var matches = rgx.Matches(endPoint);
                    return matches.Count > 0;
                }

                if (Environment.GetEnvironmentVariable("AWS_ENDPOINT") is not null ||
                    isAWS(Environment.GetEnvironmentVariable("SERVER_ENDPOINT")))
                    // This is a PASS
                    new MintLogger(nameof(ListenBucketNotificationsAsync_Test1),
                        listenBucketNotificationsSignature,
                        "Tests whether ListenBucketNotifications passes for small object",
                        TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
            }
            else
            {
                new MintLogger(nameof(ListenBucketNotificationsAsync_Test1),
                    listenBucketNotificationsSignature,
                    "Tests whether ListenBucketNotifications passes for small object",
                    TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
                await TearDown(minio, bucketName).ConfigureAwait(false);
                throw;
            }
        }
    }

    internal static async Task ListenBucketNotificationsAsync_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var events = new List<EventType> { EventType.ObjectCreatedAll };
        var rxEventData = new MinioNotificationRaw("");
        var rxEventsList = new List<NotificationEvent>();
        var bucketName = GetRandomName(15);
        var contentType = "application/json";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "contentType", contentType }, { "size", "16B" }
            };

        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(bucketName);
            var found = await minio.BucketExistsAsync(bucketExistsArgs).ConfigureAwait(false);
            if (!found)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);
                await minio.MakeBucketAsync(makeBucketArgs).ConfigureAwait(false);
            }

            void Notify(MinioNotificationRaw data)
            {
                var notification = JsonSerializer.Deserialize<MinioNotification>(data.Json);
                if (notification is not { Records: not null }) return;

                rxEventsList.AddRange(notification.Records);
            }

            var listenArgs = new ListenBucketNotificationsArgs()
                .WithBucket(bucketName)
                .WithEvents(events);
            var observable = minio.ListenBucketNotificationsAsync(listenArgs);

            var subscription = observable.Subscribe(
                ev =>
                {
                    rxEventData = ev;
                    Notify(rxEventData);
                },
                ex => throw new InvalidOperationException($"OnError: {ex.Message}"),
                () => throw new InvalidOperationException("STOPPED LISTENING FOR BUCKET NOTIFICATIONS\n"));

            // Sleep to give enough time for the subscriber to be ready
            var sleepTime = 1000; // Milliseconds
            await Task.Delay(sleepTime).ConfigureAwait(false);

            var modelJson = "{\"test2\": \"test2\"}";
            using var stream = Encoding.UTF8.GetBytes(modelJson).AsMemory().AsStream();
            var putObjectArgs = new PutObjectArgs()
                .WithObject("test2.json")
                .WithBucket(bucketName)
                .WithContentType(contentType)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length);

            _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

            // Waits until the Put event is detected
            // Times out if the event is not caught in 3 seconds
            var timeout = 3000; // Milliseconds
            var waitTime = 25; // Milliseconds
            var stTime = DateTime.UtcNow;
            while (string.IsNullOrEmpty(rxEventData.Json))
            {
                await Task.Delay(waitTime).ConfigureAwait(false);
                if ((DateTime.UtcNow - stTime).TotalMilliseconds >= timeout)
                    throw new TimeoutException("Timeout: while waiting for events");
            }

            foreach (var ev in rxEventsList) Assert.AreEqual("s3:ObjectCreated:Put", ev.EventName);

            new MintLogger(nameof(ListenBucketNotificationsAsync_Test2),
                listenBucketNotificationsSignature,
                "Tests whether ListenBucketNotifications passes for longer event processing",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(ListenBucketNotificationsAsync_Test2),
                listenBucketNotificationsSignature,
                "Tests whether ListenBucketNotifications passes for longer event processing",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListenBucketNotificationsAsync_Test3(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var events = new List<EventType> { EventType.ObjectCreatedAll };
        var rxEventData = new MinioNotificationRaw("");
        IDisposable disposable = null;
        var bucketName = GetRandomName(15);
        var suffix = ".json";
        var contentType = "application/json";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "contentType", contentType }, { "suffix", suffix }, { "size", "16B" }
            };

        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(bucketName);
            if (!await minio.BucketExistsAsync(bucketExistsArgs).ConfigureAwait(false))
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);
                await minio.MakeBucketAsync(makeBucketArgs).ConfigureAwait(false);
            }

            var notificationsArgs = new ListenBucketNotificationsArgs()
                .WithBucket(bucketName)
                .WithSuffix(suffix)
                .WithEvents(events);

            var modelJson = "{\"test3\": \"test3\"}";

            using var stream = Encoding.UTF8.GetBytes(modelJson).AsMemory().AsStream();
            var putObjectArgs = new PutObjectArgs()
                .WithObject("test3.json")
                .WithBucket(bucketName)
                .WithContentType(contentType)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length);

            Exception exception = null;
            var notifications = minio.ListenBucketNotificationsAsync(notificationsArgs);
            disposable = notifications.Subscribe(
                x => rxEventData = x,
                ex => exception = ex,
                () => { });

            // Sleep to give enough time for the subscriber to be ready
            var sleepTime = 1000; // Milliseconds
            await Task.Delay(sleepTime).ConfigureAwait(false);

            _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

            var stTime = DateTime.UtcNow;
            var waitTime = 25; // Milliseconds
            var timeout = 3000; // Milliseconds
            while (string.IsNullOrEmpty(rxEventData.Json))
            {
                await Task.Delay(waitTime).ConfigureAwait(false);
                if ((DateTime.UtcNow - stTime).TotalMilliseconds >= timeout)
                    throw new TimeoutException("Timeout: while waiting for events");
            }

            if (!string.IsNullOrEmpty(rxEventData.Json))
            {
                var notification = JsonSerializer.Deserialize<MinioNotification>(rxEventData.Json);
                Assert.IsTrue(notification.Records[0].EventName
                    .Equals("s3:ObjectCreated:Put", StringComparison.OrdinalIgnoreCase));
                new MintLogger(nameof(ListenBucketNotificationsAsync_Test3),
                    listenBucketNotificationsSignature,
                    "Tests whether ListenBucketNotifications passes for no event processing",
                    TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
            }
            else if (exception is not null)
            {
                throw exception;
            }
            else
            {
                throw new InvalidDataException("Missed Event: Bucket notification failed.");
            }
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(ListenBucketNotificationsAsync_Test3),
                listenBucketNotificationsSignature,
                "Tests whether ListenBucketNotifications passes for no event processing",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            disposable?.Dispose();
        }
    }

    #endregion

    #region Make Bucket

    internal static async Task MakeBucket_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(60);
        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName);
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var rbArgs = new RemoveBucketArgs()
            .WithBucket(bucketName);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "region", "us-east-1" } };

        try
        {
            await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            var found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsTrue(found);
            new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
        }
    }

    internal static async Task MakeBucket_Test2(IMinioClient minio, bool aws = false)
    {
        if (!aws)
            return;
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(10) + ".withperiod";
        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName);
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var rbArgs = new RemoveBucketArgs()
            .WithBucket(bucketName);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "region", "us-east-1" } };
        var testType = "Test whether make bucket passes when bucketname has a period.";

        try
        {
            await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            var found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsTrue(found);
            new MintLogger(nameof(MakeBucket_Test2), makeBucketSignature, testType, TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(MakeBucket_Test2), makeBucketSignature, testType, TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
        }
    }

    internal static async Task MakeBucket_Test3(IMinioClient minio, bool aws = false)
    {
        if (!aws)
            return;
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(60);
        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName)
            .WithLocation("eu-central-1");
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var rbArgs = new RemoveBucketArgs()
            .WithBucket(bucketName);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "region", "eu-central-1" } };
        try
        {
            await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            var found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsTrue(found);
            new MintLogger(nameof(MakeBucket_Test3), makeBucketSignature, "Tests whether MakeBucket with region passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(MakeBucket_Test3), makeBucketSignature, "Tests whether MakeBucket with region passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
        }
    }

    internal static async Task MakeBucket_Test4(IMinioClient minio, bool aws = false)
    {
        if (!aws)
            return;
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(20) + ".withperiod";
        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName)
            .WithLocation("us-west-2");
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var rbArgs = new RemoveBucketArgs()
            .WithBucket(bucketName);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "region", "us-west-2" } };
        try
        {
            await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            var found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsTrue(found);
            new MintLogger(nameof(MakeBucket_Test4), makeBucketSignature,
                "Tests whether MakeBucket with region and bucketname with . passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(MakeBucket_Test4), makeBucketSignature,
                "Tests whether MakeBucket with region and bucketname with . passes", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
        }
    }

    internal static async Task MakeBucket_Test5(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        string bucketName = null;
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "region", "us-east-1" } };

        try
        {
            _ = await Assert.ThrowsExceptionAsync<InvalidBucketNameException>(() =>
                minio.MakeBucketAsync(new MakeBucketArgs()
                    .WithBucket(bucketName))).ConfigureAwait(false);
            new MintLogger(nameof(MakeBucket_Test5), makeBucketSignature,
                "Tests whether MakeBucket throws InvalidBucketNameException when bucketName is null", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(MakeBucket_Test5), makeBucketSignature,
                "Tests whether MakeBucket throws InvalidBucketNameException when bucketName is null", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
    }

    internal static async Task MakeBucketLock_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(60);
        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName)
            .WithObjectLock();
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        var rbArgs = new RemoveBucketArgs()
            .WithBucket(bucketName);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "region", "us-east-1" } };

        try
        {
            await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            var found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            Assert.IsTrue(found);
            new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket with Lock passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket with Lock passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await minio.RemoveBucketAsync(rbArgs).ConfigureAwait(false);
        }
    }

    #endregion

    #region Put Object

    internal static async Task PutObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "application/octet-stream";
        var size = 1 * MB;
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "1MB" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var resp = await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null,
                rsg.GenerateStreamFromSeed(size)).ConfigureAwait(false);
            Assert.AreEqual(size, resp.Size);
            Assert.AreEqual(objectName, objectName);
            new MintLogger(nameof(PutObject_Test1), putObjectSignature,
                "Tests whether PutObject passes for small object", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(PutObject_Test1), putObjectSignature,
                "Tests whether PutObject passes for small object", TestStatus.FAIL, DateTime.Now - startTime, "",
                ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutObject_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "binary/octet-stream";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "6MB" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            _ = await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null,
                rsg.GenerateStreamFromSeed(6 * MB)).ConfigureAwait(false);
            new MintLogger(nameof(PutObject_Test2), putObjectSignature, "Tests whether multipart PutObject passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(PutObject_Test2), putObjectSignature, "Tests whether multipart PutObject passes",
                TestStatus.FAIL, DateTime.Now - startTime, "", ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutObject_Test3(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "custom-contenttype";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "1MB" }
            };

        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            _ = await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null,
                rsg.GenerateStreamFromSeed(1 * MB)).ConfigureAwait(false);
            new MintLogger(nameof(PutObject_Test3), putObjectSignature,
                "Tests whether PutObject with custom content-type passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(PutObject_Test3), putObjectSignature,
                "Tests whether PutObject with custom content-type passes", TestStatus.FAIL, DateTime.Now - startTime,
                "", ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutObject_Test4(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var fileName = CreateFile(1, dataFile1B);
        var contentType = "custom/contenttype";
        var metaData = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "customheader", "minio   dotnet" } };
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "1B" },
                { "size", "1B" },
                { "metaData", "customheader:minio-dotnet" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var statObject =
                await PutObject_Tester(minio, bucketName, objectName, fileName, contentType, metaData: metaData)
                    .ConfigureAwait(false);
            Assert.IsTrue(statObject is not null);
            Assert.IsTrue(statObject.MetaData is not null);
            var statMeta = new Dictionary<string, string>(statObject.MetaData, StringComparer.OrdinalIgnoreCase);
            Assert.IsTrue(statMeta.ContainsKey("Customheader"));
            Assert.IsTrue(statObject.MetaData.ContainsKey("Content-Type") &&
                          statObject.MetaData["Content-Type"]
                              .Equals("custom/contenttype", StringComparison.OrdinalIgnoreCase));
            new MintLogger(nameof(PutObject_Test4), putObjectSignature,
                "Tests whether PutObject with different content-type and custom header passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(PutObject_Test4), putObjectSignature,
                "Tests whether PutObject with different content-type and custom header passes", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            if (!IsMintEnv()) File.Delete(fileName);
        }
    }

    internal static async Task PutObject_Test5(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectName }, { "data", "1B" }, { "size", "1B" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            _ = await PutObject_Tester(minio, bucketName, objectName, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(1))
                .ConfigureAwait(false);
            new MintLogger(nameof(PutObject_Test5), putObjectSignature,
                "Tests whether PutObject with no content-type passes for small object", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(PutObject_Test5), putObjectSignature,
                "Tests whether PutObject with no content-type passes for small object", TestStatus.FAIL,
                DateTime.Now - startTime, "", ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutObject_Test7(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "application/octet-stream";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "10KB" },
                { "size", "-1" }
            };
        try
        {
            // Putobject call with unknown stream size. See if PutObjectAsync call succeeds
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(10 * KB))
            {
                long size = -1;
                var file_write_size = filestream.Length;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(size)
                    .WithContentType(contentType);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                var rmArgs = new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);
                await minio.RemoveObjectAsync(rmArgs).ConfigureAwait(false);
            }

            new MintLogger(nameof(PutObject_Test7), putObjectSignature,
                "Tests whether PutObject with unknown stream-size passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(PutObject_Test7), putObjectSignature,
                "Tests whether PutObject with unknown stream-size passes", TestStatus.FAIL, DateTime.Now - startTime,
                "", ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutObject_Test8(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "application/octet-stream";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "0B" },
                { "size", "-1" }
            };
        try
        {
            // Putobject call where unknown stream sent 0 bytes.
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(0))
            {
                long size = -1;
                var file_write_size = filestream.Length;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(size)
                    .WithContentType(contentType);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                var rmArgs = new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);
                await minio.RemoveObjectAsync(rmArgs).ConfigureAwait(false);
            }

            new MintLogger(nameof(PutObject_Test8), putObjectSignature,
                "Tests PutObject where unknown stream sends 0 bytes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(PutObject_Test8), putObjectSignature,
                "Tests PutObject where unknown stream sends 0 bytes", TestStatus.FAIL, DateTime.Now - startTime, "",
                ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutObject_Test9(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "application/octet-stream";
        var percentage = 0;
        var totalBytesTransferred = 0L;
        var progress = new Progress<ProgressReport>(progressReport =>
        {
            percentage = progressReport.Percentage;
            totalBytesTransferred = progressReport.TotalBytesTransferred;
        });
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "1MB" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            _ = await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null,
                rsg.GenerateStreamFromSeed(1 * MB), progress).ConfigureAwait(false);
            Assert.IsTrue(percentage == 100);
            Assert.IsTrue(totalBytesTransferred == 1 * MB);
            new MintLogger(nameof(PutObject_Test9), putObjectSignature,
                "Tests whether PutObject with progress passes for small object", TestStatus.PASS,
                DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(PutObject_Test9), putObjectSignature,
                "Tests whether PutObject with progress passes for small object", TestStatus.FAIL,
                DateTime.Now - startTime, "",
                ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PutObject_Test10(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "binary/octet-stream";
        var percentage = 0;
        var totalBytesTransferred = 0L;
        var progress = new Progress<ProgressReport>(progressReport =>
        {
            percentage = progressReport.Percentage;
            totalBytesTransferred = progressReport.TotalBytesTransferred;
            //Console.WriteLine(
            //    $"Percentage: {progressReport.Percentage}% TotalBytesTransferred: {progressReport.TotalBytesTransferred} bytes");
            //if (progressReport.Percentage != 100)
            //    Console.SetCursorPosition(0, Console.CursorTop - 1);
            //else Console.WriteLine();
        });
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "64MB" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            _ = await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null,
                rsg.GenerateStreamFromSeed(64 * MB), progress).ConfigureAwait(false);
            Assert.IsTrue(percentage == 100);
            Assert.IsTrue(totalBytesTransferred == 64 * MB);
            new MintLogger(nameof(PutObject_Test10), putObjectSignature,
                "Tests whether multipart PutObject with progress passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(PutObject_Test10), putObjectSignature,
                "Tests whether multipart PutObject with progress passes", TestStatus.FAIL, DateTime.Now - startTime, "",
                ex.Message, ex.ToString(), args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Copy Object

    internal static async Task CopyObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var outFileName = "outFileName-CopyObject_Test1";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);

            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                // .WithHeaders(null);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName);

            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithFile(outFileName);
            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            File.Delete(outFileName);
            var rmArgs1 = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await minio.RemoveObjectAsync(rmArgs1).ConfigureAwait(false);
            new MintLogger("CopyObject_Test1", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("CopyObject_Test1", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task CopyObject_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            // Test CopyConditions where matching ETag is not found
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);

            using var filestream = rsg.GenerateStreamFromSeed(1 * KB);
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(filestream)
                .WithObjectSize(filestream.Length)
                .WithHeaders(null);
            _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
            new MintLogger("CopyObject_Test2", copyObjectSignature,
                "Tests whether CopyObject with Etag mismatch passes", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var conditions = new CopyConditions();
            conditions.SetMatchETag("TestETag");
            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCopyConditions(conditions);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName);

            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);
        }
        catch (MinioException ex)
        {
            if (ex.Message.Contains(
                    "MinIO API responded with message=At least one of the pre-conditions you specified did not hold",
                    StringComparison.OrdinalIgnoreCase))
            {
                new MintLogger(nameof(CopyObject_Test2), copyObjectSignature,
                    "Tests whether CopyObject with Etag mismatch passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args).Log();
            }
            else
            {
                new MintLogger(nameof(CopyObject_Test2), copyObjectSignature,
                    "Tests whether CopyObject with Etag mismatch passes", TestStatus.FAIL, DateTime.Now - startTime,
                    ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(CopyObject_Test2), copyObjectSignature,
                "Tests whether CopyObject with Etag mismatch passes", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task CopyObject_Test3(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var outFileName = "outFileName-CopyObject_Test3";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            // Test CopyConditions where matching ETag is found
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);

            var conditions = new CopyConditions();
            conditions.SetMatchETag(stats.ETag);
            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCopyConditions(conditions);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName);

            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithFile(outFileName);
            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            statObjectArgs = new StatObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName);
            var dstats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            Assert.IsNotNull(dstats);
            Assert.IsTrue(dstats.ObjectName.Contains(destObjectName, StringComparison.Ordinal));
            new MintLogger("CopyObject_Test3", copyObjectSignature, "Tests whether CopyObject with Etag match passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("CopyObject_Test3", copyObjectSignature, "Tests whether CopyObject with Etag match passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task CopyObject_Test4(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var outFileName = "outFileName-CopyObject_Test4";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            // Test if objectName is defaulted to source objectName
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);

            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var conditions = new CopyConditions();
            conditions.SetMatchETag("TestETag");
            // omit dest bucket name.
            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName);

            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFile(outFileName);
            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.ObjectName.Contains(objectName, StringComparison.Ordinal));
            new MintLogger("CopyObject_Test4", copyObjectSignature,
                "Tests whether CopyObject defaults targetName to objectName", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("CopyObject_Test4", copyObjectSignature,
                "Tests whether CopyObject defaults targetName to objectName", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task CopyObject_Test5(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "6MB" },
                { "size", "6MB" }
            };
        try
        {
            // Test if multi-part copy upload for large files works as expected.
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(6 * MB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var conditions = new CopyConditions();
            conditions.SetByteRange(1024, 6291455);

            // omit dest object name.
            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCopyConditions(conditions);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName);

            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.ObjectName.Contains(objectName, StringComparison.Ordinal));
            Assert.AreEqual(6291455 - 1024 + 1, stats.Size);
            new MintLogger("CopyObject_Test5", copyObjectSignature,
                "Tests whether CopyObject  multi-part copy upload for large files works", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger("CopyObject_Test5", copyObjectSignature,
                "Tests whether CopyObject  multi-part copy upload for large files works", TestStatus.NA,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("CopyObject_Test5", copyObjectSignature,
                "Tests whether CopyObject multi-part copy upload for large files works", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            // File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task CopyObject_Test6(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var outFileName = "outFileName-CopyObject_Test6";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            // Test CopyConditions where matching ETag is found
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);

            var conditions = new CopyConditions();
            conditions.SetModified(new DateTime(2017, 8, 18));
            // Should copy object since modification date header < object modification date.
            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCopyConditions(conditions);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName);

            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithFile(outFileName);
            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            statObjectArgs = new StatObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName);
            var dstats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            Assert.IsNotNull(dstats);
            Assert.IsTrue(dstats.ObjectName.Contains(destObjectName, StringComparison.Ordinal));
            new MintLogger("CopyObject_Test6", copyObjectSignature,
                "Tests whether CopyObject with positive test for modified date passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("CopyObject_Test6", copyObjectSignature,
                "Tests whether CopyObject with positive test for modified date passes", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task CopyObject_Test7(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            // Test CopyConditions where matching ETag is found
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);

            var conditions = new CopyConditions();
            var modifiedDate = DateTime.Now;
            modifiedDate = modifiedDate.AddDays(5);
            conditions.SetModified(modifiedDate);
            // Should not copy object since modification date header > object modification date.
            try
            {
                var copySourceObjectArgs = new CopySourceObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithCopyConditions(conditions);
                var copyObjectArgs = new CopyObjectArgs()
                    .WithCopyObjectSource(copySourceObjectArgs)
                    .WithBucket(destBucketName)
                    .WithObject(destObjectName);
                await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(
                    "MinIO API responded with message=At least one of the pre-conditions you specified did not hold",
                    ex.Message);
            }

            new MintLogger("CopyObject_Test7", copyObjectSignature,
                "Tests whether CopyObject with negative test for modified date passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("CopyObject_Test7", copyObjectSignature,
                "Tests whether CopyObject with negative test for modified date passes", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task CopyObject_Test8(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
                { "copyconditions", "x-amz-metadata-directive:REPLACE" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithHeaders(new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        { "Orig", "orig-val with  spaces" }
                    });
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);

            Assert.IsTrue(stats.MetaData["Orig"] is not null);

            var copyCond = new CopyConditions();
            copyCond.SetReplaceMetadataDirective();

            // set custom metadata
            var customMetadata = new Dictionary<string, string>
                (StringComparer.Ordinal)
                {
                    { "Content-Type", "application/css" },
                    { "Mynewkey", "test   test" },
                    { "Orig", "orig-valwithoutspaces" }
                };
            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCopyConditions(copyCond);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithHeaders(customMetadata);

            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);

            statObjectArgs = new StatObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName);
            var dstats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            Assert.IsTrue(dstats.MetaData["Content-Type"] is not null);
            Assert.IsTrue(dstats.MetaData["Mynewkey"] is not null);
            Assert.IsTrue(dstats.MetaData["Content-Type"]
                .Contains("application/css", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(dstats.MetaData["Mynewkey"].Contains("test   test", StringComparison.Ordinal));
            new MintLogger("CopyObject_Test8", copyObjectSignature,
                "Tests whether CopyObject with metadata replacement passes", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("CopyObject_Test8", copyObjectSignature,
                "Tests whether CopyObject with metadata replacement passes", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task CopyObject_Test9(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var outFileName = "outFileName-CopyObject_Test9";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);

            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);

                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                var putTags = new Dictionary<string, string>
                    (StringComparer.Ordinal) { { "key1", "PutObjectTags" } };
                var setObjectTagsArgs = new SetObjectTagsArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithTagging(Tagging.GetObjectTags(putTags));
                await minio.SetObjectTagsAsync(setObjectTagsArgs).ConfigureAwait(false);
            }

            var copyTags = new Dictionary<string, string>
                (StringComparer.Ordinal) { { "key1", "CopyObjectTags" } };
            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            // CopyObject test to replace original tags
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithTagging(Tagging.GetObjectTags(copyTags))
                .WithReplaceTagsDirective(true);
            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);

            var getObjectTagsArgs = new GetObjectTagsArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName);
            var tags = await minio.GetObjectTagsAsync(getObjectTagsArgs).ConfigureAwait(false);
            Assert.IsNotNull(tags);
            var copiedTags = tags.Tags;
            Assert.IsNotNull(tags);
            Assert.IsNotNull(copiedTags);
            Assert.IsTrue(copiedTags.Count > 0);
            Assert.IsNotNull(copiedTags["key1"]);
            Assert.IsTrue(copiedTags["key1"].Contains("CopyObjectTags", StringComparison.Ordinal));
            new MintLogger("CopyObject_Test9", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("CopyObject_Test9", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Encrypted Copy Object

    internal static async Task EncryptedCopyObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var outFileName = "outFileName-EncryptedCopyObject_Test1";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            // Test Copy with SSE-C -> SSE-C encryption
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);
            using var aesEncryption = Aes.Create();
            aesEncryption.KeySize = 256;
            aesEncryption.GenerateKey();
            var ssec = new SSEC(aesEncryption.Key);
            var sseCpy = new SSECopy(aesEncryption.Key);
            using var destAesEncryption = Aes.Create();
            destAesEncryption.KeySize = 256;
            destAesEncryption.GenerateKey();
            var ssecDst = new SSEC(destAesEncryption.Key);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithServerSideEncryption(ssec);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithServerSideEncryption(sseCpy);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithServerSideEncryption(ssecDst);
            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithServerSideEncryption(ssecDst)
                .WithFile(outFileName);
            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            new MintLogger("EncryptedCopyObject_Test1", copyObjectSignature,
                    "Tests whether encrypted CopyObject passes", TestStatus.PASS, DateTime.Now - startTime, args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger("EncryptedCopyObject_Test1", copyObjectSignature,
                "Tests whether encrypted CopyObject passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("EncryptedCopyObject_Test1", copyObjectSignature,
                "Tests whether encrypted CopyObject passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task EncryptedCopyObject_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var outFileName = "outFileName-EncryptedCopyObject_Test2";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            // Test Copy of SSE-C encrypted object to unencrypted on destination side
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);
            using var aesEncryption = Aes.Create();
            aesEncryption.KeySize = 256;
            aesEncryption.GenerateKey();
            var ssec = new SSEC(aesEncryption.Key);
            var sseCpy = new SSECopy(aesEncryption.Key);

            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithServerSideEncryption(ssec);

                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithServerSideEncryption(sseCpy);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithServerSideEncryption(null);
            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithFile(outFileName);
            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            new MintLogger("EncryptedCopyObject_Test2", copyObjectSignature,
                    "Tests whether encrypted CopyObject passes", TestStatus.PASS, DateTime.Now - startTime, args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger("EncryptedCopyObject_Test2", copyObjectSignature,
                "Tests whether encrypted CopyObject passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("EncryptedCopyObject_Test2", copyObjectSignature,
                "Tests whether encrypted CopyObject passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task EncryptedCopyObject_Test3(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var outFileName = "outFileName-EncryptedCopyObject_Test3";
        var args = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "bucketName", bucketName },
            { "objectName", objectName },
            { "destBucketName", destBucketName },
            { "destObjectName", destObjectName },
            { "data", "1KB" },
            { "size", "1KB" }
        };
        try
        {
            // Test Copy of SSE-C encrypted object to unencrypted on destination side
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);
            using var aesEncryption = Aes.Create();
            aesEncryption.KeySize = 256;
            aesEncryption.GenerateKey();
            var ssec = new SSEC(aesEncryption.Key);
            var sseCpy = new SSECopy(aesEncryption.Key);
            var sses3 = new SSES3();

            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithServerSideEncryption(ssec);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithServerSideEncryption(sseCpy);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithServerSideEncryption(sses3);
            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithFile(outFileName);
            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            new MintLogger("EncryptedCopyObject_Test3", copyObjectSignature,
                    "Tests whether encrypted CopyObject passes", TestStatus.PASS, DateTime.Now - startTime, args: args)
                .Log();
        }
        catch (Exception ex)
        {
            new MintLogger("EncryptedCopyObject_Test3", copyObjectSignature,
                "Tests whether encrypted CopyObject passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    internal static async Task EncryptedCopyObject_Test4(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var destBucketName = GetRandomName(15);
        var destObjectName = GetRandomName(10);
        var outFileName = "outFileName-EncryptedCopyObject_Test4";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" }
            };
        try
        {
            // Test Copy of SSE-S3 encrypted object to SSE-S3 on destination side
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            await Setup_Test(minio, destBucketName).ConfigureAwait(false);

            var sses3 = new SSES3();
            var sseDest = new SSES3();
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithServerSideEncryption(sses3);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithServerSideEncryption(null);
            var copyObjectArgs = new CopyObjectArgs()
                .WithCopyObjectSource(copySourceObjectArgs)
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithServerSideEncryption(sses3);
            await minio.CopyObjectAsync(copyObjectArgs).ConfigureAwait(false);

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(destBucketName)
                .WithObject(destObjectName)
                .WithFile(outFileName);
            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            new MintLogger("EncryptedCopyObject_Test4", copyObjectSignature,
                    "Tests whether encrypted CopyObject passes", TestStatus.PASS, DateTime.Now - startTime, args: args)
                .Log();
        }
        catch (Exception ex)
        {
            new MintLogger("EncryptedCopyObject_Test4", copyObjectSignature,
                "Tests whether encrypted CopyObject passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
            await TearDown(minio, destBucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Get Object

    internal static async Task GetObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        string contentType = null;
        var tempFileName = "tempFile-" + GetRandomName();
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectName }, { "contentType", contentType }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);

            using (var filestream = rsg.GenerateStreamFromSeed(1 * MB))
            {
                var file_write_size = filestream.Length;
                long file_read_size = 0;
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithContentType(contentType);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream(async (stream, cancellationToken) =>
                    {
                        var fileStream = File.Create(tempFileName);

                        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                        await fileStream.DisposeAsync().ConfigureAwait(false);

                        var writtenInfo = new FileInfo(tempFileName);
                        file_read_size = writtenInfo.Length;

                        Assert.AreEqual(file_write_size, file_read_size);
                        File.Delete(tempFileName);
                    });
                _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            }

            await Task.Delay(1000).ConfigureAwait(false);
            new MintLogger("GetObject_Test1", getObjectSignature, "Tests whether GetObject as stream works",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("GetObject_Test1", getObjectSignature, "Tests whether GetObject as stream works",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            if (File.Exists(tempFileName))
                File.Delete(tempFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task GetObject_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var fileName = GetRandomName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectName }, { "fileName", fileName }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using var filestream = rsg.GenerateStreamFromSeed(1 * MB);
            // long file_write_size = filestream.Length;
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(filestream)
                .WithObjectSize(filestream.Length);

            _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            new MintLogger("GetObject_Test2", getObjectSignature, "Test setup for GetObject with a filename",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            await TearDown(minio, bucketName).ConfigureAwait(false);
            throw;
        }

        try
        {
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFile(fileName);

            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            Assert.IsTrue(File.Exists(fileName));
            new MintLogger("GetObject_Test2", getObjectSignature, "Tests whether GetObject with a file name works",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (ObjectNotFoundException)
        {
            new MintLogger("GetObject_Test2", getObjectSignature, "Tests for GetObject with a file name",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (InvalidOperationException ex)
        {
            new MintLogger("GetObject_Test2", getObjectSignature, "Tests for GetObject with a file name",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        catch (Exception ex)
        {
            new MintLogger("GetObject_Test2", getObjectSignature, "Tests for GetObject with a file name",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task GetObject_3_OffsetLength_Tests(IMinioClient minio)
        // 3 tests will run to check different values of offset and length parameters
        // when GetObject api returns part of the object as defined by the offset
        // and length parameters. Tests will be reported as GetObject_Test3,
        // GetObject_Test4 and GetObject_Test5.
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        string contentType = null;
        var tempFileName = "tempFile-" + GetRandomName();
        var tempSource = "tempSourceFile-" + GetRandomName();
        var offsetLengthTests = new Dictionary<string, List<int>>
            (StringComparer.Ordinal)
            {
                // list is {offset, length} values
                { "GetObject_Test3", new List<int> { 14, 20 } },
                { "GetObject_Test4", new List<int> { 30, 0 } },
                { "GetObject_Test5", new List<int> { 0, 25 } }
            };
        foreach (var test in offsetLengthTests)
        {
            var testName = test.Key;
            var offsetToStartFrom = test.Value[0];
            var lengthToBeRead = test.Value[1];
            var args = new Dictionary<string, string>
                (StringComparer.Ordinal)
                {
                    { "bucketName", bucketName },
                    { "objectName", objectName },
                    { "contentType", contentType },
                    { "offset", offsetToStartFrom.ToString(CultureInfo.InvariantCulture) },
                    { "length", lengthToBeRead.ToString(CultureInfo.InvariantCulture) }
                };
            try
            {
                await Setup_Test(minio, bucketName).ConfigureAwait(false);

                // Create a file with distintc byte characters to test partial
                // get object.
                var line = new[] { "abcdefghijklmnopqrstuvwxyz0123456789" };
                //   abcdefghijklmnopqrstuvwxyz0123456789
                //   012345678911234567892123456789312345
                //   ^1stChr, ^10thChr, ^20thChr, ^30th ^35thChr => characters' sequence
                // Example: offset 10 and length 4, the expected size and content
                // getObjectAsync will return are 4 and "klmn" respectively.

#if NETFRAMEWORK
                File.WriteAllLines(tempSource, line);
#else
                await File.WriteAllLinesAsync(tempSource, line).ConfigureAwait(false);
#endif
                using (var filestream = File.Open(tempSource, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var objectSize = (int)filestream.Length;
                    var expectedFileSize = lengthToBeRead;
                    var expectedContent = string.Concat(line).Substring(offsetToStartFrom, expectedFileSize);
                    if (lengthToBeRead == 0)
                    {
                        expectedFileSize = objectSize - offsetToStartFrom;
                        var noOfCtrlChars = 1;
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) noOfCtrlChars = 2;

                        expectedContent = string.Concat(line)
                            .Substring(offsetToStartFrom, expectedFileSize - noOfCtrlChars);
                    }

                    long actualFileSize;
                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithStreamData(filestream)
                        .WithObjectSize(objectSize)
                        .WithContentType(contentType);
                    await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

                    var getObjectArgs = new GetObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithOffsetAndLength(offsetToStartFrom, lengthToBeRead)
                        .WithCallbackStream(async (stream, cancellationToken) =>
                        {
                            var fileStream = File.Create(tempFileName);

                            await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                            await fileStream.DisposeAsync().ConfigureAwait(false);

                            var writtenInfo = new FileInfo(tempFileName);
                            actualFileSize = writtenInfo.Length;

                            Assert.AreEqual(expectedFileSize, actualFileSize);

                            // Checking the content
#if NETFRAMEWORK
                            var actualContent = File.ReadAllText(tempFileName);
#else
                            var actualContent = await File.ReadAllTextAsync(tempFileName, cancellationToken)
                                .ConfigureAwait(false);
#endif

                            actualContent = actualContent.Replace("\n", "", StringComparison.Ordinal)
                                .Replace("\r", "", StringComparison.Ordinal);
                            Assert.AreEqual(actualContent, expectedContent);
                        });

                    await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
                }

                new MintLogger(testName, getObjectSignature, "Tests whether GetObject returns all the data",
                    TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(testName, getObjectSignature, "Tests whether GetObject returns all the data",
                    TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                if (File.Exists(tempFileName)) File.Delete(tempFileName);
                if (File.Exists(tempSource)) File.Delete(tempSource);
                await TearDown(minio, bucketName).ConfigureAwait(false);
            }
        }
    }

#if NET6_0_OR_GREATER
    internal static async Task GetObject_AsyncCallback_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        string contentType = null;
        var fileName = GetRandomName(10);
        var destFileName = GetRandomName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectName }, { "contentType", contentType }
            };

        try
        {
            // Create a local 500MB file
            GenerateRandom500MB_File(fileName);

            // Create the bucket
            await Setup_Test(minio, bucketName).ConfigureAwait(false);

            using var file = File.OpenHandle(fileName);
            using var filestream = new FileStream(file, FileAccess.Read);
            // Upload the large file, "fileName", into the bucket
            var size = filestream.Length;
            long file_read_size = 0;
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(filestream)
                .WithObjectSize(filestream.Length)
                .WithContentType(contentType);

            _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

            async Task callbackAsync(Stream stream, CancellationToken cancellationToken)
            {
                var dest = new FileStream(destFileName, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(dest, cancellationToken).ConfigureAwait(false);
                await dest.DisposeAsync().ConfigureAwait(false);
            }

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(async (stream, cancellationToken) =>
                    await callbackAsync(stream, cancellationToken).ConfigureAwait(false));

            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            var writtenInfo = new FileInfo(destFileName);
            file_read_size = writtenInfo.Length;
            Assert.AreEqual(size, file_read_size);

            new MintLogger("GetObject_AsyncCallback_Test1", getObjectSignature,
                "Tests whether GetObject as stream works",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("GetObject_AsyncCallback_Test1", getObjectSignature,
                "Tests whether GetObject as stream works",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
            if (File.Exists(destFileName))
                File.Delete(destFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }
#endif

    internal static async Task FGetObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var outFileName = "outFileName-FGetObject_Test1";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectName }, { "fileName", outFileName }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFile(outFileName);
            _ = await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
            new MintLogger("FGetObject_Test1", getObjectSignature, "Tests whether FGetObject passes for small upload",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("FGetObject_Test1", getObjectSignature, "Tests whether FGetObject passes for small upload",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(outFileName);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region List Objects

    internal static async Task ListObjects_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var prefix = "minix";
        var objectName = prefix + GetRandomName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "prefix", prefix },
                { "recursive", "false" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var tasks = new Task[2];
            for (var i = 0; i < 2; i++)
                tasks[i] = PutObject_Task(minio, bucketName, objectName + i, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(1));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await ListObjects_Test(minio, bucketName, prefix, 2, false).ConfigureAwait(false);
            await Task.Delay(2000).ConfigureAwait(false);
            new MintLogger("ListObjects_Test1", listObjectsSignature,
                "Tests whether ListObjects lists all objects matching a prefix non-recursive", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListObjects_Test1", listObjectsSignature,
                "Tests whether ListObjects lists all objects matching a prefix non-recursive", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListObjects_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);

            await ListObjects_Test(minio, bucketName, null, 0).ConfigureAwait(false);
            await Task.Delay(2000).ConfigureAwait(false);
            new MintLogger("ListObjects_Test2", listObjectsSignature,
                "Tests whether ListObjects passes when bucket is empty", TestStatus.PASS, DateTime.Now - startTime,
                args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListObjects_Test2", listObjectsSignature,
                "Tests whether ListObjects passes when bucket is empty", TestStatus.FAIL, DateTime.Now - startTime,
                ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListObjects_Test3(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var prefix = "minix";
        var objectName = prefix + "/" + GetRandomName(10) + "/suffix";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "prefix", prefix },
                { "recursive", "true" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var tasks = new Task[2];
            for (var i = 0; i < 2; i++)
                tasks[i] = PutObject_Task(minio, bucketName, objectName + i, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(1 * KB));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await ListObjects_Test(minio, bucketName, prefix, 2).ConfigureAwait(false);
            await Task.Delay(2000).ConfigureAwait(false);
            new MintLogger("ListObjects_Test3", listObjectsSignature,
                "Tests whether ListObjects lists all objects matching a prefix and recursive", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListObjects_Test3", listObjectsSignature,
                "Tests whether ListObjects lists all objects matching a prefix and recursive", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListObjects_Test4(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectName }, { "recursive", "false" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var tasks = new Task[2];
            for (var i = 0; i < 2; i++)
                tasks[i] = PutObject_Task(minio, bucketName, objectName + i, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(1 * KB));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await ListObjects_Test(minio, bucketName, "", 2, false).ConfigureAwait(false);
            await Task.Delay(2000).ConfigureAwait(false);
            new MintLogger("ListObjects_Test4", listObjectsSignature,
                "Tests whether ListObjects lists all objects when no prefix is specified", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListObjects_Test4", listObjectsSignature,
                "Tests whether ListObjects lists all objects when no prefix is specified", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListObjects_Test5(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectNamePrefix = GetRandomName(10);
        var numObjects = 100;
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectNamePrefix }, { "recursive", "false" }
            };
        var objectNames = new List<string>();
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var tasks = new Task[numObjects];
            for (var i = 1; i <= numObjects; i++)
            {
                var objName = objectNamePrefix + i;
                tasks[i - 1] = PutObject_Task(minio, bucketName, objName, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(1));
                objectNames.Add(objName);
                // Add sleep to avoid flooding server with concurrent requests
                if (i % 50 == 0)
                    await Task.Delay(2000).ConfigureAwait(false);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await ListObjects_Test(minio, bucketName, objectNamePrefix, numObjects, false).ConfigureAwait(false);
            await Task.Delay(5000).ConfigureAwait(false);
            new MintLogger("ListObjects_Test5", listObjectsSignature,
                "Tests whether ListObjects lists all objects when number of objects == 100", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListObjects_Test5", listObjectsSignature,
                "Tests whether ListObjects lists all objects when number of objects == 100", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListObjects_Test6(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectNamePrefix = GetRandomName(10);
        var numObjects = 1015;
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectName", objectNamePrefix }, { "recursive", "false" }
            };
        var objectNamesSet = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var tasks = new Task[numObjects];
            for (var i = 1; i <= numObjects; i++)
            {
                var obj = objectNamePrefix + i;
                tasks[i - 1] = PutObject_Task(minio, bucketName, obj, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(1));
                // Add sleep to avoid flooding server with concurrent requests
                if (i % 25 == 0)
                    await Task.Delay(2000).ConfigureAwait(false);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            var count = 0;
            var listArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithPrefix(objectNamePrefix)
                .WithRecursive(false)
                .WithVersions(false);
            var observable = minio.ListObjectsAsync(listArgs);
            var subscription = observable.Subscribe(
                item =>
                {
                    Assert.IsTrue(item.Key.StartsWith(objectNamePrefix, StringComparison.OrdinalIgnoreCase));
                    if (!objectNamesSet.Add(item.Key))
                        new MintLogger("ListObjects_Test6", listObjectsSignature,
                            "Tests whether ListObjects lists more than 1000 objects correctly(max-keys = 1000)",
                            TestStatus.FAIL, DateTime.Now - startTime,
                            "Failed to add. Object already exists: " + item.Key, "", args: args).Log();

                    count++;
                },
                ex => throw ex,
                () => Assert.AreEqual(count, numObjects));
            await Task.Delay(3500).ConfigureAwait(false);
            new MintLogger("ListObjects_Test6", listObjectsSignature,
                "Tests whether ListObjects lists more than 1000 objects correctly(max-keys = 1000)", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListObjects_Test6", listObjectsSignature,
                "Tests whether ListObjects lists more than 1000 objects correctly(max-keys = 1000)", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListObjectVersions_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var prefix = "minix";
        var objectName = prefix + GetRandomName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "prefix", prefix },
                { "recursive", "false" },
                { "versions", "true" }
            };
        var objectVersions = new List<Tuple<string, string>>();
        try
        {
            await Setup_WithLock_Test(minio, bucketName).ConfigureAwait(false);
            var tasks = new Task[8];
            for (int i = 0, taskIdx = 0; i < 4; i++)
            {
                tasks[taskIdx++] = PutObject_Task(minio, bucketName, objectName + i, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(1));
                tasks[taskIdx++] = PutObject_Task(minio, bucketName, objectName + i, null, null, 0, null,
                    rsg.GenerateStreamFromSeed(1));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await ListObjects_Test(minio, bucketName, prefix, 2, false, true).ConfigureAwait(false);

            await Task.Delay(2000).ConfigureAwait(false);
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithRecursive(true)
                .WithVersions(true);
            var count = 0;
            var numObjectVersions = 8;

            var observable = minio.ListObjectsAsync(listObjectsArgs);
            var subscription = observable.Subscribe(
                item =>
                {
                    Assert.IsTrue(item.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    count++;
                    objectVersions.Add(new Tuple<string, string>(item.Key, item.VersionId));
                },
                ex => throw ex,
                () => Assert.AreEqual(count, numObjectVersions));

            await Task.Delay(4000).ConfigureAwait(false);
            new MintLogger("ListObjectVersions_Test1", listObjectsSignature,
                "Tests whether ListObjects with versions lists all objects along with all version ids for each object matching a prefix non-recursive",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListObjectVersions_Test1", listObjectsSignature,
                "Tests whether ListObjects with versions lists all objects along with all version ids for each object matching a prefix non-recursive",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListObjects_Test(IMinioClient minio, string bucketName, string prefix, int numObjects,
        bool recursive = true, bool versions = false, Dictionary<string, string> headers = null)
    {
        var startTime = DateTime.Now;
        var count = 0;
        var args = new ListObjectsArgs()
            .WithBucket(bucketName)
            .WithPrefix(prefix)
            .WithHeaders(headers)
            .WithRecursive(recursive)
            .WithVersions(versions);
        if (!versions)
        {
            var observable = minio.ListObjectsAsync(args);
            var subscription = observable.Subscribe(
                item =>
                {
                    if (!string.IsNullOrEmpty(prefix))
                        Assert.IsTrue(item.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    count++;
                },
                ex => throw ex,
                () => { });
        }
        else
        {
            var observable = minio.ListObjectsAsync(args);
            var subscription = observable.Subscribe(
                item =>
                {
                    Assert.IsTrue(item.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    count++;
                },
                ex => throw ex,
                () => { });
        }

        await Task.Delay(20000).ConfigureAwait(false);
        Assert.AreEqual(numObjects, count);
    }

    #endregion

    #region Presigned Get Object

    internal static async Task PresignedGetObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var expiresInt = 1000;
        var downloadFile = GetRandomObjectName(10);

        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString(CultureInfo.InvariantCulture) }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);

                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);

            var preArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expiresInt);
            var presigned_url = await minio.PresignedGetObjectAsync(preArgs).ConfigureAwait(false);

            await DownloadObjectAsync(minio, presigned_url, downloadFile).ConfigureAwait(false);
            var writtenInfo = new FileInfo(downloadFile);
            var file_read_size = writtenInfo.Length;
            // Compare the size of the file downloaded using the generated
            // presigned_url (expected value) and the actual object size on the server
            Assert.AreEqual(file_read_size, stats.Size);
            new MintLogger("PresignedGetObject_Test1", presignedGetObjectSignature,
                "Tests whether PresignedGetObject url retrieves object from bucket", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("PresignedGetObject_Test1", presignedGetObjectSignature,
                "Tests whether PresignedGetObject url retrieves object from bucket", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(downloadFile);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PresignedGetObject_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var expiresInt = 0;
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString(CultureInfo.InvariantCulture) }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);

                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            var preArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(0);
            var presigned_url = await minio.PresignedGetObjectAsync(preArgs).ConfigureAwait(false);
            throw new InvalidOperationException(
                "PresignedGetObjectAsync expected to throw an InvalidExpiryRangeException.");
        }
        catch (InvalidExpiryRangeException)
        {
            new MintLogger("PresignedGetObject_Test2", presignedGetObjectSignature,
                "Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (InvalidOperationException ex)
        {
            new MintLogger("PresignedGetObject_Test2", presignedGetObjectSignature,
                "Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        catch (Exception ex)
        {
            new MintLogger("PresignedGetObject_Test2", presignedGetObjectSignature,
                "Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task PresignedGetObject_Test3(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var expiresInt = 1000;
        var reqDate = DateTime.UtcNow.AddSeconds(-50);
        var downloadFile = GetRandomObjectName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString(CultureInfo.InvariantCulture) },
                {
                    "reqParams",
                    "response-content-type:application/json,response-content-disposition:attachment;filename=  MyDoc u m  e   nt.json ;"
                },
                { "reqDate", reqDate.ToString(CultureInfo.InvariantCulture) }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);

                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            var reqParams = new Dictionary<string, string>
                (StringComparer.Ordinal)
                {
                    ["response-content-type"] = "application/json",
                    ["response-content-disposition"] = "attachment;filename=  MyDoc u m  e   nt.json ;"
                };
            var preArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(1000)
                .WithHeaders(reqParams)
                .WithRequestDate(reqDate);
            var presigned_url = await minio.PresignedGetObjectAsync(preArgs).ConfigureAwait(false);

            using var response = await minio.WrapperGetAsync(presigned_url).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK ||
                string.IsNullOrEmpty(Convert.ToString(response.Content, CultureInfo.InvariantCulture)))
                throw new InvalidOperationException("Unable to download via presigned URL " + nameof(response.Content));

            Assert.IsTrue(response.Content.Headers.GetValues("Content-Type")
                .Contains(reqParams["response-content-type"], StringComparer.Ordinal));
            Assert.IsTrue(response.Content.Headers.GetValues("Content-Disposition")
                .Contains(reqParams["response-content-disposition"], StringComparer.Ordinal));
            Assert.IsTrue(response.Content.Headers.GetValues("Content-Length")
                .Contains(stats.Size.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal));

            using (var fs = new FileStream(downloadFile, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs).ConfigureAwait(false);
            }

            var writtenInfo = new FileInfo(downloadFile);
            var file_read_size = writtenInfo.Length;

            // Compare the size of the file downloaded with the generated
            // presigned_url (expected) and the actual object size on the server
            Assert.AreEqual(file_read_size, stats.Size);
            new MintLogger("PresignedGetObject_Test3", presignedGetObjectSignature,
                "Tests whether PresignedGetObject url retrieves object from bucket when override response headers sent",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("PresignedGetObject_Test3", presignedGetObjectSignature,
                "Tests whether PresignedGetObject url retrieves object from bucket when override response headers sent",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            File.Delete(downloadFile);
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Presigned Put Object

    internal static async Task PresignedPutObject_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var expiresInt = 1000;
        var fileName = CreateFile(10 * KB, dataFile10KB);

        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString(CultureInfo.InvariantCulture) }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            // Upload with presigned url
            var presignedPutObjectArgs = new PresignedPutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(1000);
            var presigned_url = await minio.PresignedPutObjectAsync(presignedPutObjectArgs).ConfigureAwait(false);
            await UploadObjectAsync(minio, presigned_url, fileName).ConfigureAwait(false);
            // Get stats for object from server
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            // Compare with file used for upload
            var writtenInfo = new FileInfo(fileName);
            var file_written_size = writtenInfo.Length;
            Assert.AreEqual(file_written_size, stats.Size);
            new MintLogger("PresignedPutObject_Test1", presignedPutObjectSignature,
                "Tests whether PresignedPutObject url uploads object to bucket", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("PresignedPutObject_Test1", presignedPutObjectSignature,
                "Tests whether PresignedPutObject url uploads object to bucket", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            if (!IsMintEnv()) File.Delete(fileName);
        }
    }

    internal static async Task PresignedPutObject_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var expiresInt = 0;

        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString(CultureInfo.InvariantCulture) }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);

                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var stats = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            var presignedPutObjectArgs = new PresignedPutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(0);
            var presigned_url = await minio.PresignedPutObjectAsync(presignedPutObjectArgs).ConfigureAwait(false);
            new MintLogger("PresignedPutObject_Test2", presignedPutObjectSignature,
                "Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (InvalidExpiryRangeException)
        {
            new MintLogger("PresignedPutObject_Test2", presignedPutObjectSignature,
                "Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("PresignedPutObject_Test2", presignedPutObjectSignature,
                "Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region List Incomplete Upload

    internal static async Task ListIncompleteUpload_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var contentType = "gzip";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "recursive", "true" } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(15));
            try
            {
                using var filestream = rsg.GenerateStreamFromSeed(50 * MB);
                var file_write_size = filestream.Length;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithContentType(contentType);
                _ = await minio.PutObjectAsync(putObjectArgs, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                var listArgs = new ListIncompleteUploadsArgs()
                    .WithBucket(bucketName);
                var observable = minio.ListIncompleteUploads(listArgs);

                var subscription = observable.Subscribe(
                    item => Assert.IsTrue(item.Key.Contains(objectName, StringComparison.Ordinal)),
                    ex => Assert.Fail());
            }
            catch (Exception ex)
            {
                new MintLogger("ListIncompleteUpload_Test1", listIncompleteUploadsSignature,
                    "Tests whether ListIncompleteUpload passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                    ex.ToString()).Log();
                return;
            }

            new MintLogger("ListIncompleteUpload_Test1", listIncompleteUploadsSignature,
                "Tests whether ListIncompleteUpload passes", TestStatus.PASS, DateTime.Now - startTime).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListIncompleteUpload_Test1", listIncompleteUploadsSignature,
                "Tests whether ListIncompleteUpload passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString()).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListIncompleteUpload_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var prefix = "minioprefix/";
        var objectName = prefix + GetRandomName(10);
        var contentType = "gzip";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "prefix", prefix }, { "recursive", "false" } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(15));
            try
            {
                using var filestream = rsg.GenerateStreamFromSeed(50 * MB);
                var file_write_size = filestream.Length;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithContentType(contentType);
                _ = await minio.PutObjectAsync(putObjectArgs, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                var listArgs = new ListIncompleteUploadsArgs()
                    .WithBucket(bucketName)
                    .WithPrefix("minioprefix")
                    .WithRecursive(false);
                var observable = minio.ListIncompleteUploads(listArgs);

                var subscription = observable.Subscribe(
                    item => Assert.AreEqual(item.Key, objectName),
                    ex => Assert.Fail());
            }

            new MintLogger("ListIncompleteUpload_Test2", listIncompleteUploadsSignature,
                "Tests whether ListIncompleteUpload passes when qualified by prefix", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListIncompleteUpload_Test2", listIncompleteUploadsSignature,
                "Tests whether ListIncompleteUpload passes when qualified by prefix", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task ListIncompleteUpload_Test3(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var prefix = "minioprefix";
        var objectName = prefix + "/" + GetRandomName(10) + "/suffix";
        var contentType = "gzip";
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName }, { "prefix", prefix }, { "recursive", "true" } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(15));
            try
            {
                using var filestream = rsg.GenerateStreamFromSeed(100 * MB);
                var file_write_size = filestream.Length;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length)
                    .WithContentType(contentType);
                _ = await minio.PutObjectAsync(putObjectArgs, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                var listArgs = new ListIncompleteUploadsArgs()
                    .WithBucket(bucketName)
                    .WithPrefix(prefix)
                    .WithRecursive(true);
                var observable = minio.ListIncompleteUploads(listArgs);

                var subscription = observable.Subscribe(
                    item => Assert.AreEqual(item.Key, objectName),
                    ex => Assert.Fail());
            }

            new MintLogger("ListIncompleteUpload_Test3", listIncompleteUploadsSignature,
                "Tests whether ListIncompleteUpload passes when qualified by prefix and recursive", TestStatus.PASS,
                DateTime.Now - startTime, args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("ListIncompleteUpload_Test3", listIncompleteUploadsSignature,
                "Tests whether ListIncompleteUpload passes when qualified by prefix and recursive", TestStatus.FAIL,
                DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Bucket Policy

    /// <summary>
    ///     Set a policy for given bucket
    /// </summary>
    /// <param name="minio"></param>
    /// <returns></returns>
    internal static async Task SetBucketPolicy_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal)
            {
                { "bucketName", bucketName }, { "objectPrefix", objectName[5..] }, { "policyType", "readonly" }
            };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var policyJson =
                $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}/foo*"",""arn:aws:s3:::{bucketName}/prefix/*""],""Sid"":""""}}]}}";
            var setPolicyArgs = new SetPolicyArgs()
                .WithBucket(bucketName)
                .WithPolicy(policyJson);

            await minio.SetPolicyAsync(setPolicyArgs).ConfigureAwait(false);
            new MintLogger("SetBucketPolicy_Test1", setBucketPolicySignature, "Tests whether SetBucketPolicy passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger("SetBucketPolicy_Test1", setBucketPolicySignature, "Tests whether SetBucketPolicy passes",
                TestStatus.NA, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("SetBucketPolicy_Test1", setBucketPolicySignature, "Tests whether SetBucketPolicy passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Get a policy for given bucket
    /// </summary>
    /// <param name="minio"></param>
    /// <returns></returns>
    internal static async Task GetBucketPolicy_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var objectName = GetRandomObjectName(10);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
            var policyJson =
                $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}/foo*"",""arn:aws:s3:::{bucketName}/prefix/*""],""Sid"":""""}}]}}";
            using (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                _ = await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var setPolicyArgs = new SetPolicyArgs()
                .WithBucket(bucketName)
                .WithPolicy(policyJson);
            var getPolicyArgs = new GetPolicyArgs()
                .WithBucket(bucketName);
            var rmPolicyArgs = new RemovePolicyArgs()
                .WithBucket(bucketName);
            await minio.SetPolicyAsync(setPolicyArgs).ConfigureAwait(false);
            var policy = await minio.GetPolicyAsync(getPolicyArgs).ConfigureAwait(false);
            await minio.RemovePolicyAsync(rmPolicyArgs).ConfigureAwait(false);
            new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes",
                TestStatus.NA, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion

    #region Bucket Lifecycle

    internal static async Task BucketLifecycleAsync_Test1(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".0", setBucketLifecycleSignature,
                "Tests whether SetBucketLifecycleAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        var rules = new List<LifecycleRule>();
        var baseDate = DateTime.Now;
        var expDate = baseDate.AddYears(1);
        var exp = new Expiration(expDate);

        var calculatedExpDate = expDate.AddDays(1).AddSeconds(-1).ToUniversalTime().Date;
        var expInDays = (calculatedExpDate.ToLocalTime().Date - baseDate.Date).TotalDays;

        var rule1 = new LifecycleRule(null, "txt", exp, null,
            new RuleFilter(null, "txt/", null), null, null, LifecycleRule.LifecycleRuleStatusEnabled);
        rules.Add(rule1);
        var lfc = new LifecycleConfiguration(rules);
        try
        {
            var lfcArgs = new SetBucketLifecycleArgs()
                .WithBucket(bucketName)
                .WithLifecycleConfiguration(lfc);
            await minio.SetBucketLifecycleAsync(lfcArgs).ConfigureAwait(false);
            new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".1", setBucketLifecycleSignature,
                    "Tests whether SetBucketLifecycleAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".1", setBucketLifecycleSignature,
                "Tests whether SetBucketLifecycleAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".1", setBucketLifecycleSignature,
                "Tests whether SetBucketLifecycleAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var lfcArgs = new GetBucketLifecycleArgs()
                .WithBucket(bucketName);
            var lfcObj = await minio.GetBucketLifecycleAsync(lfcArgs).ConfigureAwait(false);
            Assert.IsNotNull(lfcObj);
            Assert.IsNotNull(lfcObj.Rules);
            Assert.IsTrue(lfcObj.Rules.Count > 0);
            Assert.AreEqual(lfcObj.Rules.Count, lfc.Rules.Count);
            var lfcDate = DateTime.Parse(lfcObj.Rules[0].Expiration.ExpiryDate, null, DateTimeStyles.RoundtripKind);
            Assert.AreEqual((lfcDate.Date - baseDate.Date).TotalDays, expInDays);
            new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".2", getBucketLifecycleSignature,
                    "Tests whether GetBucketLifecycleAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".2", getBucketLifecycleSignature,
                "Tests whether GetBucketLifecycleAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".2", getBucketLifecycleSignature,
                "Tests whether GetBucketLifecycleAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var lfcArgs = new RemoveBucketLifecycleArgs()
                .WithBucket(bucketName);
            await minio.RemoveBucketLifecycleAsync(lfcArgs).ConfigureAwait(false);
            var getLifecycleArgs = new GetBucketLifecycleArgs()
                .WithBucket(bucketName);
            var lfcObj = await minio.GetBucketLifecycleAsync(getLifecycleArgs).ConfigureAwait(false);
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".3", deleteBucketLifecycleSignature,
                "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("The lifecycle configuration does not exist", StringComparison.OrdinalIgnoreCase))
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".3", deleteBucketLifecycleSignature,
                    "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args).Log();
            }
            else
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".3", deleteBucketLifecycleSignature,
                    "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.FAIL, DateTime.Now - startTime,
                    ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    internal static async Task BucketLifecycleAsync_Test2(IMinioClient minio)
    {
        var startTime = DateTime.Now;
        var bucketName = GetRandomName(15);
        var args = new Dictionary<string, string>
            (StringComparer.Ordinal) { { "bucketName", bucketName } };
        try
        {
            await Setup_Test(minio, bucketName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(BucketLifecycleAsync_Test2), setBucketLifecycleSignature,
                "Tests whether SetBucketLifecycleAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        var rules = new List<LifecycleRule>();
        var exp = new Expiration { Days = 30 };

        var rule1 = new LifecycleRule(null, "txt", exp, null,
            new RuleFilter(null, "txt/", null),
            null, null, LifecycleRule.LifecycleRuleStatusEnabled
        );
        rules.Add(rule1);
        var lfc = new LifecycleConfiguration(rules);
        try
        {
            var lfcArgs = new SetBucketLifecycleArgs()
                .WithBucket(bucketName)
                .WithLifecycleConfiguration(lfc);
            await minio.SetBucketLifecycleAsync(lfcArgs).ConfigureAwait(false);
            new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".1", setBucketLifecycleSignature,
                    "Tests whether SetBucketLifecycleAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".1", setBucketLifecycleSignature,
                "Tests whether SetBucketLifecycleAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".1", setBucketLifecycleSignature,
                "Tests whether SetBucketLifecycleAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var lfcArgs = new GetBucketLifecycleArgs()
                .WithBucket(bucketName);
            var lfcObj = await minio.GetBucketLifecycleAsync(lfcArgs).ConfigureAwait(false);
            Assert.IsNotNull(lfcObj);
            Assert.IsNotNull(lfcObj.Rules);
            Assert.IsTrue(lfcObj.Rules.Count > 0);
            Assert.AreEqual(lfcObj.Rules.Count, lfc.Rules.Count);
            new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".2", getBucketLifecycleSignature,
                    "Tests whether GetBucketLifecycleAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args)
                .Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".2", getBucketLifecycleSignature,
                "Tests whether GetBucketLifecycleAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
            new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".2", getBucketLifecycleSignature,
                "Tests whether GetBucketLifecycleAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
            throw;
        }

        try
        {
            var lfcArgs = new RemoveBucketLifecycleArgs()
                .WithBucket(bucketName);
            await minio.RemoveBucketLifecycleAsync(lfcArgs).ConfigureAwait(false);
            var getLifecycleArgs = new GetBucketLifecycleArgs()
                .WithBucket(bucketName);
            var lfcObj = await minio.GetBucketLifecycleAsync(getLifecycleArgs).ConfigureAwait(false);
        }
        catch (NotImplementedException ex)
        {
            new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".3", deleteBucketLifecycleSignature,
                "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.NA, DateTime.Now - startTime, ex.Message,
                ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("The lifecycle configuration does not exist", StringComparison.OrdinalIgnoreCase))
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".3", deleteBucketLifecycleSignature,
                    "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.PASS, DateTime.Now - startTime,
                    args: args).Log();
            }
            else
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".3", deleteBucketLifecycleSignature,
                    "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.FAIL, DateTime.Now - startTime,
                    ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

    #endregion
}
