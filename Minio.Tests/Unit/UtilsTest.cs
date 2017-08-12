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

namespace Minio.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using DataModel;
    using Exceptions;
    using Helper;
    using Xunit;

    public class UtilsTest
    {
        [Fact]
        public void TestBucketConfiguration()
        {
            var config = new CreateBucketConfiguration("us-west-1");
            var xs = new XmlSerializer(typeof(CreateBucketConfiguration));
            var writer = new StringWriter();
            xs.Serialize(writer, config);
            Console.Out.WriteLine(writer.ToString());
        }

        [Fact]
        public void TestCaseInsensitiveContains()
        {
            Assert.True(Utils.CaseInsensitiveContains("AbCdEF", "ef"));
            Assert.True(Utils.CaseInsensitiveContains("ef", ""));
            Assert.True(Utils.CaseInsensitiveContains("abcdef", "ef"));
            Assert.True(Utils.CaseInsensitiveContains("AbCdEF", "Bc"));
            Assert.False(Utils.CaseInsensitiveContains("abc", "xyz"));
        }

        [Fact]
        public void TestEmptyObjectName()
        {
            try
            {
                Utils.ValidateObjectName("");
            }
            catch (InvalidObjectNameException ex)
            {
                Assert.Equal(ex.Message, "Object name cannot be empty.");
            }
        }

        [Fact]
        public void TestInvalidPartSize()
        {
            try
            {
                var multiparts = Utils.CalculateMultiPartSize(5000000000000000000);
            }
            catch (EntityTooLargeException ex)
            {
                Assert.Equal(ex.Message,
                    "Your proposed upload size 5000000000000000000 exceeds the maximum allowed object size " +
                    Constants.MaxMultipartPutObjectSize);
            }
        }

        [Fact]
        public void TestIsAmazonChinaEndpoint()
        {
            Assert.False(S3Utils.IsAmazonChinaEndPoint("s3.amazonaws.com"));
            Assert.True(S3Utils.IsAmazonChinaEndPoint("s3.cn-north-1.amazonaws.com.cn"));
            Assert.False(S3Utils.IsAmazonChinaEndPoint("s3.us-west-1amazonaws.com"));
            Assert.False(S3Utils.IsAmazonChinaEndPoint("play.minio.io"));
            Assert.False(S3Utils.IsAmazonChinaEndPoint("192.168.12.1"));
            Assert.False(S3Utils.IsAmazonChinaEndPoint("storage.googleapis.com"));
        }

        [Fact]
        public void TestIsAmazonEndpoint()
        {
            Assert.True(S3Utils.IsAmazonEndPoint("s3.amazonaws.com"));
            Assert.True(S3Utils.IsAmazonEndPoint("s3.cn-north-1.amazonaws.com.cn"));
            Assert.False(S3Utils.IsAmazonEndPoint("s3.us-west-1amazonaws.com"));
            Assert.False(S3Utils.IsAmazonEndPoint("play.minio.io"));
            Assert.False(S3Utils.IsAmazonEndPoint("192.168.12.1"));
            Assert.False(S3Utils.IsAmazonEndPoint("storage.googleapis.com"));
        }

        [Fact]
        public void TestisValidEndpoint()
        {
            Assert.True(RequestUtil.IsValidEndpoint("a.b.c"));
            Assert.False(RequestUtil.IsValidEndpoint("_a.b.c"));
            Assert.False(RequestUtil.IsValidEndpoint("a.b_.c"));
            Assert.False(RequestUtil.IsValidEndpoint("a.b.c."));
            Assert.False(
                RequestUtil.IsValidEndpoint(
                    "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz12345678901234.b.com"));
            Assert.True(RequestUtil.IsValidEndpoint("0some.domain.com"));
            Assert.True(RequestUtil.IsValidEndpoint("A.domain.com"));
            Assert.True(RequestUtil.IsValidEndpoint("A.domain1.com"));
        }

        [Fact]
        public void TestObjectName()
        {
            var objName = TestHelper.GetRandomName(15);
            Utils.ValidateObjectName(objName);
        }

        [Fact]
        public void TestValidBucketName()
        {
            var testCases = new List<KeyValuePair<string, InvalidBucketNameException>>
            {
                new KeyValuePair<string, InvalidBucketNameException>(".mybucket",
                    new InvalidBucketNameException(".mybucket", "Bucket name cannot start or end with a '.' dot.")),
                new KeyValuePair<string, InvalidBucketNameException>("mybucket.",
                    new InvalidBucketNameException(".mybucket", "Bucket name cannot start or end with a '.' dot.")),
                new KeyValuePair<string, InvalidBucketNameException>("",
                    new InvalidBucketNameException("", "Bucket name cannot be empty.")),
                new KeyValuePair<string, InvalidBucketNameException>("mk",
                    new InvalidBucketNameException("mk", "Bucket name cannot be smaller than 3 characters.")),
                new KeyValuePair<string, InvalidBucketNameException>(
                    "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz123456789012345",
                    new InvalidBucketNameException(
                        "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz123456789012345",
                        "Bucket name cannot be greater than 63 characters.")),
                new KeyValuePair<string, InvalidBucketNameException>("my..bucket",
                    new InvalidBucketNameException("my..bucket", "Bucket name cannot have successive periods.")),
                new KeyValuePair<string, InvalidBucketNameException>("MyBucket",
                    new InvalidBucketNameException("MyBucket", "Bucket name cannot have upper case characters")),
                new KeyValuePair<string, InvalidBucketNameException>("my!bucket",
                    new InvalidBucketNameException("my!bucket", "Bucket name contains invalid characters.")),
                new KeyValuePair<string, InvalidBucketNameException>("mybucket", null),
                new KeyValuePair<string, InvalidBucketNameException>(
                    "mybucket1234dhdjkshdkshdkshdjkshdkjshfkjsfhjkshsjkhjkhfkjd", null)
            };

            foreach (var pair in testCases)
            {
                var bucketName = pair.Key;
                var expectedException = pair.Value;
                try
                {
                    Utils.ValidateBucketName(bucketName);
                }
                catch (InvalidBucketNameException ex)
                {
                    Assert.Equal(ex.Message, expectedException.Message);
                }
            }
        }

        [Fact]
        public void TestValidPartSize1()
        {
            // { partSize = 550502400, partCount = 9987, lastPartSize = 241172480 }
            dynamic partSizeObject = Utils.CalculateMultiPartSize(5497558138880);
            double partSize = partSizeObject.partSize;
            double partCount = partSizeObject.partCount;
            double lastPartSize = partSizeObject.lastPartSize;
            Assert.Equal(partSize, 550502400);
            Assert.Equal(partCount, 9987);
            Assert.Equal(lastPartSize, 241172480);
        }

        [Fact]
        public void TestValidPartSize2()
        {
            dynamic partSizeObject = Utils.CalculateMultiPartSize(5000000000);
            double partSize = partSizeObject.partSize;
            double partCount = partSizeObject.partCount;
            double lastPartSize = partSizeObject.lastPartSize;
            Assert.Equal(partSize, 5242880);
            Assert.Equal(partCount, 954);
            Assert.Equal(lastPartSize, 3535360);
        }

        [Fact]
        public void TestVeryLongObjectName()
        {
            try
            {
                var objName = TestHelper.GetRandomName(1025);
                Utils.ValidateObjectName(objName);
            }
            catch (InvalidObjectNameException ex)
            {
                Assert.Equal(ex.Message, "Object name cannot be greater than 1024 characters.");
            }
        }
    }
}