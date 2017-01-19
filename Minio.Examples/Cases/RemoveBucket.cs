using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class RemoveBucket
    {
        //Remove a bucket
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                await minio.Buckets.RemoveBucketAsync("bucket-name");
                Console.Out.WriteLine("bucket-name removed successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
