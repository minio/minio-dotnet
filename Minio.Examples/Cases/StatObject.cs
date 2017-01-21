using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.DataModel;
namespace Minio.Examples.Cases
{
    class StatObject
    {
        //get stats on a object
        public async static Task Run(Minio.MinioRestClient minio)
        {
            var bucketName   = "bucket-name";
            var bucketObject = "bucket-object";

           // bucketName = "asiatrip";
           // bucketObject = "asiaphotos.jpg";
            try
            {
                ObjectStat statObject = await minio.Objects.StatObjectAsync(bucketName, bucketObject);
                Console.Out.WriteLine(statObject);
            }
            catch (Exception e)
            {
                Console.WriteLine("[StatObject] {0}-{1}  Exception: {2}",bucketName, bucketObject, e);
            }
        }
    }
}
