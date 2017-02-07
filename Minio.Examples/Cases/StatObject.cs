using System;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio.Examples.Cases
{
    class StatObject
    {
        //get stats on a object
        public async static Task Run(Minio.MinioClient minio, 
                                     string bucketName = "my-bucket-name",
                                     string bucketObject="my-object-name")
        {
            try
            {
                ObjectStat statObject = await minio.Api.StatObjectAsync(bucketName, bucketObject);
                Console.Out.WriteLine(statObject);
            }
            catch (Exception e)
            {
                Console.WriteLine("[StatObject] {0}-{1}  Exception: {2}",bucketName, bucketObject, e);
            }
        }
    }
}
