using System;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class RemoveObject
    {
        //Remove an object from a bucket
        public async static Task Run(MinioClient minio,
                                     string bucketName = "my-bucket-name", 
                                     string objectName = "my-object-name")
        {
            try
            {
                await minio.Api.RemoveObjectAsync(bucketName,objectName);
                Console.Out.WriteLine("object-name removed from bucket-name successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket-Object]  Exception: {0}", e);
            }
        }
    }
}
