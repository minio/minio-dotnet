using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Exceptions;
using System.Collections.Generic;
using Minio.Helper;
using Minio.DataModel;
using System.Xml.Serialization;
using System.IO;

namespace Minio.Tests
{
    [TestClass]
    public class UtilsTest
    {
        [TestMethod]
        public void TestValidBucketName()
        {
        var testCases = new List<KeyValuePair<string, InvalidBucketNameException>>()
            {
              new KeyValuePair<string, InvalidBucketNameException>(".mybucket",new InvalidBucketNameException(".mybucket", "Bucket name cannot start or end with a '.' dot.")),
              new KeyValuePair<string, InvalidBucketNameException>("mybucket.",new InvalidBucketNameException(".mybucket", "Bucket name cannot start or end with a '.' dot.")),
              new KeyValuePair<string, InvalidBucketNameException>("",new InvalidBucketNameException("", "Bucket name cannot be empty.")),
              new KeyValuePair<string, InvalidBucketNameException>("mk",new InvalidBucketNameException("mk", "Bucket name cannot be smaller than 3 characters.")),
              new KeyValuePair<string, InvalidBucketNameException>("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz123456789012345",new InvalidBucketNameException("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz123456789012345", "Bucket name cannot be greater than 63 characters.")),
              new KeyValuePair<string, InvalidBucketNameException>("my..bucket",new InvalidBucketNameException("my..bucket", "Bucket name cannot have successive periods.")),
              new KeyValuePair<string, InvalidBucketNameException>("MyBucket",new InvalidBucketNameException("MyBucket", "Bucket name cannot have upper case characters")),
              new KeyValuePair<string, InvalidBucketNameException>("my!bucket", new InvalidBucketNameException("my!bucket", "Bucket name contains invalid characters.")),
              new KeyValuePair<string, InvalidBucketNameException>("mybucket", null ),
              new KeyValuePair<string, InvalidBucketNameException>("mybucket1234dhdjkshdkshdkshdjkshdkjshfkjsfhjkshsjkhjkhfkjd",null ),
            };
         
            foreach (KeyValuePair<string,InvalidBucketNameException> pair in testCases)
            {
                
                string bucketName = (string)pair.Key;
                InvalidBucketNameException expectedException = pair.Value;
                try
                {
                    utils.validateBucketName(bucketName);
                }
                catch (InvalidBucketNameException ex)
                {
                    Assert.AreEqual(ex.Message, expectedException.Message);
                }
                catch (Exception e)
                {
                    Assert.Fail();
                }
            }
        }
        [TestMethod]
        public void TestEmptyObjectName()
        {
            try
            {
                utils.validateObjectName("");
            }
            catch (InvalidObjectNameException ex)
            {
                Assert.AreEqual(ex.message, "Object name cannot be empty.");
            }
        }

        [TestMethod]
        public void TestVeryLongObjectName()
        {
            try
            {
                string objName = TestHelper.GetRandomName(1025);
                utils.validateObjectName(objName);
            }
            catch (InvalidObjectNameException ex)
            {
                Assert.AreEqual(ex.message, "Object name cannot be greater than 1024 characters.");
            }
            
        }

        [TestMethod]
        public void TestObjectName()
        {
            string objName = TestHelper.GetRandomName(15);
            utils.validateObjectName(objName);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestEmptyFile()
        {
            utils.ValidateFile("");
        }

        [TestMethod]
        public void TestInvalidPartSize()
        {
            try
            {
                Object multiparts = utils.CalculateMultiPartSize(5000000000000000000);
            }
            catch (EntityTooLargeException ex)
            {
                Assert.AreEqual(ex.message, "Your proposed upload size 5000000000000000000 exceeds the maximum allowed object size " + Constants.MaxMultipartPutObjectSize);
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
            Assert.AreEqual(partSize, 550502400);
            Assert.AreEqual(partCount, 9987);
            Assert.AreEqual(lastPartSize, 241172480);
        }

        [TestMethod]
        public void TestValidPartSize2()
        {
             dynamic partSizeObject = utils.CalculateMultiPartSize(5000000000);
            double partSize = partSizeObject.partSize;
            double partCount = partSizeObject.partCount;
            double lastPartSize = partSizeObject.lastPartSize;
            Assert.AreEqual(partSize, 5242880);
            Assert.AreEqual(partCount, 954);
            Assert.AreEqual(lastPartSize, 3535360);
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
            Assert.IsFalse(s3utils.IsAmazonEndPoint("play.minio.io"));
            Assert.IsFalse(s3utils.IsAmazonEndPoint("192.168.12.1"));
            Assert.IsFalse(s3utils.IsAmazonEndPoint("storage.googleapis.com"));
        }

        [TestMethod]
        public void TestIsAmazonChinaEndpoint()
        {
            Assert.IsFalse(s3utils.IsAmazonChinaEndPoint("s3.amazonaws.com"));
            Assert.IsTrue(s3utils.IsAmazonChinaEndPoint("s3.cn-north-1.amazonaws.com.cn"));
            Assert.IsFalse(s3utils.IsAmazonChinaEndPoint("s3.us-west-1amazonaws.com"));
            Assert.IsFalse(s3utils.IsAmazonChinaEndPoint("play.minio.io"));
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
            Console.Out.WriteLine(writer.ToString());
        }
    }
}
