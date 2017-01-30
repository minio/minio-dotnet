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
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                await minio.Objects.CopyObjectAsync("mountshasta", "testobject", "bobcat2t", "copi2dobj", null);
                Console.Out.WriteLine("done copying");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }

    }
}