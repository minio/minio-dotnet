/*
* Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Minio.DataModel;
using Minio.Exceptions;

namespace Minio.Functional.Tests

{
    class FunctionalTest
    {
        private static Random rnd = new Random();
        private static int MB = 1024 * 1024;
        private static string dataFile1MB = "datafile-1-MB";
        private static string dataFile6MB = "datafile-6-MB";

        private static RandomStreamGenerator rsg = new RandomStreamGenerator(100 * MB);

        // Create a file of given size from random byte array or optionally create a symbolic link
        // to the dataFileName residing in MINT_DATA_DIR
        private static String CreateFile(int size, string dataFileName = null)
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

        // Generate a random string
        public static String GetRandomName(int length = 5)
        {
            string characters = "0123456789abcdefghijklmnopqrstuvwxyz";
            if (length > 50)
                length = 50;
            StringBuilder result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(characters[rnd.Next(characters.Length)]);
            }
            return "miniodotnet" + result.ToString();
        }
        // Return true if running in Mint mode        
        public static bool IsMintEnv() 
        {
            return !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("MINT_DATA_DIR"));
        }
        // Get full path of file
        public static string GetFilePath(string fileName)
        {
            var dataDir = Environment.GetEnvironmentVariable("MINT_DATA_DIR");
            if (!String.IsNullOrEmpty(dataDir))
            {
                return dataDir + "/" + fileName;
            }
            else
            {
                string path = Directory.GetCurrentDirectory();
                return path + "/" + fileName;
            }
        }
        public static void Main(string[] args)
        {
            String endPoint = null;
            String accessKey = null;
            String secretKey = null;
            String enableHttps = "0";

            bool useAWS = Environment.GetEnvironmentVariable("AWS_ENDPOINT") != null;
            if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") != null)
            {
                endPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
                accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
                secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
                enableHttps = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
            }
            else
            {
                endPoint = "play.minio.io:9000";
                accessKey = "Q3AM3UQ867SPQQA43P2F";
                secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
                enableHttps = "1";
            }

            MinioClient minioClient = null;
            if (enableHttps.Equals("1"))
                // WithSSL() enables SSL support in Minio client
                minioClient = new MinioClient(endPoint, accessKey, secretKey).WithSSL();
            else
                minioClient = new MinioClient(endPoint, accessKey, secretKey);

            // Assign parameters before starting the test 
            string bucketName = GetRandomName();
            string objectName = GetRandomName();
            string destBucketName = GetRandomName();
            string destObjectName = GetRandomName();

            // Set app Info 
            minioClient.SetAppInfo("app-name", "app-version");
            // Set HTTP Tracing On
            // minioClient.SetTraceOn();

            // Set HTTP Tracing Off
            // minioClient.SetTraceOff();

            string runMode = Environment.GetEnvironmentVariable("MINT_MODE");

            if (!String.IsNullOrEmpty(runMode) && runMode.Equals("quick"))
            {
                runQuickTests(minioClient);
                System.Environment.Exit(0);
            }
            // Check if bucket exists
            BucketExists_Test(minioClient).Wait();

            // Create a new bucket
            MakeBucket_Test1(minioClient).Wait();
            MakeBucket_Test2(minioClient).Wait();
            if (useAWS)
            {
                MakeBucket_Test3(minioClient).Wait();
                MakeBucket_Test4(minioClient).Wait();
            }

            // Test removal of bucket
            RemoveBucket_Test1(minioClient).Wait();

            // Test ListBuckets function
            ListBuckets_Test(minioClient).Wait();

            // Test Putobject function
            PutObject_Test1(minioClient).Wait();
            PutObject_Test2(minioClient).Wait();

            PutObject_Test3(minioClient).Wait();
            PutObject_Test4(minioClient).Wait();
            PutObject_Test5(minioClient).Wait();
            PutObject_Test6(minioClient).Wait();
            PutObject_Test7(minioClient).Wait();
            PutObject_Test8(minioClient).Wait();

            // Test StatObject function
            StatObject_Test1(minioClient).Wait();

            // Test GetObjectAsync function
            GetObject_Test1(minioClient).Wait();
            GetObject_Test2(minioClient).Wait();
            GetObject_Test3(minioClient).Wait();

            // Test File GetObject and PutObject functions

            FGetObject_Test1(minioClient).Wait();
            // FIX=> FPutObject_Test1(minioClient).Wait();
            FPutObject_Test2(minioClient).Wait();

            // Test ListObjectAsync function
            ListObjects_Test1(minioClient).Wait();
            ListObjects_Test2(minioClient).Wait();
            ListObjects_Test3(minioClient).Wait();
            ListObjects_Test4(minioClient).Wait();
            ListObjects_Test5(minioClient).Wait();

            // Test RemoveObjectAsync function
            RemoveObject_Test1(minioClient).Wait();
            RemoveObjects_Test2(minioClient).Wait();

            // Test CopyObjectAsync function
            CopyObject_Test1(minioClient).Wait();
            CopyObject_Test2(minioClient).Wait();
            CopyObject_Test3(minioClient).Wait();
            CopyObject_Test4(minioClient).Wait();
            CopyObject_Test5(minioClient).Wait();
            CopyObject_Test6(minioClient).Wait();
            CopyObject_Test7(minioClient).Wait();

            // Test SetPolicyAsync function
            SetBucketPolicy_Test1(minioClient).Wait();

            // Test Presigned Get/Put operations
            PresignedGetObject_Test1(minioClient).Wait();
            PresignedGetObject_Test1(minioClient).Wait();
            PresignedGetObject_Test2(minioClient).Wait();
            PresignedGetObject_Test3(minioClient).Wait();
            PresignedPutObject_Test1(minioClient).Wait();
            PresignedPutObject_Test2(minioClient).Wait();
            // Test incomplete uploads
            ListIncompleteUpload_Test1(minioClient).Wait();
            ListIncompleteUpload_Test2(minioClient).Wait();
            ListIncompleteUpload_Test3(minioClient).Wait();
            RemoveIncompleteUpload_Test(minioClient).Wait();

            // Test GetBucket policy

            GetBucketPolicy_Test1(minioClient).Wait();
        }
        private static void runQuickTests(MinioClient minioClient)
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
        private async static Task BucketExists_Test(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName();
                await minio.MakeBucketAsync(bucketName);
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                await minio.RemoveBucketAsync(bucketName);
                new MintLogger("BucketExists_Test","Tests whether BucketExists passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("BucketExists_Test","Tests whether BucketExists passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }           
        }

        private async static Task MakeBucket_Test1(MinioClient minio)
        { 
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(length: 60);
                await minio.MakeBucketAsync(bucketName);
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                await minio.RemoveBucketAsync(bucketName);
                new MintLogger("MakeBucket_Test1","Tests whether MakeBucket passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("MakeBucket_Test1","Tests whether MakeBucket passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message,ex.ToString()).Log();
            }      
        }

        private async static Task MakeBucket_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string testType = "Test whether make bucket passes when bucketname has a period.";
            try
            {
                string bucketName = GetRandomName(length: 10) + ".withperiod";
                await minio.MakeBucketAsync(bucketName);
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                await minio.RemoveBucketAsync(bucketName);
                new MintLogger("MakeBucket_Test2",testType,TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("MakeBucket_Test2",testType,TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
 
        }
        private async static Task MakeBucket_Test3(MinioClient minio, bool aws = false)
        {
            if (!aws)
                return;
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 60);
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
                new MintLogger("MakeBucket_Test3","Tests whether MakeBucket with region passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();

            }
            catch (MinioException ex)
            {
               // Assert.AreEqual<string>(ex.message, "Your previous request to create the named bucket succeeded and you already own it.");
                new MintLogger("MakeBucket_Test3","Tests whether MakeBucket with region passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task MakeBucket_Test4(MinioClient minio, bool aws = false)
        {
            if (!aws)
                return;
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(length: 20) + ".withperiod";
            try
            {
                await minio.MakeBucketAsync(bucketName, location: "us-west-2");
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                if (found)
                {
                    await minio.RemoveBucketAsync(bucketName);

                }
                new MintLogger("MakeBucket_Test4","Tests whether MakeBucket with region and bucketname with . passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("MakeBucket_Test1","Tests whether MakeBucket with region and bucketname with . passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
                //Assert.Fail();
            }
        }

        private async static Task RemoveBucket_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(length: 60);
                await minio.MakeBucketAsync(bucketName);
                bool found = await minio.BucketExistsAsync(bucketName);
                Assert.IsTrue(found);
                await minio.RemoveBucketAsync(bucketName);
                found = await minio.BucketExistsAsync(bucketName);
                Assert.IsFalse(found);
                new MintLogger("RemoveBucket_Test1","Tests whether RemoveBucket passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("RemoveBucket_Test1","Tests whether RemoveBucket passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task ListBuckets_Test(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                var list = await minio.ListBucketsAsync();
                foreach (Bucket bucket in list.Buckets)
                {
                    // Ignore
                    continue;
                }
                new MintLogger("ListBuckets_Test","Tests whether ListBucket passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();

            }
            catch (Exception ex)
            {
                new MintLogger("ListBuckets_Test","Tests whether ListBucket passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task Setup_Test(MinioClient minio, string bucketName)
        {
            await minio.MakeBucketAsync(bucketName);
            bool found = await minio.BucketExistsAsync(bucketName);
            Assert.IsTrue(found);
        }

        private async static Task TearDown(MinioClient minio, string bucketName)
        {
            await minio.RemoveBucketAsync(bucketName);
        }
        private async static Task PutObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = "application/octet-stream";
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(1 * MB));
                await TearDown(minio, bucketName);
                new MintLogger("PutObject_Test1","Tests whether PutObject passes for small object",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutObject_Test1","Tests whether PutObject passes for small object",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

 
        private async static Task PutObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = "application/octet-stream";
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(8 * MB));
                await TearDown(minio, bucketName);
                new MintLogger("PutObject_Test2","Tests whether multipart PutObject passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutObject_Test2","Tests whether multipart PutObject passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task PutObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = "custom-contenttype";
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null, rsg.GenerateStreamFromSeed(1 * MB));
                await TearDown(minio, bucketName);
                new MintLogger("PutObject_Test3","Tests whether PutObject with custom content-type passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutObject_Test3","Tests whether PutObject with custom content-type passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task PutObject_Test4(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                // Putobject call with incorrect size of stream. See if PutObjectAsync call resumes 
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = "application/octet-stream";

                await Setup_Test(minio, bucketName);
                using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                {
                    try
                    {
                        long size = 4 * MB;
                        long file_write_size = filestream.Length;

                        await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream,
                                                size,
                                                contentType);
                    }
                    catch (UnexpectedShortReadException)
                    {
                        // PutObject failed as expected since the stream size is incorrect
                        // default to actual stream size and complete the upload
                        await PutObject_Tester(minio, bucketName, objectName, null, contentType, 0, null,rsg.GenerateStreamFromSeed(1 * MB));
                    }
                }
                await TearDown(minio, bucketName);
                new MintLogger("PutObject_Test4","Tests whether PutObject with incorrect stream-size passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutObject_Test4","Tests whether PutObject with incorrect stream-size passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task PutObject_Test5(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string fileName = CreateFile(1 * MB, dataFile1MB);
                string contentType = "custom/contenttype";
                Dictionary<string, string> metaData = new Dictionary<string, string>(){
                    { "x-amz-meta-customheader", "minio-dotnet"}
                };
                await Setup_Test(minio, bucketName);
                ObjectStat statObject = await PutObject_Tester(minio, bucketName, objectName, fileName, contentType: contentType, metaData: metaData);
                Assert.IsTrue(statObject != null);
                Assert.IsTrue(statObject.metaData != null);
                Dictionary<string, string> statMeta = new Dictionary<string, string>(statObject.metaData, StringComparer.OrdinalIgnoreCase);

                Assert.IsTrue(statMeta.ContainsKey("x-amz-meta-customheader"));
                Assert.IsTrue(statObject.metaData.ContainsKey("Content-Type") && statObject.metaData["Content-Type"].Equals("custom/contenttype"));
                await TearDown(minio, bucketName);
                if (!IsMintEnv())
                {
                    File.Delete(fileName);
                }
                new MintLogger("PutObject_Test5","Tests whether PutObject with different content-type passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("PutObject_Test5","Tests whether PutObject with different content-type passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task PutObject_Test6(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                await PutObject_Tester(minio, bucketName, objectName, null, null, 0, null, rsg.GenerateStreamFromSeed(1 * MB));
                await TearDown(minio, bucketName);
                new MintLogger("PutObject_Test6","Tests whether PutObject with no content-type passes for small object",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutObject_Test6","Tests whether PutObject with no content-type passes for small object",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task PutObject_Test7(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[7];
                for (int i = 0; i < 7; i++) {
                    tasks[i]= PutObject_Task(minio, bucketName, objectName, null, null, 0, null, rsg.GenerateStreamFromSeed(1*MB));
                }
                await Task.WhenAll(tasks);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
                new MintLogger("PutObject_Test7","Tests thread safety of minioclient on a parallel put operation",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutObject_Test7","Tests thread safety of minioclient on a parallel put operation",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task PutObject_Test8(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try {
                // Putobject call with unknown stream size. See if PutObjectAsync call succeeds 
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = "application/octet-stream";

                await Setup_Test(minio, bucketName);
                using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
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
                new MintLogger("PutObject_Test8","Tests whether PutObject with unknown stream-size passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PutObject_Test8","Tests whether PutObject with unknown stream-size passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task PutObject_Task(MinioClient minio, string bucketName, string objectName, string fileName = null, string contentType = "application/octet-stream", long size = 0, Dictionary<string, string> metaData = null, MemoryStream mstream = null)
        {
            try
            {
                System.IO.MemoryStream filestream = mstream;
                if (filestream == null)
                {
                    byte[] bs = File.ReadAllBytes(fileName);
                    filestream = new System.IO.MemoryStream(bs);

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
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
                Assert.Fail();
            }
            return;
        }
        private async static Task<ObjectStat> PutObject_Tester(MinioClient minio, string bucketName, string objectName, string fileName = null, string contentType = "application/octet-stream", long size = 0, Dictionary<string, string> metaData = null, MemoryStream mstream = null)
        {
            ObjectStat statObject = null;

            try
            {
                System.IO.MemoryStream filestream = mstream;
                if (filestream == null)
                {
                    byte[] bs = File.ReadAllBytes(fileName);
                    filestream = new System.IO.MemoryStream(bs);

                }
                using (filestream)
                {
                    long file_write_size = filestream.Length;
                    long file_read_size = 0;
                    string tempFileName = "tempfile-" + GetRandomName(5);
                    if (size == 0)
                        size = filestream.Length;

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
                        Assert.AreEqual(statObject.ContentType, contentType);

                    await minio.RemoveObjectAsync(bucketName, objectName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
                Assert.Fail();
            }
            return statObject;
        }
        private async static Task StatObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = "gzip";
                await Setup_Test(minio, bucketName);
                try
                {
                    using (var filestream = rsg.GenerateStreamFromSeed(1 * MB))
                    {
                        long file_write_size = filestream.Length;

                        await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream,
                                            filestream.Length,
                                            contentType);
                        ObjectStat statObject = await minio.StatObjectAsync(bucketName, objectName);
                        Assert.IsNotNull(statObject);
                        Assert.AreEqual(statObject.ObjectName, objectName);
                        Assert.AreEqual(statObject.Size, file_write_size);
                        Assert.AreEqual(statObject.ContentType, contentType);

                        await minio.RemoveObjectAsync(bucketName, objectName);
                    }


                }
                catch (Exception e)
                {
                    new MintLogger("StatObject_Test1","Tests whether StatObject passes",TestStatus.FAIL,(DateTime.Now - startTime),"",e.Message, e.ToString()).Log();
                    Assert.Fail();
                }
                await TearDown(minio, bucketName);
                new MintLogger("StatObject_Test1","Tests whether StatObject passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("StatObject_Test1","Tests whether StatObject passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task CopyObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string destBucketName = GetRandomName(15);
                string destObjectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
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
                new MintLogger("CopyObject_Test1","Tests whether CopyObject passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test1","Tests whether CopyObject passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task CopyObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                // Test CopyConditions where matching ETag is not found
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string destBucketName = GetRandomName(15);
                string destObjectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
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
                    Assert.AreEqual(ex.Message, "Minio API responded with message=At least one of the pre-conditions you specified did not hold");
                }

                await minio.RemoveObjectAsync(bucketName, objectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test2","Tests whether CopyObject with Etag mismatch passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test2","Tests whether CopyObject with Etag mismatch passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task CopyObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                // Test CopyConditions where matching ETag is found
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string destBucketName = GetRandomName(15);
                string destObjectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                {
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);
                }
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);

                CopyConditions conditions = new CopyConditions();
                conditions.SetMatchETag(stats.ETag);
                try
                {
                    await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, conditions);

                }
                catch (MinioException)
                {
                    Assert.Fail();
                }

                string outFileName = "outFileName";
                ObjectStat dstats = await minio.StatObjectAsync(destBucketName, destObjectName);
                Assert.IsNotNull(dstats);
                Assert.AreEqual(dstats.ETag, stats.ETag);
                Assert.AreEqual(dstats.ObjectName, destObjectName);
                await minio.GetObjectAsync(destBucketName, destObjectName, outFileName);
                File.Delete(outFileName);

                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test3","Tests whether CopyObject with Etag match passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test3","Tests whether CopyObject with Etag match passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }

        }
        private async static Task CopyObject_Test4(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {

            // Test if objectName is defaulted to source objectName
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string destBucketName = GetRandomName(15);
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);

                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
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
                new MintLogger("CopyObject_Test4","Tests whether CopyObject defaults targetName to objectName",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test4","Tests whether CopyObject defaults targetName to objectName",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }

        }
        private async static Task CopyObject_Test5(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                // Test if multi-part copy upload for large files works as expected.
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string destBucketName = GetRandomName(15);
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

                new MintLogger("CopyObject_Test5","Tests whether CopyObject  multi-part copy upload for large files works",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test5","Tests whether CopyObject  multi-part copy upload for large files works",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
       
        }

        private async static Task CopyObject_Test6(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                // Test CopyConditions where matching ETag is found
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string destBucketName = GetRandomName(15);
                string destObjectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                {
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);
                }
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);

                CopyConditions conditions = new CopyConditions();
                conditions.SetModified(new DateTime(2017, 8, 18));
                // Should copy object since modification date header < object modification date.
                try
                {
                    await minio.CopyObjectAsync(bucketName, objectName, destBucketName, destObjectName, conditions);

                }
                catch (MinioException)
                {
                    Assert.Fail();
                }

                string outFileName = "outFileName";
                ObjectStat dstats = await minio.StatObjectAsync(destBucketName, destObjectName);
                Assert.IsNotNull(dstats);
                Assert.AreEqual(dstats.ETag, stats.ETag);
                Assert.AreEqual(dstats.ObjectName, destObjectName);
                await minio.GetObjectAsync(destBucketName, destObjectName, outFileName);
                File.Delete(outFileName);

                await minio.RemoveObjectAsync(bucketName, objectName);
                await minio.RemoveObjectAsync(destBucketName, destObjectName);


                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test6","Tests whether CopyObject with positive test for modified date passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test6","Tests whether CopyObject with positive test for modified date passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }

        }
        private async static Task CopyObject_Test7(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                // Test CopyConditions where matching ETag is found
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string destBucketName = GetRandomName(15);
                string destObjectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                await Setup_Test(minio, destBucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
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
                    Assert.AreEqual("Minio API responded with message=At least one of the pre-conditions you specified did not hold",ex.Message);
                }

                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                await TearDown(minio, destBucketName);
                new MintLogger("CopyObject_Test7","Tests whether CopyObject with negative test for modified date passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("CopyObject_Test7","Tests whether CopyObject with negative test for modified date passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }

        }

        private async static Task GetObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = null;
                await Setup_Test(minio, bucketName);
                try
                {
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
                }
                catch (Exception e)
                {
                    new MintLogger("GetObject_Test1","Tests whether GetObject as stream works",TestStatus.FAIL,(DateTime.Now - startTime),"",e.Message, e.ToString()).Log();
                    Assert.Fail();
                }
                await TearDown(minio, bucketName);

                new MintLogger("GetObject_Test1","Tests whether GetObject as stream works",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("GetObject_Test1","Tests whether GetObject as stream works",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        
        }
        private async static Task GetObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string fileName = GetRandomName(10);
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
                new MintLogger("GetObject_Test2","Tests for non-existent GetObject",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("GetObject_Test2","Tests for non-existent GetObject",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
          
        }
        private async static Task GetObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = null;
                await Setup_Test(minio, bucketName);
                try
                {
                    using (System.IO.MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
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
            }
            catch (Exception e)
            {
                new MintLogger("GetObject_Test3","Tests whether GetObject returns all the data",TestStatus.FAIL,(DateTime.Now - startTime),"",e.Message, e.ToString()).Log();
                Assert.Fail();
            }
            await TearDown(minio, bucketName);
            new MintLogger("GetObject_Test3","Tests whether GetObject returns all the data",TestStatus.PASS,(DateTime.Now - startTime)).Log();
        }
        catch (MinioException ex)
        {
            new MintLogger("GetObject_Test3","Tests whether GetObject returns all the data",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
        }

        }
        private async static Task FGetObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {     
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                string outFileName = "outFileName";
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                {
                    await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            filestream, filestream.Length, null);

                }
                await minio.GetObjectAsync(bucketName, objectName, outFileName);
                File.Delete(outFileName);
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
                new MintLogger("FGetObject_Test1","Tests whether FGetObject passes for small upload",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("FGetObject_Test1","Tests whether FGetObject passes for small upload",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
       
        }

        private async static Task FPutObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string fileName = CreateFile(6 * MB, dataFile6MB);
                await Setup_Test(minio, bucketName);
                await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            fileName);

                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                if (!IsMintEnv())
                {
                    File.Delete(fileName);
                }
                new MintLogger("FPutObject_Test1","Tests whether FPutObject for multipart upload passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("FPutObject_Test1","Tests whether FPutObject for multipart upload passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }

        }

        private async static Task FPutObject_Test2(MinioClient minio)
        {
             DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string fileName = CreateFile(1 * MB, dataFile1MB);
                await Setup_Test(minio, bucketName);
                await minio.PutObjectAsync(bucketName,
                                            objectName,
                                            fileName);

                await minio.RemoveObjectAsync(bucketName, objectName);
                if (!IsMintEnv())
                {
                    File.Delete(fileName);
                }
                await TearDown(minio, bucketName);
                new MintLogger("FPutObject_Test2","Tests whether FPutObject for small upload passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("FPutObject_Test2","Tests whether FPutObject for small upload passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task ListObjects_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string prefix = "minix";
                string objectName = prefix + GetRandomName(10);
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++) {
                    tasks[i]= PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1*MB));
                }
                await Task.WhenAll(tasks);
               
                ListObjects_Test(minio, bucketName, prefix, 2,false).Wait();
                System.Threading.Thread.Sleep(5000);
                await minio.RemoveObjectAsync(bucketName, objectName + "0");
                await minio.RemoveObjectAsync(bucketName, objectName + "1");
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test1","Tests whether ListObjects lists all objects matching a prefix non-recursive",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test1","Tests whether ListObjects lists all objects matching a prefix non-recursive",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task ListObjects_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                await Setup_Test(minio, bucketName);

                ListObjects_Test(minio, bucketName, null, 0).Wait(5000);
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test2","Tests whether ListObjects passes when bucket is empty",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test2","Tests whether ListObjects passes when bucket is empty",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

         private async static Task ListObjects_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string prefix = "minix";
                string objectName = prefix + "/"+ GetRandomName(10) + "/suffix";
                await Setup_Test(minio, bucketName);
                  Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++) {
                    tasks[i]= PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1*MB));
                }
                await Task.WhenAll(tasks);

                ListObjects_Test(minio, bucketName, prefix, 2,true).Wait();
                System.Threading.Thread.Sleep(5000);
                await minio.RemoveObjectAsync(bucketName, objectName + "0");
                await minio.RemoveObjectAsync(bucketName, objectName + "1");
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test3","Tests whether ListObjects lists all objects matching a prefix and recursive",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test3","Tests whether ListObjects lists all objects matching a prefix and recursive",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task ListObjects_Test4(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                
                string objectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++) {
                    tasks[i]= PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1*MB));
                }
                await Task.WhenAll(tasks);
               
                ListObjects_Test(minio, bucketName, null, 2,false).Wait();
                System.Threading.Thread.Sleep(5000);
                await minio.RemoveObjectAsync(bucketName, objectName + "0");
                await minio.RemoveObjectAsync(bucketName, objectName + "1");
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test4","Tests whether ListObjects lists all objects when no prefix is specified",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test4","Tests whether ListObjects lists all objects when no prefix is specified",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
         private async static Task ListObjects_Test5(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectNamePrefix = GetRandomName(10);
                int numObjects = 1050;
                await Setup_Test(minio, bucketName);
                Task[] tasks = new Task[numObjects];
                for (int i = 1; i <= numObjects; i++) {
                    tasks[i - 1]= PutObject_Task(minio, bucketName, objectNamePrefix + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(1));
                }
                await Task.WhenAll(tasks);
               
                ListObjects_Test(minio, bucketName, objectNamePrefix, numObjects,false).Wait();
                System.Threading.Thread.Sleep(5000);
                for(int index=1; index <= numObjects; index++)
                {
                    string objectName = objectNamePrefix + index.ToString();
                    await minio.RemoveObjectAsync(bucketName,objectName);
                }
                await TearDown(minio, bucketName);
                new MintLogger("ListObjects_Test5","Tests whether ListObjects lists all objects when number of objects > 1000",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListObjects_Test5","Tests whether ListObjects lists all objects when number of objects > 1000",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task ListObjects_Test(MinioClient minio, string bucketName, string prefix, int numObjects, bool recursive = true)
        {
            int count = 0;
            try
            {
                IObservable<Item> observable = minio.ListObjectsAsync(bucketName, prefix, recursive);
                IDisposable subscription = observable.Subscribe(
                    item =>
                    {
                        Assert.IsTrue(item.Key.StartsWith(prefix));
                        count += 1;
                    },
                    ex => Console.WriteLine("OnError: {0}", ex),
                    () =>
                    {
                        //Console.WriteLine("Listed all objects in bucket " + bucketName + "\n");
                        Assert.AreEqual(count, numObjects);

                    });

            }
            catch (Exception)
            {
               // Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }

        private async static Task RemoveObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                {
                    await Setup_Test(minio, bucketName);

                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                    await minio.RemoveObjectAsync(bucketName, objectName);
                    await TearDown(minio, bucketName);

                }
                new MintLogger("RemoveObject_Test1","Tests whether RemoveObjectAsync for existing object passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("RemoveObject_Test1","Tests whether RemoveObjectAsync for existing object passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task RemoveObjects_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(6);
                int count = 1005;
                Task[] tasks = new Task[count];
                List<string> objectsList = new List<string>();
                await Setup_Test(minio, bucketName);
                for (int i = 0; i < count; i++)
                {
                    tasks[i] = PutObject_Task(minio, bucketName, objectName + i.ToString(), null, null, 0, null, rsg.GenerateStreamFromSeed(5));
                    objectsList.Add(objectName + i.ToString());
                }
                Task.WhenAll(tasks).Wait();
                System.Threading.Thread.Sleep(5000);
                IObservable<DeleteError> observable = await minio.RemoveObjectAsync(bucketName, objectsList);
                IDisposable subscription = observable.Subscribe(
                   deleteError => Console.WriteLine("Object: {0}", deleteError.Key),
                   ex => Console.WriteLine("OnError: {0}", ex),
                   () =>
                   {
                       TearDown(minio, bucketName).Wait();
                   });
                new MintLogger("RemoveObjects_Test2","Tests whether RemoveObjectAsync for multi objects delete passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("RemoveObjects_Test2", "Tests whether RemoveObjectAsync for multi objects delete passes", TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }


        private async static Task PresignedGetObject_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string downloadFile = "downloadFileName";
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);
                string presigned_url = await minio.PresignedGetObjectAsync(bucketName, objectName, 1000);
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
                new MintLogger("PresignedGetObject_Test1","Tests whether PresignedGetObject url retrieves object from bucket",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("PresignedGetObject_Test1","Tests whether PresignedGetObject url retrieves object from bucket",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task PresignedGetObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomName(10);
            try
            {
                try
                {

                    await Setup_Test(minio, bucketName);
                    using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                        await minio.PutObjectAsync(bucketName,
                                                    objectName,
                                                    filestream, filestream.Length, null);
                    ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);
                    string presigned_url = await minio.PresignedGetObjectAsync(bucketName, objectName, 0);
                    new MintLogger("PresignedGetObject_Test2","Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.",TestStatus.FAIL,(DateTime.Now - startTime),"","","").Log();

                }
                catch (InvalidExpiryRangeException)
                {
                    new MintLogger("PresignedGetObject_Test2","Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.",TestStatus.PASS,(DateTime.Now - startTime)).Log();
                }
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedGetObject_Test2","Tests whether PresignedGetObject url retrieves object from bucket when invalid expiry is set.",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task PresignedGetObject_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string downloadFile = "downloadFileName";
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);
                Dictionary<string, string> reqParams = new Dictionary<string,string>();
                reqParams["response-content-type"] = "application/json";
                string presigned_url = await minio.PresignedGetObjectAsync(bucketName, objectName, 1000, reqParams);
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
                new MintLogger("PresignedGetObject_Test3","Tests whether PresignedGetObject url retrieves object from bucket when override response headers sent",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("PresignedGetObject_Test3","Tests whether PresignedGetObject url retrieves object from bucket when override response headers sent",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        private async static Task PresignedPutObject_Test1(MinioClient minio)
        {
             DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string fileName = CreateFile(1 * MB, dataFile1MB);
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
                if (!IsMintEnv())
                {
                    File.Delete(fileName);
                }
                new MintLogger("PresignedPutObject_Test1","Tests whether PresignedPutObject url uploads object to bucket",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("PresignedPutObject_Test1","Tests whether PresignedPutObject url uploads object to bucket",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task PresignedPutObject_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomName(10);
            try
            {
                try
                {

                    await Setup_Test(minio, bucketName);
                    using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                        await minio.PutObjectAsync(bucketName,
                                                    objectName,
                                                    filestream, filestream.Length, null);
                    ObjectStat stats = await minio.StatObjectAsync(bucketName, objectName);
                    string presigned_url = await minio.PresignedPutObjectAsync(bucketName, objectName, 0);
                    new MintLogger("PresignedPutObject_Test2","Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.",TestStatus.FAIL,(DateTime.Now - startTime),"","","").Log();

                }
                catch (InvalidExpiryRangeException)
                {
                    new MintLogger("PresignedPutObject_Test2","Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.",TestStatus.PASS,(DateTime.Now - startTime)).Log();
                }
                await minio.RemoveObjectAsync(bucketName, objectName);
                await TearDown(minio, bucketName);
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedPutObject_Test2","Tests whether PresignedPutObject url retrieves object from bucket when invalid expiry is set.",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        
        private static async Task UploadObjectAsync(string url, string filePath)
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

        private async static Task PresignedPostPolicy_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            string bucketName = GetRandomName(15);
            string objectName = GetRandomName(10);
            string metadataKey = GetRandomName(10);
            string metadataValue = GetRandomName(10);
            string fileName = CreateFile(1 * MB, dataFile1MB);

            try
            {
                await Setup_Test(minio, bucketName);
                await minio.PutObjectAsync(bucketName,
                            objectName,
                            fileName);

                // Generate presigned post policy url
                PostPolicy form = new PostPolicy();
                DateTime expiration = DateTime.UtcNow;
                form.SetExpires(expiration.AddDays(10));
                form.SetKey(objectName);
                form.SetBucket(bucketName);
                form.SetUserMetadata(metadataKey, metadataValue);
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
                PolicyType policy = await minio.GetPolicyAsync(bucketName, objectName.Substring(5));
                Assert.AreEqual(policy.GetType(), PolicyType.READ_ONLY);
                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                if (!IsMintEnv())
                {
                    File.Delete(fileName);
                }
                new MintLogger("PresignedPostPolicy_Test1","Tests whether PresignedPostPolicy url applies policy on server",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (Exception ex)
            {
                new MintLogger("PresignedPostPolicy_Test1","Tests whether PresignedPostPolicy url applies policy on server",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }

            await minio.RemoveObjectAsync(bucketName, objectName);

            await TearDown(minio, bucketName);
            File.Delete(fileName);

        }

        private async static Task ListIncompleteUpload_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = "gzip";
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
                                                    contentType, cancellationToken:cts.Token);
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
                catch (Exception e)
                {
                    new MintLogger("ListIncompleteUpload_Test1","Tests whether ListIncompleteUpload passes",TestStatus.FAIL,(DateTime.Now - startTime),"",e.Message, e.ToString()).Log();
                    Assert.Fail();
                }
                await TearDown(minio, bucketName);
                new MintLogger("ListIncompleteUpload_Test1","Tests whether ListIncompleteUpload passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListIncompleteUpload_Test1","Tests whether ListIncompleteUpload passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
    private async static Task ListIncompleteUpload_Test2(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = "minioprefix/" + GetRandomName(10);
                string contentType = "gzip";
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
                                                    contentType, cancellationToken:cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    IObservable<Upload> observable = minio.ListIncompleteUploads(bucketName,"minioprefix",false);

                    IDisposable subscription = observable.Subscribe(
                        item => Assert.AreEqual(item.Key, objectName),
                        ex => Assert.Fail());   

                    await minio.RemoveIncompleteUploadAsync(bucketName, objectName);
                }
                catch (Exception e)
                {
                    new MintLogger("ListIncompleteUpload_Test2","Tests whether ListIncompleteUpload passes when qualified by prefix",TestStatus.FAIL,(DateTime.Now - startTime),"",e.Message, e.ToString()).Log();
                    Assert.Fail();
                }
                await TearDown(minio, bucketName);
                new MintLogger("ListIncompleteUpload_Test2","Tests whether ListIncompleteUpload passes when qualified by prefix",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListIncompleteUpload_Test2","Tests whether ListIncompleteUpload passes when qualified by prefix",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task ListIncompleteUpload_Test3(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = "minioprefix/" + GetRandomName(10) + "/suffix";
                string contentType = "gzip";
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
                                                    contentType, cancellationToken:cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    IObservable<Upload> observable = minio.ListIncompleteUploads(bucketName,"minioprefix",true);

                    IDisposable subscription = observable.Subscribe(
                        item => Assert.AreEqual(item.Key, objectName),
                        ex => Assert.Fail());   

                    await minio.RemoveIncompleteUploadAsync(bucketName, objectName);
                }
                catch (Exception e)
                {
                    new MintLogger("ListIncompleteUpload_Test3","Tests whether ListIncompleteUpload passes when qualified by prefix and recursive",TestStatus.FAIL,(DateTime.Now - startTime),"",e.Message, e.ToString()).Log();
                    Assert.Fail();
                }
                await TearDown(minio, bucketName);
                new MintLogger("ListIncompleteUpload_Test3","Tests whether ListIncompleteUpload passes when qualified by prefix and recursive",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("ListIncompleteUpload_Test3","Tests whether ListIncompleteUpload passes when qualified by prefix and recursive",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        private async static Task RemoveIncompleteUpload_Test(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                string contentType = "csv";
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
                                                    contentType, cancellationToken:cts.Token);
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
                catch (Exception e)
                {
                    new MintLogger("RemoveIncompleteUpload_Test1","Tests whether RemoveIncompleteUpload passes.",TestStatus.FAIL,(DateTime.Now - startTime),"",e.Message, e.ToString()).Log();
                    Assert.Fail();
                }
                await TearDown(minio, bucketName);
                new MintLogger("RemoveIncompleteUpload_Test1","Tests whether RemoveIncompleteUpload passes.",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("RemoveIncompleteUpload_Test1","Tests whether RemoveIncompleteUpload passes.",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
        // Set a policy for given bucket
        private async static Task SetBucketPolicy_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                await minio.SetPolicyAsync(bucketName,
                                    objectName.Substring(5),
                                    PolicyType.READ_ONLY);
                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                new MintLogger("SetBucketPolicy_Test1","Tests whether SetBucketPolicy passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("SetBucketPolicy_Test1","Tests whether SetBucketPolicy passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }

        // Get a policy for given bucket
        private async static Task GetBucketPolicy_Test1(MinioClient minio)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string bucketName = GetRandomName(15);
                string objectName = GetRandomName(10);
                await Setup_Test(minio, bucketName);
                using (MemoryStream filestream = rsg.GenerateStreamFromSeed(1 * MB))
                    await minio.PutObjectAsync(bucketName,
                                                objectName,
                                                filestream, filestream.Length, null);
                await minio.SetPolicyAsync(bucketName,
                                    objectName.Substring(5),
                                    PolicyType.READ_ONLY);
                PolicyType policy = await minio.GetPolicyAsync(bucketName, objectName.Substring(5));
                Assert.IsTrue(policy.Equals(PolicyType.READ_ONLY));
                await minio.RemoveObjectAsync(bucketName, objectName);

                await TearDown(minio, bucketName);
                new MintLogger("GetBucketPolicy_Test1","Tests whether GetBucketPolicy passes",TestStatus.PASS,(DateTime.Now - startTime)).Log();
            }
            catch (MinioException ex)
            {
                new MintLogger("GetBucketPolicy_Test1","Tests whether GetBucketPolicy passes",TestStatus.FAIL,(DateTime.Now - startTime),"",ex.Message, ex.ToString()).Log();
            }
        }
    }
    internal class RandomStreamGenerator
    {
        private readonly Random _random = new Random();
        private readonly byte[] _seedBuffer;

        public RandomStreamGenerator(int maxBufferSize)
        {
            _seedBuffer = new byte[maxBufferSize];

            _random.NextBytes(_seedBuffer);
        }

        public MemoryStream GenerateStreamFromSeed(int size)
        {
            int randomWindow = _random.Next(0, size);

            byte[] buffer = new byte[size];

            Buffer.BlockCopy(_seedBuffer, randomWindow, buffer, 0, size - randomWindow);
            Buffer.BlockCopy(_seedBuffer, 0, buffer, size - randomWindow, randomWindow);

            return new MemoryStream(buffer);
        }
    }
}
