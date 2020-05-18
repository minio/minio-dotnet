/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Minio.DataModel;
using Moq;

namespace Minio.Tests
{
    [TestClass]
    public class AuthenticatorTest
    {
        [TestMethod]
        public void TestAnonymousInsecureRequestHeaders()
        {
            //test anonymous insecure request headers
            var authenticator = new V4Authenticator(false, null, null);
            Assert.IsTrue(authenticator.isAnonymous);
          
            IRestClient restClient = new RestClient("http://localhost:9000");
            IRestRequest request = new RestRequest("bucketname/objectname", RestSharp.Method.PUT);
            request.AddBody("body of request");
            authenticator.Authenticate(restClient, request);
            Assert.IsFalse(hasPayloadHeader(request, "x-amz-content-sha256"));
            Assert.IsTrue(hasPayloadHeader(request, "Content-MD5"));
        }

        [TestMethod]
        public void TestAnonymousSecureRequestHeaders()
        {
            //test anonymous secure request headers
            var authenticator = new V4Authenticator(true, null, null);
            Assert.IsTrue(authenticator.isAnonymous);

            IRestClient restClient = new RestClient("http://localhost:9000");
            IRestRequest request = new RestRequest("bucketname/objectname", RestSharp.Method.PUT);
            request.AddBody("body of request");
            authenticator.Authenticate(restClient, request);
            Assert.IsFalse(hasPayloadHeader(request, "x-amz-content-sha256"));
            Assert.IsTrue(hasPayloadHeader(request, "Content-MD5"));
        }

        [TestMethod]
        public void TestSecureRequestHeaders()
        {
            // secure authenticated requests
            var authenticator = new V4Authenticator(true, "accesskey", "secretkey");
            Assert.IsTrue(authenticator.isSecure);
            Assert.IsFalse(authenticator.isAnonymous);

            IRestClient restClient = new RestClient("http://localhost:9000");
            IRestRequest request = new RestRequest("bucketname/objectname", RestSharp.Method.PUT);
            request.AddBody("body of request");
            authenticator.Authenticate(restClient, request);
            Assert.IsTrue(hasPayloadHeader(request, "x-amz-content-sha256"));
            Assert.IsTrue(hasPayloadHeader(request, "Content-Md5"));
            Tuple<string, object> match = GetHeaderKV(request, "x-amz-content-sha256");
            Assert.IsTrue(match != null && ((string)match.Item2).Equals("UNSIGNED-PAYLOAD"));
        }

        [TestMethod]
        public void TestInsecureRequestHeaders()
        {
            // insecure authenticated requests
            var authenticator = new V4Authenticator(false, "accesskey", "secretkey");
            Assert.IsFalse(authenticator.isSecure);
            Assert.IsFalse(authenticator.isAnonymous);
            IRestClient restClient = new RestClient("http://localhost:9000");
            IRestRequest request = new RestRequest("bucketname/objectname", RestSharp.Method.PUT);
            request.AddBody("body of request");
            authenticator.Authenticate(restClient, request);
            Assert.IsTrue(hasPayloadHeader(request, "x-amz-content-sha256"));
            Assert.IsFalse(hasPayloadHeader(request, "Content-Md5"));
        }

        [TestMethod]
        public void TestPresignedPostPolicy()
        {
            DateTime requestDate = new DateTime(2020, 05, 01, 15, 45, 33, DateTimeKind.Utc);
            var authenticator = new V4Authenticator(false, "my-access-key", "secretkey");

            var policy = new PostPolicy();
            policy.SetBucket("bucket-name");
            policy.SetKey("object-name");

            policy.SetAlgorithm("AWS4-HMAC-SHA256");
            var region = "mock-location";
            policy.SetCredential(authenticator.GetCredentialString(requestDate, region));
            policy.SetDate(requestDate);
            policy.SetSessionToken(null);

            string policyBase64 = policy.Base64();
            string signature = authenticator.PresignPostSignature(region, requestDate, policyBase64);

            policy.SetPolicy(policyBase64);
            policy.SetSignature(signature);

            var headers = new Dictionary<string, string>
            {
                {"bucket", "bucket-name"},
                {"key", "object-name"},
                {"x-amz-algorithm", "AWS4-HMAC-SHA256"},
                {"x-amz-credential", "my-access-key/20200501/mock-location/s3/aws4_request"},
                {"x-amz-date", "20200501T154533Z"},
                {"policy", "eyJleHBpcmF0aW9uIjoiMDAwMS0wMS0wMVQwMDowMDowMC4wMDBaIiwiY29uZGl0aW9ucyI6W1siZXEiLCIkYnVja2V0IiwiYnVja2V0LW5hbWUiXSxbImVxIiwiJGtleSIsIm9iamVjdC1uYW1lIl0sWyJlcSIsIiR4LWFtei1hbGdvcml0aG0iLCJBV1M0LUhNQUMtU0hBMjU2Il0sWyJlcSIsIiR4LWFtei1jcmVkZW50aWFsIiwibXktYWNjZXNzLWtleS8yMDIwMDUwMS9tb2NrLWxvY2F0aW9uL3MzL2F3czRfcmVxdWVzdCJdLFsiZXEiLCIkeC1hbXotZGF0ZSIsIjIwMjAwNTAxVDE1NDUzM1oiXV19"},
                {"x-amz-signature", "ec6dad862909ee905cfab3ef87ede0e666eebd6b8f00d28e5df104a8fcbd4027"},
            };

            CollectionAssert.AreEquivalent(headers, policy.GetFormData());
        }

        [TestMethod]
        public void GetPresignCanonicalRequestTest()
        {
            var authenticator = new V4Authenticator(false, "my-access-key", "my-secret-key");

            var request = new Uri(
                "http://localhost:9001/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host");
            var headersToSign = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                {"X-Special".ToLowerInvariant(), "special"},
                {"Content-Language".ToLowerInvariant(), "en"},
            };

            var canonicalRequest = authenticator.GetPresignCanonicalRequest(Method.PUT, request, headersToSign);
            Assert.AreEqual(string.Join('\n', new[]
                {
                    "PUT",
                    "/bucket/object-name",
                    "X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&content-language=en&x-special=special",
                    "host:localhost:9001",
                    "",
                    "host",
                    "UNSIGNED-PAYLOAD"
                }),
                canonicalRequest);

        }

        private static Mock<IRestClient> MockRestClient(string baseUrl)
        {
            var restClient = new Mock<IRestClient>(MockBehavior.Strict);
            restClient.SetupProperty(rc => rc.BaseUrl, new Uri(baseUrl, UriKind.Absolute));
            restClient.Setup(rc => rc.BuildUri(It.IsAny<IRestRequest>()))
                .Returns((IRestRequest rr) => new RestClient(baseUrl).BuildUri(rr));
            return restClient;
        }

        [TestMethod]
        public void GetPresignCanonicalRequestWithParametersTest()
        {
            var authenticator = new V4Authenticator(false, "my-access-key", "my-secret-key");

            var request = new Uri(
                "http://localhost:9001/bucket/object-name?uploadId=upload-id&partNumber=1&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host");
            var headersToSign = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                {"X-Special".ToLowerInvariant(), "special"},
                {"Content-Language".ToLowerInvariant(), "en"},
            };

            var canonicalRequest = authenticator.GetPresignCanonicalRequest(Method.PUT, request, headersToSign);
            Assert.AreEqual(string.Join('\n', new[]
                {
                    "PUT",
                    "/bucket/object-name",
                    "X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&content-language=en&partNumber=1&uploadId=upload-id&x-special=special",
                    "host:localhost:9001",
                    "",
                    "host",
                    "UNSIGNED-PAYLOAD"
                }),
                canonicalRequest);
        }

        private Tuple<string, object> GetHeaderKV(IRestRequest request, string headername)
        {
            var headers = request.Parameters.Where(p => p.Type.Equals(ParameterType.HttpHeader)).ToList();
            List<string> headerKeys = new List<string>();
            foreach (Parameter header in headers)
            {
                string headerName = header.Name.ToLower();
                if (headerName.Contains(headername.ToLower()))
                {
                    return Tuple.Create(headerName, header.Value);
                }
            }
            return null;
        }

        private bool hasPayloadHeader(IRestRequest request, string headerName)
        {
            Tuple<string, object> match = GetHeaderKV(request, headerName);
            return match != null;
        }
    }
}
