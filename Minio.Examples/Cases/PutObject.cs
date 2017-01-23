using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Examples.Cases
{
    class PutObject
    {
        //get object in a bucket
        public async static Task Run(Minio.MinioRestClient minio)
        {
            try
            {
                //test1- simple upload 
               // byte[] data = System.Text.Encoding.UTF8.GetBytes("hello world");

                //await minio.Objects.PutObjectAsync("asiatrip", "hellotext", new MemoryStream(data), 11, "application/octet-stream");
                //test2 - multipart upload
               // String fileName = "C:\\Users\\vagrant\\Downloads\\go1.7.4.windows-amd64.msi";
                String fileName = "C:\\Users\\vagrant\\Downloads\\multipart-2parts";
                byte[] bs = File.ReadAllBytes(fileName);
                System.IO.MemoryStream filestream = new System.IO.MemoryStream(bs);
                await minio.Objects.PutObjectAsync("mountshasta", "full-upload-multi", filestream, filestream.Length, "application/octet-stream");
                Console.Out.WriteLine("done uploading");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
      
    }
}
