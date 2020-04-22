/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Minio.Exceptions;

namespace Minio.Tests
{
    [TestClass]
    public class RetryHandlerTest
    {
        [TestMethod]
        public async Task TestRetryPolicyOnSuccess()
        {
            var client = new MinioClient("play.min.io",
                                         "Q3AM3UQ867SPQQA43P2F",
                                         "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG").WithSSL();

            int invokeCount = 0;
            client.WithRetryPolicy(
                async (callback) =>
                {
                    invokeCount++;
                    return await callback();
                });

            var result = await client.BucketExistsAsync(Guid.NewGuid().ToString());
            Assert.IsFalse(result);
            Assert.AreEqual(invokeCount, 1);
        }

        [TestMethod]
        public async Task TestRetryPolicyOnFailure()
        {
            var client = new MinioClient("play.min.io",
                                         "Q3AM3UQ867SPQQA43P2F",
                                         "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG").WithSSL();

            int invokeCount = 0;
            var retryCount = 3;
            client.WithRetryPolicy(
                async (callback) =>
                {
                    Exception exception = null;
                    for (int i = 0; i < retryCount; i++)
                    {
                        invokeCount++;
                        try
                        {
                            return await callback();
                        }
                        catch (BucketNotFoundException ex)
                        {
                            exception = ex;
                        }
                    }
                    throw exception;
                });

            await Assert.ThrowsExceptionAsync<BucketNotFoundException>(
                () => client.GetObjectAsync(Guid.NewGuid().ToString(), "", s => { }));
            Assert.AreEqual(invokeCount, retryCount);
        }
    }
}
