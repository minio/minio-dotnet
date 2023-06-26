/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
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

using Minio.Exceptions;
using Polly;

namespace Minio.Examples.Cases;

internal static class RetryPolicyHelper
{
    private const int defaultRetryCount = 3;
    private static readonly TimeSpan defaultRetryInterval = TimeSpan.FromMilliseconds(200);

    private static readonly TimeSpan defaultMaxRetryInterval = TimeSpan.FromSeconds(10);

    // exponential backoff with jitter, truncated by max retry interval
    public static TimeSpan CalcBackoff(int retryAttempt, TimeSpan retryInterval, TimeSpan maxRetryInterval)
    {
        // 0.8..1.2
        var jitter = 0.8 + new Random(Environment.TickCount).NextDouble() * 0.4;
        // (2^retryCount - 1) * jitter
        var scaleCoeff = (Math.Pow(2.0, retryAttempt) - 1.0) * jitter;
        // Apply scale coefficient
        var result = TimeSpan.FromMilliseconds(retryInterval.TotalMilliseconds * scaleCoeff);
        // Truncate by max retry interval
        return result < maxRetryInterval ? result : maxRetryInterval;
    }

    public static PolicyBuilder<ResponseResult> CreatePolicyBuilder()
    {
        return Policy<ResponseResult>
            .Handle<ConnectionException>()
            .Or<InternalClientException>(ex =>
                ex.Message.StartsWith("Unsuccessful response from server", StringComparison.InvariantCulture));
    }

    public static AsyncPolicy<ResponseResult> GetDefaultRetryPolicy()
    {
        return GetDefaultRetryPolicy(defaultRetryCount, defaultRetryInterval, defaultMaxRetryInterval);
    }

    public static AsyncPolicy<ResponseResult> GetDefaultRetryPolicy(
        int retryCount,
        TimeSpan retryInterval,
        TimeSpan maxRetryInterval)
    {
        return CreatePolicyBuilder()
            .WaitAndRetryAsync(
                retryCount,
                i => CalcBackoff(i, retryInterval, maxRetryInterval));
    }

    public static RetryPolicyHandler AsRetryDelegate(this AsyncPolicy<ResponseResult> policy)
    {
        return policy is null
            ? null
            : async executeCallback => await policy.ExecuteAsync(executeCallback).ConfigureAwait(false);
    }

    public static MinioClient WithRetryPolicy(this MinioClient client, AsyncPolicy<ResponseResult> policy)
    {
        return client.WithRetryPolicy(policy.AsRetryDelegate());
    }
}