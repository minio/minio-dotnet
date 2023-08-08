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

using System.Net;
using Minio;
using Minio.DataModel.Args;

namespace SimpleTest;

public static class Program
{
    private static async Task Main(string[] args)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                               | SecurityProtocolType.Tls11
                                               | SecurityProtocolType.Tls12;

        // Note: s3 AccessKey and SecretKey needs to be added in App.config file
        // See instructions in README.md on running examples for more information.
        using var minio = new MinioClient()
            .WithEndpoint("play.min.io")
            .WithCredentials("Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG")
            .WithSSL()
            .Build();

        var listBuckets = await minio.ListBucketsAsync().ConfigureAwait(false);

        foreach (var bucket in listBuckets.Buckets)
            Console.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);

        //Supply a new bucket name
        var bucketName = "mynewbucket";
        if (await IsBucketExists(minio, bucketName).ConfigureAwait(false))
        {
            var remBuckArgs = new RemoveBucketArgs().WithBucket(bucketName);
            await minio.RemoveBucketAsync(remBuckArgs).ConfigureAwait(false);
        }

        var mkBktArgs = new MakeBucketArgs().WithBucket(bucketName);
        await minio.MakeBucketAsync(mkBktArgs).ConfigureAwait(false);

        var found = await IsBucketExists(minio, bucketName).ConfigureAwait(false);
        Console.WriteLine("Bucket exists? = " + found);
        _ = Console.ReadLine();
    }

    private static Task<bool> IsBucketExists(IMinioClient minio, string bucketName)
    {
        var bktExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        return minio.BucketExistsAsync(bktExistsArgs);
    }
}
