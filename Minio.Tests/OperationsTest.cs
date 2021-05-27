using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Exceptions;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

using Moq;


namespace Minio.Tests
{
    [TestClass]
    public class OperationsTest
    {
        private DateTime _requestDate = new DateTime(2020, 05, 01, 15, 45, 33, DateTimeKind.Utc);

        private static bool IsLocationRequest(HttpRequestMessageBuilder httpRequest)
        {
            // todo how to test this with mock client.
            var resource = httpRequest.RequestUri.LocalPath;
            return resource.Contains("?") == false &&
                   httpRequest.QueryParameters.ContainsKey("location");
        }


        private async Task<bool> ObjectExistsAsync(MinioClient client, string bucket, string objectName)
        {  
            try
            {
                await client.GetObjectAsync("bucket", objectName, stream => { });

                return true;
            }
            catch (ObjectNotFoundException e)
            {
                return false;
            }
        }

        [TestMethod]
        public async Task PresignedGetObject()
        {
            // todo how to test this with mock client.
            var client = new MinioClient(endpoint: "play.min.io", "Q3AM3UQ867SPQQA43P2F", "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG");

            var bucket = "bucket";
            var objectName = "object-name";

            if (!await client.BucketExistsAsync(bucket))
            {
                await client.MakeBucketAsync(bucket);
            }

            if (!await this.ObjectExistsAsync(client, bucket, objectName))
            {
                var helloData = Encoding.UTF8.GetBytes("hello world");
                var helloStream = new MemoryStream();
                helloStream.Write(helloData);
                helloStream.Seek(0, SeekOrigin.Begin);
                await client.PutObjectAsync(bucket, objectName, helloStream, helloData.Length);
            }

            PresignedGetObjectArgs presignedGetArgs = new PresignedGetObjectArgs()
                                                                    .WithBucket("bucket")
                                                                    .WithObject("object-name")
                                                                    .WithExpiry(3600)
                                                                    .WithRequestDate(_requestDate);
            var signedUrl = await client.PresignedGetObjectAsync(bucket, objectName, 3600, null, _requestDate);

            Assert.AreEqual(
                "http://play.min.io/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=Q3AM3UQ867SPQQA43P2F%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&X-Amz-Signature=d4202da690618f77142d6f0557c97839f0773b2c718082e745cd9b199aa6b28f",
                signedUrl);
        }

        [TestMethod]
        public async Task PresignedGetObjectWithHeaders()
        {
            // todo how to test this with mock client.
            var client = new MinioClient(endpoint: "play.min.io", "Q3AM3UQ867SPQQA43P2F", "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG");
            var bucket = "bucket";
            var objectName = "object-name";

            Dictionary<string, string> reqParams = new Dictionary<string, string>
            {
                {"Response-Content-Disposition", "attachment; filename=\"filename.jpg\""},
            };

            if (!await client.BucketExistsAsync(bucket))
            {
                await client.MakeBucketAsync(bucket);
            }

            if (!await this.ObjectExistsAsync(client, bucket, objectName))
            {
                var helloData = Encoding.UTF8.GetBytes("hello world");
                var helloStream = new MemoryStream();
                helloStream.Write(helloData);
                await client.PutObjectAsync(bucket, objectName, helloStream, helloData.Length);
            }

            var signedUrl = await client.PresignedGetObjectAsync(bucket, objectName, 3600, reqParams, _requestDate);

            Assert.AreEqual(
                "http://play.min.io/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=Q3AM3UQ867SPQQA43P2F%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&response-content-disposition=attachment%3B%20filename%3D%22filename.jpg%22&X-Amz-Signature=de66f04dd4ac35838b9e83d669f7b5a70b452c6468e2b4a9e9c29f42e7fa102d",
                signedUrl);
        }
    }
}