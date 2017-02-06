using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
namespace Minio.Examples.Cases
{
    class GetObject
    {
        //get object in a bucket
        public async static Task Run(Minio.MinioRestClient minio,
                                     string bucketName="my-bucket-name",
                                     string objectName="my-object-name")
        {
            try
            {
                await minio.Api.GetObjectAsync(bucketName, objectName, 
                (stream) =>
                {
                    stream.CopyTo(Console.OpenStandardOutput());
                });
                             
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
