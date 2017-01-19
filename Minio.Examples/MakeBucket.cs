using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples
{
    public class MakeBucket
    {
        static int Main()
        {
            var minio = new Minio.Api.MinioRestClient("play.minio.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                ).WithSSL();
        
            Task.WaitAll(minio.Buckets.MakeBucketAsync("bucket-name")); // block while the task completes
            Console.ReadLine();
            return 0;
        }
    }
}
