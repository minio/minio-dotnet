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

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio.Tests;

[TestClass]
public class UtilsTest
{
    [TestMethod]
    [ExpectedException(typeof(InvalidBucketNameException))]
    public void TestValidBucketName()
    {
        var testCases = new List<KeyValuePair<string, InvalidBucketNameException>>
        {
            new(".mybucket",
                new InvalidBucketNameException(".mybucket", "Bucket name cannot start or end with a '.' dot.")),
            new("mybucket.",
                new InvalidBucketNameException(".mybucket", "Bucket name cannot start or end with a '.' dot.")),
            new("", new InvalidBucketNameException("", "Bucket name cannot be empty.")),
            new("mk", new InvalidBucketNameException("mk", "Bucket name cannot be smaller than 3 characters.")),
            new("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz123456789012345",
                new InvalidBucketNameException("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz123456789012345",
                    "Bucket name cannot be greater than 63 characters.")),
            new("my..bucket",
                new InvalidBucketNameException("my..bucket", "Bucket name cannot have successive periods.")),
            new("MyBucket",
                new InvalidBucketNameException("MyBucket", "Bucket name cannot have upper case characters")),
            new("my!bucket", new InvalidBucketNameException("my!bucket", "Bucket name contains invalid characters.")),
            new("mybucket", null),
            new("mybucket1234dhdjkshdkshdkshdjkshdkjshfkjsfhjkshsjkhjkhfkjd", null)
        };

        foreach (var pair in testCases)
        {
            var bucketName = pair.Key;
            var expectedException = pair.Value;
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
            var objName = TestHelper.GetRandomName(1025);
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
        var objName = TestHelper.GetRandomName(15);
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
    public void TestInvalidPUTPartSize()
    {
        try
        {
            var multiparts = utils.CalculateMultiPartSize(5000000000000000000);
        }
        catch (EntityTooLargeException ex)
        {
            Assert.AreEqual(ex.ServerMessage,
                "Your proposed upload size 5000000000000000000 exceeds the maximum allowed object size " +
                Constants.MaxMultipartPutObjectSize);
            throw;
        }
    }

    [TestMethod]
    [ExpectedException(typeof(EntityTooLargeException))]
    public void TestInvalidCOPYPartSize()
    {
        try
        {
            var multiparts = utils.CalculateMultiPartSize(5000000000000000000, true);
        }
        catch (EntityTooLargeException ex)
        {
            Assert.AreEqual(ex.ServerMessage,
                "Your proposed upload size 5000000000000000000 exceeds the maximum allowed object size " +
                Constants.MaxMultipartPutObjectSize);
            throw;
        }
    }

    [TestMethod]
    public void TestValidPartSize1()
    {
        // { partSize = 550502400, partCount = 9987, lastPartSize = 241172480 }
        dynamic partSizeObject = utils.CalculateMultiPartSize(5497558138880);
        double partSize = partSizeObject.partSize;
        double partCount = partSizeObject.partCount;
        double lastPartSize = partSizeObject.lastPartSize;
        Assert.AreEqual(partSize, 553648128);
        Assert.AreEqual(partCount, 9930);
        Assert.AreEqual(lastPartSize, 385875968);
    }

    [TestMethod]
    public void TestValidPartSize2()
    {
        dynamic partSizeObject = utils.CalculateMultiPartSize(500000000000, true);
        double partSize = partSizeObject.partSize;
        double partCount = partSizeObject.partCount;
        double lastPartSize = partSizeObject.lastPartSize;
        Assert.AreEqual(partSize, 536870912);
        Assert.AreEqual(partCount, 932);
        Assert.AreEqual(lastPartSize, 173180928);
    }

    [TestMethod]
    public void TestCaseInsensitiveContains()
    {
        Assert.IsTrue(utils.CaseInsensitiveContains("ef", ""));
        Assert.IsTrue(utils.CaseInsensitiveContains("abcdef", "ef"));
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
    public void TestBucketConfiguration()
    {
        var config = new CreateBucketConfiguration("us-west-1");
        var xs = new XmlSerializer(typeof(CreateBucketConfiguration));
        var writer = new StringWriter();
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
        Assert.IsFalse(
            RequestUtil.IsValidEndpoint("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz12345678901234.b.com"));
        Assert.IsTrue(RequestUtil.IsValidEndpoint("0some.domain.com"));
        Assert.IsTrue(RequestUtil.IsValidEndpoint("A.domain.com"));
        Assert.IsTrue(RequestUtil.IsValidEndpoint("A.domain1.com"));
    }
}