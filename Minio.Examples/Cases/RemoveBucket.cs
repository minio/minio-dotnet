using System;
using System.Threading.Tasks;
namespace Minio.Examples.Cases
{
    class RemoveBucket
    {
        //Remove a bucket
        public async static Task Run(MinioRestClient minio)
        {
            try
            {
                await minio.Api.RemoveBucketAsync("dminio");
                Console.Out.WriteLine("bucket-name removed successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
