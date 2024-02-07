﻿/*
 * MinIO .NET Library for Newtera TDM, (C) 2020 MinIO, Inc.
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtera.DataModel.Args;
using Newtera.Exceptions;

namespace Newtera.Tests;

[TestClass]
public class RetryHandlerTest
{
    [TestMethod]
    public async Task TestRetryPolicyOnSuccess()
    {
        using var client = new NewteraClient()
            .WithEndpoint(TestHelper.Endpoint)
            .WithCredentials(TestHelper.AccessKey, TestHelper.SecretKey)
            .WithSSL()
            .Build();

        var invokeCount = 0;
        _ = client.WithRetryPolicy(
            async callback =>
            {
                invokeCount++;
                return await callback().ConfigureAwait(false);
            });

        var bktArgs = new BucketExistsArgs()
            .WithBucket(Guid.NewGuid().ToString());
        var result = await client.BucketExistsAsync(bktArgs).ConfigureAwait(false);
        Assert.IsFalse(result);
        Assert.AreEqual(invokeCount, 1);
    }

    [TestMethod]
    public async Task TestRetryPolicyOnFailure()
    {
        using var client = new NewteraClient()
            .WithEndpoint(TestHelper.Endpoint)
            .WithCredentials(TestHelper.AccessKey, TestHelper.SecretKey)
            .WithSSL()
            .Build();

        var invokeCount = 0;
        var retryCount = 3;
        _ = client.WithRetryPolicy(
            async callback =>
            {
                Exception exception = null;
                for (var i = 0; i < retryCount; i++)
                {
                    invokeCount++;
                    try
                    {
                        return await callback().ConfigureAwait(false);
                    }
                    catch (BucketNotFoundException ex)
                    {
                        exception = ex;
                    }
                }

                throw exception;
            });

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(Guid.NewGuid().ToString())
            .WithObject("aa")
            .WithCallbackStream(s => { });
        _ = await Assert.ThrowsExceptionAsync<BucketNotFoundException>(
            () => client.GetObjectAsync(getObjectArgs)).ConfigureAwait(false);
        Assert.AreEqual(invokeCount, retryCount);
    }
}
