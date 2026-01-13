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

using Minio.DataModel.Args;

namespace Minio.Examples.Cases;

internal static class RemoveObjects
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
            if (objectsList is not null)
            {
                try
                {
                    var objArgs = new RemoveObjectsArgs()
                        .WithBucket(bucketName)
                        .WithObjects(objectsList);
                    foreach (var objDeleteError in await minio.RemoveObjectsAsync(objArgs).ConfigureAwait(false))
                        Console.WriteLine($"Object: {objDeleteError.Key}");
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"OnError: {exc}");
                }

                Console.WriteLine($"Removed objects in list from {bucketName}\n");
                return;
            }

            try
            {
                var objVersionsArgs = new RemoveObjectsArgs()
                    .WithBucket(bucketName)
                    .WithObjectsVersions(objectsVersionsList);
                foreach (var objVerDeleteError in await minio.RemoveObjectsAsync(objVersionsArgs).ConfigureAwait(false))
                    Console.WriteLine($"Object: {objVerDeleteError.Key} " +
                                      $"Object Version: {objVerDeleteError.VersionId}");
            }
            catch (Exception exc)
            {
                Console.WriteLine($"OnError: {exc}");
            }

            Console.WriteLine($"Removed objects versions from {bucketName}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket-Object]  Exception: {e}");
        }
    }
}
