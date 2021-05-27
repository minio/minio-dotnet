using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Exceptions;

using System.IO;
using System.Text;
using System.Net.Http;

using Moq;


namespace Minio.Tests
{
    [TestClass]
    public class OperationsTest
    {
        private DateTime _requestDate = new DateTime(2020, 05, 01, 15, 45, 33, DateTimeKind.Utc);

        private static bool IsLocationRequest(HttpRequestMessageBuilder httpRequest)
        {
            var resource = httpRequest.RequestUri.LocalPath;
            return resource.Contains("?") == false &&
                   httpRequest.QueryParameters.ContainsKey("location");
        }


        private static Mock<HttpClient> MockHttpClient(Uri initialBaseUrl)
        {  
            string location = "mock-location";
            Uri baseUrl = initialBaseUrl;    // captured state
            
            var httpClient = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            httpClient.SetupSet(hc =>  .BaseAddress = It.IsAny<Uri>()).Callback((Uri value) => baseUrl = value);
            httpClient.SetupGet(hc => hc.BaseAddress).Returns(() => baseUrl);
            httpClient.Setup(hc =>
                    hc.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((HttpResponseMessage rs, CancellationToken ct) => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("<?xml version=\"1.0\" encoding=\"UTF-8\"?><GetBucketLocationOutput><LocationConstraint>{location}</LocationConstraint></GetBucketLocationOutput>")
                });
            httpClient.Setup(hc => (It.IsAny<HttpRequestMessageBuilder>()))
                .Returns((HttpRequestMessageBuilder rr) => new HttpClient().B // .UseUrlEncoder(HttpUtility.UrlEncode).BuildUri(rr));
            httpClient.SetupProperty(hc => hc.Authenticator);
            return httpClient;
            // // todo how to test this with mock client.   // RestSharp
            // var resour ce = httpRequest.RequestUri.LocalPath;
            // return resour ce.Contains("?") == false &&
            //        httpRequest.QueryParameters.ContainsKey("location");
        }

        private async Task<bool> ObjectExistsAsync(MinioClient client, string bucket, string objectName)
        {
            try
            {
                await client.GetObjectAsync("bucket", objectName, stream => { });

                return true;
            }
            catch (ObjectNotFoundException)
            {
                return false;
            }
        }

        [TestMethod]
        public async Task PresignedGetObject()
        {
            // todo how to test this with mock client.
            // var client = new MinioClient(endpoint: "play.min.io", "Q3AM3UQ867SPQQA43P2F", "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG");

            // var bucket = "bucket";
            // var objectName = "object-name";

            // if (!await client.BucketExistsAsync(bucket))
            // {
            //     await client.MakeBucketAsync(bucket);
            // }

            var client = new MinioClient()
                                    .WithCredentials("my-access-key", "my-secret-key")
                                    .WithEndpoint("localhost", 9001)
                                    .Build();

            // Mock<HttpClient> httpClient = MockClient(client.uri);
            var mockFactory = new Mock<IHttpClientFactory>();
            
            PresignedGetObjectArgs presignedGetArgs = new PresignedGetObjectArgs()
                                                                    .WithBucket("bucket")
                                                                    .WithObject("object-name")
                                                                    .WithExpiry(3600)
                                                                    .WithRequestDate(_requestDate);
            var signedUrl = await client.PresignedGetObjectAsync(presignedGetArgs);
            // var client = new MinioClient(endpoint: "localhost:9001", "my-access-key", "my-secret-key");   // RestSharp
            // var signedUrl = await client.PresignedGetObjectAsync("bucket", "object-name", 3600, null, _requestDate);

            Assert.AreEqual(
                "http://localhost:9001/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&X-Amz-Signature=6dfd01cd302737c58c80e9ca1ec4abaa34e85d9ab3156d5704ea7b88bc9bdd37",
                signedUrl);
            if (!await this.ObjectExistsAsync(client, bucket, objectName))
            {
                var helloData = Encoding.UTF8.GetBytes("hello world");
                var helloStream = new MemoryStream();
                helloStream.Write(helloData);
                helloStream.Seek(0, SeekOrigin.Begin);
                await client.PutObjectAsync(bucket, objectName, helloStream, helloData.Length);
            }
            var signedUrl = await client.PresignedGetObjectAsync(bucket, objectName, 3600, null, _requestDate);

            Assert.AreEqual(
                "http://play.min.io/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=Q3AM3UQ867SPQQA43P2F%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&X-Amz-Signature=d4202da690618f77142d6f0557c97839f0773b2c718082e745cd9b199aa6b28f",
                signedUrl);
        }

        [TestMethod]
        public async Task PresignedGetObjectWithHeaders()
        {
        //     var client = new MinioClient()
        //                             .WithEndpoint("localhost", 9001)
        //                             .WithCredentials("my-access-key", "my-secret-key")
        //                             .Build();

        //     Mock<HttpClient> httpClient = MockClient(client.BaseUrl);

        //     Dictionary<string, string> reqParams = new Dictionary<string, string>
        //     {
        //         {"Response-Content-Disposition", "attachment; filename=\"filename.jpg\""},
        //     };
        //     PresignedGetObjectArgs presignedGetArgs = new PresignedGetObjectArgs()
        //                                                             .WithBucket("bucket")
        //                                                             .WithObject("object-name")
        //                                                             .WithExpiry(3600)
        //                                                             .WithRequestDate(_requestDate)
        //                                                             .WithHeaders(reqParams);
        //     var signedUrl = await client.PresignedGetObjectAsync(presignedGetArgs);
        //     Assert.IsTrue(signedUrl.Equals("http://localhost:9001/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&response-content-disposition=attachment%3B%20filename%3D%22filename.jpg%22&X-Amz-Signature=33e766c15afc36558d37e995b5997d16def98a0aa622f0b518811eafcb60b910"));
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