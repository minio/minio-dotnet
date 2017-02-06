using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio;
using System.Net;
using Minio.Exceptions;
using System.Text.RegularExpressions;

namespace Minio.Examples
{
    class Program
    {
        public static void Main(string[] args)
        {
           
            var minioClient2 = new Minio.MinioRestClient("play.minio.io:9000",
              "Q3AM3UQ867SPQQA43P2F",
              "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
              ).WithSSL();
             
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                     | SecurityProtocolType.Tls11
                                     | SecurityProtocolType.Tls12;
            
            var endPoint = Environment.GetEnvironmentVariable("AWS_ENDPOINT");
            var accessKey = Environment.GetEnvironmentVariable("MY_AWS_ACCESS_KEY");
            var secretKey = Environment.GetEnvironmentVariable("MY_AWS_SECRET_KEY");

            var minioClient = new MinioRestClient(endPoint,
                                    accessKey: accessKey, 
                                    secretKey: secretKey).WithSSL();
            try
            {
             
                string bucketName = "testminiopolicyzzz";
                string objectName ="testobject";
                string prefix = "mult";
                string smallFilePath = "C:\\Users\\vagrant\\Downloads\\hellotext";
                string uploadFilePath = "C:\\Users\\vagrant\\Downloads\\go1.7.4.windows-amd64.msi";
                string downloadFilePath = "C:\\Users\\vagrant\\Downloads\\downloaded-object";
                string destBucketName = "mtky2";
                string destObjectName = "copyrighted_copy.txt";
                string removeObject = "newmulti-406";
                //Set app Info 
                minioClient.SetAppInfo("app-name", "app-version");
                
                Cases.MakeBucket.Run(minioClient, bucketName).Wait();
                Cases.BucketExists.Run(minioClient, bucketName).Wait();
                Cases.ListBuckets.Run(minioClient).Wait();
                Cases.ListObjects.Run(minioClient, bucketName);
                Cases.PutObject.Run(minioClient, bucketName, objectName, smallFilePath).Wait();
                Cases.GetObject.Run(minioClient, bucketName, objectName).Wait();
                Cases.FPutObject.Run(minioClient, bucketName, objectName,uploadFilePath).Wait();
               
                Cases.FGetObject.Run(minioClient, bucketName, objectName,downloadFilePath).Wait();
               
                Cases.RemoveBucket.Run(minioClient, bucketName).Wait();
                Cases.ListIncompleteUploads.Run(minioClient, bucketName, prefix:prefix);
                Cases.RemoveIncompleteUpload.Run(minioClient, bucketName, removeObject).Wait();
               
                Cases.GetBucketPolicy.Run(minioClient, bucketName).Wait();
                
                Cases.SetBucketPolicy.Run(minioClient, bucketName).Wait();
                Cases.StatObject.Run(minioClient, bucketName, objectName).Wait();
                Cases.CopyObject.Run(minioClient, bucketName, objectName, destBucketName, destObjectName).Wait();
                Console.ReadLine();
            }
            catch(ClientException ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
           
        }

    }
}
