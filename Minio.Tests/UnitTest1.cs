/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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

using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Exceptions;

namespace Minio.Tests
{
    /// <summary>
    ///     Summary description for UnitTest1
    /// </summary>
    [TestClass]
    [Ignore("Class was previously skipped by unit tests.. See #211")]
    public class UnitTest1
    {
        public UnitTest1()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                                   | SecurityProtocolType.Tls11
                                                   | SecurityProtocolType.Tls12;
            using var minio = new MinioClient()
                .WithEndpoint(TestHelper.Endpoint)
                .WithCredentials(TestHelper.AccessKey, TestHelper.SecretKey)
                .WithSSL()
                .Build();
        }

        /// <summary>
        ///     Gets or sets the test context which provides
        ///     information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestWithUrl()
        {
            using var minio = new MinioClient()
                .WithEndpoint("localhost", 9000)
                .Build();
        }

        [TestMethod]
        public void TestWithoutPort()
        {
            using var minio = new MinioClient()
                .WithEndpoint("localhost")
                .Build();
        }

        [TestMethod]
        public void TestWithTrailingSlash()
        {
            using var minio = new MinioClient()
                .WithEndpoint("localhost:9000/")
                .Build();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidEndpointException))]
        public void TestUrlFailsWithMalformedScheme()
        {
            using var minio = new MinioClient()
                .WithEndpoint("htp://localhost:9000")
                .Build();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidEndpointException))]
        public void TestUrlFailsWithPath()
        {
            using var minio = new MinioClient()
                .WithEndpoint("localhost:9000/foo")
                .Build();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidEndpointException))]
        public void TestUrlFailsWithQuery()
        {
            using var minio = new MinioClient()
                .WithEndpoint("http://localhost:9000/?foo=bar")
                .Build();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSetAppInfoFailsNullApp()
        {
            using var client = new MinioClient()
                .WithEndpoint("localhost", 9000)
                .Build();
            client.SetAppInfo(null, "1.2.2");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSetAppInfoFailsNullVersion()
        {
            using var client = new MinioClient()
                .WithEndpoint("localhost", 9000)
                .Build();
            client.SetAppInfo("Hello-App", null);
        }

        [TestMethod]
        public void TestSetAppInfoSuccess()
        {
            using var client = new MinioClient()
                .WithEndpoint("localhost", 9000)
                .Build();
            client.SetAppInfo("Hello-App", "1.2.1");
        }

        [TestMethod]
        public void TestEndpointSuccess()
        {
            using var client = new MinioClient()
                .WithEndpoint("s3.amazonaws.com")
                .Build();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidEndpointException))]
        public void TestEndpointFailure()
        {
            using var client = new MinioClient()
                .WithEndpoint("s3-us-west-1.amazonaws.com")
                .Build();
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
    }
}