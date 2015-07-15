/*
 * Minimal Object Storage Library, (C) 2015 Minio, Inc.
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
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Client;
using Minio.Client.Xml;
using System.IO;
using Minio.Client.Errors;

namespace Minio.ClientTests
{
    /// <summary>
    /// Summary description for IntegrationTest
    /// </summary>
    [TestClass]
    public class IntegrationTest
    {
        public IntegrationTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        private static readonly string bucket = "goroutine-dotnet";
        private static ObjectStorageClient client = ObjectStorageClient.GetClient("https://s3-us-west-2.amazonaws.com", "", "");
        private static ObjectStorageClient standardClient = ObjectStorageClient.GetClient("https://s3.amazonaws.com", "", "");
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        [ExpectedException(typeof(BucketExistsException))]
        public void MakeBucket()
        {
            client.MakeBucket(bucket);
        }

        [TestMethod]
        public void makeBucketWithAcl()
        {
            client.MakeBucket(bucket + "-auth", Acl.AuthenticatedRead);
            Acl acl = client.GetBucketAcl(bucket + "-auth");
            Console.Out.WriteLine(acl);
        }

        [TestMethod]
        [ExpectedException(typeof(BucketNotFoundException))]
        public void RemoveAuthBucket()
        {
            client.RemoveBucket(bucket + "-auth");
            client.GetBucketAcl(bucket + "-auth");
        }

        [TestMethod]
        [ExpectedException(typeof(RedirectionException))]
        public void RemoveFromAnotherRegion()
        {
            standardClient.RemoveBucket(bucket);
        }

        [TestMethod]
        [ExpectedException(typeof(AccessDeniedException))]
        public void RemoveFromAnotherUser()
        {
            standardClient.RemoveBucket("bucket");
        }

        [TestMethod]
        [ExpectedException(typeof(RedirectionException))]
        public void RemoveFromAnotherUserAnotherRegion()
        {
            client.RemoveBucket("bucket");
        }

        [TestMethod]
        [ExpectedException(typeof(BucketExistsException))]
        public void MakeInStandard()
        {
            client.MakeBucket(bucket + "-standard");
        }

        /*
         * ACLS
         */

        [TestMethod]
        public void TestAcls()
        {
            client.SetBucketAcl(bucket, Acl.PublicReadWrite);
            Assert.AreEqual(Acl.PublicReadWrite, client.GetBucketAcl(bucket));

            client.SetBucketAcl(bucket, Acl.PublicRead);
            Assert.AreEqual(Acl.PublicRead, client.GetBucketAcl(bucket));

            client.SetBucketAcl(bucket, Acl.AuthenticatedRead);
            Assert.AreEqual(Acl.AuthenticatedRead, client.GetBucketAcl(bucket));

            client.SetBucketAcl(bucket, Acl.Private);
            Assert.AreEqual(Acl.Private, client.GetBucketAcl(bucket));
        }

        [TestMethod]
        [ExpectedException(typeof(BucketNotFoundException))]
        public void TestAclNonExistingBucket()
        {
            client.SetBucketAcl(bucket + "-no-exist", Acl.AuthenticatedRead);
        }

        [TestMethod]
        [ExpectedException(typeof(AccessDeniedException))]
        public void TestSetAclNotOwned()
        {
            standardClient.SetBucketAcl("bucket", Acl.AuthenticatedRead);
        }

        [TestMethod]
        [ExpectedException(typeof(RedirectionException))]
        public void TestSetAclDifferentRegion()
        {
            standardClient.SetBucketAcl(bucket, Acl.AuthenticatedRead);
        }

        [TestMethod]
        [ExpectedException(typeof(RedirectionException))]
        public void TestSetAclDifferentRegionNotOwned()
        {
            client.SetBucketAcl("bucket", Acl.AuthenticatedRead);
        }

        // TODO List Buckets (populate with more data first)

        /*
         * LIST BUCKETS
         */

        [TestMethod]
        public void ListBuckets()
        {
            var buckets = client.ListBuckets();
            foreach (Bucket bucket in buckets)
            {
                Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(AccessDeniedException))]
        public void ListBucketsBadCredentials()
        {
            var badCredentials = ObjectStorageClient.GetClient("https://s3.amazonaws.com", "BADACCESS", "BADSECRET");
            badCredentials.ListBuckets();
        }

        [TestMethod]
        [ExpectedException(typeof(AccessDeniedException))]
        public void ListBucketsNoCredentials()
        {
            var unauthedClient = ObjectStorageClient.GetClient("https://s3.amazonaws.com");
            unauthedClient.ListBuckets();
        }

        [TestMethod]
        public void BucketExists()
        {
            bool exists = client.BucketExists(bucket);
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public void BucketExistsOwnedByOtherUser()
        {
            bool exists = standardClient.BucketExists("bucket");
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public void BuckExistsAnotherRegion()
        {
            bool exists = standardClient.BucketExists(bucket);
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public void BucketExistsAnotherRegionAnotherUser()
        {
            bool exists = client.BucketExists("bucket");
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public void BucketExistsNonExistantBucket()
        {
            bool exists = client.BucketExists(bucket + "-missing");
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public void GetAndSetBucketAcl()
        {
            client.SetBucketAcl(bucket, Acl.PublicRead);
            Acl acl = client.GetBucketAcl(bucket);
            Assert.AreEqual(Acl.PublicRead, acl);

            client.SetBucketAcl(bucket, Acl.PublicReadWrite);
            acl = client.GetBucketAcl(bucket);
            Assert.AreEqual(Acl.PublicReadWrite, acl);

            client.SetBucketAcl(bucket, Acl.AuthenticatedRead);
            acl = client.GetBucketAcl(bucket);
            Assert.AreEqual(Acl.AuthenticatedRead, acl);

            client.SetBucketAcl(bucket, Acl.Private);
            acl = client.GetBucketAcl(bucket);
            Assert.AreEqual(Acl.Private, acl);
        }

        [TestMethod]
        public void RemoveBucket()
        {
            string bucketToDelete = bucket + "-delme";

            try
            {
                client.RemoveBucket(bucketToDelete);
            }
            catch (ClientException ex)
            {
                Console.Out.WriteLine(ex);
            }


            client.MakeBucket(bucketToDelete);
            Assert.IsTrue(client.BucketExists(bucketToDelete));

            client.RemoveBucket(bucketToDelete);
            Assert.IsFalse(client.BucketExists(bucketToDelete));
        }

        [TestMethod]
        public void GetObject()
        {
            client.GetObject(bucket, "hello_world", (stream) =>
            {
                byte[] buffer = new byte[11];
                stream.Read(buffer, 0, 11);
                Assert.AreEqual("hello world", System.Text.Encoding.UTF8.GetString(buffer));
            });
        }

        [TestMethod]
        public void GetObjectWithOffset()
        {
            client.GetObject(bucket, "hello_world", 2, (stream) =>
            {
                byte[] buffer = new byte[(int)stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                string result = System.Text.Encoding.UTF8.GetString(buffer);
                Console.Out.WriteLine(result);
                Assert.AreEqual("llo world", result);
            });
        }

        [TestMethod]
        public void GetObjectWithOffsetAndLength()
        {
            client.GetObject(bucket, "hello_world", 2, 5, (stream) =>
            {
                byte[] buffer = new byte[(int)stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                string result = System.Text.Encoding.UTF8.GetString(buffer);
                Console.Out.WriteLine(result);
                Assert.AreEqual("llo w", result);
            });
        }

        [TestMethod]
        public void PutSmallObject()
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("hello world");
            client.PutObject(bucket, "smallobj", 11, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        [ExpectedException(typeof(DataSizeMismatchException))]
        public void PutSmallObjectTooSmall()
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("hello world");
            client.PutObject(bucket, "toosmall", 10, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        [ExpectedException(typeof(DataSizeMismatchException))]
        public void PutSmallObjectTooLarge()
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("hello world");
            client.PutObject(bucket, "toolarge", 12, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void PutSmallTextFile()
        {
            string filePath = "..\\..\\..\\README.md";
            FileInfo fileInfo = new FileInfo(filePath);
            FileStream file = File.OpenRead(filePath);
            
            client.PutObject(bucket, "small/text_file", fileInfo.Length, "application/octet-stream", file);
        }

        [TestMethod]
        public void PutSmallBinaryFile()
        {
            string filePath = "C:\\Users\\fkautz\\Documents\\Visual Studio 2013\\Projects\\Minio.Client\\packages\\RestSharp.105.1.0\\lib\\net4\\RestSharp.dll";
            FileInfo fileInfo = new FileInfo(filePath);
            FileStream file = File.OpenRead(filePath);

            client.PutObject(bucket, "small/binary_file", fileInfo.Length, "application/octet-stream", file);
        }

        [TestMethod]
        public void PutSmallFileWithQuestionMark()
        {
            string filePath = "..\\..\\..\\README.md";
            FileInfo fileInfo = new FileInfo(filePath);
            FileStream file = File.OpenRead(filePath);

            client.PutObject(bucket, "small/ob?ject", fileInfo.Length, "text/plain", file);
        }

        [TestMethod]
        public void PutSmallFileWithHash()
        {
            string filePath = "..\\..\\..\\README.md";
            FileInfo fileInfo = new FileInfo(filePath);
            FileStream file = File.OpenRead(filePath);

            client.PutObject(bucket, "small/ob#ject", fileInfo.Length, "text/plain", file);
        }

        [TestMethod]
        public void PutSmallFileWithUnicode1()
        {
            string filePath = "..\\..\\..\\README.md";
            FileInfo fileInfo = new FileInfo(filePath);
            FileStream file = File.OpenRead(filePath);

            client.PutObject(bucket, "small/世界", fileInfo.Length, "text/plain", file);
        }

        [TestMethod]
        public void PutSmallFileWithUnicode2()
        {
            string filePath = "..\\..\\..\\README.md";
            FileInfo fileInfo = new FileInfo(filePath);
            FileStream file = File.OpenRead(filePath);

            client.PutObject(bucket, "small/世界世", fileInfo.Length, "text/plain", file);
        }

        [TestMethod]
        public void PutSmallFileWithUnicode3()
        {
            string filePath = "..\\..\\..\\README.md";
            FileInfo fileInfo = new FileInfo(filePath);
            FileStream file = File.OpenRead(filePath);

            client.PutObject(bucket, "small/世界世界", fileInfo.Length, "text/plain", file);
        }

        [TestMethod]
        public void PutSmallFileWithContentType()
        {
            string filePath = "..\\..\\..\\README.md";
            FileInfo fileInfo = new FileInfo(filePath);
            FileStream file = File.OpenRead(filePath);

            client.PutObject(bucket, "small/text_plain", fileInfo.Length, "text/plain", file);
        }

        [TestMethod]
        public void PutLargeObject()
        {
            byte[] data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "large/object", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void PutLargeObjectWithContentType()
        {
            byte[] data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "large/text_plain", 11 * 1024 * 1024, "text/plain", new MemoryStream(data));
        }

        [TestMethod]
        public void PutLargeObjectWithQuestionMark()
        {
            byte[] data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "large/obj?ect", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void PutLargeObjectWithHash()
        {
            byte[] data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "large/obj#ect", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void PutLargeObjectWithUnicode1()
        {
            byte[] data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "large/世界", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void PutLargeObjectWithUnicode2()
        {
            byte[] data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "large/世界世", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void PutLargeObjectWithUnicode3()
        {
            byte[] data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "large/世界世界", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        [ExpectedException(typeof(DataSizeMismatchException))]
        public void PutLargeObjectWithUnicode4()
        {
            byte[] data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "large/世界世界", 11 * 1024 * 1024-1, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void PutLargeObjectResume()
        {
            byte[] data = new byte[9 * 1024 * 1024];
            for (int i = 0; i < 9 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            try
            {
                client.PutObject(bucket, "largeobj", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
                Assert.Fail("Should throw mismatch error");
            }
            catch (DataSizeMismatchException err)
            {
                Console.Out.WriteLine(err);
            }

            data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "largeobj", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void PutLargeObjectDataTooLarge()
        {
            byte[] data = new byte[16 * 1024 * 1024];
            for (int i = 0; i < 16 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            try
            {
                client.PutObject(bucket, "largeobj", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
                Assert.Fail("Should throw mismatch error");
            }
            catch (DataSizeMismatchException err)
            {
                Console.Out.WriteLine(err);
            }

            data = new byte[11 * 1024 * 1024];
            for (int i = 0; i < 11 * 1024 * 1024; i++)
            {
                data[i] = (byte)'a';
            }
            client.PutObject(bucket, "largeobj", 11 * 1024 * 1024, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void PutDirObject()
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("hello world");
            client.PutObject(bucket, "dir/one", 11, "application/octet-stream", new MemoryStream(data));
            client.PutObject(bucket, "dir/two", 11, "application/octet-stream", new MemoryStream(data));
        }

        [TestMethod]
        public void ListObjects()
        {
            var items = client.ListObjects(bucket);

            foreach (Item item in items)
            {
                Console.Out.WriteLine("{0} {1} {2} {3}", item.Key, item.LastModifiedDateTime, item.Size, item.ETag);
            }
        }

        [TestMethod]
        public void ListObjectsWithPrefix()
        {
            var items = client.ListObjects(bucket, "dir");

            foreach (Item item in items)
            {
                Console.Out.WriteLine("{0} {1} {2} {3}", item.Key, item.LastModified, item.Size, item.ETag);
            }
        }

        [TestMethod]
        public void ListObjectsWithPrefixAndNoRecursive()
        {
            var items = client.ListObjects(bucket, null, false);

            foreach (Item item in items)
            {
                Console.Out.WriteLine("{0} {1} {2} {3}", item.Key, item.LastModified, item.Size, item.ETag);
            }
        }

        [TestMethod]
        public void ListAllIncompleteUploads()
        {
            var uploads = client.ListAllIncompleteUploads(bucket);

            foreach (Upload upload in uploads)
            {
                Console.Out.WriteLine(upload.Key + " " + upload.Initiated + " " + upload.UploadId);
            }
        }

        [TestMethod]
        public void DropAllUploads()
        {
            try
            {
                var stream = new MemoryStream(new byte[10]);
                client.PutObject(bucket, "incomplete1", 6 * 1024 * 1024, null, stream);
                Assert.Fail();
            }
            catch (DataSizeMismatchException err)
            {
                Console.Out.WriteLine(err);
            }
            try
            {
                var stream = new MemoryStream(new byte[10]);
                client.PutObject(bucket, "incomplete2", 6 * 1024 * 1024, null, stream);
                Assert.Fail();
            }
            catch (DataSizeMismatchException err)
            {
                Console.Out.WriteLine(err);
            }
            try
            {
                var stream = new MemoryStream(new byte[10]);
                client.PutObject(bucket, "incomplete3", 6 * 1024 * 1024, null, stream);
                Assert.Fail();
            }
            catch (DataSizeMismatchException err)
            {
                Console.Out.WriteLine(err);
            }
            var uploads = client.ListAllIncompleteUploads(bucket);
            var uploadCount = 0;
            foreach (Upload upload in uploads)
            {
                uploadCount++;
            }
            Assert.AreNotEqual(0, uploadCount);

            client.DropIncompleteUpload(bucket, "incomplete1");
            uploads = client.ListAllIncompleteUploads(bucket);
            foreach (Upload upload in uploads)
            {
                Assert.AreNotEqual("incomplete1", upload.Key);
            }
            client.DropAllIncompleteUploads(bucket);
            uploadCount = 0;
            foreach (Upload upload in uploads)
            {
                uploadCount++;
            }
            Assert.AreEqual(0, uploadCount);
        }

        [TestMethod]
        public void TestStatObject()
        {
            ObjectStat stat = client.StatObject(bucket, "smallobj");
            Console.Out.WriteLine(stat);
        }
    }
}
