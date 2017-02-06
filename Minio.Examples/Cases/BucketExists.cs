using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class BucketExists
    {
        //Check if a bucket exists
        public async static Task Run(Minio.MinioRestClient minio,
                                     string bucketName = "my-bucket-name")
        {
            try
            {
                bool found = await minio.Api.BucketExistsAsync(bucketName);
                Console.Out.WriteLine("bucket-name was " + ((found == true) ? "found" : "not found"));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
