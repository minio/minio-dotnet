﻿/*
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
using Minio.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Minio.Tests
{
    [TestClass]
    public class UtilsTest
    {
        private const long minimumPartSize = 5 * 1024L * 1024L;

        [TestMethod]
        [ExpectedException(typeof(InvalidBucketNameException))]
        public void TestValidBucketName()
        {
            var testCases = new List<KeyValuePair<string, InvalidBucketNameException>>
            {
              new KeyValuePair<string, InvalidBucketNameException>(".mybucket", new InvalidBucketNameException(".mybucket", "Bucket name cannot start or end with a '.' dot.")),
              new KeyValuePair<string, InvalidBucketNameException>("mybucket.", new InvalidBucketNameException(".mybucket", "Bucket name cannot start or end with a '.' dot.")),
              new KeyValuePair<string, InvalidBucketNameException>("", new InvalidBucketNameException("", "Bucket name cannot be empty.")),
              new KeyValuePair<string, InvalidBucketNameException>("mk", new InvalidBucketNameException("mk", "Bucket name cannot be smaller than 3 characters.")),
              new KeyValuePair<string, InvalidBucketNameException>("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz123456789012345", new InvalidBucketNameException("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz123456789012345", "Bucket name cannot be greater than 63 characters.")),
              new KeyValuePair<string, InvalidBucketNameException>("my..bucket", new InvalidBucketNameException("my..bucket", "Bucket name cannot have successive periods.")),
              new KeyValuePair<string, InvalidBucketNameException>("MyBucket", new InvalidBucketNameException("MyBucket", "Bucket name cannot have upper case characters")),
              new KeyValuePair<string, InvalidBucketNameException>("my!bucket", new InvalidBucketNameException("my!bucket", "Bucket name contains invalid characters.")),
              new KeyValuePair<string, InvalidBucketNameException>("mybucket", null ),
              new KeyValuePair<string, InvalidBucketNameException>("mybucket1234dhdjkshdkshdkshdjkshdkjshfkjsfhjkshsjkhjkhfkjd", null),
            };

            foreach (KeyValuePair<string, InvalidBucketNameException> pair in testCases)
            {
                string bucketName = pair.Key;
                InvalidBucketNameException expectedException = pair.Value;
                try
                {
                    utils.ValidateBucketName(bucketName);
                }
                catch (InvalidBucketNameException ex)
                {
                    Assert.AreEqual(ex.Message, expectedException.Message);
                    throw;
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidObjectNameException))]
        public void TestEmptyObjectName()
        {
            try
            {
                utils.ValidateObjectName("");
            }
            catch (InvalidObjectNameException ex)
            {
                Assert.AreEqual(ex.ServerMessage, "Object name cannot be empty.");
                throw;
            }
        }

        [TestMethod]
        public void TestVeryLongObjectName()
        {
            try
            {
                string objName = TestHelper.GetRandomName(1025);
                utils.ValidateObjectName(objName);
            }
            catch (InvalidObjectNameException ex)
            {
                Assert.AreEqual(ex.ServerMessage, "Object name cannot be greater than 1024 characters.");
            }
            
        }

        [TestMethod]
        public void TestObjectName()
        {
            string objName = TestHelper.GetRandomName(15);
            utils.ValidateObjectName(objName);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestEmptyFile()
        {
            utils.ValidateFile("");
        }

        [TestMethod]
        public void TestFileWithoutExtension()
        {
            utils.ValidateFile("xxxx");
        }

        [TestMethod]
        public void TestFileWithExtension()
        {
            utils.ValidateFile("xxxx.xml");
        }

        [TestMethod]
        [ExpectedException(typeof(EntityTooLargeException))]
        public void TestInvalidPartSize()
        {
            try
            {
                (long partSize, int partCount, long lastPartSize) multiparts = utils.CalculateMultiPartSize(5_000_000_000_000_000_000, minimumPartSize);
            }
            catch (EntityTooLargeException ex)
            {
                Assert.AreEqual(ex.ServerMessage, "Your proposed upload size 5000000000000000000 exceeds the maximum allowed object size " + Constants.MaxMultipartPutObjectSize);
                throw;
            }
        }

        [TestMethod]
        public void TestValidPartSize1()
        {
            // { partSize = 550502400, partCount = 9987, lastPartSize = 241172480 }
            (long partSize, int partCount, long lastPartSize) = utils.CalculateMultiPartSize(5_497_558_138_880, minimumPartSize);
            Assert.AreEqual(partSize, 550_502_400);
            Assert.AreEqual(partCount, 9_987);
            Assert.AreEqual(lastPartSize, 241_172_480);
        }

        [TestMethod]
        public void TestValidPartSize2()
        {
            (long partSize, int partCount, long lastPartSize) = utils.CalculateMultiPartSize(5_000_000_000, minimumPartSize);
            Assert.AreEqual(partSize, 5_242_880);
            Assert.AreEqual(partCount, 954);
            Assert.AreEqual(lastPartSize, 3_535_360);
        }

        [TestMethod]
        public void TestValidPartSize3()
        {
            (long partSize, int partCount, long lastPartSize) = utils.CalculateMultiPartSize(5_000_000_000, 512 * 1024L * 1024L);
            Assert.AreEqual(partSize, 536_870_912);
            Assert.AreEqual(partCount, 10);
            Assert.AreEqual(lastPartSize, 168_161_792);
        }

        [TestMethod]
        public void TestCaseInsensitiveContains()
        {
            Assert.IsTrue(utils.CaseInsensitiveContains("AbCdEF", "ef"));
            Assert.IsTrue(utils.CaseInsensitiveContains("ef", ""));
            Assert.IsTrue(utils.CaseInsensitiveContains("abcdef", "ef"));
            Assert.IsTrue(utils.CaseInsensitiveContains("AbCdEF", "Bc"));
            Assert.IsFalse(utils.CaseInsensitiveContains("abc", "xyz"));
        }

        [TestMethod]
        public void TestIsAmazonEndpoint()
        {
            Assert.IsTrue(s3utils.IsAmazonEndPoint("s3.amazonaws.com"));
            Assert.IsTrue(s3utils.IsAmazonEndPoint("s3.cn-north-1.amazonaws.com.cn"));
            Assert.IsFalse(s3utils.IsAmazonEndPoint("s3.us-west-1amazonaws.com"));
            Assert.IsFalse(s3utils.IsAmazonEndPoint("play.min.io"));
            Assert.IsFalse(s3utils.IsAmazonEndPoint("192.168.12.1"));
            Assert.IsFalse(s3utils.IsAmazonEndPoint("storage.googleapis.com"));
        }

        [TestMethod]
        public void TestIsAmazonChinaEndpoint()
        {
            Assert.IsFalse(s3utils.IsAmazonChinaEndPoint("s3.amazonaws.com"));
            Assert.IsTrue(s3utils.IsAmazonChinaEndPoint("s3.cn-north-1.amazonaws.com.cn"));
            Assert.IsFalse(s3utils.IsAmazonChinaEndPoint("s3.us-west-1amazonaws.com"));
            Assert.IsFalse(s3utils.IsAmazonChinaEndPoint("play.min.io"));
            Assert.IsFalse(s3utils.IsAmazonChinaEndPoint("192.168.12.1"));
            Assert.IsFalse(s3utils.IsAmazonChinaEndPoint("storage.googleapis.com"));
        }

        [TestMethod]
        public  void TestBucketConfiguration()
        {
            CreateBucketConfiguration config = new CreateBucketConfiguration("us-west-1");
            XmlSerializer xs = new XmlSerializer(typeof(CreateBucketConfiguration));
            StringWriter writer = new StringWriter();
            xs.Serialize(writer, config);
            Console.WriteLine(writer.ToString());
        }

        [TestMethod]
        public void TestisValidEndpoint()
        {
            Assert.IsTrue(RequestUtil.IsValidEndpoint("a.b.c"));
            Assert.IsTrue(RequestUtil.IsValidEndpoint("a_b_c"));
            Assert.IsTrue(RequestUtil.IsValidEndpoint("a_b.c"));
            Assert.IsFalse(RequestUtil.IsValidEndpoint("_a.b.c"));
            Assert.IsFalse(RequestUtil.IsValidEndpoint("a.b_.c"));
            Assert.IsFalse(RequestUtil.IsValidEndpoint("a.b.c."));
            Assert.IsFalse(RequestUtil.IsValidEndpoint("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz12345678901234.b.com"));
            Assert.IsTrue(RequestUtil.IsValidEndpoint("0some.domain.com"));
            Assert.IsTrue(RequestUtil.IsValidEndpoint("A.domain.com"));
            Assert.IsTrue(RequestUtil.IsValidEndpoint("A.domain1.com"));
        }
    }
}
