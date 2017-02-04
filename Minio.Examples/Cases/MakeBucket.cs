using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    public class MakeBucket
    {
        //Make a bucket
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                await minio.Api.MakeBucketAsync("mybucket");
                Console.Out.WriteLine("mybucket created successfully");

            } 
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }

        
    }
}
