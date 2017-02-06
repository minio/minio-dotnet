using System;
using System.IO;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class PutObject
    {
        //Put an object from a local stream into bucket
        public async static Task Run(Minio.MinioRestClient minio,
                                     string bucketName = "my-bucket-name", 
                                     string objectName = "my-object-name",
                                     string fileName="location-of-file")
        {
            try
            {
                byte[] bs = File.ReadAllBytes(fileName);
                System.IO.MemoryStream filestream = new System.IO.MemoryStream(bs);

                await minio.Api.PutObjectAsync(bucketName,
                                           objectName,
                                           filestream,
                                           filestream.Length,
                                           "application/octet-stream");

                Console.Out.WriteLine("done uploading");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
      
    }
}
