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
    using Exceptions;
    using Xunit;

    /// <summary>
    ///     Summary description for UnitTest1
    /// </summary>
    public class UrlTests
    {
        [Fact]
        public void TestEndpointFailure()
        {
            Assert.Throws<InvalidEndpointException>(() => { MinioClient.Create("s3-us-west-1.amazonaws.com"); });
        }

        [Fact]
        public void TestEndpointSuccess()
        {
            MinioClient.Create("s3.amazonaws.com");
        }

        [Fact]
        public void TestSetAppInfoSuccess()
        {
            var client = MinioClient.Create("localhost:9000");
            client.SetCustomUserAgent("Hello-App 1.2.1");
        }

        [Fact]
        public void TestUrlFailsWithMalformedScheme()
        {
            Assert.Throws<InvalidEndpointException>(() => { MinioClient.Create("http://localhost:9000"); });
        }

        [Fact]
        public void TestUrlFailsWithPath()
        {
            Assert.Throws<InvalidEndpointException>(() => { MinioClient.Create("localhost:9000/foo"); });
        }

        [Fact]
        public void TestUrlFailsWithQuery()
        {
            Assert.Throws<InvalidEndpointException>(() => { MinioClient.Create("localhost:9000/?foo=bar"); });
        }

        [Fact]
        public void TestWithoutPort()
        {
            MinioClient.Create("localhost");
        }

        [Fact]
        public void TestWithTrailingSlash()
        {
            MinioClient.Create("localhost:9000/");
        }

        [Fact]
        public void TestWithUrl()
        {
            MinioClient.Create("localhost:9000");
        }

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
    }
}