using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.DataModel;
namespace Minio.Examples.Cases
{
    class GetBucketPolicy
    {
        //get bucket policy
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                PolicyType policy = await minio.Api.GetPolicyAsync("testminiopolicy",objectPrefix:"bnds");
                Console.Out.WriteLine("POLICY: " + policy.GetType().ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
