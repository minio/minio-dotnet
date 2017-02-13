using MinioCore2;
using MinioCore2.Exceptions;
using System;
namespace MinioCoreTest
{
    public class Program
    {
        static void Main(string[] args)
        {

            //ServicePointManager.Expect100Continue = true;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
            //                         | SecurityProtocolType.Tls11
            //                         | SecurityProtocolType.Tls12;
            // WinHttpHandler httpHandler = new WinHttpHandler();
            //  httpHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            var endPoint = "play.minio.io:9000";
            var accessKey = "Q3AM3UQ867SPQQA43P2F";
            var secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
           // var endPoint = Environment.GetEnvironmentVariable("AWS_ENDPOINT");
           // var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
           // var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

            var minioClient = new MinioClient(endPoint, accessKey, secretKey).WithSSL();
            try
            {
                // Change these parameters before running examples 
                string bucketName = "testminiopolicy";
                string objectName = "goldengate.jpg";
                string objectPrefix = "gold";
                string smallFilePath = "C:\\Users\\vagrant\\Downloads\\hellotext";
                string uploadFilePath = "C:\\Users\\vagrant\\Downloads\\go1.7.4.windows-amd64.msi";
                string downloadFilePath = "C:\\Users\\vagrant\\Downloads\\downloaded-object";
                string destBucketName = "testminiopoli";
                string destObjectName = "goldengate_copy.jpg";
                string removeObject = "goldengate_pic";

                // Set app Info 
                minioClient.SetAppInfo("app-name", "app-version");

                // Set HTTP Tracing On
               // minioClient.SetTraceOn();

                // Set HTTP Tracing Off
                // minioClient.SetTraceOff();

                //* UNCOMMENT CASE TO RUN A TEST 

               // Cases.BucketExists.Run(minioClient, bucketName).Wait();

                Cases.MakeBucket.Run(minioClient, bucketName).Wait();

                Cases.ListBuckets.Run(minioClient).Wait();
                Cases.ListObjects.Run(minioClient, bucketName);
                Cases.PutObject.Run(minioClient, bucketName, objectName, smallFilePath).Wait();
                Cases.GetObject.Run(minioClient, bucketName, objectName).Wait();
                Cases.FPutObject.Run(minioClient, bucketName, objectName, uploadFilePath).Wait();

                Cases.FGetObject.Run(minioClient, bucketName, objectName, downloadFilePath).Wait();

                Cases.RemoveObject.Run(minioClient, bucketName, objectName).Wait();
                Cases.RemoveBucket.Run(minioClient, bucketName).Wait();
                Cases.ListIncompleteUploads.Run(minioClient, bucketName, prefix: objectPrefix);
                Cases.RemoveIncompleteUpload.Run(minioClient, bucketName, removeObject).Wait();

                Cases.GetBucketPolicy.Run(minioClient, bucketName).Wait();

                Cases.SetBucketPolicy.Run(minioClient, bucketName).Wait();
                Cases.StatObject.Run(minioClient, bucketName, objectName).Wait();
                Cases.CopyObject.Run(minioClient, bucketName, objectName, destBucketName, destObjectName).Wait();

                Cases.PresignedGetObject.Run(minioClient);
                Cases.PresignedPostPolicy.Run(minioClient);
                Cases.PresignedPutObject.Run(minioClient);
                
                Console.ReadLine();
            }
            catch (MinioException ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
        }
    }
}
