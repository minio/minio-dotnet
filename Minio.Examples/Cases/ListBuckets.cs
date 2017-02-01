using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio.Examples.Cases
{
    class ListBuckets
    {
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                var list = await minio.Api.ListBucketsAsync();
                foreach (Bucket bucket in list.Buckets)
                {
                    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }

       
    }
}
