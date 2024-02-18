﻿/*
 * Newtera .NET Library for Newtera TDM, (C) 2017 Newtera, Inc.
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

using System.Net;
using Newtera;
using Newtera.DataModel.Args;

namespace SimpleTest;

public static class Program
{
    private static async Task Main()
    {
        // Note: s3 AccessKey and SecretKey needs to be added in App.config file
        // See instructions in README.md on running examples for more information.
        using var newtera = new NewteraClient()
            .WithEndpoint("localhost:8080")
            .WithCredentials("demo1",
                "888")
            .Build();

        var listBuckets = await newtera.ListBucketsAsync().ConfigureAwait(false);

        foreach (var bucket in listBuckets.Buckets)
            Console.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);

        //Supply a new bucket name
        var bucketName = "tdm";

        var found = await IsBucketExists(newtera, bucketName).ConfigureAwait(false);
        Console.WriteLine("Bucket exists? = " + found);
        _ = Console.ReadLine();
    }

    private static Task<bool> IsBucketExists(INewteraClient newtera, string bucketName)
    {
        var bktExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        return newtera.BucketExistsAsync(bktExistsArgs);
    }
}
