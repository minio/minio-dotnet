/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017-2021 MinIO, Inc.
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

[TestClass]
public class NegativeTest
{
    [TestMethod]
    public async Task TestNoConnectionError()
    {
        // invalid uri
        using var minio = new MinioClient()
            .WithEndpoint("localhost", 12121)
            .WithCredentials("minio", "minio")
            .Build();
        var args = new BucketExistsArgs()
            .WithBucket("test");

        var ex = await Assert.ThrowsExceptionAsync<ConnectionException>(() => minio.BucketExistsAsync(args));
        Assert.IsNotNull(ex.ServerResponse);
    }

    [TestMethod]
    public async Task TestInvalidBucketNameError()
    {
        var badName = new string('A', 260);
        using var minio = new MinioClient()
            .WithEndpoint(TestHelper.Endpoint)
            .WithCredentials(TestHelper.AccessKey, TestHelper.SecretKey)
            .WithSSL()
            .Build();
        var args = new BucketExistsArgs()
            .WithBucket(badName);
        await Assert.ThrowsExceptionAsync<InvalidBucketNameException>(() => minio.BucketExistsAsync(args));
    }

    [TestMethod]
    public async Task TestInvalidObjectNameError()
    {
        var badName = new string('A', 260);
        var bucketName = Guid.NewGuid().ToString("N");
        using var minio = new MinioClient()
            .WithEndpoint(TestHelper.Endpoint)
            .WithCredentials(TestHelper.AccessKey, TestHelper.SecretKey)
            .WithSSL()
            .Build();

        try
        {
            const int tryCount = 5;
            var mkBktArgs = new MakeBucketArgs()
                .WithBucket(bucketName);
            await minio.MakeBucketAsync(mkBktArgs);

            var statObjArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(badName);
            var ex = await Assert.ThrowsExceptionAsync<InvalidObjectNameException>(
                () => minio.StatObjectAsync(statObjArgs));
            for (var i = 0;
                 i < tryCount && ex.ServerResponse?.StatusCode.Equals(HttpStatusCode.ServiceUnavailable) == true;
                 ++i)
                ex = await Assert.ThrowsExceptionAsync<InvalidObjectNameException>(
                    () => minio.StatObjectAsync(statObjArgs));

            Assert.AreEqual(ex.Response.Code, "InvalidObjectName");

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(badName)
                .WithCallbackStream(s => { });
            ex = await Assert.ThrowsExceptionAsync<InvalidObjectNameException>(
                () => minio.GetObjectAsync(getObjectArgs));
            Assert.AreEqual(ex.Response.Code, "InvalidObjectName");
        }
        finally
        {
            var args = new RemoveBucketArgs()
                .WithBucket(bucketName);
            await minio.RemoveBucketAsync(args);
        }
    }
}