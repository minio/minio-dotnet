using System;
using Minio;
using Minio.Exceptions;
using Minio.DataModel;
using System.Threading.Tasks;

namespace FileUploader
{
    class FileUpload
    {
        static void Main(string[] args)
        {
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
            var bucketName = "mymusic"; //<==== change this
            var location   = "us-east-1";
            // Upload the zip file
            //var objectName = "golden-oldies.zip";
            //var filePath = "/tmp/golden-oldies.zip";
           // var contentType = "application/zip";

            try
            {
                bool success = await minio.Buckets.MakeBucketAsync(bucketName, location);
                if (!success) {
                    bool found = await minio.Buckets.BucketExistsAsync(bucketName);
                    Console.Out.WriteLine("bucket-name was " + ((found == true) ? "found" : "not found"));
                }
                else { 
                    // to be implemented
                    //var size =  await minio.Buckets.FPutObject(bucketName, objectName, filePath, contentType);  
                    //Console.Out.WriteLine("Successfully uploaded " + objectName + " of size" + size);
                }
               
            }
            catch (Exception e)
            {
                Console.WriteLine("[Bucket]  Exception: {0}", e);
            }
        }
   

    }
}
