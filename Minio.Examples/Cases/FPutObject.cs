using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class FPutObject
    {

        //Upload object to bucket from file
        public async static Task Run(Minio.MinioRestClient minio, 
                                      string bucketName = "my-bucket-name",
                                      string objectName = "my-object-name",
                                      string fileName = "from where")
        {
            try
            {
                 await minio.Api.PutObjectAsync(bucketName,
                                                objectName, 
                                                fileName,
                                                contentType: "application/octet-stream");
                Console.Out.WriteLine("done uploading");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
