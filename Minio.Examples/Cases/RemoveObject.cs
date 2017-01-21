using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio;
namespace Minio.Examples.Cases
{
    class RemoveObject
    {
        //Remove an object from a bucket
        public async static Task Run(MinioRestClient minio)
        {
            try
            {
                await minio.Objects.RemoveObjectAsync("bucket-name","object-name");
                Console.Out.WriteLine("object-name removed from bucket-name successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket-Object]  Exception: {0}", e);
            }
        }
    }
}
