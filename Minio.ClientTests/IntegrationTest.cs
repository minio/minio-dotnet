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
using Minio.Client.xml;
using System.IO;

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
        [ExpectedException(typeof(RequestException))]
        public void MakeBucket()
        {
            client.MakeBucket(bucket);
        }

        [TestMethod]
        public void ListBuckets()
        {
            var buckets = client.ListBuckets();
            foreach (Bucket bucket in buckets.Buckets)
            {
                Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDate);
            }
        }

        [TestMethod]
        public void BucketExists()
        {
            bool exists = client.BucketExists(bucket);
            Assert.IsTrue(exists);
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
            catch (RequestException ex)
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
                Console.Out.WriteLine("{0} {1} {2} {3}", item.Key, item.LastModified, item.Size, item.ETag);
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
        public void ListUnfinishedUploads()
        {
            var uploads = client.ListUnfinishedUploads(bucket);

            foreach (Upload upload in uploads)
            {
                Console.Out.WriteLine(upload.Key + " " + upload.Initiated + " " + upload.UploadId);
            }
        }
    }
}
