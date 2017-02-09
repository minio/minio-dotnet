using System;
using System.Net;
using Minio.Exceptions;

namespace Minio.Examples
{
    class Program
    {
        public static void Main(string[] args)
        {
           /*
            * Uncomment to use Play minio server
            var minioClient = new Minio.MinioClient("play.minio.io:9000",
              "Q3AM3UQ867SPQQA43P2F",
              "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
              ).WithSSL();
             */
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                     | SecurityProtocolType.Tls11
                                     | SecurityProtocolType.Tls12;
            
            var endPoint = Environment.GetEnvironmentVariable("AWS_ENDPOINT");
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");


            var minioClient = new MinioClient(endPoint,
                                    accessKey: accessKey, 
                                    secretKey: secretKey).WithSSL();
            try
            {
                // Change these parameters before running examples 
                string bucketName       = "sanfrancisco";
                string objectName       = "goldengate_pic";
                string objectPrefix     = "gold";
                string smallFilePath    = "C:\\Users\\vagrant\\Downloads\\hello.txt";
                string uploadFilePath   = "C:\\Users\\vagrant\\Downloads\\go1.7.4.windows-amd64.msi";
                string downloadFilePath = "C:\\Users\\vagrant\\Downloads\\downloaded-object";
                string destBucketName   = "backup_folder";
                string destObjectName   = "goldengate_copy";
                string removeObject     = "goldengate_pic";
               
                // Set app Info 
                minioClient.SetAppInfo("app-name", "app-version");
             
                // Set HTTP Tracing On
                minioClient.SetTraceOn();

                // Set HTTP Tracing On
                minioClient.SetTraceOff();

                //* UNCOMMENT CASE TO RUN A TEST 

                //Cases.MakeBucket.Run(minioClient, bucketName).Wait();
                //Cases.BucketExists.Run(minioClient, bucketName).Wait();
                //Cases.ListBuckets.Run(minioClient).Wait();
                //Cases.ListObjects.Run(minioClient, bucketName);
                //Cases.PutObject.Run(minioClient, bucketName, objectName, smallFilePath).Wait();
                //Cases.GetObject.Run(minioClient, bucketName, objectName).Wait();
                //Cases.FPutObject.Run(minioClient, bucketName, objectName,uploadFilePath).Wait();

                //Cases.FGetObject.Run(minioClient, bucketName, objectName,downloadFilePath).Wait();

                //Cases.RemoveBucket.Run(minioClient, bucketName).Wait();
                //Cases.ListIncompleteUploads.Run(minioClient, bucketName, prefix:objectPrefix);
                //Cases.RemoveIncompleteUpload.Run(minioClient, bucketName, removeObject).Wait();

                //Cases.GetBucketPolicy.Run(minioClient, bucketName).Wait();

                //Cases.SetBucketPolicy.Run(minioClient, bucketName).Wait();
                //Cases.StatObject.Run(minioClient, bucketName, objectName).Wait();
                //Cases.CopyObject.Run(minioClient, bucketName, objectName, destBucketName, destObjectName).Wait();
               // Cases.PresignedGetObject.Run();
               // Cases.PresignedPostPolicy.Run();
               // Cases.PresignedPutObject.Run();

                Console.ReadLine();
            }
            catch(MinioException ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
           
        }

    }
}
