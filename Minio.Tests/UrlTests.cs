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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Configuration;
using Minio.Exceptions;
using Minio;
namespace Minio.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UrlTests
    {
        public UrlTests()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                    | SecurityProtocolType.Tls11
                                    | SecurityProtocolType.Tls12;
            var minio = new MinioClient("play.min.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG");

        }

        private TestContext testContextInstance;

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
        public void TestWithUrl()
        {
            new MinioClient(endpoint:"localhost:9000");
        }

        [TestMethod]
        public void TestWithoutPort()
        {
            new MinioClient("localhost");
        }

        [TestMethod]
        public void TestWithTrailingSlash()
        {
            new MinioClient("localhost:9000/");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidEndpointException))]
        public void TestUrlFailsWithMalformedScheme()
        {
            new MinioClient("http://localhost:9000");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidEndpointException))]
        public void TestUrlFailsWithPath()
        {
            new MinioClient("localhost:9000/foo");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidEndpointException))]
        public void TestUrlFailsWithQuery()
        {
            new MinioClient("localhost:9000/?foo=bar");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSetAppInfoFailsNullApp()
        {
            var client = new MinioClient("localhost:9000");
            client.SetAppInfo(null, "1.2.2");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSetAppInfoFailsNullVersion()
        {
            var client = new MinioClient("localhost:9000");
            client.SetAppInfo("Hello-App", null);
        }

        [TestMethod]
        public void TestSetAppInfoSuccess()
        {
            var client = new MinioClient("localhost:9000");
            client.SetAppInfo("Hello-App", "1.2.1");
        }

        [TestMethod]
        public void TestEndpointSuccess()
        {
            new MinioClient("s3.amazonaws.com");
        }

        [TestMethod]
        public void TestEndpointSuccess2()
        {
            new MinioClient("s3-us-west-1.amazonaws.com");
        }

       
    }
}



