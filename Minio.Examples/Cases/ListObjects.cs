/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017-2021 MinIO, Inc.
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

using Minio.DataModel;
using System;

namespace Minio.Examples.Cases
{
    class ListObjects
    {
        // List objects matching optional prefix in a specified bucket.
        public static void Run(MinioClient minio,
                                     string bucketName = "my-bucket-name",
                                     string prefix = null,
                                     bool recursive = true,
                                     bool versions=false)
        {
            try
            {
                Console.WriteLine("Running example for API: ListObjectsAsync");
                ListObjectsArgs listArgs = new ListObjectsArgs()
                                                    .WithBucket(bucketName)
                                                    .WithPrefix(prefix)
                                                    .WithRecursive(recursive);
                IObservable<Item> observable = minio.ListObjectsAsync(listArgs);

                IDisposable subscription = observable.Subscribe(
                    item => Console.WriteLine($"Object: {item.Key}"),
                    ex => Console.WriteLine($"OnError: {ex}"),
                    () => Console.WriteLine($"Listed all objects in bucket {bucketName}\n"));

                if (versions)
                {
                    listArgs.WithVersions(true);
                    IObservable<VersionItem> observableVer = minio.ListObjectVersionsAsync(listArgs);

                    IDisposable subscriptionVer = observableVer.Subscribe(
                        item => Console.WriteLine($"Object: {item.Key} Version: {item.VersionId}"),
                        ex => Console.WriteLine($"OnError: {ex}"),
                        () => Console.WriteLine($"Listed all objects in bucket {bucketName}\n"));
                }
                // subscription.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }
}
