using System;
using Minio;
using Minio.Exceptions;
using Minio.DataModel;
using System.Threading.Tasks;
using System.Net;

namespace FileUploader
{
    /// <summary>
    /// 
    /// </summary>
    class FileUpload
    {
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                                 | SecurityProtocolType.Tls11
                                                 | SecurityProtocolType.Tls12;
            var endpoint  = "play.minio.io:9000";
            var accessKey = "Q3AM3UQ867SPQQA43P2F";
            var secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            try
            { 
                var minio = new MinioRestClient(endpoint, accessKey, secretKey).WithSSL();
                FileUpload.Run(minio).Wait();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }
        //Check if a bucket exists
        private async static Task Run(MinioRestClient minio)
        {
            // Make a new bucket called mymusic.
            var bucketName = "mymusic-folder"; //<==== change this
            var location   = "us-east-1";
            // Upload the zip file
            var objectName = "my-golden-oldies.mp3";
            var filePath = "C:\\Users\\vagrant\\Downloads\\golden_oldies.mp3";
            var contentType = "application/zip";

            try
            {
                bool success = await minio.Api.MakeBucketAsync(bucketName, location);
                if (!success) {
                    bool found = await minio.Api.BucketExistsAsync(bucketName);
                    Console.Out.WriteLine("bucket-name was " + ((found == true) ? "found" : "not found"));
                }
                else { 
                    await minio.Api.PutObjectAsync(bucketName, objectName, filePath, contentType);  
                    Console.Out.WriteLine("Successfully uploaded " + objectName );
                }
               
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
   

    }
}
