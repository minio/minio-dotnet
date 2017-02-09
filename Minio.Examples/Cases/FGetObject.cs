using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class FGetObject
    {
        //Download object from bucket into local file
        public async static Task Run(Minio.MinioClient minio, 
                                     string bucketName = "my-bucket-name",
                                     string objectName = "my-object-name",
                                     string fileName="local-filename")
        {
            try
            {
                await minio.Api.GetObjectAsync(bucketName, objectName, fileName);

            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
