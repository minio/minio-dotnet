using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class FGetObject
    {
        //get object in a bucket
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                string fileName = "C:\\Users\\vagrant\\Downloads\\sDownload3";

                await minio.Objects.GetObjectAsync("mountshasta", "full-upload-multi", fileName);

            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
