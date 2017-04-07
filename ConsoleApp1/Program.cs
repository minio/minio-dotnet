using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            string endPoint = Environment.GetEnvironmentVariable("AWS_ENDPOINT");
            string accessKey = Environment.GetEnvironmentVariable("MY_AWS_ACCESS_KEY");

            string secretKey = Environment.GetEnvironmentVariable("MY_AWS_SECRET_KEY");
            /*
            endPoint = "play.minio.io";
            accessKey = "Q3AM3UQ867SPQQA43P2F";
            secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            ServicePointManager.Expect100Continue = true;
            */
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                     | SecurityProtocolType.Tls11
                                     | SecurityProtocolType.Tls12;

            // WithSSL() enables SSL support in Minio client
            var minioClient = new Minio.MinioClient(endPoint, accessKey, secretKey).WithSSL();

            try
            {
                // Assign parameters before starting the test 
                 string bucketName = "miniodotnetjlx2s";  //for eastern      
                bucketName = "testminiopolicy";
                string objectName = "testobject4000";
                Program.Run(minioClient, bucketName, objectName).Wait();
                Console.ReadLine();

            }
            catch (MinioException ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
        }

        public static async Task Run(MinioClient client,string bucketName, string objectName)
        {
            try
            {
               // PolicyType policy = await client.GetPolicyAsync(bucketName);
                /*
                string presigned_get_url = await client.PresignedGetObjectAsync(bucketName, objectName, 1000);
                Console.Out.WriteLine("PRESIGNED_GET_URL:" + presigned_get_url);

                string presigned_put_url = await client.PresignedPutObjectAsync(bucketName, objectName, 1000);
                Console.Out.WriteLine(presigned_put_url);
                UploadObject(presigned_put_url);
                */
                PostPolicy form = new PostPolicy();
                DateTime expiration = DateTime.UtcNow;
                form.SetExpires(expiration.AddDays(10));
                form.SetKey(objectName);
                form.SetBucket(bucketName);
                form.SetContentRange(1, 10);

                Tuple<string,Dictionary<string, string>> tuple = await client.PresignedPostPolicyAsync(form);
                string curlCommand = "curl ";
                foreach (KeyValuePair<string, string> pair in tuple.Item2)
                {
                    curlCommand = curlCommand + " -F " + pair.Key + "=" + pair.Value;
                }
                curlCommand = curlCommand + " -F file=@/etc/bashrc " + tuple.Item1;
                Console.Out.WriteLine("PRESIGNED_POLICY_CURL_REQUEST:" + curlCommand);
               

            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Exception ", e.Message);
            }
        }
        static void UploadObject(string url)
        {
            HttpWebRequest httpRequest = WebRequest.Create(url) as HttpWebRequest;
            httpRequest.Method = "PUT";
            using (Stream dataStream = httpRequest.GetRequestStream())
            {
                byte[] buffer = new byte[8000];
                using (FileStream fileStream = new FileStream("C:\\Users\\vagrant\\Downloads\\testobject", FileMode.Open, FileAccess.Read))
                {
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        dataStream.Write(buffer, 0, bytesRead);
                    }
                }
            }

            HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
        }

    }
}
