using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.DataModel.Policy;
using Minio.DataModel;

namespace Minio.Examples.Cases
{
    class SetBucketPolicy
    {
        //set bucket policy
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                await minio.Api.SetPolicyAsync("mountshasta2", "bobcat",PolicyType.READ_ONLY);

            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
