using System;
using Minio.Api;
using System.Threading;
using System.Threading.Tasks;
using Minio.Api.DataModel;

namespace Minio.Examples
{
    class ListBuckets
    {
        static int Main()
        {
            var minio = new Minio.Api.MinioRestClient("play.minio.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                ).WithSSL();
            var getListBucketsTask = minio.Buckets.ListBucketsAsync();
         
            Task.WaitAll(getListBucketsTask); // block while the task completes
            var list = getListBucketsTask.Result;
            foreach (Bucket bucket in list.Buckets)
            {
                Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
            }
            return 0;
        }
    }
}
