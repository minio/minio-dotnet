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

namespace Minio.Examples.Cases;

public static class CustomRequestLogger
{
    // Check if a bucket exists
    public static async Task Run(IMinioClient minio)
    {
        if (minio is null) throw new ArgumentNullException(nameof(minio));

        try
        {
            Console.WriteLine("Running example for: set custom request logger");
            minio.SetTraceOn(new MyRequestLogger());
            await minio.ListBucketsAsync().ConfigureAwait(false);
            minio.SetTraceOff();
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}