using System;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class RemoveIncompleteUpload
    {
      
        //Remove incomplete upload object from a bucket
        public async static Task Run(MinioRestClient minio, 
                                     string bucketName = "my-bucket-name",
                                     string objectName = "my-object-name")
        {
            try
            {
                await minio.Api.RemoveIncompleteUploadAsync(bucketName, objectName);
                Console.Out.WriteLine("object-name removed from bucket-name successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket-Object]  Exception: {0}", e);
            }
        }
    }
}
