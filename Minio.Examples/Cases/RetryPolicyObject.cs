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

using Minio.DataModel.Args;
using Minio.Exceptions;
using Polly;

namespace Minio.Examples.Cases;

internal static class RetryPolicyObject
{
    // Polly retry policy sample
    public static async Task Run(MinioClient minio,
        string bucketName = "my-bucket-name",
        string bucketObject = "my-object-name")
    {
        if (minio is null) throw new ArgumentNullException(nameof(minio));

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
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket("bad-bucket")
                    .WithObject("bad-file")
                    .WithCallbackStream(s => { });
                await minio.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
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
