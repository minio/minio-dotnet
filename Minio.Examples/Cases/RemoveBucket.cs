using System;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class RemoveBucket
    {
        //Remove a bucket
        public async static Task Run(MinioClient minio, 
                                     string bucketName = "my-bucket-name")
        {
            try
            {
                await minio.Api.RemoveBucketAsync(bucketName);
                Console.Out.WriteLine("bucket-name removed successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
