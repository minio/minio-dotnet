using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class RemoveIncompleteUpload
    {
      
        //Remove incomplete upload object from a bucket
        public async static Task Run(MinioRestClient minio)
        {
            try
            {
                var bucketName = "bucket-name";
                var bucketObject = "bucket-object";

                bucketName = "mountshasta";
                bucketObject = "newmulti-225";
                await minio.Api.RemoveIncompleteUploadAsync(bucketName, bucketObject);
                Console.Out.WriteLine("object-name removed from bucket-name successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket-Object]  Exception: {0}", e);
            }
        }
    }
}
