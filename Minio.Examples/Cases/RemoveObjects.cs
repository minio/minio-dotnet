/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class RemoveObjects
    {
        // Remove a list of objects from a bucket
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket-name",
                                     List<string> objectsList=null)
        {
            try
            {
                Console.Out.WriteLine("Running example for API: RemoveObjectAsync");
                IObservable<DeleteError> observable = await minio.RemoveObjectAsync(bucketName, objectsList);
                IDisposable subscription = observable.Subscribe(
                   deleteError => Console.WriteLine("Object: {0}", deleteError.Key),
                   ex => Console.WriteLine("OnError: {0}", ex),
                   () =>
                   {
                       Console.WriteLine("Listed all delete errors for remove objects on  " + bucketName + "\n");
                   });
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket-Object]  Exception: {0}", e);
            }
        }
    }
}
