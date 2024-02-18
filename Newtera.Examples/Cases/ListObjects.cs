/*
 * Newtera .NET Library for Newtera TDM, (C) 2017-2021 Newtera, Inc.
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

using Newtera.DataModel.Args;

namespace Newtera.Examples.Cases;

internal static class ListObjects
{
    // List objects matching optional prefix in a specified bucket.
    public static void Run(INewteraClient newtera,
        string bucketName = "my-bucket-name",
        string prefix = null,
        bool recursive = true)
    {
        try
        {
            Console.WriteLine("Running example for API: ListObjectsAsync");
            var listArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithPrefix(prefix)
                .WithRecursive(recursive);
            var observable = newtera.ListObjectsAsync(listArgs);
            var subscription = observable.Subscribe(
                item => Console.WriteLine($"Object: {item.Key}"),
                ex => Console.WriteLine($"OnError: {ex}"),
                () => Console.WriteLine($"Listed all objects in bucket {bucketName}\n"));
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}
