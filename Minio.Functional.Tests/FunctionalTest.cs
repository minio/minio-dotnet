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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Web;
using System.ComponentModel;
using System.Linq;

using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using Minio.DataModel.ILM;
using Minio.DataModel.Tags;
using Minio.DataModel.ObjectLock;
using Minio.Exceptions;
using Newtonsoft.Json;

namespace Minio.Functional.Tests
{
    public class FunctionalTest
    {
        private static readonly Random rnd = new Random();
        private const int KB = 1024;
        private const int MB = 1024 * 1024;
        private const int GB = 1024 * 1024 * 1024;

        private const string dataFile1B = "datafile-1-b";

        private const string dataFile10KB = "datafile-10-kB";
        private const string dataFile6MB = "datafile-6-MB";

        private static RandomStreamGenerator rsg = new RandomStreamGenerator(100 * MB);
        private const string makeBucketSignature = "Task MakeBucketAsync(string bucketName, string location = 'us-east-1', CancellationToken cancellationToken = default(CancellationToken))";
        private const string listBucketsSignature = "Task<ListAllMyBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default(CancellationToken))";
        private const string bucketExistsSignature = "Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeBucketSignature = "Task RemoveBucketAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string listObjectsSignature = "IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = false, CancellationToken cancellationToken = default(CancellationToken))";
        private const string putObjectSignature = "Task PutObjectAsync(PutObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getObjectSignature = "Task GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string listIncompleteUploadsSignature = "IObservable<Upload> ListIncompleteUploads(ListIncompleteUploads args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string listenBucketNotificationsSignature = "IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(ListenBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string copyObjectSignature = "Task<CopyObjectResult> CopyObjectAsync(CopyObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string statObjectSignature = "Task<ObjectStat> StatObjectAsync(StatObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeObjectSignature1 = "Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeObjectSignature2 = "Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeIncompleteUploadSignature = "Task RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string presignedPutObjectSignature = "Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args)";
        private const string presignedGetObjectSignature = "Task<string> PresignedGetObjectAsync(PresignedGetObjectArgs args)";
        private const string presignedPostPolicySignature = "Task<Dictionary<string, string>> PresignedPostPolicyAsync(PresignedPostPolicyArgs args)";
        private const string getBucketPolicySignature = "Task<string> GetPolicyAsync(GetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setBucketPolicySignature = "Task SetPolicyAsync(SetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getBucketNotificationSignature = "Task<BucketNotification> GetBucketNotificationAsync(GetBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setBucketNotificationSignature = "Task SetBucketNotificationAsync(SetBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeAllBucketsNotificationSignature = "Task RemoveAllBucketNotificationsAsync(RemoveAllBucketNotifications args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setBucketEncryptionSignature = "Task SetBucketEncryptionAsync(SetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getBucketEncryptionSignature = "Task<ServerSideEncryptionConfiguration> GetBucketEncryptionAsync(GetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeBucketEncryptionSignature = "Task RemoveBucketEncryptionAsync(RemoveBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string selectObjectSignature = "Task<SelectResponseStream> SelectObjectContentAsync(SelectObjectContentArgs args,CancellationToken cancellationToken = default(CancellationToken))";
        private const string setObjectLegalHoldSignature = "Task SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getObjectLegalHoldSignature = "Task<bool> GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setObjectLockConfigurationSignature = "Task SetObjectLockConfigurationAsync(SetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getObjectLockConfigurationSignature = "Task<ObjectLockConfiguration> GetObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string deleteObjectLockConfigurationSignature = "Task RemoveObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getBucketTagsSignature = "Task<Tagging> GetBucketTagsAsync(GetBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setBucketTagsSignature = "Task SetBucketTagsAsync(SetBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string deleteBucketTagsSignature = "Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setVersioningSignature = "Task SetVersioningAsync(SetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getVersioningSignature = "Task<VersioningConfiguration> GetVersioningAsync(GetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeVersioningSignature = "Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getObjectTagsSignature = "Task<Tagging> GetObjectTagsAsync(GetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setObjectTagsSignature = "Task SetObjectTagsAsync(SetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string deleteObjectTagsSignature = "Task RemoveObjectTagsAsync(RemoveObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setObjectRetentionSignature = "Task SetObjectRetentionAsync(SetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getObjectRetentionSignature = "Task<ObjectRetentionConfiguration> GetObjectRetentionAsync(GetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string clearObjectRetentionSignature = "Task ClearObjectRetentionAsync(ClearObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getBucketLifecycleSignature = "Task<LifecycleConfiguration> GetBucketLifecycleAsync(GetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setBucketLifecycleSignature = "Task SetBucketLifecycleAsync(SetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))";
        private const string deleteBucketLifecycleSignature = "Task RemoveBucketLifecycleAsync(RemoveBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private static string Bash(string cmd)
        {
            var Replacements = new Dictionary<string, string>()
                                                { {"$", "\\$"}, {"(", "\\("},
                                                  {")", "\\)"}, {"{", "\\{"},
                                                  {"}", "\\}"}, {"[", "\\["},
                                                  {"]", "\\]"}, {"@", "\\@"},
                                                  {"%", "\\%"}, {"&", "\\&"},
                                                  {"#", "\\#"}, {"+", "\\+"} };

            foreach (string toReplace in Replacements.Keys)
            {
                cmd = cmd.Replace(toReplace, Replacements[toReplace]);
            }
            var cmdNoReturn = cmd + " >/dev/null 2>&1";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{cmdNoReturn}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadLine();
            process.WaitForExit();

            return result;
        }

        // Create a file of given size from random byte array or optionally create a symbolic link
        // to the dataFileName residing in MINT_DATA_DIR
        private static string CreateFile(int size, string dataFileName = null)
        {
            string fileName = GetRandomName();

            if (!IsMintEnv())
            {
                byte[] data = new byte[size];
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
            string characters = "abcd+%$#@&{}[]()";
            StringBuilder result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(characters[rnd.Next(characters.Length)]);
            }
            return result.ToString();
        }

        // Generate a random string
        public static string GetRandomName(int length = 5)
        {
            var characters = "0123456789abcdefghijklmnopqrstuvwxyz";
            if (length > 50)
            {
                length = 50;
            }

            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(characters[rnd.Next(characters.Length)]);
            }

            return "minio-dotnet-example-" + result.ToString();
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
            if (!string.IsNullOrEmpty(dataDir))
            {
                return $"{dataDir}/{fileName}";
            }

            string path = Directory.GetCurrentDirectory();
            return $"{path}/{fileName}";
        }

        internal static void RunCoreTests(MinioClient minioClient)
        {
            // Check if bucket exists
            BucketExists_Test(minioClient).Wait();

            // Create a new bucket
            MakeBucket_Test1(minioClient).Wait();
            PutObject_Test1(minioClient).Wait();
            PutObject_Test2(minioClient).Wait();
            ListObjects_Test1(minioClient).Wait();
            RemoveObject_Test1(minioClient).Wait();
            CopyObject_Test1(minioClient).Wait();

            // Test SetPolicyAsync function
            SetBucketPolicy_Test1(minioClient).Wait();

            // Test Presigned Get/Put operations
            PresignedGetObject_Test1(minioClient).Wait();
            PresignedPutObject_Test1(minioClient).Wait();

            // Test incomplete uploads
            ListIncompleteUpload_Test1(minioClient).Wait();
            RemoveIncompleteUpload_Test(minioClient).Wait();

            // Test GetBucket policy
            GetBucketPolicy_Test1(minioClient).Wait();
        }

        internal async static Task BucketExists_Test(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName();
            MakeBucketArgs mbArgs = new MakeBucketArgs()
                                                .WithBucket(bucketName);
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            RemoveBucketArgs rbArgs = new RemoveBucketArgs()
                                                .WithBucket(bucketName);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
            };

            try
            {
                await minio.MakeBucketAsync(mbArgs);
                bool found = await minio.BucketExistsAsync(beArgs);
                Assert.IsTrue(found);
                new MintLogger(nameof(BucketExists_Test), bucketExistsSignature, "Tests whether BucketExists passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketExists_Test), bucketExistsSignature, "Tests whether BucketExists passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(BucketExists_Test), bucketExistsSignature, "Tests whether BucketExists passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await minio.RemoveBucketAsync(rbArgs);
            }
        }

        #region Make Bucket

        internal async static Task MakeBucket_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 60);
            MakeBucketArgs mbArgs = new MakeBucketArgs()
                                                .WithBucket(bucketName);
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            RemoveBucketArgs rbArgs = new RemoveBucketArgs()
                                                .WithBucket(bucketName);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "us-east-1" },
            };

            try
            {
                await minio.MakeBucketAsync(mbArgs);
                bool found = await minio.BucketExistsAsync(beArgs);
                Assert.IsTrue(found);
                new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await minio.RemoveBucketAsync(rbArgs);
            }
        }

        internal async static Task MakeBucket_Test2(MinioClient minio, bool aws = false)
        {
            if (!aws)
                return;
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 10) + ".withperiod";
            MakeBucketArgs mbArgs = new MakeBucketArgs()
                                                .WithBucket(bucketName);
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            RemoveBucketArgs rbArgs = new RemoveBucketArgs()
                                                .WithBucket(bucketName);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "us-east-1" },
            };
            string testType = "Test whether make bucket passes when bucketname has a period.";

            try
            {
                await minio.MakeBucketAsync(mbArgs);
                bool found = await minio.BucketExistsAsync(beArgs);
                Assert.IsTrue(found);
                new MintLogger(nameof(MakeBucket_Test2), makeBucketSignature, testType, TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(MakeBucket_Test2), makeBucketSignature, testType, TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await minio.RemoveBucketAsync(rbArgs);
            }
        }

        internal async static Task MakeBucket_Test3(MinioClient minio, bool aws = false)
        {
            if (!aws)
                return;
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 60);
            MakeBucketArgs mbArgs = new MakeBucketArgs()
                                            .WithBucket(bucketName)
                                            .WithLocation("eu-central-1");
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            RemoveBucketArgs rbArgs = new RemoveBucketArgs()
                                                .WithBucket(bucketName);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "eu-central-1" },
            };
            try
            {
                await minio.MakeBucketAsync(mbArgs);
                bool found = await minio.BucketExistsAsync(beArgs);
                Assert.IsTrue(found);
                new MintLogger(nameof(MakeBucket_Test3), makeBucketSignature, "Tests whether MakeBucket with region passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();

            }
            catch (Exception ex)
            {
                new MintLogger(nameof(MakeBucket_Test3), makeBucketSignature, "Tests whether MakeBucket with region passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await minio.RemoveBucketAsync(rbArgs);
            }
        }

        internal async static Task MakeBucket_Test4(MinioClient minio, bool aws = false)
        {
            if (!aws)
                return;
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 20) + ".withperiod";
            MakeBucketArgs mbArgs = new MakeBucketArgs()
                                            .WithBucket(bucketName)
                                            .WithLocation("us-west-2");
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            RemoveBucketArgs rbArgs = new RemoveBucketArgs()
                                                .WithBucket(bucketName);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "us-west-2" },
            };
            try
            {
                await minio.MakeBucketAsync(mbArgs);
                bool found = await minio.BucketExistsAsync(beArgs);
                Assert.IsTrue(found);
                new MintLogger(nameof(MakeBucket_Test4), makeBucketSignature, "Tests whether MakeBucket with region and bucketname with . passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(MakeBucket_Test4), makeBucketSignature, "Tests whether MakeBucket with region and bucketname with . passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await minio.RemoveBucketAsync(rbArgs);
            }
        }

        internal async static Task MakeBucket_Test5(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = null;
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "us-east-1" },
            };

            try
            {
                await Assert.ThrowsExceptionAsync<InvalidBucketNameException>(() =>
                    minio.MakeBucketAsync(new MakeBucketArgs()
                                                .WithBucket(bucketName)));
                new MintLogger(nameof(MakeBucket_Test5), makeBucketSignature, "Tests whether MakeBucket throws InvalidBucketNameException when bucketName is null", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(MakeBucket_Test5), makeBucketSignature, "Tests whether MakeBucket throws InvalidBucketNameException when bucketName is null", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
        }

        internal async static Task MakeBucketLock_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 60);
            MakeBucketArgs mbArgs = new MakeBucketArgs()
                                                .WithBucket(bucketName)
                                                .WithObjectLock();
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            RemoveBucketArgs rbArgs = new RemoveBucketArgs()
                                                .WithBucket(bucketName);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "us-east-1" },
            };

            try
            {
                await minio.MakeBucketAsync(mbArgs);
                bool found = await minio.BucketExistsAsync(beArgs);
                Assert.IsTrue(found);
                new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket with Lock passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket with Lock passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await minio.RemoveBucketAsync(rbArgs);
            }
        }

        #endregion

        internal async static Task RemoveBucket_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 20);
            MakeBucketArgs mbArgs = new MakeBucketArgs()
                                                .WithBucket(bucketName);
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            RemoveBucketArgs rbArgs = new RemoveBucketArgs()
                                                .WithBucket(bucketName);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
            };

            bool found = false;
            try
            {
                await minio.MakeBucketAsync(mbArgs);
                found = await minio.BucketExistsAsync(beArgs);
                Assert.IsTrue(found);
                await minio.RemoveBucketAsync(rbArgs);
                found = await minio.BucketExistsAsync(beArgs);
                Assert.IsFalse(found);
                new MintLogger(nameof(RemoveBucket_Test1), removeBucketSignature, "Tests whether RemoveBucket passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(RemoveBucket_Test1), removeBucketSignature, "Tests whether RemoveBucket passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                if (found)
                    await minio.RemoveBucketAsync(rbArgs);
            }
        }

        internal async static Task ListBuckets_Test(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            var args = new Dictionary<string, string>();
            var bucketList = new List<Bucket>();
            string bucketName = "buucketnaame";
            int noOfBuckets = 5;
            try
            {
                foreach (int indx in Enumerable.Range(1, noOfBuckets))
                {
                    await Setup_Test(minio, bucketName + indx.ToString());
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("Bucket already owned by you"))
                {
                    // You have your bucket already created, continue
                }
                else
                    throw;
            }
            try
            {
                var list = await minio.ListBucketsAsync();
                bucketList = list.Buckets;
                bucketList = bucketList.Where(x => x.Name.StartsWith(bucketName)).ToList<Bucket>();
                Assert.AreEqual(noOfBuckets, bucketList.Count());
                bucketList.Sort(delegate (Bucket x, Bucket y)
                {
                    if (x.Name == y.Name) return 0;
                    else if (x.Name == null) return -1;
                    else if (y.Name == null) return 1;
                    else return x.Name.CompareTo(y.Name);
                });
                int indx = 0;
                foreach (Bucket bucket in bucketList)
                {
                    indx++;
                    Assert.AreEqual(bucketName + indx.ToString(), bucket.Name);
                    continue;
                }
                new MintLogger(nameof(ListBuckets_Test), listBucketsSignature, "Tests whether ListBucket passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ListBuckets_Test), listBucketsSignature, "Tests whether ListBucket passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                foreach (Bucket bucket in bucketList)
                {
                    RemoveBucketArgs rbArgs = new RemoveBucketArgs()
                                                        .WithBucket(bucket.Name);
                    await minio.RemoveBucketAsync(rbArgs);
                }

            }
        }

        internal async static Task Setup_Test(MinioClient minio, string bucketName)
        {
            MakeBucketArgs mbArgs = new MakeBucketArgs()
                                                .WithBucket(bucketName);
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            await minio.MakeBucketAsync(mbArgs);
            bool found = await minio.BucketExistsAsync(beArgs);
            Assert.IsTrue(found);
        }

        internal async static Task Setup_WithLock_Test(MinioClient minio, string bucketName)
        {
            MakeBucketArgs mbArgs = new MakeBucketArgs()
                                                .WithBucket(bucketName)
                                                .WithObjectLock();
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            await minio.MakeBucketAsync(mbArgs);
            bool found = await minio.BucketExistsAsync(beArgs);
            Assert.IsTrue(found);
        }

        internal async static Task TearDown(MinioClient minio, string bucketName)
        {
            BucketExistsArgs beArgs = new BucketExistsArgs()
                                                .WithBucket(bucketName);
            bool bktExists = await minio.BucketExistsAsync(beArgs);
            if (!bktExists)
                return;
            List<Task> taskList = new List<Task>();
            bool getVersions = false;
            // Get Versioning/Retention Info.
            GetObjectLockConfigurationArgs lockConfigurationArgs =
                                            new GetObjectLockConfigurationArgs()
                                                        .WithBucket(bucketName);
            ObjectLockConfiguration lockConfig = null;
            VersioningConfiguration versioningConfig = null;
            try
            {
                versioningConfig = await minio.GetVersioningAsync(new GetVersioningArgs()
                                                            .WithBucket(bucketName));
                if (versioningConfig != null && (versioningConfig.Status.Contains("Enabled") || versioningConfig.Status.Contains("Suspended")))
                {
                    getVersions = true;
                }
                lockConfig = await minio.GetObjectLockConfigurationAsync(lockConfigurationArgs);
            }
            catch (MissingObjectLockConfigurationException)
            {
                // This exception is expected for those buckets created without a lock.
            }
            catch (NotImplementedException)
            {
                // No throw. Move to the next step without versions.
            }
            catch (Exception)
            {
                throw;
            }
            List<Task> tasks = new List<Task>();
            ListObjectsArgs listObjectsArgs = new ListObjectsArgs()
                                                    .WithBucket(bucketName)
                                                    .WithRecursive(true)
                                                    .WithVersions(getVersions);
            List<Tuple<string, string>> objectNamesVersions =
                                            new List<Tuple<string, string>>();
            List<string> objectNames = new List<string>();
            IObservable<Item> observable = minio.ListObjectsAsync(listObjectsArgs);

            List<Exception> exceptionList = new List<Exception>();
            IDisposable subscription = observable.Subscribe(
                (item) =>
                {
                    if (getVersions)
                        objectNamesVersions.Add(new Tuple<string, string>(item.Key, item.VersionId));
                    else
                        objectNames.Add(item.Key);
                },
                ex =>
                {
                    // Collect all exceptions but the one raised because the bucket is empty
                    if (ex.GetType().ToString() != "Minio.EmptyBucketOperation")
                    {
                        exceptionList.Add(ex);
                        return;
                    }
                },
                () =>
                {
                    return;
                });
            System.Threading.Thread.Sleep(4500);
            if (lockConfig != null && lockConfig.ObjectLockEnabled.Equals(ObjectLockConfiguration.LockEnabled))
            {
                foreach (var item in objectNamesVersions)
                {
                    GetObjectRetentionArgs objectRetentionArgs = new GetObjectRetentionArgs()
                                                                                .WithBucket(bucketName)
                                                                                .WithObject(item.Item1)
                                                                                .WithVersionId(item.Item2);
                    ObjectRetentionConfiguration retentionConfig = await minio.GetObjectRetentionAsync(objectRetentionArgs);
                    bool bypassGovMode = (retentionConfig.Mode == RetentionMode.GOVERNANCE) ? true : false;
                    RemoveObjectArgs removeObjectArgs = new RemoveObjectArgs()
                                                                    .WithBucket(bucketName)
                                                                    .WithObject(item.Item1)
                                                                    .WithVersionId(item.Item2);
                    if (bypassGovMode)
                        removeObjectArgs = removeObjectArgs.WithBypassGovernanceMode(bypassGovMode);
                    Task t = minio.RemoveObjectAsync(removeObjectArgs);
                    tasks.Add(t);
                }
            }
            else
            {
                if (objectNamesVersions.Count > 0)
                {
                    RemoveObjectsArgs removeObjectArgs = new RemoveObjectsArgs()
                                                                    .WithBucket(bucketName)
                                                                    .WithObjectsVersions(objectNamesVersions);
                    Task t = minio.RemoveObjectsAsync(removeObjectArgs);
                    tasks.Add(t);
                }
                if (objectNames.Count > 0)
                {
                    RemoveObjectsArgs removeObjectArgs = new RemoveObjectsArgs()
                                                                    .WithBucket(bucketName)
                                                                    .WithObjects(objectNames);

                    Task t = minio.
                    RemoveObjectsAsync(removeObjectArgs);
                    tasks.Add(t);
                }
            }
            await Task.WhenAll(tasks);
            RemoveBucketArgs rbArgs = new RemoveBucketArgs()
                                                .WithBucket(bucketName);
            await minio.RemoveBucketAsync(rbArgs);
        }
        internal static string XmlStrToJsonStr(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            string json = JsonConvert.SerializeXmlNode(doc);
            return json;
        }

        #region Put Object

        internal async static Task PutObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "application/octet-stream";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "1MB" }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(1 * MB));
                new MintLogger(nameof(PutObject_Test1), putObjectSignature, "Tests whether PutObject passes for small object", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test1), putObjectSignature, "Tests whether PutObject passes for small object", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PutObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "binary/octet-stream";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "6MB" }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(6 * MB));
                new MintLogger(nameof(PutObject_Test2), putObjectSignature, "Tests whether multipart PutObject passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test2), putObjectSignature, "Tests whether multipart PutObject passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PutObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "custom-contenttype";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "1MB" }
            };

            try
            {
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(1 * MB));
                new MintLogger(nameof(PutObject_Test3), putObjectSignature, "Tests whether PutObject with custom content-type passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test3), putObjectSignature, "Tests whether PutObject with custom content-type passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PutObject_Test4(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string fileName = CreateFile(1, dataFile1B);
            string contentType = "custom/contenttype";
            var metaData = new Dictionary<string, string>
            {
                { "customheader", "minio   dotnet" }
            };
            var args = new Dictionary<string, string>
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
                await Setup_Test(minio, bucketName);
                ObjectStat statObject = await PutObject_Tester(minio, bucketName, objectName, fileName, contentType: contentType, metaData: metaData);
                Assert.IsTrue(statObject != null);
                Assert.IsTrue(statObject.MetaData != null);
                var statMeta = new Dictionary<string, string>(statObject.MetaData, StringComparer.OrdinalIgnoreCase);
                Assert.IsTrue(statMeta.ContainsKey("Customheader"));
                Assert.IsTrue(statObject.MetaData.ContainsKey("Content-Type") && statObject.MetaData["Content-Type"].Equals("custom/contenttype"));
                new MintLogger(nameof(PutObject_Test4), putObjectSignature, "Tests whether PutObject with different content-type and custom header passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test4), putObjectSignature, "Tests whether PutObject with different content-type and custom header passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
                if (!IsMintEnv())
                {
                    File.Delete(fileName);
                }
            }
        }

        internal async static Task PutObject_Test5(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "data", "1B" },
                { "size", "1B" },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, null, 0, null, rsg.GenerateStreamFromSeed(1));
                new MintLogger(nameof(PutObject_Test5), putObjectSignature, "Tests whether PutObject with no content-type passes for small object", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test5), putObjectSignature, "Tests whether PutObject with no content-type passes for small object", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PutObject_Test7(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "application/octet-stream";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "10KB" },
                { "size", "-1" },
            };
            try
            {
                // Putobject call with unknown stream size. See if PutObjectAsync call succeeds
                await Setup_Test(minio, bucketName);
                using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(10 * KB))
                {

                    long size = -1;
                    long file_write_size = filestream.Length;

                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(size)
                                                            .WithContentType(contentType);
                    await minio.PutObjectAsync(putObjectArgs);
                    RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                    await minio.RemoveObjectAsync(rmArgs);
                }
                new MintLogger(nameof(PutObject_Test7), putObjectSignature, "Tests whether PutObject with unknown stream-size passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test7), putObjectSignature, "Tests whether PutObject with unknown stream-size passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PutObject_Test8(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "application/octet-stream";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "0B" },
                { "size", "-1" },
            };
            try
            {
                // Putobject call where unknown stream sent 0 bytes.
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(0))
                {
                    long size = -1;
                    long file_write_size = filestream.Length;

                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(size)
                                                            .WithContentType(contentType);
                    await minio.PutObjectAsync(putObjectArgs);
                    RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                    await minio.RemoveObjectAsync(rmArgs);
                }
                new MintLogger(nameof(PutObject_Test8), putObjectSignature, "Tests PutObject where unknown stream sends 0 bytes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test8), putObjectSignature, "Tests PutObject where unknown stream sends 0 bytes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #endregion

        internal async static Task PutGetStatEncryptedObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "application/octet-stream";
            string tempFileName = "tempFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Putobject with SSE-C encryption.
                await Setup_Test(minio, bucketName);
                Aes aesEncryption = Aes.Create();
                aesEncryption.KeySize = 256;
                aesEncryption.GenerateKey();
                var ssec = new SSEC(aesEncryption.Key);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    long file_write_size = filestream.Length;

                    long file_read_size = 0;
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithServerSideEncryption(ssec)
                                                            .WithContentType(contentType);
                    await minio.PutObjectAsync(putObjectArgs);

                    GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithServerSideEncryption(ssec)
                                                            .WithCallbackStream((stream) =>
                                                                                {
                                                                                    var fileStream = File.Create(tempFileName);
                                                                                    stream.CopyTo(fileStream);
                                                                                    fileStream.Dispose();
                                                                                    FileInfo writtenInfo = new FileInfo(tempFileName);
                                                                                    file_read_size = writtenInfo.Length;

                                                                                    Assert.AreEqual(file_write_size, file_read_size);
                                                                                    File.Delete(tempFileName);
                                                                                });
                    StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithServerSideEncryption(ssec);
                    await minio.StatObjectAsync(statObjectArgs);
                    await minio.GetObjectAsync(getObjectArgs);
                }
                new MintLogger("PutGetStatEncryptedObject_Test1", putObjectSignature, "Tests whether Put/Get/Stat Object with encryption passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger("PutGetStatEncryptedObject_Test1", putObjectSignature, "Tests whether Put/Get/Stat Object with encryption passes", TestStatus.NA, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutGetStatEncryptedObject_Test1", putObjectSignature, "Tests whether Put/Get/Stat Object with encryption passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PutGetStatEncryptedObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "application/octet-stream";
            string tempFileName = "tempFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "6MB" },
                { "size", "6MB" },
            };
            try
            {
                // Test multipart Put with SSE-C encryption
                await Setup_Test(minio, bucketName);
                Aes aesEncryption = Aes.Create();
                aesEncryption.KeySize = 256;
                aesEncryption.GenerateKey();
                var ssec = new SSEC(aesEncryption.Key);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(6 * MB))
                {
                    long file_write_size = filestream.Length;

                    long file_read_size = 0;
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithContentType(contentType)
                                                            .WithServerSideEncryption(ssec);
                    await minio.PutObjectAsync(putObjectArgs);

                    GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithServerSideEncryption(ssec)
                                                            .WithCallbackStream((stream) =>
                                                                                {
                                                                                    var fileStream = File.Create(tempFileName);
                                                                                    stream.CopyTo(fileStream);
                                                                                    fileStream.Dispose();
                                                                                    FileInfo writtenInfo = new FileInfo(tempFileName);
                                                                                    file_read_size = writtenInfo.Length;

                                                                                    Assert.AreEqual(file_write_size, file_read_size);
                                                                                    File.Delete(tempFileName);
                                                                                });
                    StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithServerSideEncryption(ssec);
                    await minio.StatObjectAsync(statObjectArgs);
                    await minio.GetObjectAsync(getObjectArgs);
                }
                new MintLogger("PutGetStatEncryptedObject_Test2", putObjectSignature, "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger("PutGetStatEncryptedObject_Test2", putObjectSignature, "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.NA, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutGetStatEncryptedObject_Test2", putObjectSignature, "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
                throw;
            }
            finally
            {
                File.Delete(tempFileName);
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PutGetStatEncryptedObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "application/octet-stream";
            string tempFileName = "tempFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "6MB" },
                { "size", "6MB" },
            };
            try
            {
                // Test multipart Put/Get/Stat with SSE-S3 encryption
                await Setup_Test(minio, bucketName);
                Aes aesEncryption = Aes.Create();
                var sses3 = new SSES3();

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(6 * MB))
                {
                    long file_write_size = filestream.Length;
                    long file_read_size = 0;
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithServerSideEncryption(sses3)
                                                            .WithContentType(contentType);
                    await minio.PutObjectAsync(putObjectArgs);

                    GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithCallbackStream((stream) =>
                                                                                {
                                                                                    var fileStream = File.Create(tempFileName);
                                                                                    stream.CopyTo(fileStream);
                                                                                    fileStream.Dispose();
                                                                                    FileInfo writtenInfo = new FileInfo(tempFileName);
                                                                                    file_read_size = writtenInfo.Length;

                                                                                    Assert.AreEqual(file_write_size, file_read_size);
                                                                                    File.Delete(tempFileName);
                                                                                });
                    StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName);
                    await minio.StatObjectAsync(statObjectArgs);
                    await minio.GetObjectAsync(getObjectArgs);
                }
                new MintLogger("PutGetStatEncryptedObject_Test3", putObjectSignature, "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutGetStatEncryptedObject_Test3", putObjectSignature, "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PutObject_Task(MinioClient minio, string bucketName, string objectName, string fileName = null, string contentType = "application/octet-stream", long size = 0, Dictionary<string, string> metaData = null, MemoryStream mstream = null)
        {
            DateTime startTime = DateTime.Now;

            MemoryStream filestream = mstream;
            if (filestream == null)
            {
                byte[] bs = File.ReadAllBytes(fileName);
                filestream = new MemoryStream(bs);

            }
            using (filestream)
            {
                long file_write_size = filestream.Length;
                string tempFileName = "tempfile-" + GetRandomName(5);
                if (size == 0)
                    size = filestream.Length;

                PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithStreamData(filestream)
                                                        .WithObjectSize(size)
                                                        .WithContentType(contentType)
                                                        .WithHeaders(metaData);
                await minio.PutObjectAsync(putObjectArgs);
                File.Delete(tempFileName);
            }
        }

        internal async static Task<ObjectStat> PutObject_Tester(MinioClient minio,
            string bucketName, string objectName, string fileName = null,
            string contentType = "application/octet-stream", long size = 0,
            Dictionary<string, string> metaData = null, MemoryStream mstream = null)
        {
            ObjectStat statObject = null;
            DateTime startTime = DateTime.Now;

            MemoryStream filestream = mstream;
            if (filestream == null)
            {
                byte[] bs = File.ReadAllBytes(fileName);
                filestream = new MemoryStream(bs);
            }

            using (filestream)
            {
                long file_write_size = filestream.Length;
                string tempFileName = "tempfile-" + GetRandomName(5);
                if (size == 0)
                {
                    size = filestream.Length;
                }
                PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithStreamData(filestream)
                                                        .WithObjectSize(size)
                                                        .WithContentType(contentType)
                                                        .WithHeaders(metaData);
                await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);

                statObject = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
                Assert.IsNotNull(statObject);
                Assert.IsTrue(statObject.ObjectName.Equals(objectName));
                Assert.AreEqual(statObject.Size, size);

                if (contentType != null)
                {
                    Assert.IsNotNull(statObject.ContentType);
                    Assert.IsTrue(statObject.ContentType.Equals(contentType));
                }

                RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                                    .WithBucket(bucketName)
                                                    .WithObject(objectName);
                await minio.RemoveObjectAsync(rmArgs);
            }
            return statObject;
        }

        internal async static Task StatObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "gzip";

            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "data", "1KB" },
                { "size", "1KB" },
            };

            try
            {
                await Setup_Test(minio, bucketName).ConfigureAwait(false);
                await PutObject_Tester(minio, bucketName, objectName, null, null, 0, null, rsg.GenerateStreamFromSeed(1 * KB)).ConfigureAwait(false);
                new MintLogger(nameof(StatObject_Test1), statObjectSignature, "Tests whether StatObject passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(StatObject_Test1), statObjectSignature, "Tests whether statObjectSignature passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #region Copy Object

        internal async static Task CopyObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);
                    // .WithHeaders(null);
                    await minio.PutObjectAsync(putObjectArgs);
                }

                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName);

                await minio.CopyObjectAsync(copyObjectArgs);

                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(destObjectName)
                                                        .WithFile(outFileName);
                await minio.GetObjectAsync(getObjectArgs);
                File.Delete(outFileName);
                RemoveObjectArgs rmArgs1 = new RemoveObjectArgs()
                                                    .WithBucket(bucketName)
                                                    .WithObject(objectName);

                await minio.RemoveObjectAsync(rmArgs1);
                new MintLogger("CopyObject_Test1", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("CopyObject_Test1", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task CopyObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Test CopyConditions where matching ETag is not found
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithHeaders(null);
                    await minio.PutObjectAsync(putObjectArgs);
                }
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test2", copyObjectSignature, "Tests whether CopyObject with Etag mismatch passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }

            try
            {
                CopyConditions conditions = new CopyConditions();
                conditions.SetMatchETag("TestETag");
                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithCopyConditions(conditions);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName);

                await minio.CopyObjectAsync(copyObjectArgs);
            }
            catch (MinioException ex)
            {
                if (ex.Message.Contains("MinIO API responded with message=At least one of the pre-conditions you specified did not hold"))
                {
                    new MintLogger(nameof(CopyObject_Test2), copyObjectSignature, "Tests whether CopyObject with Etag mismatch passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
                }
                else
                {
                    new MintLogger(nameof(CopyObject_Test2), copyObjectSignature, "Tests whether CopyObject with Etag mismatch passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                    throw;
                }
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(CopyObject_Test2), copyObjectSignature, "Tests whether CopyObject with Etag mismatch passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task CopyObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Test CopyConditions where matching ETag is found
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);
                    await minio.PutObjectAsync(putObjectArgs);
                }
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);

                CopyConditions conditions = new CopyConditions();
                conditions.SetMatchETag(stats.ETag);
                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithCopyConditions(conditions);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName);

                await minio.CopyObjectAsync(copyObjectArgs);
                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(destObjectName)
                                                        .WithFile(outFileName);
                await minio.GetObjectAsync(getObjectArgs);
                statObjectArgs = new StatObjectArgs()
                                            .WithBucket(destBucketName)
                                            .WithObject(destObjectName);
                ObjectStat dstats = await minio.StatObjectAsync(statObjectArgs);
                Assert.IsNotNull(dstats);
                Assert.IsTrue(dstats.ObjectName.Contains(destObjectName));
                new MintLogger("CopyObject_Test3", copyObjectSignature, "Tests whether CopyObject with Etag match passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("CopyObject_Test3", copyObjectSignature, "Tests whether CopyObject with Etag match passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task CopyObject_Test4(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Test if objectName is defaulted to source objectName
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);
                    await minio.PutObjectAsync(putObjectArgs);
                }
                CopyConditions conditions = new CopyConditions();
                conditions.SetMatchETag("TestETag");
                // omit dest bucket name.
                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName);

                await minio.CopyObjectAsync(copyObjectArgs);

                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithFile(outFileName);
                await minio.GetObjectAsync(getObjectArgs);
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);
                Assert.IsNotNull(stats);
                Assert.IsTrue(stats.ObjectName.Contains(objectName));
                new MintLogger("CopyObject_Test4", copyObjectSignature, "Tests whether CopyObject defaults targetName to objectName", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("CopyObject_Test4", copyObjectSignature, "Tests whether CopyObject defaults targetName to objectName", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task CopyObject_Test5(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            // string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "6MB" },
                { "size", "6MB" },
            };
            try
            {
                // Test if multi-part copy upload for large files works as expected.
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(6 * MB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);
                    await minio.PutObjectAsync(putObjectArgs);
                }
                CopyConditions conditions = new CopyConditions();
                conditions.SetByteRange(1024, 6291455);

                // omit dest object name.
                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithCopyConditions(conditions);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName);

                await minio.CopyObjectAsync(copyObjectArgs);
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);
                Assert.IsNotNull(stats);
                Assert.IsTrue(stats.ObjectName.Contains(objectName));
                Assert.AreEqual(6291455 - 1024 + 1, stats.Size);
                new MintLogger("CopyObject_Test5", copyObjectSignature, "Tests whether CopyObject  multi-part copy upload for large files works", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger("CopyObject_Test5", copyObjectSignature, "Tests whether CopyObject  multi-part copy upload for large files works", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("CopyObject_Test5", copyObjectSignature, "Tests whether CopyObject multi-part copy upload for large files works", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                // File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task CopyObject_Test6(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Test CopyConditions where matching ETag is found
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);
                    await minio.PutObjectAsync(putObjectArgs);
                }
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);

                CopyConditions conditions = new CopyConditions();
                conditions.SetModified(new DateTime(2017, 8, 18));
                // Should copy object since modification date header < object modification date.
                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithCopyConditions(conditions);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName);

                await minio.CopyObjectAsync(copyObjectArgs);
                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(destObjectName)
                                                        .WithFile(outFileName);
                await minio.GetObjectAsync(getObjectArgs);
                statObjectArgs = new StatObjectArgs()
                                            .WithBucket(destBucketName)
                                            .WithObject(destObjectName);
                ObjectStat dstats = await minio.StatObjectAsync(statObjectArgs);
                Assert.IsNotNull(dstats);
                Assert.IsTrue(dstats.ObjectName.Contains(destObjectName));
                new MintLogger("CopyObject_Test6", copyObjectSignature, "Tests whether CopyObject with positive test for modified date passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("CopyObject_Test6", copyObjectSignature, "Tests whether CopyObject with positive test for modified date passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task CopyObject_Test7(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Test CopyConditions where matching ETag is found
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);
                    await minio.PutObjectAsync(putObjectArgs);
                }
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);

                CopyConditions conditions = new CopyConditions();
                DateTime modifiedDate = DateTime.Now;
                modifiedDate = modifiedDate.AddDays(5);
                conditions.SetModified(modifiedDate);
                // Should not copy object since modification date header > object modification date.
                try
                {
                    CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                            .WithBucket(bucketName)
                                                                            .WithObject(objectName)
                                                                            .WithCopyConditions(conditions);
                    CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                                .WithCopyObjectSource(copySourceObjectArgs)
                                                                .WithBucket(destBucketName)
                                                                .WithObject(destObjectName);
                    await minio.CopyObjectAsync(copyObjectArgs);
                }
                catch (Exception ex)
                {
                    Assert.AreEqual("MinIO API responded with message=At least one of the pre-conditions you specified did not hold", ex.Message);
                }
                new MintLogger("CopyObject_Test7", copyObjectSignature, "Tests whether CopyObject with negative test for modified date passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("CopyObject_Test7", copyObjectSignature, "Tests whether CopyObject with negative test for modified date passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        public static void objPrint(Object obj)
        {
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                string name = descriptor.Name;
                object value = descriptor.GetValue(obj);
                Console.WriteLine("{0}={1}", name, value);
            }
        }
        public static void Print(Object obj)
        {
            foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object value = prop.GetValue(obj, new object[] { });
                Console.WriteLine("{0} = {1}", prop.Name, value);
            }
            Console.WriteLine("DONE!\n\n");
        }

        public static void printDict(Dictionary<string, string> d)
        {
            if (d != null)
            {
                foreach (KeyValuePair<string, string> kv in d)
                {
                    Console.WriteLine("     {0} = {1}", kv.Key, kv.Value);
                }
            }
            Console.WriteLine("DONE!\n\n");
        }

        internal async static Task CopyObject_Test8(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
                { "copyconditions", "x-amz-metadata-directive:REPLACE" },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                .WithBucket(bucketName)
                                .WithObject(objectName)
                                .WithStreamData(filestream)
                                .WithObjectSize(filestream.Length)
                                .WithHeaders(new Dictionary<string, string> { { "Orig", "orig-val with  spaces" } });
                    await minio.PutObjectAsync(putObjectArgs);
                }
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);

                Assert.IsTrue(stats.MetaData["Orig"] != null);

                CopyConditions copyCond = new CopyConditions();
                copyCond.SetReplaceMetadataDirective();


                // set custom metadata
                var customMetadata = new Dictionary<string, string>
                {
                    { "Content-Type", "application/css" },
                    { "Mynewkey", "test   test" },
                    { "Orig", "orig-valwithoutspaces" }
                };
                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithCopyConditions(copyCond);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName)
                                                            .WithHeaders(customMetadata);

                await minio.CopyObjectAsync(copyObjectArgs);

                statObjectArgs = new StatObjectArgs()
                                            .WithBucket(destBucketName)
                                            .WithObject(destObjectName);
                ObjectStat dstats = await minio.StatObjectAsync(statObjectArgs);
                Assert.IsTrue(dstats.MetaData["Content-Type"] != null);
                Assert.IsTrue(dstats.MetaData["Mynewkey"] != null);
                Assert.IsTrue(dstats.MetaData["Content-Type"].Contains("application/css"));
                Assert.IsTrue(dstats.MetaData["Mynewkey"].Contains("test   test"));
                new MintLogger("CopyObject_Test8", copyObjectSignature, "Tests whether CopyObject with metadata replacement passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("CopyObject_Test8", copyObjectSignature, "Tests whether CopyObject with metadata replacement passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task CopyObject_Test9(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);

                    await minio.PutObjectAsync(putObjectArgs);
                    Dictionary<string, string> putTags = new Dictionary<string, string> {
                        {"key1", "PutObjectTags"}
                    };
                    SetObjectTagsArgs setObjectTagsArgs = new SetObjectTagsArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithTagging(Tagging.GetObjectTags(putTags));
                    await minio.SetObjectTagsAsync(setObjectTagsArgs);
                }

                Dictionary<string, string> copyTags = new Dictionary<string, string> {
                    {"key1", "CopyObjectTags"}
                };
                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName);
                // CopyObject test to replace original tags
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName)
                                                            .WithTagging(Tagging.GetObjectTags(copyTags))
                                                            .WithReplaceTagsDirective(true);
                await minio.CopyObjectAsync(copyObjectArgs);

                GetObjectTagsArgs getObjectTagsArgs = new GetObjectTagsArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(destObjectName);
                var tags = await minio.GetObjectTagsAsync(getObjectTagsArgs);
                Assert.IsNotNull(tags);
                var copiedTags = tags.GetTags();
                Assert.IsNotNull(tags);
                Assert.IsNotNull(copiedTags);
                Assert.IsTrue(copiedTags.Count > 0);
                Assert.IsNotNull(copiedTags["key1"]);
                Assert.IsTrue(copiedTags["key1"].Contains("CopyObjectTags"));
                new MintLogger("CopyObject_Test9", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("CopyObject_Test9", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        #endregion

        #region Encrypted Copy Object

        internal async static Task EncryptedCopyObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Test Copy with SSE-C -> SSE-C encryption
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                Aes aesEncryption = Aes.Create();
                aesEncryption.KeySize = 256;
                aesEncryption.GenerateKey();
                var ssec = new SSEC(aesEncryption.Key);
                var sseCpy = new SSECopy(aesEncryption.Key);
                Aes destAesEncryption = Aes.Create();
                destAesEncryption.KeySize = 256;
                destAesEncryption.GenerateKey();
                var ssecDst = new SSEC(destAesEncryption.Key);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithServerSideEncryption(ssec);
                    await minio.PutObjectAsync(putObjectArgs);
                }

                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithServerSideEncryption(sseCpy);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName)
                                                            .WithServerSideEncryption(ssecDst);
                await minio.CopyObjectAsync(copyObjectArgs);
                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(destObjectName)
                                                        .WithServerSideEncryption(ssecDst)
                                                        .WithFile(outFileName);
                await minio.GetObjectAsync(getObjectArgs);
                new MintLogger("EncryptedCopyObject_Test1", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger("EncryptedCopyObject_Test1", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("EncryptedCopyObject_Test1", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task EncryptedCopyObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Test Copy of SSE-C encrypted object to unencrypted on destination side
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                Aes aesEncryption = Aes.Create();
                aesEncryption.KeySize = 256;
                aesEncryption.GenerateKey();
                var ssec = new SSEC(aesEncryption.Key);
                var sseCpy = new SSECopy(aesEncryption.Key);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithServerSideEncryption(ssec);

                    await minio.PutObjectAsync(putObjectArgs);
                }

                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithServerSideEncryption(sseCpy);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName)
                                                            .WithServerSideEncryption(null);
                await minio.CopyObjectAsync(copyObjectArgs);

                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(destObjectName)
                                                        .WithFile(outFileName);
                await minio.GetObjectAsync(getObjectArgs);
                new MintLogger("EncryptedCopyObject_Test2", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger("EncryptedCopyObject_Test2", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("EncryptedCopyObject_Test2", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task EncryptedCopyObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Test Copy of SSE-C encrypted object to unencrypted on destination side
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                Aes aesEncryption = Aes.Create();
                aesEncryption.KeySize = 256;
                aesEncryption.GenerateKey();
                var ssec = new SSEC(aesEncryption.Key);
                var sseCpy = new SSECopy(aesEncryption.Key);
                var sses3 = new SSES3();

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithServerSideEncryption(ssec);
                    await minio.PutObjectAsync(putObjectArgs);
                }

                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithServerSideEncryption(sseCpy);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName)
                                                            .WithServerSideEncryption(sses3);
                await minio.CopyObjectAsync(copyObjectArgs);
                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(destObjectName)
                                                        .WithFile(outFileName);
                await minio.GetObjectAsync(getObjectArgs);
                new MintLogger("EncryptedCopyObject_Test3", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("EncryptedCopyObject_Test3", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        internal async static Task EncryptedCopyObject_Test4(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string destBucketName = GetRandomName(15);
            string destObjectName = GetRandomName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "destBucketName", destBucketName },
                { "destObjectName", destObjectName },
                { "data", "1KB" },
                { "size", "1KB" },
            };
            try
            {
                // Test Copy of SSE-S3 encrypted object to SSE-S3 on destination side
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);

                var sses3 = new SSES3();
                var sseDest = new SSES3();
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithServerSideEncryption(sses3);
                    await minio.PutObjectAsync(putObjectArgs);
                }

                CopySourceObjectArgs copySourceObjectArgs = new CopySourceObjectArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithServerSideEncryption(null);
                CopyObjectArgs copyObjectArgs = new CopyObjectArgs()
                                                            .WithCopyObjectSource(copySourceObjectArgs)
                                                            .WithBucket(destBucketName)
                                                            .WithObject(destObjectName)
                                                            .WithServerSideEncryption(sses3);
                await minio.CopyObjectAsync(copyObjectArgs);

                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(destBucketName)
                                                        .WithObject(destObjectName)
                                                        .WithFile(outFileName);
                await minio.GetObjectAsync(getObjectArgs);
                new MintLogger("EncryptedCopyObject_Test4", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("EncryptedCopyObject_Test4", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
            }
        }

        #endregion

        #region Get Object

        internal async static Task GetObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = null;
            string tempFileName = "tempFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
            };
            try
            {
                await Setup_Test(minio, bucketName);

                using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                {
                    long file_write_size = filestream.Length;
                    long file_read_size = 0;
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithContentType(contentType);
                    await minio.PutObjectAsync(putObjectArgs);

                    GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithCallbackStream((stream) =>
                                                                                {
                                                                                    var fileStream = File.Create(tempFileName);
                                                                                    stream.CopyTo(fileStream);
                                                                                    fileStream.Dispose();
                                                                                    FileInfo writtenInfo = new FileInfo(tempFileName);
                                                                                    file_read_size = writtenInfo.Length;

                                                                                    Assert.AreEqual(file_write_size, file_read_size);
                                                                                    File.Delete(tempFileName);
                                                                                });
                    await minio.GetObjectAsync(getObjectArgs);
                }
                System.Threading.Thread.Sleep(1000);
                new MintLogger("GetObject_Test1", getObjectSignature, "Tests whether GetObject as stream works", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("GetObject_Test1", getObjectSignature, "Tests whether GetObject as stream works", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                if (File.Exists(tempFileName))
                    File.Delete(tempFileName);
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task GetObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string fileName = GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "fileName", fileName },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                {
                    // long file_write_size = filestream.Length;
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);

                    await minio.PutObjectAsync(putObjectArgs);
                }

            }
            catch (Exception ex)
            {
                new MintLogger("GetObject_Test2", getObjectSignature, "Test setup for GetObject with a filename", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                await TearDown(minio, bucketName);
                throw;
            }

            try
            {
                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithFile(fileName);

                await minio.GetObjectAsync(getObjectArgs);
                Assert.IsTrue(File.Exists(fileName));
                new MintLogger("GetObject_Test2", getObjectSignature, "Tests whether GetObject with a file name works", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (ObjectNotFoundException)
            {
                new MintLogger("GetObject_Test2", getObjectSignature, "Tests for GetObject with a file name", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (InvalidOperationException ex)
            {
                new MintLogger("GetObject_Test2", getObjectSignature, "Tests for GetObject with a file name", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            catch (Exception ex)
            {
                new MintLogger("GetObject_Test2", getObjectSignature, "Tests for GetObject with a file name", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task GetObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = null;
            string tempFileName = "tempFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "1024L" },
                { "length", "10L" },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(10 * KB))
                {
                    long file_write_size = 10L;
                    long file_read_size = 0;
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithContentType(contentType);
                    await minio.PutObjectAsync(putObjectArgs);

                    GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithOffsetAndLength(1024L, file_write_size)
                                                            .WithCallbackStream((stream) =>
                                                                                {
                                                                                    var fileStream = File.Create(tempFileName);
                                                                                    stream.CopyTo(fileStream);
                                                                                    fileStream.Dispose();
                                                                                    FileInfo writtenInfo = new FileInfo(tempFileName);
                                                                                    file_read_size = writtenInfo.Length;

                                                                                    Assert.AreEqual(file_write_size, file_read_size);
                                                                                    File.Delete(tempFileName);
                                                                                });

                    await minio.GetObjectAsync(getObjectArgs);
                }
                new MintLogger("GetObject_Test3", getObjectSignature, "Tests whether GetObject returns all the data", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("GetObject_Test3", getObjectSignature, "Tests whether GetObject returns all the data", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                if (File.Exists(tempFileName))
                    File.Delete(tempFileName);
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task FGetObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "fileName", outFileName },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);
                    await minio.PutObjectAsync(putObjectArgs);

                }
                GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithFile(outFileName);
                await minio.GetObjectAsync(getObjectArgs);
                new MintLogger("FGetObject_Test1", getObjectSignature, "Tests whether FGetObject passes for small upload", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("FGetObject_Test1", getObjectSignature, "Tests whether FGetObject passes for small upload", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(outFileName);
                await TearDown(minio, bucketName);
            }
        }

        #endregion

        internal async static Task FPutObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string fileName = CreateFile(6 * MB, dataFile6MB);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "fileName", fileName },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithFileName(fileName);
                await minio.PutObjectAsync(putObjectArgs);
                new MintLogger("FPutObject_Test1", putObjectSignature, "Tests whether FPutObject for multipart upload passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("FPutObject_Test1", putObjectSignature, "Tests whether FPutObject for multipart upload passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
                if (!IsMintEnv())
                {
                    File.Delete(fileName);
                }
            }
        }

        internal async static Task FPutObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string fileName = CreateFile(10 * KB, dataFile10KB);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "fileName", fileName },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithFileName(fileName);
                await minio.PutObjectAsync(putObjectArgs);
                new MintLogger("FPutObject_Test2", putObjectSignature, "Tests whether FPutObject for small upload passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("FPutObject_Test2", putObjectSignature, "Tests whether FPutObject for small upload passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
                if (!IsMintEnv())
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    File.Delete(fileName);
                }
            }
        }

        #region List Objects

        internal async static Task ListObjects_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string prefix = "minix";
            string objectName = prefix + GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "prefix", prefix },
                { "recursive", "false" },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1));
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, prefix, 2, false);
                System.Threading.Thread.Sleep(2000);
                new MintLogger("ListObjects_Test1", listObjectsSignature, "Tests whether ListObjects lists all objects matching a prefix non-recursive", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListObjects_Test1", listObjectsSignature, "Tests whether ListObjects lists all objects matching a prefix non-recursive", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task ListObjects_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
            };
            try
            {
                await Setup_Test(minio, bucketName);

                ListObjects_Test(minio, bucketName, null, 0);
                System.Threading.Thread.Sleep(2000);
                new MintLogger("ListObjects_Test2", listObjectsSignature, "Tests whether ListObjects passes when bucket is empty", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListObjects_Test2", listObjectsSignature, "Tests whether ListObjects passes when bucket is empty", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task ListObjects_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string prefix = "minix";
            string objectName = prefix + "/" + GetRandomName(10) + "/suffix";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "prefix", prefix },
                { "recursive", "true" }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1 * KB));
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, prefix, 2, true);
                System.Threading.Thread.Sleep(2000);
                new MintLogger("ListObjects_Test3", listObjectsSignature, "Tests whether ListObjects lists all objects matching a prefix and recursive", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListObjects_Test3", listObjectsSignature, "Tests whether ListObjects lists all objects matching a prefix and recursive", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task ListObjects_Test4(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "recursive", "false" }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1 * KB));
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, "", 2, false);
                System.Threading.Thread.Sleep(2000);
                new MintLogger("ListObjects_Test4", listObjectsSignature, "Tests whether ListObjects lists all objects when no prefix is specified", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListObjects_Test4", listObjectsSignature, "Tests whether ListObjects lists all objects when no prefix is specified", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task ListObjects_Test5(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectNamePrefix = GetRandomName(10);
            int numObjects = 100;
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectNamePrefix },
                { "recursive", "false" }
            };
            List<string> objectNames = new List<string>();
            try
            {
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[numObjects];
                for (int i = 1; i <= numObjects; i++)
                {
                    string objName = objectNamePrefix + i.ToString();
                    tasks[i - 1] = PutObject_Task(minio, bucketName, objName, null, null, 0, null, rsg.GenerateStreamFromSeed(1));
                    objectNames.Add(objName);
                    // Add sleep to avoid flooding server with concurrent requests
                    if (i % 50 == 0)
                    {
                        System.Threading.Thread.Sleep(2000);
                    }
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, objectNamePrefix, numObjects, false);
                System.Threading.Thread.Sleep(5000);
                new MintLogger("ListObjects_Test5", listObjectsSignature, "Tests whether ListObjects lists all objects when number of objects == 100", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListObjects_Test5", listObjectsSignature, "Tests whether ListObjects lists all objects when number of objects == 100", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task ListObjects_Test6(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectNamePrefix = GetRandomName(10);
            int numObjects = 1015;
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectNamePrefix },
                { "recursive", "false" }
            };
            HashSet<string> objectNamesSet = new HashSet<string>();
            try
            {
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[numObjects];
                for (int i = 1; i <= numObjects; i++)
                {
                    string obj = objectNamePrefix + i.ToString();
                    tasks[i - 1] = PutObject_Task(minio, bucketName, obj, null, null, 0, null, rsg.GenerateStreamFromSeed(1));
                    // Add sleep to avoid flooding server with concurrent requests
                    if (i % 25 == 0)
                    {
                        System.Threading.Thread.Sleep(2000);
                    }
                }
                await Task.WhenAll(tasks);
                int count = 0;
                ListObjectsArgs listArgs = new ListObjectsArgs()
                                                    .WithBucket(bucketName)
                                                    .WithPrefix(objectNamePrefix)
                                                    .WithRecursive(false)
                                                    .WithVersions(false);
                IObservable<Item> observable = minio.ListObjectsAsync(listArgs);
                IDisposable subscription = observable.Subscribe(
                    item =>
                    {
                        Assert.IsTrue(item.Key.StartsWith(objectNamePrefix));
                        if (!objectNamesSet.Add(item.Key))
                        {
                            new MintLogger("ListObjects_Test6", listObjectsSignature, "Tests whether ListObjects lists more than 1000 objects correctly(max-keys = 1000)", TestStatus.FAIL, (DateTime.Now - startTime), "Failed to add. Object already exists: " + item.Key, "", args: args).Log();
                        }
                        count += 1;
                    },
                    ex => throw ex,
                    () =>
                    {
                        Assert.AreEqual(count, numObjects);
                    });
                System.Threading.Thread.Sleep(3500);
                new MintLogger("ListObjects_Test6", listObjectsSignature, "Tests whether ListObjects lists all objects when number of objects == 100", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListObjects_Test6", listObjectsSignature, "Tests whether ListObjects lists all objects when number of objects == 100", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task ListObjectVersions_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string prefix = "minix";
            string objectName = prefix + GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "prefix", prefix },
                { "recursive", "false" },
                { "versions", "true" }
            };
            List<Tuple<string, string>> objectVersions = new List<Tuple<string, string>>();
            try
            {
                await Setup_WithLock_Test(minio, bucketName);
                Task[] tasks = new Task[8];
                for (int i = 0, taskIdx = 0; i < 4; i++)
                {
                    tasks[taskIdx++] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1));
                    tasks[taskIdx++] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1));
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, prefix, 2, false, true);
                System.Threading.Thread.Sleep(2000);
                ListObjectsArgs listObjectsArgs = new ListObjectsArgs()
                                                            .WithBucket(bucketName)
                                                            .WithRecursive(true)
                                                            .WithVersions(true);
                int count = 0;
                int numObjectVersions = 8;

                IObservable<Item> observable = minio.ListObjectsAsync(listObjectsArgs);
                IDisposable subscription = observable.Subscribe(
                    item =>
                    {
                        Assert.IsTrue(item.Key.StartsWith(prefix));
                        count += 1;
                        objectVersions.Add(new Tuple<string, string>(item.Key, item.VersionId));
                    },
                    ex => throw ex,
                    () =>
                    {
                        Assert.AreEqual(count, numObjectVersions);
                    });

                System.Threading.Thread.Sleep(4000);
                new MintLogger("ListObjectVersions_Test1", listObjectsSignature, "Tests whether ListObjects with versions lists all objects along with all version ids for each object matching a prefix non-recursive", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListObjectVersions_Test1", listObjectsSignature, "Tests whether ListObjects with versions lists all objects along with all version ids for each object matching a prefix non-recursive", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }


        internal static void ListObjects_Test(MinioClient minio, string bucketName, string prefix, int numObjects, bool recursive = true, bool versions = false)
        {
            DateTime startTime = DateTime.Now;
            int count = 0;
            ListObjectsArgs args = new ListObjectsArgs()
                                            .WithBucket(bucketName)
                                            .WithPrefix(prefix)
                                            .WithRecursive(recursive)
                                            .WithVersions(versions);
            if (!versions)
            {
                IObservable<Item> observable = minio.ListObjectsAsync(args);
                IDisposable subscription = observable.Subscribe(
                    item =>
                    {
                        Assert.IsTrue(item.Key.StartsWith(prefix));
                        count += 1;
                    },
                    ex => throw ex,
                    () =>
                    {
                        Assert.AreEqual(count, numObjects);
                    });
                return;
            }
            else
            {
                IObservable<Item> observable = minio.ListObjectsAsync(args);
                IDisposable subscription = observable.Subscribe(
                    item =>
                    {
                        Assert.IsTrue(item.Key.StartsWith(prefix));
                        count += 1;
                    },
                    ex => throw ex,
                    () =>
                    {
                        Assert.AreEqual(count, numObjects);
                    });
            }
        }

        #endregion

        internal async static Task RemoveObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
            };
            try
            {
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    await Setup_Test(minio, bucketName);
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);

                    await minio.PutObjectAsync(putObjectArgs);
                }
                new MintLogger("RemoveObject_Test1", removeObjectSignature1, "Tests whether RemoveObjectAsync for existing object passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("RemoveObject_Test1", removeObjectSignature1, "Tests whether RemoveObjectAsync for existing object passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task RemoveObjects_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(6);
            List<string> objectsList = new List<string>();
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectNames", "[" + objectName + "0..." + objectName + "50]" },
            };
            try
            {
                int count = 50;
                Task[] tasks = new Task[count];
                await Setup_Test(minio, bucketName);
                for (int i = 0; i < count; i++)
                {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(5));
                    objectsList.Add(objectName + i.ToString());
                }
                Task.WhenAll(tasks).Wait();
                System.Threading.Thread.Sleep(1000);
                new MintLogger("RemoveObjects_Test2", removeObjectSignature2, "Tests whether RemoveObjectAsync for multi objects delete passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("RemoveObjects_Test2", removeObjectSignature2, "Tests whether RemoveObjectAsync for multi objects delete passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task RemoveObjects_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(6);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectNames", "[" + objectName + "0..." + objectName + "50]" },
            };
            try
            {
                int count = 50;
                Task[] tasks = new Task[count * 2];
                List<string> objectsList = new List<string>();
                await Setup_WithLock_Test(minio, bucketName);
                for (int i = 0; i < (count * 2);)
                {
                    tasks[i++] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(5));
                    tasks[i++] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(5));
                    objectsList.Add(objectName + i.ToString());
                }
                Task.WhenAll(tasks).Wait();
                System.Threading.Thread.Sleep(1000);
                ListObjectsArgs listObjectsArgs = new ListObjectsArgs()
                                                            .WithBucket(bucketName)
                                                            .WithRecursive(true)
                                                            .WithVersions(true);
                IObservable<Item> observable = minio.ListObjectsAsync(listObjectsArgs);
                List<Tuple<string, string>> objVersions = new List<Tuple<string, string>>();
                IDisposable subscription = observable.Subscribe(
                    item =>
                    {
                        objVersions.Add(new Tuple<string, string>(item.Key, item.VersionId));
                    },
                    ex => throw ex,
                    async () =>
                    {
                        RemoveObjectsArgs removeObjectsArgs = new RemoveObjectsArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObjectsVersions(objVersions);
                        IObservable<DeleteError> rmObservable = await minio.RemoveObjectsAsync(removeObjectsArgs);
                        List<DeleteError> deList = new List<DeleteError>();
                        IDisposable rmSub = rmObservable.Subscribe(
                        err =>
                        {
                            deList.Add(err);
                        },
                        ex =>
                        {
                            throw ex;
                        },
                        async () =>
                        {
                            await TearDown(minio, bucketName).ConfigureAwait(false);
                        });
                    });

                Thread.Sleep(2 * 1000);
                new MintLogger("RemoveObjects_Test3", removeObjectSignature2, "Tests whether RemoveObjectsAsync for multi objects/versions delete passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger("RemoveObjects_Test3", removeObjectSignature2, "Tests whether RemoveObjectsAsync for multi objects/versions delete passes", TestStatus.NA, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("RemoveObjects_Test3", removeObjectSignature2, "Tests whether RemoveObjectsAsync for multi objects/versions delete passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
                throw;
            }
        }

        #region Presigned Get Object

        internal async static Task PresignedGetObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            int expiresInt = 1000;
            string downloadFile = "downloadFileName";

            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString() }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);

                    await minio.PutObjectAsync(putObjectArgs);
                }
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);

                PresignedGetObjectArgs preArgs = new PresignedGetObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithExpiry(expiresInt);
                string presigned_url = await minio.PresignedGetObjectAsync(preArgs);
                WebRequest httpRequest = WebRequest.Create(presigned_url);
                // Execute http request to get the object 
                var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null));
                // Create the object from the captured stream response
                Stream stream = response.GetResponseStream();
                var fileStream = File.Create(downloadFile);
                stream.CopyTo(fileStream);

                stream.Dispose();
                fileStream.Dispose();
                FileInfo writtenInfo = new FileInfo(downloadFile);
                long file_read_size = writtenInfo.Length;
                // Compare the size of the file downloaded using the generated
                // presigned_url (expected value) and the actual object size on the server
                Assert.AreEqual(file_read_size, stats.Size);
                new MintLogger("PresignedGetObject_Test1", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedGetObject_Test1", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(downloadFile);
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PresignedGetObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            int expiresInt = 0;
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString() }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);

                    await minio.PutObjectAsync(putObjectArgs);
                }
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);
                PresignedGetObjectArgs preArgs = new PresignedGetObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithExpiry(0);
                string presigned_url = await minio.PresignedGetObjectAsync(preArgs);
                throw new InvalidOperationException("PresignedGetObjectAsync expected to throw an InvalidExpiryRangeException.");
            }
            catch (InvalidExpiryRangeException)
            {
                new MintLogger("PresignedGetObject_Test2", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (InvalidOperationException ex)
            {
                new MintLogger("PresignedGetObject_Test2", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedGetObject_Test2", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task PresignedGetObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            int expiresInt = 1000;
            DateTime reqDate = DateTime.UtcNow.AddSeconds(-50);
            string downloadFile = "downloadFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString() },
                { "reqParams", "response-content-type:application/json,response-content-disposition:attachment;filename=MyDocument.json;" },
                { "reqDate", reqDate.ToString() },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);

                    await minio.PutObjectAsync(putObjectArgs);
                }
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);
                var reqParams = new Dictionary<string, string>
                {
                    ["response-content-type"] = "application/json",
                    ["response-content-disposition"] = "attachment;filename=MyDocument.json;"
                };
                PresignedGetObjectArgs preArgs = new PresignedGetObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithExpiry(1000)
                                                                .WithHeaders(reqParams)
                                                                .WithRequestDate(reqDate);
                string presigned_url = await minio.PresignedGetObjectAsync(preArgs);
                WebRequest httpRequest = WebRequest.Create(presigned_url);
                var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null));
                Assert.IsTrue(response.ContentType.Contains(reqParams["response-content-type"]));
                Assert.IsTrue(response.Headers["Content-Disposition"].Contains("attachment;filename=MyDocument.json;"));
                Assert.IsTrue(response.Headers["Content-Type"].Contains("application/json"));
                Assert.IsTrue(response.Headers["Content-Length"].Contains(stats.Size.ToString()));
                Stream stream = response.GetResponseStream();
                var fileStream = File.Create(downloadFile);
                stream.CopyTo(fileStream);
                fileStream.Dispose();
                FileInfo writtenInfo = new FileInfo(downloadFile);
                long file_read_size = writtenInfo.Length;

                // Compare the size of the file downloaded with the generated
                // presigned_url (expected) and the actual object size on the server
                Assert.AreEqual(file_read_size, stats.Size);
                new MintLogger("PresignedGetObject_Test3", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket when override response headers sent", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedGetObject_Test3", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket when override response headers sent", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                File.Delete(downloadFile);
                await TearDown(minio, bucketName);
            }
        }

        #endregion

        #region Presigned Put Object

        internal async static Task PresignedPutObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            int expiresInt = 1000;
            string fileName = CreateFile(10 * KB, dataFile10KB);

            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString() },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                // Upload with presigned url
                PresignedPutObjectArgs presignedPutObjectArgs = new PresignedPutObjectArgs()
                                                                            .WithBucket(bucketName)
                                                                            .WithObject(objectName)
                                                                            .WithExpiry(1000);
                string presigned_url = await minio.PresignedPutObjectAsync(presignedPutObjectArgs);
                await UploadObjectAsync(presigned_url, fileName);
                // Get stats for object from server
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);
                // Compare with file used for upload
                FileInfo writtenInfo = new FileInfo(fileName);
                long file_written_size = writtenInfo.Length;
                Assert.AreEqual(file_written_size, stats.Size);
                new MintLogger("PresignedPutObject_Test1", presignedPutObjectSignature, "Tests whether PresignedPutObject url uploads object to bucket", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedPutObject_Test1", presignedPutObjectSignature, "Tests whether PresignedPutObject url uploads object to bucket", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
                if (!IsMintEnv())
                {
                    File.Delete(fileName);
                }
            }
        }

        internal async static Task PresignedPutObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            int expiresInt = 0;

            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresInt", expiresInt.ToString() },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);

                    await minio.PutObjectAsync(putObjectArgs);
                }
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                ObjectStat stats = await minio.StatObjectAsync(statObjectArgs);
                PresignedPutObjectArgs presignedPutObjectArgs = new PresignedPutObjectArgs()
                                                                            .WithBucket(bucketName)
                                                                            .WithObject(objectName)
                                                                            .WithExpiry(0);
                string presigned_url = await minio.PresignedPutObjectAsync(presignedPutObjectArgs);
                new MintLogger("PresignedPutObject_Test2", presignedPutObjectSignature, "Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (InvalidExpiryRangeException)
            {
                new MintLogger("PresignedPutObject_Test2", presignedPutObjectSignature, "Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedPutObject_Test2", presignedPutObjectSignature, "Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #endregion

        internal static async Task UploadObjectAsync(string url, string filePath)
        {
            HttpWebRequest httpRequest = WebRequest.Create(url) as HttpWebRequest;
            httpRequest.Method = "PUT";
            using (var dataStream = await Task.Factory.FromAsync<Stream>(httpRequest.BeginGetRequestStream, httpRequest.EndGetRequestStream, null))
            {
                byte[] buffer = new byte[8000];
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(dataStream);
                }
            }

            var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null));
        }

        internal async static Task PresignedPostPolicy_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string metadataKey = GetRandomName(10);
            string metadataValue = GetRandomName(10);

            // Generate presigned post policy url
            PostPolicy formPolicy = new PostPolicy();
            var expiresOn = DateTime.UtcNow.AddMinutes(15);
            formPolicy.SetExpires(expiresOn);
            formPolicy.SetBucket(bucketName);
            formPolicy.SetKey(objectName);
            formPolicy.SetUserMetadata(metadataKey, metadataValue);

            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "expiresOn", expiresOn.ToString() },
            };

            // File to be uploaded
            var size = 10 * KB;
            var sizeExpected = 10240;
            var contentType = "application/octet-stream";
            string fileName = CreateFile(size, dataFile10KB);

            try
            {
                // Creates the bucket
                await Setup_Test(minio, bucketName);

                var polArgs = new PresignedPostPolicyArgs().WithBucket(bucketName)
                                                           .WithObject(objectName)
                                                           .WithPolicy(formPolicy);

                var policyTuple = await minio.PresignedPostPolicyAsync(polArgs);
                var uri = policyTuple.Item1.AbsoluteUri;

                var curlCommand = "curl --insecure";
                foreach (KeyValuePair<string, string> pair in policyTuple.Item2)
                {
                    curlCommand += $" -F {pair.Key}=\"{pair.Value}\"";
                }
                curlCommand += $" -F file=\"@{fileName}\" {uri}";

                Bash(curlCommand);

                // Validate
                StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                .WithBucket(bucketName)
                                                .WithObject(objectName);

                var statObject = await minio.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
                Assert.IsNotNull(statObject);
                Assert.IsTrue(statObject.ObjectName.Equals(objectName));
                Assert.AreEqual(statObject.Size, sizeExpected);
                Assert.IsTrue(statObject.MetaData["Content-Type"] != null);
                Assert.IsTrue(statObject.ContentType.Equals(contentType));
                Assert.IsTrue(statObject.MetaData[metadataKey].Equals(metadataValue));

                new MintLogger("PresignedPostPolicy_Test1", presignedPostPolicySignature, "Tests whether PresignedPostPolicy url applies policy on server", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedPostPolicy_Test1", presignedPostPolicySignature, "Tests whether PresignedPostPolicy url applies policy on server", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }

            if (!IsMintEnv())
            {
                File.Delete(fileName);
            }

        }

        #region List Incomplete Upload

        internal async static Task ListIncompleteUpload_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "gzip";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "recursive", "true" }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(15));
                try
                {
                    using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(50 * MB))
                    {
                        long file_write_size = filestream.Length;

                        PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithStreamData(filestream)
                                                                .WithObjectSize(filestream.Length)
                                                                .WithContentType(contentType);
                        await minio.PutObjectAsync(putObjectArgs, cancellationToken: cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    ListIncompleteUploadsArgs listArgs = new ListIncompleteUploadsArgs()
                                                                    .WithBucket(bucketName);
                    IObservable<Upload> observable = minio.ListIncompleteUploads(listArgs);

                    IDisposable subscription = observable.Subscribe(
                        item =>
                        {
                            Assert.IsTrue(item.Key.Contains(objectName));
                        },
                        ex =>
                        {
                            Assert.Fail();
                        });
                }
                catch (Exception ex)
                {
                    new MintLogger("ListIncompleteUpload_Test1", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString()).Log();
                    return;
                }
                new MintLogger("ListIncompleteUpload_Test1", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes", TestStatus.PASS, (DateTime.Now - startTime)).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListIncompleteUpload_Test1", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString()).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task ListIncompleteUpload_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string prefix = "minioprefix/";
            string objectName = prefix + GetRandomName(10);
            string contentType = "gzip";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "prefix", prefix },
                { "recursive", "false" }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(15));
                try
                {
                    using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(50 * MB))
                    {
                        long file_write_size = filestream.Length;

                        PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithStreamData(filestream)
                                                                .WithObjectSize(filestream.Length)
                                                                .WithContentType(contentType);
                        await minio.PutObjectAsync(putObjectArgs, cancellationToken: cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    ListIncompleteUploadsArgs listArgs = new ListIncompleteUploadsArgs()
                                                                    .WithBucket(bucketName)
                                                                    .WithPrefix("minioprefix")
                                                                    .WithRecursive(false);
                    IObservable<Upload> observable = minio.ListIncompleteUploads(listArgs);

                    IDisposable subscription = observable.Subscribe(
                        item => Assert.AreEqual(item.Key, objectName),
                        ex => Assert.Fail());
                }
                new MintLogger("ListIncompleteUpload_Test2", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes when qualified by prefix", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListIncompleteUpload_Test2", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes when qualified by prefix", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task ListIncompleteUpload_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string prefix = "minioprefix";
            string objectName = prefix + "/" + GetRandomName(10) + "/suffix";
            string contentType = "gzip";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "prefix", prefix },
                { "recursive", "true" }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(15));
                try
                {
                    using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(100 * MB))
                    {
                        long file_write_size = filestream.Length;

                        PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithStreamData(filestream)
                                                                .WithObjectSize(filestream.Length)
                                                                .WithContentType(contentType);
                        await minio.PutObjectAsync(putObjectArgs, cancellationToken: cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    ListIncompleteUploadsArgs listArgs = new ListIncompleteUploadsArgs()
                                                                    .WithBucket(bucketName)
                                                                    .WithPrefix(prefix)
                                                                    .WithRecursive(true);
                    IObservable<Upload> observable = minio.ListIncompleteUploads(listArgs);

                    IDisposable subscription = observable.Subscribe(
                        item => Assert.AreEqual(item.Key, objectName),
                        ex => Assert.Fail());
                }
                new MintLogger("ListIncompleteUpload_Test3", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes when qualified by prefix and recursive", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("ListIncompleteUpload_Test3", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes when qualified by prefix and recursive", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #endregion

        internal async static Task RemoveIncompleteUpload_Test(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = "csv";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(2));
                try
                {
                    using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(30 * MB))
                    {
                        long file_write_size = filestream.Length;

                        PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithStreamData(filestream)
                                                                .WithObjectSize(filestream.Length)
                                                                .WithContentType(contentType);
                        await minio.PutObjectAsync(putObjectArgs, cancellationToken: cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    RemoveIncompleteUploadArgs rmArgs = new RemoveIncompleteUploadArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName);
                    await minio.RemoveIncompleteUploadAsync(rmArgs);

                    ListIncompleteUploadsArgs listArgs = new ListIncompleteUploadsArgs()
                                                                    .WithBucket(bucketName);
                    IObservable<Upload> observable = minio.ListIncompleteUploads(listArgs);

                    IDisposable subscription = observable.Subscribe(
                        item => Assert.Fail(),
                        ex => Assert.Fail());
                }
                new MintLogger("RemoveIncompleteUpload_Test", removeIncompleteUploadSignature, "Tests whether RemoveIncompleteUpload passes.", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("RemoveIncompleteUpload_Test", removeIncompleteUploadSignature, "Tests whether RemoveIncompleteUpload passes.", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #region Bucket Policy

        /// <summary>
        /// Set a policy for given bucket
        /// </summary>
        /// <param name="minio"></param>
        /// <returns></returns>
        internal async static Task SetBucketPolicy_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectPrefix", objectName.Substring(5) },
                { "policyType", "readonly" }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);
                    await minio.PutObjectAsync(putObjectArgs);
                }
                string policyJson = $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}/foo*"",""arn:aws:s3:::{bucketName}/prefix/*""],""Sid"":""""}}]}}";
                var setPolicyArgs = new SetPolicyArgs()
                                            .WithBucket(bucketName)
                                            .WithPolicy(policyJson);

                await minio.SetPolicyAsync(setPolicyArgs);
                new MintLogger("SetBucketPolicy_Test1", setBucketPolicySignature, "Tests whether SetBucketPolicy passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger("SetBucketPolicy_Test1", setBucketPolicySignature, "Tests whether SetBucketPolicy passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("SetBucketPolicy_Test1", setBucketPolicySignature, "Tests whether SetBucketPolicy passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        /// <summary>
        /// Get a policy for given bucket
        /// </summary>
        /// <param name="minio"></param>
        /// <returns></returns>
        internal async static Task GetBucketPolicy_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                string policyJson = $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}/foo*"",""arn:aws:s3:::{bucketName}/prefix/*""],""Sid"":""""}}]}}";
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length);
                    await minio.PutObjectAsync(putObjectArgs);
                }
                var setPolicyArgs = new SetPolicyArgs()
                                            .WithBucket(bucketName)
                                            .WithPolicy(policyJson);
                var getPolicyArgs = new GetPolicyArgs()
                                            .WithBucket(bucketName);
                var rmPolicyArgs = new RemovePolicyArgs()
                                            .WithBucket(bucketName);
                await minio.SetPolicyAsync(setPolicyArgs);
                string policy = await minio.GetPolicyAsync(getPolicyArgs);
                await minio.RemovePolicyAsync(rmPolicyArgs);
                new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }
        #endregion


        #region Bucket Notifications

        internal async static Task ListenBucketNotificationsAsync_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomName(10);
            string contentType = "application/octet-stream";
            IDisposable subscription = null;
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "contentType", contentType },
                { "size", "1KB" }
            };
            try
            {
                await Setup_Test(minio, bucketName);

                var received = new List<MinioNotificationRaw>();

                List<EventType> eventsList = new List<EventType>();
                eventsList.Add(EventType.ObjectCreatedAll);

                ListenBucketNotificationsArgs listenArgs = new ListenBucketNotificationsArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithEvents(eventsList);
                IObservable<MinioNotificationRaw> events = minio.ListenBucketNotificationsAsync(listenArgs);
                subscription = events.Subscribe(
                    ev => received.Add(ev),
                    ex => { },
                    () => { }
                );

                await PutObject_Tester(minio, bucketName, objectName, null, contentType,
                                       0, null, rsg.GenerateStreamFromSeed(1 * KB));

                // wait for notifications
                bool eventDetected = false;
                for (int attempt = 0; attempt < 10; attempt++)
                {

                    if (received.Count > 0)
                    {
                        // Check if there is any unexpected error returned
                        // and captured in the receivedJson list, like
                        // "NotImplemented" api error. If so, we throw an exception
                        // and skip running this test
                        if (received.Count > 1 && received[1].json.StartsWith("<Error><Code>"))
                        {

                            // Although the attribute is called "json",
                            // returned data in list "received" is in xml
                            // format and it is an error.Here, we convert xml
                            // into json format.
                            string receivedJson = XmlStrToJsonStr(received[1].json);


                            // Cleanup the "Error" key encapsulating "receivedJson"
                            // data. This is required to match and convert json data
                            // "receivedJson" into class "ErrorResponse"
                            int len = "{'Error':".Length;
                            string trimmedFront = receivedJson.Substring(len);
                            string trimmedFull = trimmedFront.Substring(0, trimmedFront.Length - 1);

                            ErrorResponse err = JsonConvert.DeserializeObject<ErrorResponse>(trimmedFull);

                            Exception ex = new UnexpectedMinioException(err.Message);
                            if (err.Code == "NotImplemented")
                                ex = new NotImplementedException(err.Message);

                            throw ex;
                        }

                        MinioNotification notification = JsonConvert.DeserializeObject<MinioNotification>(received[0].json);

                        if (notification.Records != null)
                        {
                            Assert.AreEqual(1, notification.Records.Length);
                            Assert.IsTrue(notification.Records[0].eventName.Contains("s3:ObjectCreated:Put"));
                            Assert.IsTrue(objectName.Contains(HttpUtility.UrlDecode(notification.Records[0].s3.objectMeta.key)));
                            Assert.IsTrue(contentType.Contains(notification.Records[0].s3.objectMeta.contentType));
                            eventDetected = true;
                            break;
                        }
                    }
                }
                // subscription.Dispose();
                if (!eventDetected)
                    throw new UnexpectedMinioException("Failed to detect the expected bucket notification event.");

                new MintLogger(nameof(ListenBucketNotificationsAsync_Test1),
                    listenBucketNotificationsSignature,
                    "Tests whether ListenBucketNotifications passes for small object",
                    TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ListenBucketNotificationsAsync_Test1),
                    listenBucketNotificationsSignature,
                    "Tests whether ListenBucketNotifications passes for small object",
                    TestStatus.NA, (DateTime.Now - startTime), ex.Message,
                    ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                if (ex.Message == "Listening for bucket notification is specific" +
                                 " only to `minio` server endpoints")
                {
                    // This is expected when bucket notification
                    // is requested against AWS.
                    // Check if endPoint is AWS
                    bool isAWS(string endPoint)
                    {
                        Regex rgx = new Regex("^s3\\.?.*\\.amazonaws\\.com", RegexOptions.IgnoreCase);
                        MatchCollection matches = rgx.Matches(endPoint);
                        return matches.Count > 0;
                    }
                    if (Environment.GetEnvironmentVariable("AWS_ENDPOINT") != null ||
                        isAWS(Environment.GetEnvironmentVariable("SERVER_ENDPOINT")))
                    {
                        // This is a PASS
                        new MintLogger(nameof(ListenBucketNotificationsAsync_Test1),
                        listenBucketNotificationsSignature,
                        "Tests whether ListenBucketNotifications passes for small object",
                        TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
                    }
                }
                else
                {
                    new MintLogger(nameof(ListenBucketNotificationsAsync_Test1),
                        listenBucketNotificationsSignature,
                        "Tests whether ListenBucketNotifications passes for small object",
                        TestStatus.FAIL, (DateTime.Now - startTime), ex.Message,
                        ex.ToString(), args: args).Log();
                    throw;
                }
            }
            finally
            {
                await TearDown(minio, bucketName);
                if (subscription != null)
                    subscription.Dispose();
            }

        }

        #endregion

        #region Select Object Content

        internal async static Task SelectObjectContent_Test(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string outFileName = "outFileName";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "fileName", outFileName },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                StringBuilder csvString = new StringBuilder();
                csvString.AppendLine("Employee,Manager,Group");
                csvString.AppendLine("Employee4,Employee2,500");
                csvString.AppendLine("Employee3,Employee1,500");
                csvString.AppendLine("Employee1,,1000");
                csvString.AppendLine("Employee5,Employee1,500");
                csvString.AppendLine("Employee2,Employee1,800");
                var csvBytes = Encoding.UTF8.GetBytes(csvString.ToString());
                using (var stream = new MemoryStream(csvBytes))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(stream)
                                                            .WithObjectSize(stream.Length);
                    await minio.PutObjectAsync(putObjectArgs);

                }

                var inputSerialization = new SelectObjectInputSerialization()
                {
                    CompressionType = SelectCompressionType.NONE,
                    CSV = new CSVInputOptions()
                    {
                        FileHeaderInfo = CSVFileHeaderInfo.None,
                        RecordDelimiter = "\n",
                        FieldDelimiter = ",",
                    }
                };
                var outputSerialization = new SelectObjectOutputSerialization()
                {
                    CSV = new CSVOutputOptions()
                    {
                        RecordDelimiter = "\n",
                        FieldDelimiter = ",",
                    }
                };
                SelectObjectContentArgs selArgs = new SelectObjectContentArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithExpressionType(QueryExpressionType.SQL)
                                                                .WithQueryExpression("select * from s3object")
                                                                .WithInputSerialization(inputSerialization)
                                                                .WithOutputSerialization(outputSerialization);
                var resp = await minio.SelectObjectContentAsync(selArgs).ConfigureAwait(false);
                var output = await new StreamReader(resp.Payload).ReadToEndAsync();
                var csvStringNoWS = Regex.Replace(csvString.ToString(), @"\s+", "");
                var outputNoWS = Regex.Replace(output, @"\s+", "");
                // Compute MD5 for a better result.
                var hashedOutputBytes = System.Security.Cryptography.MD5
                                                                .Create()
                                                                .ComputeHash(Encoding.UTF8.GetBytes(outputNoWS));
                var outputMd5 = Convert.ToBase64String(hashedOutputBytes);
                var hashedCSVBytes = System.Security.Cryptography.MD5
                                                                .Create()
                                                                .ComputeHash(System.Text.Encoding.UTF8.GetBytes(csvStringNoWS));
                var csvMd5 = Convert.ToBase64String(hashedCSVBytes);

                Assert.IsTrue(csvMd5.Contains(outputMd5));
                new MintLogger("SelectObjectContent_Test", selectObjectSignature, "Tests whether SelectObjectContent passes for a select query", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("SelectObjectContent_Test", selectObjectSignature, "Tests whether SelectObjectContent passes for a select query", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
                File.Delete(outFileName);
            }
        }

        #endregion


        #region Bucket Encryption
        internal async static Task BucketEncryptionsAsync_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName }
            };
            try
            {
                await Setup_Test(minio, bucketName);
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), setBucketEncryptionSignature, "Tests whether SetBucketEncryptionAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                SetBucketEncryptionArgs encryptionArgs = new SetBucketEncryptionArgs()
                                                                    .WithBucket(bucketName);
                await minio.SetBucketEncryptionAsync(encryptionArgs);
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), setBucketEncryptionSignature, "Tests whether SetBucketEncryptionAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), setBucketEncryptionSignature, "Tests whether SetBucketEncryptionAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), setBucketEncryptionSignature, "Tests whether SetBucketEncryptionAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                GetBucketEncryptionArgs encryptionArgs = new GetBucketEncryptionArgs()
                                                                        .WithBucket(bucketName);
                var config = await minio.GetBucketEncryptionAsync(encryptionArgs).ConfigureAwait(false);
                Assert.IsNotNull(config);
                Assert.IsNotNull(config.Rule);
                Assert.IsNotNull(config.Rule.Apply);
                Assert.IsTrue(config.Rule.Apply.SSEAlgorithm.Contains("AES256"));
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), getBucketEncryptionSignature, "Tests whether GetBucketEncryptionAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), getBucketEncryptionSignature, "Tests whether GetBucketEncryptionAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), getBucketEncryptionSignature, "Tests whether GetBucketEncryptionAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                RemoveBucketEncryptionArgs rmEncryptionArgs = new RemoveBucketEncryptionArgs()
                                                                        .WithBucket(bucketName);
                await minio.RemoveBucketEncryptionAsync(rmEncryptionArgs).ConfigureAwait(false);
                GetBucketEncryptionArgs encryptionArgs = new GetBucketEncryptionArgs()
                                                                        .WithBucket(bucketName);
                var config = await minio.GetBucketEncryptionAsync(encryptionArgs).ConfigureAwait(false);
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketEncryptionsAsync_Test1), removeBucketEncryptionSignature, "Tests whether RemoveBucketEncryptionAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The server side encryption configuration was not found"))
                {
                    new MintLogger(nameof(BucketEncryptionsAsync_Test1), removeBucketEncryptionSignature, "Tests whether RemoveBucketEncryptionAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
                }
                else
                {
                    new MintLogger(nameof(BucketEncryptionsAsync_Test1), removeBucketEncryptionSignature, "Tests whether RemoveBucketEncryptionAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                    throw;
                }
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #endregion

        #region Legal Hold Status
        internal async static Task LegalHoldStatusAsync_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName }
            };
            try
            {
                await Setup_WithLock_Test(minio, bucketName);
            }
            catch (NotImplementedException ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature, "Tests whether SetObjectLegalHoldAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                return;
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature, "Tests whether SetObjectLegalHoldAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithContentType(null);
                    await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                }
                SetObjectLegalHoldArgs legalHoldArgs = new SetObjectLegalHoldArgs()
                                                                    .WithBucket(bucketName)
                                                                    .WithObject(objectName)
                                                                    .WithLegalHold(true);
                await minio.SetObjectLegalHoldAsync(legalHoldArgs);
                new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature, "Tests whether SetObjectLegalHoldAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature, "Tests whether SetObjectLegalHoldAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(LegalHoldStatusAsync_Test1), setObjectLegalHoldSignature, "Tests whether SetObjectLegalHoldAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }

            try
            {
                GetObjectLegalHoldArgs getLegalHoldArgs = new GetObjectLegalHoldArgs()
                                                                    .WithBucket(bucketName)
                                                                    .WithObject(objectName);
                bool enabled = await minio.GetObjectLegalHoldAsync(getLegalHoldArgs);
                Assert.IsTrue(enabled);
                RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                                    .WithBucket(bucketName)
                                                    .WithObject(objectName);

                await minio.RemoveObjectAsync(rmArgs);
                new MintLogger(nameof(LegalHoldStatusAsync_Test1), getObjectLegalHoldSignature, "Tests whether GetObjectLegalHoldAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(LegalHoldStatusAsync_Test1), getObjectLegalHoldSignature, "Tests whether GetObjectLegalHoldAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(LegalHoldStatusAsync_Test1), getObjectLegalHoldSignature, "Tests whether GetObjectLegalHoldAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #endregion

        #region Bucket Tagging
        internal async static Task BucketTagsAsync_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName }
            };
            Dictionary<string, string> tags = new Dictionary<string, string>()
                                {
                                    {"key1", "value1"},
                                    {"key2", "value2"},
                                    {"key3", "value3"}
                                };
            try
            {
                await Setup_Test(minio, bucketName);
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(BucketTagsAsync_Test1), setBucketTagsSignature, "Tests whether SetBucketTagsAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }

            try
            {
                SetBucketTagsArgs tagsArgs = new SetBucketTagsArgs()
                                                            .WithBucket(bucketName)
                                                            .WithTagging(Tagging.GetBucketTags(tags));
                await minio.SetBucketTagsAsync(tagsArgs);
                new MintLogger(nameof(BucketTagsAsync_Test1), setBucketTagsSignature, "Tests whether SetBucketTagsAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketTagsAsync_Test1), setBucketTagsSignature, "Tests whether SetBucketTagsAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketTagsAsync_Test1), setBucketTagsSignature, "Tests whether SetBucketTagsAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                GetBucketTagsArgs tagsArgs = new GetBucketTagsArgs()
                                                        .WithBucket(bucketName);
                var tagObj = await minio.GetBucketTagsAsync(tagsArgs);
                Assert.IsNotNull(tagObj);
                Assert.IsNotNull(tagObj.GetTags());
                var tagsRes = tagObj.GetTags();
                Assert.AreEqual(tagsRes.Count, tags.Count);

                new MintLogger(nameof(BucketTagsAsync_Test1), getBucketTagsSignature, "Tests whether GetBucketTagsAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketTagsAsync_Test1), getBucketTagsSignature, "Tests whether GetBucketTagsAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(BucketTagsAsync_Test1), getBucketTagsSignature, "Tests whether GetBucketTagsAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                await TearDown(minio, bucketName);
                throw;
            }
            try
            {
                RemoveBucketTagsArgs tagsArgs = new RemoveBucketTagsArgs()
                                                                .WithBucket(bucketName);
                await minio.RemoveBucketTagsAsync(tagsArgs);
                GetBucketTagsArgs getTagsArgs = new GetBucketTagsArgs()
                                                        .WithBucket(bucketName);
                var tagObj = await minio.GetBucketTagsAsync(getTagsArgs);
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketTagsAsync_Test1), deleteBucketTagsSignature, "Tests whether RemoveBucketTagsAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The TagSet does not exist"))
                {
                    new MintLogger(nameof(BucketTagsAsync_Test1), deleteBucketTagsSignature, "Tests whether RemoveBucketTagsAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
                }
                else
                {
                    new MintLogger(nameof(BucketTagsAsync_Test1), deleteBucketTagsSignature, "Tests whether RemoveBucketTagsAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                    throw;
                }
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #endregion

        #region Object Tagging
        internal async static Task ObjectTagsAsync_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName}
            };
            Dictionary<string, string> tags = new Dictionary<string, string>()
                                {
                                    {"key1", "value1"},
                                    {"key2", "value2"},
                                    {"key3", "value3"}
                                };
            try
            {
                await Setup_Test(minio, bucketName);
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectTagsAsync_Test1), setObjectTagsSignature, "Tests whether SetObjectTagsAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            bool exceptionThrown = false;
            try
            {
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithContentType(null);
                    await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                }
                SetObjectTagsArgs tagsArgs = new SetObjectTagsArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithTagging(Tagging.GetObjectTags(tags));
                await minio.SetObjectTagsAsync(tagsArgs);
                new MintLogger(nameof(ObjectTagsAsync_Test1), setObjectTagsSignature, "Tests whether SetObjectTagsAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                exceptionThrown = true;
                new MintLogger(nameof(ObjectTagsAsync_Test1), setObjectTagsSignature, "Tests whether SetObjectTagsAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                new MintLogger(nameof(ObjectTagsAsync_Test1), setObjectTagsSignature, "Tests whether SetObjectTagsAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                exceptionThrown = false;
                GetObjectTagsArgs tagsArgs = new GetObjectTagsArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                var tagObj = await minio.GetObjectTagsAsync(tagsArgs);
                Assert.IsNotNull(tagObj);
                Assert.IsNotNull(tagObj.GetTags());
                var tagsRes = tagObj.GetTags();
                Assert.AreEqual(tagsRes.Count, tags.Count);
                new MintLogger(nameof(ObjectTagsAsync_Test1), getObjectTagsSignature, "Tests whether GetObjectTagsAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                exceptionThrown = true;
                new MintLogger(nameof(ObjectTagsAsync_Test1), getObjectTagsSignature, "Tests whether GetObjectTagsAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                new MintLogger(nameof(ObjectTagsAsync_Test1), getObjectTagsSignature, "Tests whether GetObjectTagsAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            if (exceptionThrown)
            {
                await TearDown(minio, bucketName);
                return;
            }
            try
            {
                RemoveObjectTagsArgs tagsArgs = new RemoveObjectTagsArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName);
                await minio.RemoveObjectTagsAsync(tagsArgs);
                GetObjectTagsArgs getTagsArgs = new GetObjectTagsArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                var tagObj = await minio.GetObjectTagsAsync(getTagsArgs);
                Assert.IsNotNull(tagObj);
                var tagsRes = tagObj.GetTags();
                Assert.IsNull(tagsRes);
                new MintLogger(nameof(ObjectTagsAsync_Test1), deleteObjectTagsSignature, "Tests whether RemoveObjectTagsAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ObjectTagsAsync_Test1), deleteObjectTagsSignature, "Tests whether RemoveObjectTagsAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectTagsAsync_Test1), deleteObjectTagsSignature, "Tests whether RemoveObjectTagsAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #endregion

        #region Object Versioning
        internal async static Task ObjectVersioningAsync_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName}
            };
            try
            {
                await Setup_Test(minio, bucketName);
                {
                    // Set versioning enabled test
                    SetVersioningArgs setVersioningArgs = new SetVersioningArgs()
                                                                    .WithBucket(bucketName)
                                                                    .WithVersioningEnabled();
                    await minio.SetVersioningAsync(setVersioningArgs);

                    // Twice, for 2 versions.
                    using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                    {
                        PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithStreamData(filestream)
                                                                .WithObjectSize(filestream.Length)
                                                                .WithContentType(null);
                        await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                    }
                    using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                    {
                        PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithStreamData(filestream)
                                                                .WithObjectSize(filestream.Length)
                                                                .WithContentType(null);
                        await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                    }
                    new MintLogger(nameof(ObjectVersioningAsync_Test1), setVersioningSignature, "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();

                    int objectVersionCount = 2;
                    int objectVersionIndex = 0;
                    ListObjectsArgs listArgs = new ListObjectsArgs()
                                                        .WithBucket(bucketName);
                    IObservable<Item> observable = minio.ListObjectsAsync(listArgs);
                    List<Tuple<string, string>> objVersions = new List<Tuple<string, string>>();
                    IDisposable subscription = observable.Subscribe(
                        item =>
                        {
                            objVersions.Add(new Tuple<string, string>(item.Key, item.VersionId));
                            objectVersionIndex++;
                        },
                        ex => throw ex,
                        () =>
                        {
                            Assert.IsTrue((objectVersionIndex == objectVersionCount));
                        });
                    System.Threading.Thread.Sleep(1500);
                }

                {
                    // Get Versioning Test
                    GetVersioningArgs getVersioningArgs = new GetVersioningArgs()
                                                                        .WithBucket(bucketName);
                    VersioningConfiguration versioningConfig = await minio.GetVersioningAsync(getVersioningArgs);
                    Assert.IsNotNull(versioningConfig);
                    Assert.IsNotNull(versioningConfig.Status);
                    Assert.IsTrue(versioningConfig.Status.ToLower().Equals("enabled"));

                    new MintLogger(nameof(ObjectVersioningAsync_Test1), getVersioningSignature, "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
                }
                {
                    // Suspend Versioning test.
                    SetVersioningArgs setVersioningArgs = new SetVersioningArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithVersioningSuspended();
                    await minio.SetVersioningAsync(setVersioningArgs);

                    int objectCount = 1;
                    int objectIndex = 0;
                    ListObjectsArgs listArgs = new ListObjectsArgs()
                                                            .WithBucket(bucketName);
                    IObservable<Item> observable = minio.ListObjectsAsync(listArgs);
                    List<Tuple<string>> objects = new List<Tuple<string>>();
                    IDisposable subscription = observable.Subscribe(
                        item =>
                        {
                            objects.Add(new Tuple<string>(item.Key));
                            objectIndex++;
                        },
                        ex => throw ex,
                        () =>
                        {
                            Assert.IsTrue((objectIndex == objectCount));
                        });
                    System.Threading.Thread.Sleep(1500);
                    new MintLogger(nameof(ObjectVersioningAsync_Test1), removeVersioningSignature, "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
                }
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ObjectVersioningAsync_Test1), setVersioningSignature, "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectVersioningAsync_Test1), setVersioningSignature, "Tests whether SetVersioningAsync/GetVersioningAsync/RemoveVersioningAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #endregion


        #region Object Lock Configuration
        internal async static Task ObjectLockConfigurationAsync_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName }
            };
            bool setLockNotImplemented = false;
            bool getLockNotImplemented = false;

            try
            {
                await Setup_WithLock_Test(minio, bucketName);
                //TODO: Use it for testing and remove
                {
                    ObjectRetentionConfiguration objectRetention = new ObjectRetentionConfiguration(DateTime.Today.AddDays(3), RetentionMode.GOVERNANCE);
                    using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                    {
                        // Twice, for 2 versions.
                        PutObjectArgs putObjectArgs1 = new PutObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithStreamData(filestream)
                                                                .WithObjectSize(filestream.Length)
                                                                .WithRetentionConfiguration(objectRetention)
                                                                .WithContentType(null);
                        await minio.PutObjectAsync(putObjectArgs1).ConfigureAwait(false);
                    }
                    using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                    {
                        PutObjectArgs putObjectArgs2 = new PutObjectArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName)
                                                                .WithStreamData(filestream)
                                                                .WithObjectSize(filestream.Length)
                                                                .WithRetentionConfiguration(objectRetention)
                                                                .WithContentType(null);
                        await minio.PutObjectAsync(putObjectArgs2).ConfigureAwait(false);
                    }
                }
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature, "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                await TearDown(minio, bucketName);
                return;
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature, "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                await TearDown(minio, bucketName);
                throw;
            }
            try
            {
                SetObjectLockConfigurationArgs objectLockArgs = new SetObjectLockConfigurationArgs()
                                                                            .WithBucket(bucketName)
                                                                            .WithLockConfiguration(
                                                                                new ObjectLockConfiguration(RetentionMode.GOVERNANCE, 33)
                                                                            );
                await minio.SetObjectLockConfigurationAsync(objectLockArgs);
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature, "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                setLockNotImplemented = true;
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature, "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), setObjectLockConfigurationSignature, "Tests whether SetObjectLockConfigurationAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                await TearDown(minio, bucketName);
                throw;
            }
            try
            {
                GetObjectLockConfigurationArgs objectLockArgs = new GetObjectLockConfigurationArgs()
                                                                            .WithBucket(bucketName);
                var config = await minio.GetObjectLockConfigurationAsync(objectLockArgs);
                Assert.IsNotNull(config);
                Assert.IsTrue(config.ObjectLockEnabled.Contains(ObjectLockConfiguration.LockEnabled));
                Assert.IsNotNull(config.Rule);
                Assert.IsNotNull(config.Rule.DefaultRetention);
                Assert.AreEqual(config.Rule.DefaultRetention.Days, 33);
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), getObjectLockConfigurationSignature, "Tests whether GetObjectLockConfigurationAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                getLockNotImplemented = true;
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), getObjectLockConfigurationSignature, "Tests whether GetObjectLockConfigurationAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), getObjectLockConfigurationSignature, "Tests whether GetObjectLockConfigurationAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                if (setLockNotImplemented || getLockNotImplemented)
                {
                    // Cannot test Remove Object Lock with Set & Get Object Lock implemented.
                    new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), deleteObjectLockConfigurationSignature, "Tests whether RemoveObjectLockConfigurationAsync passes", TestStatus.NA, (DateTime.Now - startTime), "Functionality that is not implemented", "", args: args).Log();
                    await TearDown(minio, bucketName);
                    return;
                }

                RemoveObjectLockConfigurationArgs objectLockArgs = new RemoveObjectLockConfigurationArgs()
                                                                            .WithBucket(bucketName);
                await minio.RemoveObjectLockConfigurationAsync(objectLockArgs);
                GetObjectLockConfigurationArgs getObjectLockArgs = new GetObjectLockConfigurationArgs()
                                                                            .WithBucket(bucketName);
                var config = await minio.GetObjectLockConfigurationAsync(getObjectLockArgs);
                Assert.IsNotNull(config);
                Assert.IsNull(config.Rule);
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), deleteObjectLockConfigurationSignature, "Tests whether RemoveObjectLockConfigurationAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), deleteObjectLockConfigurationSignature, "Tests whether RemoveObjectLockConfigurationAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectLockConfigurationAsync_Test1), deleteObjectLockConfigurationSignature, "Tests whether RemoveObjectLockConfigurationAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            finally
            {
                System.Threading.Thread.Sleep(1500);
                await TearDown(minio, bucketName);
            }
        }

        #endregion


        #region Object Retention
        internal async static Task ObjectRetentionAsync_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName }
            };

            try
            {
                await Setup_WithLock_Test(minio, bucketName);
            }
            catch (NotImplementedException ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature, "Tests whether SetObjectRetentionAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                return;
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature, "Tests whether SetObjectRetentionAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                int plusDays = 10;
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithStreamData(filestream)
                                                            .WithObjectSize(filestream.Length)
                                                            .WithContentType(null);
                    await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                }
                DateTime untilDate = DateTime.Now.AddDays(plusDays);
                SetObjectRetentionArgs setRetentionArgs = new SetObjectRetentionArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName)
                                                                        .WithRetentionMode(RetentionMode.GOVERNANCE)
                                                                        .WithRetentionUntilDate(untilDate);
                await minio.SetObjectRetentionAsync(setRetentionArgs);
                new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature, "Tests whether SetObjectRetentionAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature, "Tests whether SetObjectRetentionAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(ObjectRetentionAsync_Test1), setObjectRetentionSignature, "Tests whether SetObjectRetentionAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }

            try
            {
                GetObjectRetentionArgs getRetentionArgs = new GetObjectRetentionArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName);
                ObjectRetentionConfiguration config = await minio.GetObjectRetentionAsync(getRetentionArgs);
                double plusDays = 10.0;
                Assert.IsNotNull(config);
                Assert.AreEqual(config.Mode, RetentionMode.GOVERNANCE);
                DateTime untilDate = DateTime.Parse(config.RetainUntilDate, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.AreEqual(Math.Ceiling((untilDate - DateTime.Now).TotalDays), plusDays);
                new MintLogger(nameof(ObjectRetentionAsync_Test1), getObjectRetentionSignature, "Tests whether GetObjectRetentionAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ObjectRetentionAsync_Test1), getObjectRetentionSignature, "Tests whether GetObjectRetentionAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(ObjectRetentionAsync_Test1), getObjectRetentionSignature, "Tests whether GetObjectRetentionAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }

            try
            {
                ClearObjectRetentionArgs clearRetentionArgs = new ClearObjectRetentionArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName);
                await minio.ClearObjectRetentionAsync(clearRetentionArgs);
                GetObjectRetentionArgs getRetentionArgs = new GetObjectRetentionArgs()
                                                                        .WithBucket(bucketName)
                                                                        .WithObject(objectName);
                ObjectRetentionConfiguration config = await minio.GetObjectRetentionAsync(getRetentionArgs);
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(ObjectRetentionAsync_Test1), clearObjectRetentionSignature, "Tests whether ClearObjectRetentionAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                bool errMsgLock = ex.Message.Contains("The specified object does not have a ObjectLock configuration");
                if (errMsgLock)
                    new MintLogger(nameof(ObjectRetentionAsync_Test1), clearObjectRetentionSignature, "Tests whether ClearObjectRetentionAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
                else
                {
                    new MintLogger(nameof(ObjectRetentionAsync_Test1), clearObjectRetentionSignature, "Tests whether ClearObjectRetentionAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                    await TearDown(minio, bucketName);
                    throw;
                }
            }

            try
            {
                RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                                    .WithBucket(bucketName)
                                                    .WithObject(objectName);

                await minio.RemoveObjectAsync(rmArgs);
                await TearDown(minio, bucketName);
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ObjectRetentionAsync_Test1), clearObjectRetentionSignature, "TearDown operation ClearObjectRetentionAsync", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
        }

        #endregion


        #region Bucket Lifecycle
        internal async static Task BucketLifecycleAsync_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName }
            };
            try
            {
                await Setup_Test(minio, bucketName);
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketLifecycleAsync_Test1), setBucketLifecycleSignature, "Tests whether SetBucketLifecycleAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }

            List<LifecycleRule> rules = new List<LifecycleRule>();
            Expiration exp = new Expiration(DateTime.Now.AddYears(1));
            var compareDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            var expInDays = (compareDate.AddYears(1) - compareDate).TotalDays;

            LifecycleRule rule1 = new LifecycleRule(null, "txt", exp, null,
                new RuleFilter(null, "txt/", null),
                null, null, LifecycleRule.LIFECYCLE_RULE_STATUS_ENABLED
                );
            rules.Add(rule1);
            LifecycleConfiguration lfc = new LifecycleConfiguration(rules);
            try
            {
                SetBucketLifecycleArgs lfcArgs = new SetBucketLifecycleArgs()
                                                            .WithBucket(bucketName)
                                                            .WithLifecycleConfiguration(lfc);
                await minio.SetBucketLifecycleAsync(lfcArgs);
                new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".1", setBucketLifecycleSignature, "Tests whether SetBucketLifecycleAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".1", setBucketLifecycleSignature, "Tests whether SetBucketLifecycleAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".1", setBucketLifecycleSignature, "Tests whether SetBucketLifecycleAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                GetBucketLifecycleArgs lfcArgs = new GetBucketLifecycleArgs()
                                                            .WithBucket(bucketName);
                var lfcObj = await minio.GetBucketLifecycleAsync(lfcArgs);
                Assert.IsNotNull(lfcObj);
                Assert.IsNotNull(lfcObj.Rules);
                Assert.IsTrue(lfcObj.Rules.Count > 0);
                Assert.AreEqual(lfcObj.Rules.Count, lfc.Rules.Count);
                DateTime lfcDate = DateTime.Parse(lfcObj.Rules[0].Expiration.Date, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.AreEqual(Math.Floor((lfcDate - compareDate).TotalDays), expInDays);
                new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".2", getBucketLifecycleSignature, "Tests whether GetBucketLifecycleAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".2", getBucketLifecycleSignature, "Tests whether GetBucketLifecycleAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".2", getBucketLifecycleSignature, "Tests whether GetBucketLifecycleAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                RemoveBucketLifecycleArgs lfcArgs = new RemoveBucketLifecycleArgs()
                                                                .WithBucket(bucketName);
                await minio.RemoveBucketLifecycleAsync(lfcArgs);
                GetBucketLifecycleArgs getLifecycleArgs = new GetBucketLifecycleArgs()
                                                                    .WithBucket(bucketName);
                var lfcObj = await minio.GetBucketLifecycleAsync(getLifecycleArgs);
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".3", deleteBucketLifecycleSignature, "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The lifecycle configuration does not exist"))
                    new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".3", deleteBucketLifecycleSignature, "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
                else
                {
                    new MintLogger(nameof(BucketLifecycleAsync_Test1) + ".3", deleteBucketLifecycleSignature, "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                    throw;
                }
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        internal async static Task BucketLifecycleAsync_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName }
            };
            try
            {
                await Setup_Test(minio, bucketName);
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketLifecycleAsync_Test2), setBucketLifecycleSignature, "Tests whether SetBucketLifecycleAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }

            List<LifecycleRule> rules = new List<LifecycleRule>();
            Expiration exp = new Expiration();
            exp.Days = 30;

            LifecycleRule rule1 = new LifecycleRule(null, "txt", exp, null,
                new RuleFilter(null, "txt/", null),
                null, null, LifecycleRule.LIFECYCLE_RULE_STATUS_ENABLED
                );
            rules.Add(rule1);
            LifecycleConfiguration lfc = new LifecycleConfiguration(rules);
            try
            {
                SetBucketLifecycleArgs lfcArgs = new SetBucketLifecycleArgs()
                                                            .WithBucket(bucketName)
                                                            .WithLifecycleConfiguration(lfc);
                await minio.SetBucketLifecycleAsync(lfcArgs);
                new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".1", setBucketLifecycleSignature, "Tests whether SetBucketLifecycleAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".1", setBucketLifecycleSignature, "Tests whether SetBucketLifecycleAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".1", setBucketLifecycleSignature, "Tests whether SetBucketLifecycleAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                GetBucketLifecycleArgs lfcArgs = new GetBucketLifecycleArgs()
                                                            .WithBucket(bucketName);
                var lfcObj = await minio.GetBucketLifecycleAsync(lfcArgs);
                Assert.IsNotNull(lfcObj);
                Assert.IsNotNull(lfcObj.Rules);
                Assert.IsTrue(lfcObj.Rules.Count > 0);
                Assert.AreEqual(lfcObj.Rules.Count, lfc.Rules.Count);
                new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".2", getBucketLifecycleSignature, "Tests whether GetBucketLifecycleAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".2", getBucketLifecycleSignature, "Tests whether GetBucketLifecycleAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                await TearDown(minio, bucketName);
                new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".2", getBucketLifecycleSignature, "Tests whether GetBucketLifecycleAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                throw;
            }
            try
            {
                RemoveBucketLifecycleArgs lfcArgs = new RemoveBucketLifecycleArgs()
                                                                .WithBucket(bucketName);
                await minio.RemoveBucketLifecycleAsync(lfcArgs);
                GetBucketLifecycleArgs getLifecycleArgs = new GetBucketLifecycleArgs()
                                                                    .WithBucket(bucketName);
                var lfcObj = await minio.GetBucketLifecycleAsync(getLifecycleArgs);
            }
            catch (NotImplementedException ex)
            {
                new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".3", deleteBucketLifecycleSignature, "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.NA, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The lifecycle configuration does not exist"))
                    new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".3", deleteBucketLifecycleSignature, "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
                else
                {
                    new MintLogger(nameof(BucketLifecycleAsync_Test2) + ".3", deleteBucketLifecycleSignature, "Tests whether RemoveBucketLifecycleAsync passes", TestStatus.FAIL, (DateTime.Now - startTime), ex.Message, ex.ToString(), args: args).Log();
                    throw;
                }
            }
            finally
            {
                await TearDown(minio, bucketName);
            }
        }

        #endregion

    }
}