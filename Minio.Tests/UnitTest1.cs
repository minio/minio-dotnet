/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2015 Minio, Inc.
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
using NUnit.Framework;

namespace Minio.Tests
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public void TestWithUrl()
        {
            new MinioClient("http://localhost:9000");
        }

        [Test]
        public void TestWithoutPort()
        {
            new MinioClient("http://localhost");
        }

        [Test]
        public void TestWithTrailingSlash()
        {
            new MinioClient("http://localhost:9000/");
        }

        [Test]
        [ExpectedException(typeof(Errors.InvalidEndpointException))]
        public void TestUrlFailsWithMalformedScheme()
        {
            new MinioClient("htp://localhost:9000");
        }

        [Test]
        [ExpectedException(typeof(Errors.InvalidEndpointException))]
        public void TestUrlFailsWithPath()
        {
            new MinioClient("http://localhost:9000/foo");
        }

        [Test]
        [ExpectedException(typeof(Errors.InvalidEndpointException))]
        public void TestUrlFailsWithQuery()
        {
            new MinioClient("http://localhost:9000/?foo=bar");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSetAppInfoFailsNullApp()
        {
            var client = new MinioClient("http://localhost:9000");
            client.SetAppInfo(null, "1.2.2");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSetAppInfoFailsNullVersion()
        {
            var client = new MinioClient("http://localhost:9000");
            client.SetAppInfo("Hello-App", null);
        }

        [Test]
        public void TestSetAppInfoSuccess()
        {
            var client = new MinioClient("http://localhost:9000");
            client.SetAppInfo("Hello-App", "1.2.1");
        }

        [Test]
        public void TestEndpointSuccess()
        {
            new MinioClient("s3.amazonaws.com");
        }

        [Test]
        [ExpectedException(typeof(Errors.InvalidEndpointException))]
        public void TestEndpointFailure()
        {
            new MinioClient("s3-us-west-1.amazonaws.com");
        }
    }
}
