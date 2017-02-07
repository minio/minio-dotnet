using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class CopyObject
    {
        //copy object from one bucket to another
        public async static Task Run(Minio.MinioClient minio,
                                     string fromBucketName="from-bucket-name",
                                     string fromObjectName="from-object-name",
                                     string destBucketName="dest-bucket",
                                     string destObjectName="to-object-name")
        {
            try
            {
                //Optionally pass copy conditions
                await minio.Api.CopyObjectAsync(fromBucketName, 
                                                fromObjectName, 
                                                destBucketName, 
                                                destObjectName, 
                                                copyConditions:null);
                Console.Out.WriteLine("done copying");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }

    }
}