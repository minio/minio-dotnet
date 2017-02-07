using System;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio.Examples.Cases
{
    class SetBucketPolicy
    {
        //set bucket policy
        public async static Task Run(Minio.MinioClient minio, 
                                     string bucketName = "my-bucket-name",
                                     string objectPrefix="")
        {
            try
            {
                //Change policy type parameter
                await minio.Api.SetPolicyAsync(bucketName, 
                                               objectPrefix,
                                               PolicyType.READ_ONLY);

            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
