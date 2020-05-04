using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp;

namespace Minio.Tests
{
    [TestClass]
    public class OperationsTest
    {
        private DateTime _requestDate = new DateTime(2020, 05, 01, 15, 45, 33, DateTimeKind.Utc);

        private static bool IsLocationRequest(IRestRequest rr)
        {
            return rr.Resource.Contains("?") == false &&
                   rr.Parameters.Contains(new Parameter("location", "", ParameterType.QueryString));
        }

        private static Mock<IRestClient> MockRestClient(Uri initialBaseUrl)
        {
            string location = "mock-location";
            Uri baseUrl = initialBaseUrl; // captured state
            var restClient = new Mock<IRestClient>(MockBehavior.Strict);
            restClient.SetupSet(rc => rc.BaseUrl = It.IsAny<Uri>()).Callback((Uri value) => baseUrl = value);
            restClient.SetupGet(rc => rc.BaseUrl).Returns(() => baseUrl);
            restClient.Setup(rc =>
                    rc.ExecuteTaskAsync(It.Is((IRestRequest rr) => IsLocationRequest(rr)), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IRestRequest rr, CancellationToken ct) => new RestResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><GetBucketLocationOutput><LocationConstraint>{location}</LocationConstraint></GetBucketLocationOutput>"
                });
            restClient.Setup(rc => rc.BuildUri(It.IsAny<IRestRequest>()))
                .Returns((IRestRequest rr) => new RestClient {BaseUrl = baseUrl }.UseUrlEncoder(HttpUtility.UrlEncode).BuildUri(rr));
            restClient.SetupProperty(rc => rc.Authenticator);
            return restClient;
        }

        [TestMethod]
        public async Task PresignedGetObject()
        {
            var client = new MinioClient(endpoint:"localhost:9001", "my-access-key", "my-secret-key");

            Mock<IRestClient> restClient = MockRestClient(client.restClient.BaseUrl);
            client.restClient = restClient.Object;

            var signedUrl = await client.PresignedGetObjectAsync("bucket", "object-name", 3600, null, _requestDate);

            Assert.AreEqual(
                "http://localhost:9001/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&&X-Amz-Signature=6dfd01cd302737c58c80e9ca1ec4abaa34e85d9ab3156d5704ea7b88bc9bdd37",
                signedUrl);
        }

        [TestMethod]
        public async Task PresignedGetObjectWithHeaders()
        {
            var client = new MinioClient(endpoint:"localhost:9001", "my-access-key", "my-secret-key");

            Mock<IRestClient> restClient = MockRestClient(client.restClient.BaseUrl);
            client.restClient = restClient.Object;

            Dictionary<string, string> reqParams = new Dictionary<string, string>
            {
                {"Response-Content-Disposition", "attachment; filename=\"filename.jpg\""},
            };
            var signedUrl = await client.PresignedGetObjectAsync("bucket", "object-name", 3600, reqParams, _requestDate);

            Assert.AreEqual(
                "http://localhost:9001/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&response-content-disposition=attachment%3B%20filename%3D%22filename.jpg%22&X-Amz-Signature=33e766c15afc36558d37e995b5997d16def98a0aa622f0b518811eafcb60b910",
                signedUrl);
        }
    }
}