using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Minio.Examples
{
    [MemoryDiagnoser]
    [GcServer]
    public class PrefTests
    {
        private static Random rnd = new Random();
        private static int UNIT_MB = 1024 * 1024;

        // Create a file of given size from random byte array
        private static String CreateFile(int size)
        {
            String fileName = GetRandomName();
            byte[] data = new byte[size];
            rnd.NextBytes(data);

            File.WriteAllBytes(fileName, data);

            return fileName;
        }

        // Generate a random string
        public static String GetRandomName()
        {
            string characters = "0123456789abcdefghijklmnopqrstuvwxyz";
            StringBuilder result = new StringBuilder(5);
            for (int i = 0; i < 5; i++)
            {
                result.Append(characters[rnd.Next(characters.Length)]);
            }
            return "minio-dotnet-example-" + result.ToString();
        }
        MinioClient minioClient;
        private string bucketName;
        private string smallFileName;
        private string bigFileName;
        private string objectName;

        [GlobalSetup]
        public void Init()
        {
            String endPoint = null;
            String accessKey = null;
            String secretKey = null;
            bool enableHTTPS = false;
            if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") != null)
            {
                endPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
                accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
                secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
                if (Environment.GetEnvironmentVariable("ENABLE_HTTPS") != null)
                    enableHTTPS = Environment.GetEnvironmentVariable("ENABLE_HTTPS").Equals("1");
            }
            else
            {
                endPoint = "play.minio.io:9000";
                accessKey = "Q3AM3UQ867SPQQA43P2F";
                secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
                enableHTTPS = true;
            }

            minioClient = null;
            if (enableHTTPS)
                minioClient = new Minio.MinioClient(endPoint, accessKey, secretKey).WithSSL();
            else
                minioClient = new Minio.MinioClient(endPoint, accessKey, secretKey);

            bucketName = GetRandomName();
            smallFileName = CreateFile(1 * UNIT_MB);
            bigFileName = CreateFile(6 * UNIT_MB);
            objectName = GetRandomName();
            minioClient.SetAppInfo("app-name", "app-version");

            Cases.MakeBucket.Run(minioClient, bucketName).Wait();
        }

        [GlobalCleanup]
        public void Clean()
        {
            //Cases.RemoveBucket.Run(minioClient, bucketName).Wait();

            File.Delete(smallFileName);
            File.Delete(bigFileName);
        }

        [Benchmark]
        public void PutObjectAsync_small()
        {
            Cases.PutObject.Run(minioClient, bucketName, objectName, smallFileName).Wait();
        }

        [Benchmark]
        public void PutObjectAsyncFast_small()
        {
            Cases.PutObjectWithRealStream.Run(minioClient, bucketName, objectName, smallFileName).Wait();
        }

        [Benchmark]
        public void PutObjectAsync_big()
        {
            Cases.PutObject.Run(minioClient, bucketName, objectName, bigFileName).Wait();
        }

        [Benchmark]
        public void PutObjectAsyncFast_big()
        {
            Cases.PutObjectWithRealStream.Run(minioClient, bucketName, objectName, bigFileName).Wait();
        }
    }
}
