using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class FPutObject
    {

        //get object in a bucket
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                //TODO uncomment later
                //await minio.Objects.PutObjectAsync("bucket-name", "object-name", "fileName", "optional-content-type-or-null");

                //TODO end uncomment

                //TODO comment out for release
                // String fileName = "C:\\Users\\vagrant\\Downloads\\go1.7.4.windows-amd64.msi";
                String fileName = "C:\\Users\\vagrant\\Downloads\\multipart-2parts";
                await minio.Api.PutObjectAsync("mountshasta", "full-upload-fromfile2", fileName,contentType: "application/octet-stream");
                //TODO end comment out for release
                Console.Out.WriteLine("done uploading");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
    }
}
