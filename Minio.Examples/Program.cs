using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio;
using System.Net;
using Minio.Exceptions;
namespace Minio.Examples
{
    class Program
    {
        public static void Main(string[] args)
        {
           /* 
            var minioClient = new Minio.MinioRestClient("play.minio.io:9000",
              "Q3AM3UQ867SPQQA43P2F",
              "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
              ).WithSSL();
              */
            //ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                     | SecurityProtocolType.Tls11
                                     | SecurityProtocolType.Tls12;
            
            var endPoint = Environment.GetEnvironmentVariable("AWS_ENDPOINT");
            var accessKey = Environment.GetEnvironmentVariable("MY_AWS_ACCESS_KEY");
            var secretKey = Environment.GetEnvironmentVariable("MY_AWS_SECRET_KEY");

            endPoint = "s3-us-west-1.amazonaws.com";
            var minioClient = new MinioRestClient(endPoint,
                                    accessKey: accessKey, 
                                    secretKey: secretKey).WithSSL();
            try
            {
                //Set app Info 
                minioClient.SetAppInfo("app-name", "app-version");
                Cases.RemoveBucket.Run(minioClient).Wait();

                //Cases.BucketExists.Run(minioClient).Wait();
                //Cases.ListBuckets.Run(minioClient).Wait();


                Cases.MakeBucket.Run(minioClient).Wait();
                Cases.ListBuckets.Run(minioClient).Wait();

                Cases.FPutObject.Run(minioClient).Wait();
 
               // Cases.GetObject.Run(minioClient).Wait();
  
                Cases.GetObject.Run(minioClient).Wait();
                Cases.StatObject.Run(minioClient).Wait();
                Cases.PutObject.Run(minioClient).Wait();
                Cases.ListIncompleteUploads.Run(minioClient);
                Cases.RemoveIncompleteUpload.Run(minioClient).Wait();
                Cases.CopyObject.Run(minioClient).Wait();
                Cases.ListObjects.Run(minioClient).Wait();
                Cases.FGetObject.Run(minioClient).Wait();
              
                Console.ReadLine();
            }
            catch(ClientException ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
           
        }

    }
}
