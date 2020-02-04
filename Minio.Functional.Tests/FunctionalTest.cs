/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
* (C) 2017, 2018, 2019, 2020 MinIO, Inc.
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using Minio.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Minio.Functional.Tests
{
    public class FunctionalTest
    {
        private static readonly Random rnd = new Random();
        private const int KB = 1024;
        private const int MB = 1024 * 1024;

        private const string dataFile1B = "datafile-1-b";

        private const string dataFile10KB = "datafile-10-kB";
        private const string dataFile6MB = "datafile-6-MB";

        private static RandomStreamGenerator rsg = new RandomStreamGenerator(100 * MB);
        private const string makeBucketSignature = "Task MakeBucketAsync(string bucketName, string location = 'us-east-1', CancellationToken cancellationToken = default(CancellationToken))";
        private const string listBucketsSignature = "Task<ListAllMyBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default(CancellationToken))";
        private const string bucketExistsSignature = "Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeBucketSignature = "Task RemoveBucketAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string listObjectsSignature = "IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = false, CancellationToken cancellationToken = default(CancellationToken))";
        private const string listIncompleteUploadsSignature = "IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix, bool recursive, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getObjectSignature1 = "Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getObjectSignature2 = "Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getObjectSignature3 = "Task GetObjectAsync(string bucketName, string objectName, string fileName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string putObjectSignature1 = "Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType, Dictionary<string, string> metaData=null, CancellationToken cancellationToken = default(CancellationToken))";
        private const string putObjectSignature2 = "Task PutObjectAsync(string bucketName, string objectName, string filePath, string contentType=null, Dictionary<string, string> metaData=null, CancellationToken cancellationToken = default(CancellationToken))";
        private const string listenBucketNotificationsSignature = "IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(string bucketName, IList<EventType> events, string prefix, string suffix, CancellationToken cancellationToken = default(CancellationToken))";
        private const string statObjectSignature = "Task<ObjectStat> StatObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string copyObjectSignature = "Task<CopyObjectResult> CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeObjectSignature1 = "Task RemoveObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeObjectSignature2 = "Task<IObservable<DeleteError>> RemoveObjectAsync(string bucketName, IEnumerable<string> objectsList, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeIncompleteUploadSignature = "Task RemoveIncompleteUploadAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string presignedGetObjectSignature = "Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string, string> reqParams = null)";
        private const string presignedPutObjectSignature = "Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)";
        private const string presignedPostPolicySignature = "Task<Dictionary<string, string>> PresignedPostPolicyAsync(PostPolicy policy)";
        private const string getBucketPolicySignature = "Task<string> GetPolicyAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setBucketPolicySignature = "Task SetPolicyAsync(string bucketName, string policyJson, CancellationToken cancellationToken = default(CancellationToken))";
        private const string getBucketNotificationSignature = "Task<BucketNotification> GetBucketNotificationAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string setBucketNotificationSignature = "Task SetBucketNotificationAsync(string bucketName, BucketNotification notification, CancellationToken cancellationToken = default(CancellationToken))";
        private const string removeAllBucketsNotificationSignature = "Task RemoveAllBucketNotificationsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";
        private const string selectObjectSignature = "Task<SelectResponseStream> SelectObjectContentAsync(string bucketName, string objectName, SelectObjectOptions opts, CancellationToken cancellationToken = default(CancellationToken))";

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
            string characters = "abcd+&%$#@*&{}[]()";
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

            return $"miniodotnet{result}";
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
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
            };

            try
            {
                await minio.MakeBucketAsync(bucketName);
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                await minio.RemoveBucketAsync(bucketName);
                new MintLogger(nameof(BucketExists_Test), bucketExistsSignature, "Tests whether BucketExists passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger(nameof(BucketExists_Test), bucketExistsSignature, "Tests whether BucketExists passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        #region Make Bucket

        internal async static Task MakeBucket_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 60);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "us-east-1" },
            };

            try
            {
                await minio.MakeBucketAsync(bucketName);
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                await minio.RemoveBucketAsync(bucketName);
                new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger(nameof(MakeBucket_Test1), makeBucketSignature, "Tests whether MakeBucket passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task MakeBucket_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 10) + ".withperiod";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "us-east-1" },
            };
            string testType = "Test whether make bucket passes when bucketname has a period.";

            try
            {
                await minio.MakeBucketAsync(bucketName);
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                await minio.RemoveBucketAsync(bucketName);
                new MintLogger(nameof(MakeBucket_Test2), makeBucketSignature, testType, TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger(nameof(MakeBucket_Test2), makeBucketSignature, testType, TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task MakeBucket_Test3(MinioClient minio, bool aws = false)
        {
            if (!aws)
                return;
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 60);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "eu-central-1" },
            };
            try
            {
                await minio.MakeBucketAsync(bucketName, location: "eu-central-1");
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                if (found)
                {
                    await minio.MakeBucketAsync(bucketName);
                    await minio.RemoveBucketAsync(bucketName);

                }
                new MintLogger(nameof(MakeBucket_Test3), makeBucketSignature, "Tests whether MakeBucket with region passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();

            }
            catch (MinioException ex)
            {
               // Assert.AreEqual<string>(ex.message, "Your previous request to create the named bucket succeeded and you already own it.");
                new MintLogger(nameof(MakeBucket_Test3), makeBucketSignature, "Tests whether MakeBucket with region passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task MakeBucket_Test4(MinioClient minio, bool aws = false)
        {
            if (!aws)
                return;
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 20) + ".withperiod";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "region", "us-west-2" },
            };
            try
            {
                await minio.MakeBucketAsync(bucketName, location: "us-west-2");
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                if (found)
                {
                    await minio.RemoveBucketAsync(bucketName);

                }
                new MintLogger(nameof(MakeBucket_Test4), makeBucketSignature, "Tests whether MakeBucket with region and bucketname with . passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger(nameof(MakeBucket_Test4), makeBucketSignature, "Tests whether MakeBucket with region and bucketname with . passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        #endregion

        internal async static Task RemoveBucket_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 20);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
            };

            try
            {
                await minio.MakeBucketAsync(bucketName);
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                await minio.RemoveBucketAsync(bucketName);
                found = await minio.BucketExistsAsync(bucketName);
                Assert.IsFalse(found);
                new MintLogger(nameof(RemoveBucket_Test1), removeBucketSignature, "Tests whether RemoveBucket passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger(nameof(RemoveBucket_Test1), removeBucketSignature, "Tests whether RemoveBucket passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task ListBuckets_Test(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            var args = new Dictionary<string, string>();
            try
            {
                var list = await minio.ListBucketsAsync();
                foreach (Bucket bucket in list.Buckets)
                {
                    // Ignore
                    continue;
                }
                new MintLogger(nameof(ListBuckets_Test), listBucketsSignature, "Tests whether ListBucket passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ListBuckets_Test), listBucketsSignature, "Tests whether ListBucket passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task Setup_Test(MinioClient minio, string bucketName)
        {
            await minio.MakeBucketAsync(bucketName);
            bool found = await minio.BucketExistsAsync(bucketName);
            Assert.IsTrue(found);
        }

        internal async static Task TearDown(MinioClient minio, string bucketName)
        {
            await minio.RemoveBucketAsync(bucketName);
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
                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(1 * KB));
                await TearDown(minio, bucketName);
                new MintLogger(nameof(PutObject_Test1), putObjectSignature1, "Tests whether PutObject passes for small object", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test1), putObjectSignature1, "Tests whether PutObject passes for small object", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task PutObject_Test2(MinioClient minio)
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
                { "size", "6MB" }
            };
            try
            {
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(6 * MB));
                await TearDown(minio, bucketName);
                new MintLogger(nameof(PutObject_Test2), putObjectSignature1, "Tests whether multipart PutObject passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test2), putObjectSignature1, "Tests whether multipart PutObject passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(1 * KB));
                await TearDown(minio, bucketName);
                new MintLogger(nameof(PutObject_Test3), putObjectSignature1, "Tests whether PutObject with custom content-type passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test3), putObjectSignature1, "Tests whether PutObject with custom content-type passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                Assert.IsTrue(statMeta.ContainsKey("X-Amz-Meta-Customheader"));
                Assert.IsTrue(statObject.MetaData.ContainsKey("Content-Type") && statObject.MetaData["Content-Type"].Equals("custom/contenttype"));
                await TearDown(minio, bucketName);
                new MintLogger(nameof(PutObject_Test4), putObjectSignature1, "Tests whether PutObject with different content-type passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger(nameof(PutObject_Test4), putObjectSignature1, "Tests whether PutObject with different content-type passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
            if (!IsMintEnv())
            {
                File.Delete(fileName);
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
                await TearDown(minio, bucketName);
                new MintLogger(nameof(PutObject_Test5), putObjectSignature1, "Tests whether PutObject with no content-type passes for small object", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test5), putObjectSignature1, "Tests whether PutObject with no content-type passes for small object", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task PutObject_Test6(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = "parallelput";
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectName },
                { "data", "10KB" },
                { "size", "10KB" },
            };
            try
            {
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[7];
                for (int i = 0; i < 7; i++) {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName, null, null, 0, null, rsg.GenerateStreamFromSeed(10*KB));
                }
                await Task.WhenAll(tasks);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
                new MintLogger(nameof(PutObject_Test6), putObjectSignature1, "Tests thread safety of minioclient on a parallel put operation", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test6), putObjectSignature1, "Tests thread safety of minioclient on a parallel put operation", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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

                        await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream,
                                                size,
                                                contentType);
                        await minio.RemoveObjectAsync(bucketName, objectName);
                        await TearDown(minio, bucketName);
                }
                new MintLogger(nameof(PutObject_Test7), putObjectSignature1, "Tests whether PutObject with unknown stream-size passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test7), putObjectSignature1, "Tests whether PutObject with unknown stream-size passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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

                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream,
                                            size,
                                            contentType);
                    await minio.RemoveObjectAsync(bucketName, objectName);
                    await TearDown(minio, bucketName);
                }
                new MintLogger(nameof(PutObject_Test8), putObjectSignature1, "Tests PutObject where unknown stream sends 0 bytes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(PutObject_Test8), putObjectSignature1, "Tests PutObject where unknown stream sends 0 bytes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        #endregion

        internal async static Task PutGetStatEncryptedObject_Test1(MinioClient minio)
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
                    string tempFileName = "tempFileName";
                    long file_read_size = 0;
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream,
                                            filestream.Length,
                                            contentType, sse: ssec);

                    await minio.GetObjectAsync(bucketName, objectName,
                    (stream) =>
                    {
                        var fileStream = File.Create(tempFileName);
                        stream.CopyTo(fileStream);
                        fileStream.Dispose();
                        FileInfo writtenInfo = new FileInfo(tempFileName);
                        file_read_size = writtenInfo.Length;

                        Assert.AreEqual(file_read_size, file_write_size);
                        File.Delete(tempFileName);
                    }, sse:ssec);
                    await minio.StatObjectAsync(bucketName, objectName, sse:ssec);
                    await minio.RemoveObjectAsync(bucketName, objectName);
                }
                await TearDown(minio, bucketName);

                new MintLogger("PutGetStatEncryptedObject_Test1", putObjectSignature1, "Tests whether Put/Get/Stat Object with encryption passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutGetStatEncryptedObject_Test1", putObjectSignature1, "Tests whether Put/Get/Stat Object with encryption passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task PutGetStatEncryptedObject_Test2(MinioClient minio)
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
                    string tempFileName = "tempFileName";
                    long file_read_size = 0;
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream,
                                            filestream.Length,
                                            contentType, sse: ssec);

                    await minio.GetObjectAsync(bucketName, objectName,
                    (stream) =>
                    {
                        var fileStream = File.Create(tempFileName);
                        stream.CopyTo(fileStream);
                        fileStream.Dispose();
                        FileInfo writtenInfo = new FileInfo(tempFileName);
                        file_read_size = writtenInfo.Length;

                        Assert.AreEqual(file_read_size, file_write_size);
                        File.Delete(tempFileName);
                    }, sse:ssec);
                    await minio.StatObjectAsync(bucketName, objectName, sse:ssec);
                    await minio.RemoveObjectAsync(bucketName, objectName);
                }
                await TearDown(minio, bucketName);

                new MintLogger("PutGetStatEncryptedObject_Test2", putObjectSignature1, "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutGetStatEncryptedObject_Test2", putObjectSignature2, "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task PutGetStatEncryptedObject_Test3(MinioClient minio)
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
                    string tempFileName = "tempFileName";
                    long file_read_size = 0;
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream,
                                            filestream.Length,
                                            contentType, sse: sses3);

                    await minio.GetObjectAsync(bucketName, objectName,
                    (stream) =>
                    {
                        var fileStream = File.Create(tempFileName);
                        stream.CopyTo(fileStream);
                        fileStream.Dispose();
                        FileInfo writtenInfo = new FileInfo(tempFileName);
                        file_read_size = writtenInfo.Length;

                        Assert.AreEqual(file_read_size, file_write_size);
                        File.Delete(tempFileName);
                    });
                    await minio.StatObjectAsync(bucketName, objectName);
                    await minio.RemoveObjectAsync(bucketName, objectName);
                }
                await TearDown(minio, bucketName);

                new MintLogger("PutGetStatEncryptedObject_Test3", putObjectSignature1, "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutGetStatEncryptedObject_Test3", putObjectSignature2, "Tests whether Put/Get/Stat multipart upload with encryption passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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

                await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream,
                                            size,
                                            contentType,
                                            metaData: metaData);
                File.Delete(tempFileName);
            }
        }

        internal async static Task<ObjectStat> PutObject_Tester(MinioClient minio, string bucketName, string objectName, string fileName = null, string contentType = "application/octet-stream", long size = 0, Dictionary<string, string> metaData = null, MemoryStream mstream = null)
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
                long file_read_size = 0;
                string tempFileName = "tempfile-" + GetRandomName(5);
                if (size == 0)
                {
                    size = filestream.Length;
                }

                await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream,
                                            size,
                                            contentType,
                                            metaData: metaData);
                await minio.GetObjectAsync(bucketName, objectName,
                (stream) =>
                {
                    var fileStream = File.Create(tempFileName);
                    stream.CopyTo(fileStream);
                    fileStream.Dispose();
                    FileInfo writtenInfo = new FileInfo(tempFileName);
                    file_read_size = writtenInfo.Length;

                    Assert.AreEqual(file_read_size, file_write_size);
                    File.Delete(tempFileName);
                });
                statObject = await minio.StatObjectAsync(bucketName, objectName);
                Assert.IsNotNull(statObject);
                Assert.AreEqual(statObject.ObjectName, objectName);
                Assert.AreEqual(statObject.Size, file_read_size);
                if (contentType != null)
                {
                    Assert.AreEqual(statObject.ContentType, contentType);
                }

                await minio.RemoveObjectAsync(bucketName, objectName);
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
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, null, 0, null, rsg.GenerateStreamFromSeed(1 * KB));

                await TearDown(minio, bucketName);
                new MintLogger(nameof(StatObject_Test1), statObjectSignature, "Tests whether StatObject passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger(nameof(StatObject_Test1), statObjectSignature, "Tests whether StatObject passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);
                }

                await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName);
                string outFileName = "outFileName";

                await minio.GetObjectAsync(destBucketName, destObjectName, outFileName);
                File.Delete(outFileName);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test1", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test1", copyObjectSignature, "Tests whether CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);
                }
                CopyConditions conditions = new CopyConditions();
                conditions.SetMatchETag("TestETag");
                try
                {
                    await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, conditions);

                }
                catch (MinioException ex)
                {
                    Assert.AreEqual(ex.Message, "MinIO API responded with message=At least one of the pre-conditions you specified did not hold");
                }

                await minio.RemoveObjectAsync(bucketName, objectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test2", copyObjectSignature, "Tests whether CopyObject with Etag mismatch passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test2", copyObjectSignature, "Tests whether CopyObject with Etag mismatch passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task CopyObject_Test3(MinioClient minio)
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);
                }
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);

                CopyConditions conditions = new CopyConditions();
                conditions.SetMatchETag(stats.ETag);
                await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, conditions);
                string outFileName = "outFileName";
                ObjectStat dstats = await minio.StatObjectAsync(destBucketName, destObjectName);
                Assert.IsNotNull(dstats);
                Assert.AreEqual(dstats.ObjectName, destObjectName);
                await minio.GetObjectAsync(destBucketName, destObjectName, outFileName);
                File.Delete(outFileName);

                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test3", copyObjectSignature, "Tests whether CopyObject with Etag match passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test3", copyObjectSignature, "Tests whether CopyObject with Etag match passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }

        }

        internal async static Task CopyObject_Test4(MinioClient minio)
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);
                }
                CopyConditions conditions = new CopyConditions();
                conditions.SetMatchETag("TestETag");
                // omit dest bucket name.
                await minio.CopyObjectAsync(bucketName, objectName, destBucketName);
                string outFileName = "outFileName";

                await minio.GetObjectAsync(bucketName, objectName, outFileName);
                File.Delete(outFileName);
                ObjectStat stats = await minio.StatObjectAsync(destBucketName, objectName);
                Assert.IsNotNull(stats);
                Assert.AreEqual(stats.ObjectName, objectName);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, objectName);
                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test4", copyObjectSignature, "Tests whether CopyObject defaults targetName to objectName", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test4", copyObjectSignature, "Tests whether CopyObject defaults targetName to objectName", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }

        }

        internal async static Task CopyObject_Test5(MinioClient minio)
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);
                }
                CopyConditions conditions = new CopyConditions();
                conditions.SetByteRange(1024, 6291455);

                // omit dest object name.
                await minio.CopyObjectAsync(bucketName, objectName, destBucketName, copyConditions: conditions);
                string outFileName = "outFileName";

                await minio.GetObjectAsync(bucketName, objectName, outFileName);
                File.Delete(outFileName);
                ObjectStat stats = await minio.StatObjectAsync(destBucketName, objectName);
                Assert.IsNotNull(stats);
                Assert.AreEqual(stats.ObjectName, objectName);
                Assert.AreEqual(stats.Size, 6291455 - 1024 + 1);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, objectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);

                new MintLogger("CopyObject_Test5", copyObjectSignature, "Tests whether CopyObject  multi-part copy upload for large files works", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                if (ex.message.Equals("A header you provided implies functionality that is not implemented"))
                {
                    new MintLogger("CopyObject_Test5", copyObjectSignature, "Tests whether CopyObject  multi-part copy upload for large files works", TestStatus.NA, (DateTime.Now - startTime), args:args).Log();
                }
                else
                {
                    new MintLogger("CopyObject_Test5", copyObjectSignature, "Tests whether CopyObject  multi-part copy upload for large files works", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
                }
            }

        }

        internal async static Task CopyObject_Test6(MinioClient minio)
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);
                }
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);

                CopyConditions conditions = new CopyConditions();
                conditions.SetModified(new DateTime(2017, 8, 18));
                // Should copy object since modification date header < object modification date.
                await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, conditions);
                string outFileName = "outFileName";
                ObjectStat dstats = await minio.StatObjectAsync(destBucketName, destObjectName);
                Assert.IsNotNull(dstats);
                Assert.AreEqual(dstats.ObjectName, destObjectName);
                await minio.GetObjectAsync(destBucketName, destObjectName, outFileName);
                File.Delete(outFileName);

                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test6", copyObjectSignature, "Tests whether CopyObject with positive test for modified date passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test6", copyObjectSignature, "Tests whether CopyObject with positive test for modified date passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);
                }
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);

                CopyConditions conditions = new CopyConditions();
                DateTime modifiedDate = DateTime.Now;
                modifiedDate = modifiedDate.AddDays(5);
                conditions.SetModified(modifiedDate);
                // Should not copy object since modification date header > object modification date.
                try
                {
                    await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, conditions);

                }
                catch (Exception ex)
                {
                    Assert.AreEqual("MinIO API responded with message=At least one of the pre-conditions you specified did not hold", ex.Message);
                }

                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);

                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test7", copyObjectSignature, "Tests whether CopyObject with negative test for modified date passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test7", copyObjectSignature, "Tests whether CopyObject with negative test for modified date passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }

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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, metaData:new Dictionary<string, string>{{"Orig", "orig-val with  spaces"}});
                }
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);

                Assert.IsTrue(stats.MetaData["X-Amz-Meta-Orig"] != null) ;

                CopyConditions copyCond = new CopyConditions();
                copyCond.SetReplaceMetadataDirective();

                // set custom metadata
                var metadata = new Dictionary<string, string>
                {
                    { "Content-Type", "application/css" },
                    { "Mynewkey", "test   test" }
                };
                await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, copyConditions:copyCond, metadata: metadata);

                ObjectStat dstats = await minio.StatObjectAsync(destBucketName, destObjectName);
                Assert.IsTrue(dstats.MetaData["X-Amz-Meta-Mynewkey"] != null);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test8", copyObjectSignature, "Tests whether CopyObject with metadata replacement passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test8", copyObjectSignature, "Tests whether CopyObject with metadata replacement passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null, sse:ssec);
                }

                await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, sseSrc:sseCpy, sseDest:ssecDst);
                string outFileName = "outFileName";

                await minio.GetObjectAsync(destBucketName, destObjectName, outFileName, sse:ssecDst);
                File.Delete(outFileName);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("EncryptedCopyObject_Test1", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("EncryptedCopyObject_Test1", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task EncryptedCopyObject_Test2(MinioClient minio)
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null, sse:ssec);
                }

                await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, sseSrc:sseCpy, sseDest:null);
                string outFileName = "outFileName";

                await minio.GetObjectAsync(destBucketName, destObjectName, outFileName);
                File.Delete(outFileName);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("EncryptedCopyObject_Test2", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("EncryptedCopyObject_Test2", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task EncryptedCopyObject_Test3(MinioClient minio)
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null, sse:ssec);
                }

                await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, sseSrc:sseCpy, sseDest:sses3);
                string outFileName = "outFileName";

                await minio.GetObjectAsync(destBucketName, destObjectName, outFileName);
                File.Delete(outFileName);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("EncryptedCopyObject_Test3", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("EncryptedCopyObject_Test3", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task EncryptedCopyObject_Test4(MinioClient minio)
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
                // Test Copy of SSE-S3 encrypted object to SSE-S3 on destination side
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);

                var sses3 = new SSES3();
                var sseDest = new SSES3();
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                {
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null, sse:sses3);
                }

                await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, sseSrc:null, sseDest:sses3);
                string outFileName = "outFileName";

                await minio.GetObjectAsync(destBucketName, destObjectName, outFileName);
                File.Delete(outFileName);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("EncryptedCopyObject_Test4", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("EncryptedCopyObject_Test4", copyObjectSignature, "Tests whether encrypted CopyObject passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                    string tempFileName = "tempFileName";
                    long file_read_size = 0;
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream,
                                            filestream.Length,
                                            contentType);

                    await minio.GetObjectAsync(bucketName, objectName,
                    (stream) =>
                    {
                        var fileStream = File.Create(tempFileName);
                        stream.CopyTo(fileStream);
                        fileStream.Dispose();
                        FileInfo writtenInfo = new FileInfo(tempFileName);
                        file_read_size = writtenInfo.Length;

                        Assert.AreEqual(file_read_size, file_write_size);
                        File.Delete(tempFileName);
                    });

                    await minio.RemoveObjectAsync(bucketName, objectName);
                }
                await TearDown(minio, bucketName);

                new MintLogger("GetObject_Test1", getObjectSignature1, "Tests whether GetObject as stream works", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("GetObject_Test1", getObjectSignature1, "Tests whether GetObject as stream works", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                try
                {
                    await minio.GetObjectAsync(bucketName, objectName, fileName);

                }
                catch (ObjectNotFoundException ex)
                {
                    Assert.AreEqual(ex.message, "Not found.");
                }

                await TearDown(minio, bucketName);
                new MintLogger("GetObject_Test2", getObjectSignature1, "Tests for non-existent GetObject", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("GetObject_Test2", getObjectSignature1, "Tests for non-existent GetObject", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }

        }

        internal async static Task GetObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            string contentType = null;
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
                    string tempFileName = "tempFileName";
                    long file_read_size = 0;
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream,
                                            filestream.Length,
                                            contentType);

                    await minio.GetObjectAsync(bucketName, objectName, 1024L, file_write_size,
                    (stream) =>
                    {
                        var fileStream = File.Create(tempFileName);
                        stream.CopyTo(fileStream);
                        fileStream.Dispose();
                        FileInfo writtenInfo = new FileInfo(tempFileName);
                        file_read_size = writtenInfo.Length;

                        Assert.AreEqual(file_read_size, file_write_size);
                        File.Delete(tempFileName);
                    });

                    await minio.RemoveObjectAsync(bucketName, objectName);
                }
                await TearDown(minio, bucketName);
                new MintLogger("GetObject_Test3", getObjectSignature2, "Tests whether GetObject returns all the data", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("GetObject_Test3", getObjectSignature2, "Tests whether GetObject returns all the data", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);

                }
                await minio.GetObjectAsync(bucketName, objectName, outFileName);
                File.Delete(outFileName);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
                new MintLogger("FGetObject_Test1", getObjectSignature3, "Tests whether FGetObject passes for small upload", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("FGetObject_Test1", getObjectSignature3, "Tests whether FGetObject passes for small upload", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            fileName);

                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                new MintLogger("FPutObject_Test1", putObjectSignature2, "Tests whether FPutObject for multipart upload passes", TestStatus.PASS, (DateTime.Now - startTime), args: args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("FPutObject_Test1", putObjectSignature2, "Tests whether FPutObject for multipart upload passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
            if (!IsMintEnv())
            {
                File.Delete(fileName);
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
                await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            fileName);

                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
                new MintLogger("FPutObject_Test2", putObjectSignature2, "Tests whether FPutObject for small upload passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("FPutObject_Test2", putObjectSignature2, "Tests whether FPutObject for small upload passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
            if (!IsMintEnv())
            {
                File.Delete(fileName);
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
                for (int i = 0; i < 2; i++) {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1));
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, prefix, 2, false).Wait();
                System.Threading.Thread.Sleep(2000);

                await minio.RemoveObjectAsync(bucketName, objectName + "0");
                await minio.RemoveObjectAsync(bucketName, objectName + "1");
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test1", listObjectsSignature, "Tests whether ListObjects lists all objects matching a prefix non-recursive", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test1", listObjectsSignature, "Tests whether ListObjects lists all objects matching a prefix non-recursive", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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

                ListObjects_Test(minio, bucketName, null, 0).Wait(1000);
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test2", listObjectsSignature, "Tests whether ListObjects passes when bucket is empty", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test2", listObjectsSignature, "Tests whether ListObjects passes when bucket is empty", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task ListObjects_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string prefix = "minix";
            string objectName = prefix + "/"+ GetRandomName(10) + "/suffix";
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
                for (int i = 0; i < 2; i++) {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1*KB));
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, prefix, 2, true).Wait();
                System.Threading.Thread.Sleep(2000);
                await minio.RemoveObjectAsync(bucketName, objectName + "0");
                await minio.RemoveObjectAsync(bucketName, objectName + "1");
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test3", listObjectsSignature, "Tests whether ListObjects lists all objects matching a prefix and recursive", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test3", listObjectsSignature, "Tests whether ListObjects lists all objects matching a prefix and recursive", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                for (int i = 0; i < 2; i++) {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1*KB));
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, "", 2, false).Wait();
                System.Threading.Thread.Sleep(2000);

                await minio.RemoveObjectAsync(bucketName, objectName + "0");
                await minio.RemoveObjectAsync(bucketName, objectName + "1");
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test4", listObjectsSignature, "Tests whether ListObjects lists all objects when no prefix is specified", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test4", listObjectsSignature, "Tests whether ListObjects lists all objects when no prefix is specified", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task ListObjects_Test5(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectNamePrefix = GetRandomName(10);
            var args = new Dictionary<string, string>
            {
                { "bucketName", bucketName },
                { "objectName", objectNamePrefix },
                { "recursive", "false" }
            };
            try
            {
                int numObjects = 100;
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[numObjects];
                for (int i = 1; i <= numObjects; i++) {
                    tasks[i - 1] = PutObject_Task(minio, bucketName, objectNamePrefix + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1));
                    // Add sleep to avoid flooding server with concurrent requests
                    if (i % 50 == 0) {
                        System.Threading.Thread.Sleep(2000);
                    }
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, objectNamePrefix, numObjects, false).Wait();
                System.Threading.Thread.Sleep(5000);
                for(int index=1; index <= numObjects; index++)
                {
                    string objectName = objectNamePrefix + index.ToString();
                    await minio.RemoveObjectAsync(bucketName, objectName);
                }
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test5", listObjectsSignature, "Tests whether ListObjects lists all objects when number of objects == 100", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test5", listObjectsSignature, "Tests whether ListObjects lists all objects when number of objects == 100", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task ListObjects_Test(MinioClient minio, string bucketName, string prefix, int numObjects, bool recursive = true)
        {
            DateTime startTime = DateTime.Now;
            int count = 0;
            IObservable<Item> observable = minio.ListObjectsAsync(bucketName, prefix, recursive);
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

                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                    await minio.RemoveObjectAsync(bucketName, objectName);
                    await TearDown(minio, bucketName);
                }
                new MintLogger("RemoveObject_Test1", removeObjectSignature1, "Tests whether RemoveObjectAsync for existing object passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("RemoveObject_Test1", removeObjectSignature1, "Tests whether RemoveObjectAsync for existing object passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task RemoveObjects_Test2(MinioClient minio)
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
                Task[] tasks = new Task[count];
                List<string> objectsList = new List<string>();
                await Setup_Test(minio, bucketName);
                for (int i = 0; i < count; i++)
                {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(5));
                    objectsList.Add(objectName + i.ToString());
                }
                Task.WhenAll(tasks).Wait();
                System.Threading.Thread.Sleep(1000);
                DeleteError de;
                IObservable<DeleteError> observable = await minio.RemoveObjectAsync(bucketName, objectsList);
                IDisposable subscription = observable.Subscribe(
                   deleteError => de = deleteError,
                   () =>
                   {
                       TearDown(minio, bucketName).Wait();
                   });
                new MintLogger("RemoveObject_Test2", removeObjectSignature2, "Tests whether RemoveObjectAsync for multi objects delete passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("RemoveObjects_Test2", removeObjectSignature2, "Tests whether RemoveObjectAsync for multi objects delete passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);
                string presigned_url = await minio.PresignedGetObjectAsync(bucketName, objectName, expiresInt);
                WebRequest httpRequest = WebRequest.Create(presigned_url);
                var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null));
                Stream stream = response.GetResponseStream();
                var fileStream = File.Create(downloadFile);
                stream.CopyTo(fileStream);
                fileStream.Dispose();
                FileInfo writtenInfo = new FileInfo(downloadFile);
                long file_read_size = writtenInfo.Length;
                // Compare size of file downloaded  with presigned curl request and actual object size on server
                Assert.AreEqual(file_read_size, stats.Size);

                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                File.Delete(downloadFile);
                new MintLogger("PresignedGetObject_Test1", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("PresignedGetObject_Test1", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                try
                {
                    await Setup_Test(minio, bucketName);
                    using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                        await minio.PutObjectAsync(bucketName,
                                                    objectName,
                                                    filestream, filestream.Length, null);
                    ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);
                    string presigned_url = await minio.PresignedGetObjectAsync(bucketName, objectName, 0);
                }
                catch (InvalidExpiryRangeException)
                {
                    new MintLogger("PresignedGetObject_Test2", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
                }
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedGetObject_Test2", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }

        internal async static Task PresignedGetObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomObjectName(10);
            int expiresInt = 1000;
            DateTime reqDate = DateTime.UtcNow.AddSeconds(-50);
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
                string downloadFile = "downloadFileName";
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);
                var reqParams = new Dictionary<string, string>
                {
                    ["response-content-type"] = "application/json",
                    ["response-content-disposition"] = "attachment;filename=MyDocument.json;"
                };
                string presigned_url = await minio.PresignedGetObjectAsync(bucketName, objectName, 1000, reqParams, reqDate);
                WebRequest httpRequest = WebRequest.Create(presigned_url);
                var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null));
                Assert.AreEqual(response.ContentType, reqParams["response-content-type"]);
                Assert.AreEqual(response.Headers["Content-Disposition"], "attachment;filename=MyDocument.json;");
                Assert.AreEqual(response.Headers["Content-Type"], "application/json");
                Assert.AreEqual(response.Headers["Content-Length"], stats.Size.ToString());
                Stream stream = response.GetResponseStream();
                var fileStream = File.Create(downloadFile);
                stream.CopyTo(fileStream);
                fileStream.Dispose();
                FileInfo writtenInfo = new FileInfo(downloadFile);
                long file_read_size = writtenInfo.Length;

                // Compare size of file downloaded  with presigned curl request and actual object size on server
                Assert.AreEqual(file_read_size, stats.Size);

                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                File.Delete(downloadFile);
                new MintLogger("PresignedGetObject_Test3", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket when override response headers sent", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("PresignedGetObject_Test3", presignedGetObjectSignature, "Tests whether PresignedGetObject url retrieves object from bucket when override response headers sent", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                string presigned_url = await minio.PresignedPutObjectAsync(bucketName, objectName, 1000);
                await UploadObjectAsync(presigned_url, fileName);
                // Get stats for object from server
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);
                // Compare with file used for upload
                FileInfo writtenInfo = new FileInfo(fileName);
                long file_written_size = writtenInfo.Length;
                Assert.AreEqual(file_written_size, stats.Size);

                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                new MintLogger("PresignedPutObject_Test1", presignedPutObjectSignature, "Tests whether PresignedPutObject url uploads object to bucket", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("PresignedPutObject_Test1", presignedPutObjectSignature, "Tests whether PresignedPutObject url uploads object to bucket", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
            if (!IsMintEnv())
            {
                File.Delete(fileName);
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
                try
                {
                    await Setup_Test(minio, bucketName);
                    using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * KB))
                        await minio.PutObjectAsync(bucketName,
                                                    objectName,
                                                    filestream, filestream.Length, null);
                    ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);
                    string presigned_url = await minio.PresignedPutObjectAsync(bucketName, objectName, 0);
                }
                catch (InvalidExpiryRangeException)
                {
                    new MintLogger("PresignedPutObject_Test2", presignedPutObjectSignature, "Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
                }
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedPutObject_Test2", presignedPutObjectSignature, "Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
            PostPolicy form = new PostPolicy();
            DateTime expiration = DateTime.UtcNow;
            form.SetExpires(expiration.AddDays(10));
            form.SetKey(objectName);
            form.SetBucket(bucketName);
            form.SetUserMetadata(metadataKey, metadataValue);
            var args = new Dictionary<string, string>
            {
                { "form", form.Base64() },
            };
            string fileName = CreateFile(10 * KB, dataFile10KB);

            try
            {
                await Setup_Test(minio, bucketName);
                await minio.PutObjectAsync(bucketName,
                            objectName,
                            fileName);
                var pairs = new List<KeyValuePair<string, string>>();
                string url = "https://s3.amazonaws.com/" + bucketName;
                Tuple<string, System.Collections.Generic.Dictionary<string, string>> policyTuple = await minio.PresignedPostPolicyAsync(form);
                var httpClient = new HttpClient();

                using (var stream = File.OpenRead(fileName))
                {
                    MultipartFormDataContent multipartContent = new MultipartFormDataContent();
                    multipartContent.Add(new StreamContent(stream), fileName, objectName);
                    multipartContent.Add(new FormUrlEncodedContent(pairs));
                    var response = await httpClient.PostAsync(url, multipartContent);
                    response.EnsureSuccessStatusCode();
                }

                // Validate
                string policy = await minio.GetPolicyAsync(bucketName);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
                new MintLogger("PresignedPostPolicy_Test1", presignedPostPolicySignature, "Tests whether PresignedPostPolicy url applies policy on server", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedPostPolicy_Test1", presignedPostPolicySignature, "Tests whether PresignedPostPolicy url applies policy on server", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }

            await minio.RemoveObjectAsync(bucketName, objectName);

            await TearDown(minio, bucketName);
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
                cts.CancelAfter(TimeSpan.FromMilliseconds(50));
                try
                {
                    using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(10 * MB))
                    {
                        long file_write_size = filestream.Length;

                        await minio.PutObjectAsync(bucketName,
                                                    objectName,
                                                    filestream,
                                                    filestream.Length,
                                                    contentType, cancellationToken: cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    IObservable<Upload> observable = minio.ListIncompleteUploads(bucketName);

                    IDisposable subscription = observable.Subscribe(
                        item => Assert.AreEqual(item.Key, objectName),
                        ex => Assert.Fail());

                    await minio.RemoveIncompleteUploadAsync(bucketName, objectName);
                }
                catch (Exception ex)
                {
                    new MintLogger("ListIncompleteUpload_Test1", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString()).Log();
                    return;
                }
                await TearDown(minio, bucketName);
                new MintLogger("ListIncompleteUpload_Test1", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes", TestStatus.PASS, (DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListIncompleteUpload_Test1", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString()).Log();
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
                cts.CancelAfter(TimeSpan.FromMilliseconds(60));
                try
                {
                    using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(10 * MB))
                    {
                        long file_write_size = filestream.Length;

                        await minio.PutObjectAsync(bucketName,
                                                    objectName,
                                                    filestream,
                                                    filestream.Length,
                                                    contentType, cancellationToken: cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    IObservable<Upload> observable = minio.ListIncompleteUploads(bucketName, "minioprefix", false);

                    IDisposable subscription = observable.Subscribe(
                        item => Assert.AreEqual(item.Key, objectName),
                        ex => Assert.Fail());

                    await minio.RemoveIncompleteUploadAsync(bucketName, objectName);
                }
                await TearDown(minio, bucketName);
                new MintLogger("ListIncompleteUpload_Test2", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes when qualified by prefix", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListIncompleteUpload_Test2", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes when qualified by prefix", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                cts.CancelAfter(TimeSpan.FromMilliseconds(50));
                try
                {
                    using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(6 * MB))
                    {
                        long file_write_size = filestream.Length;

                        await minio.PutObjectAsync(bucketName,
                                                    objectName,
                                                    filestream,
                                                    filestream.Length,
                                                    contentType, cancellationToken: cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    IObservable<Upload> observable = minio.ListIncompleteUploads(bucketName, prefix, true);

                    IDisposable subscription = observable.Subscribe(
                        item => Assert.AreEqual(item.Key, objectName),
                        ex => Assert.Fail());

                    await minio.RemoveIncompleteUploadAsync(bucketName, objectName);
                }
                await TearDown(minio, bucketName);
                new MintLogger("ListIncompleteUpload_Test3", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes when qualified by prefix and recursive", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListIncompleteUpload_Test3", listIncompleteUploadsSignature, "Tests whether ListIncompleteUpload passes when qualified by prefix and recursive", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                cts.CancelAfter(TimeSpan.FromMilliseconds(10));
                try
                {
                    using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(6 * MB))
                    {
                        long file_write_size = filestream.Length;

                        await minio.PutObjectAsync(bucketName,
                                                    objectName,
                                                    filestream,
                                                    filestream.Length,
                                                    contentType, cancellationToken: cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    await minio.RemoveIncompleteUploadAsync(bucketName, objectName);

                    IObservable<Upload> observable = minio.ListIncompleteUploads(bucketName);

                    IDisposable subscription = observable.Subscribe(
                        item => Assert.Fail(),
                        ex => Assert.Fail());
                }
                await TearDown(minio, bucketName);
                new MintLogger("RemoveIncompleteUpload_Test", removeIncompleteUploadSignature, "Tests whether RemoveIncompleteUpload passes.", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("RemoveIncompleteUpload_Test", removeIncompleteUploadSignature, "Tests whether RemoveIncompleteUpload passes.", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                string policyJson = $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}/foo*"",""arn:aws:s3:::{bucketName}/prefix/*""],""Sid"":""""}}]}}";
                await minio.SetPolicyAsync(bucketName,
                                    policyJson);
                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                new MintLogger("SetBucketPolicy_Test1", setBucketPolicySignature, "Tests whether SetBucketPolicy passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("SetBucketPolicy_Test1", setBucketPolicySignature, "Tests whether SetBucketPolicy passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                await minio.SetPolicyAsync(bucketName,
                                    policyJson);
                string policy = await minio.GetPolicyAsync(bucketName);
                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }
        }
        #endregion


        #region Bucket Notifications

        internal async static Task ListenBucketNotificationsAsync_Test1(MinioClient minio)
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
                Console.WriteLine($"ListenBucketNotificationsAsync starting: bucketName={bucketName}");

                await Setup_Test(minio, bucketName);
                // Thread.Sleep(10 * 1000);
                Console.WriteLine($"ListenBucketNotificationsAsync starting: bucketName={bucketName}");

                var received = new List<MinioNotificationRaw>();

                IObservable<MinioNotificationRaw> events = minio.ListenBucketNotificationsAsync(bucketName, new List<EventType> { EventType.ObjectCreatedAll });
                IDisposable subscription = events.Subscribe(
                    ev => {
                        Console.WriteLine($"ListenBucketNotificationsAsync received: " + ev.json);
                        received.Add(ev);
                    },
                    ex => throw ex,
                    // ex => new MintLogger(nameof(ListenBucketNotificationsAsync_Test1), listenBucketNotificationsSignature, "ListenBucketNotificationsAsync_Test1", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log(),
                    () => Console.WriteLine($"ListenBucketNotificationsAsync finished"));
                Thread.Sleep(2 * 1000);

                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(1 * KB));

                await TearDown(minio, bucketName);
                subscription.Dispose();

                // wait for notifications
                var testOutcome = TestStatus.FAIL;
                for (int attempt = 0; attempt < 10; attempt++) {

                    if (received.Count > 0) {
                        MinioNotification notification = JsonConvert.DeserializeObject<MinioNotification>(received[0].json);

                        Console.WriteLine($"   round-trip deserialisation: {JsonConvert.SerializeObject(notification, Formatting.Indented)}");

                        Assert.AreEqual(1, notification.Records.Length);
                        Assert.AreEqual("s3:ObjectCreated:Put", notification.Records[0].eventName);
                        Assert.AreEqual(bucketName, notification.Records[0].s3.bucketMeta.name);
                        Assert.AreEqual(objectName, System.Web.HttpUtility.UrlDecode(notification.Records[0].s3.objectMeta.key));
                        Assert.AreEqual(contentType, notification.Records[0].s3.objectMeta.contentType);
                        Console.WriteLine("PASSED");
                        testOutcome = TestStatus.PASS;
                        break;
                    } else {
                        Console.WriteLine($"ListenBucketNotificationsAsync: waiting for notification (t={attempt})");
                    }

                    Thread.Sleep(2000);
                }
                Console.WriteLine($"outcome: {testOutcome}");

                new MintLogger(nameof(ListenBucketNotificationsAsync_Test1), listenBucketNotificationsSignature, "Tests whether ListenBucketNotifications passes for small object", testOutcome, (DateTime.Now - startTime), args:args).Log();
            }
            catch (Exception ex)
            {
                new MintLogger(nameof(ListenBucketNotificationsAsync_Test1), listenBucketNotificationsSignature, "Tests whether ListenBucketNotifications passes for small object", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
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
                var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvString.ToString());
                using (var stream = new MemoryStream(csvBytes))
                {
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            stream, stream.Length, null);

                }
                var opts = new SelectObjectOptions()
                {
                    ExpressionType = QueryExpressionType.SQL,
                    Expression = "select * from s3object",
                    InputSerialization = new SelectObjectInputSerialization()
                    {
                        CompressionType = SelectCompressionType.NONE,
                        CSV = new CSVInputOptions()
                        {
                            FileHeaderInfo = CSVFileHeaderInfo.None,
				            RecordDelimiter = "\n",
				            FieldDelimiter = ",",
                        }
                    },
                    OutputSerialization = new SelectObjectOutputSerialization()
                    {
                        CSV = new CSVOutputOptions()
                        {
                            RecordDelimiter = "\n",
                            FieldDelimiter =  ",",
                        }
                    }
                };

                var resp = await  minio.SelectObjectContentAsync(bucketName, objectName, opts);
                var output = await new StreamReader(resp.Payload).ReadToEndAsync();
                Assert.AreEqual(output,csvString.ToString());
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
                new MintLogger("SelectObjectContent_Test", selectObjectSignature, "Tests whether SelectObjectContent passes for a select query", TestStatus.PASS, (DateTime.Now - startTime), args:args).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("SelectObjectContent_Test", selectObjectSignature, "Tests whether SelectObjectContent passes for a select query", TestStatus.FAIL, (DateTime.Now - startTime), "", ex.Message, ex.ToString(), args).Log();
            }

        }

        #endregion
    }
}
