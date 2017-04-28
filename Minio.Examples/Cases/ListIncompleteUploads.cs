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

using Minio.DataModel;
using System;


namespace Minio.Examples.Cases
{
    class ListIncompleteUploads
    {
        // List incomplete uploads on the bucket matching specified prefix
        public static void Run(Minio.MinioClient minio,
                               string bucketName = "my-bucket-name", 
                                   string prefix = "my-object-name",
                                  bool recursive = true)
        {
            try
            {
                Console.Out.WriteLine("Running example for API: ListIncompleteUploads");

                IObservable<Upload> observable = minio.ListIncompleteUploads(bucketName, prefix, recursive);

                IDisposable subscription = observable.Subscribe(
                    item => Console.WriteLine("OnNext: {0}", item.Key),
                    ex => Console.WriteLine("OnError: {0}", ex.Message),
                    () => Console.WriteLine("Listed the pending uploads to bucket " + bucketName));

                Console.Out.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
        }
    }
}
