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

namespace Minio.Tests;

/// <summary>
///     Summary description for UnitTest2
/// </summary>
[TestClass]
public class UnitTest2
{
    public UnitTest2()
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
        using var client = new MinioClient()
            .WithEndpoint("localhost", 9000)
            .WithCredentials("minio", "minio")
            .Build();
    }

    [TestMethod]
    public void TestWithoutPort()
    {
        using var client = new MinioClient()
            .WithEndpoint("localhost")
            .WithCredentials("minio", "minio")
            .Build();
    }

    [TestMethod]
    public void TestWithTrailingSlash()
    {
        using var client = new MinioClient()
            .WithEndpoint("localhost", 9000)
            .WithCredentials("minio", "minio")
            .Build();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidEndpointException))]
    public void TestUrlFailsWithMalformedScheme()
    {
        using var client = new MinioClient()
            .WithEndpoint("htp://localhost", 9000)
            .WithCredentials("minio", "minio")
            .Build();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidEndpointException))]
    public void TestUrlFailsWithPath()
    {
        using var client = new MinioClient().WithEndpoint("localhost:9000/foo").WithCredentials("minio", "minio")
            .Build();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidEndpointException))]
    public void TestUrlFailsWithQuery()
    {
        using var client = new MinioClient()
            .WithEndpoint("localhost:9000/?foo=bar")
            .WithCredentials("minio", "minio")
            .Build();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void TestSetAppInfoFailsNullApp()
    {
        using var client = new MinioClient()
            .WithEndpoint("localhost", 9000)
            .WithCredentials("minio", "minio")
            .Build();
        _ = client.SetAppInfo(null, "1.2.2");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void TestSetAppInfoFailsNullVersion()
    {
        using var client = new MinioClient()
            .WithEndpoint("localhost", 9000)
            .WithCredentials("minio", "minio")
            .Build();
        _ = client.SetAppInfo("Hello-App", null);
    }

    [TestMethod]
    public void TestSetAppInfoSuccess()
    {
        using var client = new MinioClient()
            .WithEndpoint("localhost", 9000)
            .WithCredentials("minio", "minio")
            .Build();
        _ = client.SetAppInfo("Hello-App", "1.2.1");
    }

    [TestMethod]
    public void TestEndpointSuccess()
    {
        using var client = new MinioClient()
            .WithEndpoint("s3.amazonaws.com")
            .WithCredentials("minio", "minio")
            .Build();
    }
}
