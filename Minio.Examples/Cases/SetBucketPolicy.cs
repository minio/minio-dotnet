using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class SetBucketPolicy
    {
        //set bucket policy
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                //await minio.Buckets.SetPolicyAsync("mountshasta", objectPrefix: "mult");

            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
