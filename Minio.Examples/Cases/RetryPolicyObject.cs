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

using System;
using System.Threading.Tasks;

using Minio.Exceptions;

using Polly;

namespace Minio.Examples.Cases
{
    static class RetryPolicyHelper
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
                .Or<InternalClientException>(ex => ex.Message.StartsWith("Unsuccessful response from server"));
        }

        public static AsyncPolicy<ResponseResult> GetDefaultRetryPolicy() =>
            GetDefaultRetryPolicy(defaultRetryCount, defaultRetryInterval, defaultMaxRetryInterval);

        public static AsyncPolicy<ResponseResult> GetDefaultRetryPolicy(
            int retryCount,
            TimeSpan retryInterval,
            TimeSpan maxRetryInterval) =>
            CreatePolicyBuilder()
                .WaitAndRetryAsync(
                    retryCount,
                    i => CalcBackoff(i, retryInterval, maxRetryInterval));

        public static RetryPolicyHandlingDelegate AsRetryDelegate(this AsyncPolicy<ResponseResult> policy) =>
            policy == null
                ? (RetryPolicyHandlingDelegate)null
                : async executeCallback => await policy.ExecuteAsync(executeCallback);

        public static MinioClient WithRetryPolicy(this MinioClient client, AsyncPolicy<ResponseResult> policy) =>
            client.WithRetryPolicy(policy.AsRetryDelegate());
    }

    class RetryPolicyObject
    {
        // Polly retry policy sample
        public async static Task Run(MinioClient minio,
            string bucketName = "my-bucket-name",
            string bucketObject = "my-object-name")
        {
            try
            {
                var customPolicy = RetryPolicyHelper
                    .CreatePolicyBuilder()
                    .Or<BucketNotFoundException>()
                    .RetryAsync(
                        2,
                        (r, i) => Console.WriteLine($"On retry #{i}. Result: {r.Exception?.Message}"));

                minio.WithRetryPolicy(customPolicy);

                Console.WriteLine("Running example for API: RetryPolicyObject");

                try
                {
                    GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                            .WithBucket("bad-bucket")
                                                            .WithObject("bad-file")
                                                            .WithCallbackStream(s => { });
                    await minio.GetObjectAsync(getObjectArgs);
                }
                catch (BucketNotFoundException ex)
                {
                    Console.WriteLine("Request failed: " + ex.Message);
                }

                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[StatObject] {bucketName}-{bucketObject} Exception: {e}");
            }
            finally
            {
                minio.WithRetryPolicy(null);
            }
        }
    }
}
    }
}
