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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Minio.Examples.Cases;

internal class RemoveObjects
{
    // Remove a list of objects from a bucket
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        List<string> objectsList = null,
        List<Tuple<string, string>> objectsVersionsList = null)
    {
        try
        {
            Console.WriteLine("Running example for API: RemoveObjectsAsync");
            if (objectsList != null)
            {
                var objArgs = new RemoveObjectsArgs()
                    .WithBucket(bucketName)
                    .WithObjects(objectsList);
                var objectsOservable = await minio.RemoveObjectsAsync(objArgs).ConfigureAwait(false);
                var objectsSubscription = objectsOservable.Subscribe(
                    objDeleteError => Console.WriteLine($"Object: {objDeleteError.Key}"),
                    ex => Console.WriteLine($"OnError: {ex}"),
                    () => { Console.WriteLine($"Removed objects in list from {bucketName}\n"); });
                return;
            }

            var objVersionsArgs = new RemoveObjectsArgs()
                .WithBucket(bucketName)
                .WithObjectsVersions(objectsVersionsList);
            var observable = await minio.RemoveObjectsAsync(objVersionsArgs).ConfigureAwait(false);
            var subscription = observable.Subscribe(
                objVerDeleteError => Console.WriteLine($"Object: {objVerDeleteError.Key} " +
                                                       $"Object Version: {objVerDeleteError.VersionId}"),
                ex => Console.WriteLine($"OnError: {ex}"),
                () => { Console.WriteLine($"Removed objects versions from {bucketName}\n"); });
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket-Object]  Exception: {e}");
        }
    }
}