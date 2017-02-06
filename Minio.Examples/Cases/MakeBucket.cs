using System;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    public class MakeBucket
    {
        //Make a bucket
        public async static Task Run(Minio.MinioRestClient minio,
                                     string bucketName="my-bucket-name")
        {
            try
            {
                await minio.Api.MakeBucketAsync(bucketName);
                Console.Out.WriteLine("bucket-name created successfully");
            } 
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }

        
    }
}
