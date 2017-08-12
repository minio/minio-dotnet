/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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

namespace Minio.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Rest;
    using RestSharp.Portable;
    using Xunit;

    public class AuthenticatorTest
    {
        private Tuple<string, object> GetHeaderKV(IRestRequest request, string headername)
        {
            var headers = request.Parameters.Where(p => p.Type.Equals(ParameterType.HttpHeader)).ToList();
            var headerKeys = new List<string>();
            foreach (var header in headers)
            {
                var headerName = header.Name.ToLower();
                if (headerName.Contains(headername.ToLower()))
                {
                    return new Tuple<string, object>(headerName, header.Value);
                }
            }
            return null;
        }

        private bool hasPayloadHeader(IRestRequest request, string headerName)
        {
            var match = this.GetHeaderKV(request, headerName);
            return match != null;
        }

        [Fact]
        public void TestAnonymousInsecureRequestHeaders()
        {
            //test anonymous insecure request headers
            var minio = (DefaultMinioClient)MinioClient.Create("localhost:9000", null, null);
            var authenticator = new V4Authenticator(minio);
            Assert.True(authenticator.IsAnonymous);

            IRestClient restClient = new RestClient("http://localhost:9000");
            IRestRequest request = new RestRequest("bucketname/objectname", Method.PUT);
            request.AddBody("body of request");
            authenticator.PreAuthenticate(restClient, request, null);
            Assert.False(this.hasPayloadHeader(request, "x-amz-content-sha256"));
            Assert.True(this.hasPayloadHeader(request, "Content-MD5"));
        }

        [Fact]
        public async Task TestAnonymousSecureRequestHeaders()
        {
            //test anonymous secure request headers
            var minio = (DefaultMinioClient)MinioClient.Create("localhost:9000", null, null).WithSsl();
            var authenticator = new V4Authenticator(minio);
            Assert.True(authenticator.IsAnonymous);

            IRestClient restClient = new RestClient("http://localhost:9000");
            IRestRequest request = new RestRequest("bucketname/objectname", Method.PUT);
            request.AddBody("body of request");
            await authenticator.PreAuthenticate(restClient, request, null);
            Assert.False(this.hasPayloadHeader(request, "x-amz-content-sha256"));
            Assert.True(this.hasPayloadHeader(request, "Content-MD5"));
        }

        [Fact]
        public async Task TestInsecureRequestHeaders()
        {
            // insecure authenticated requests
            var minio = (DefaultMinioClient)MinioClient.Create("localhost:9000", "accesskey", "secretkey");
            var authenticator = new V4Authenticator(minio);
            Assert.False(authenticator.IsSecure);
            Assert.False(authenticator.IsAnonymous);
            IRestClient restClient = new RestClient("http://localhost:9000");
            IRestRequest request = new RestRequest("bucketname/objectname", Method.PUT);
            request.AddBody("body of request");
            await authenticator.PreAuthenticate(restClient, request, null);
            Assert.True(this.hasPayloadHeader(request, "x-amz-content-sha256"));
            Assert.False(this.hasPayloadHeader(request, "Content-Md5"));
        }

        [Fact]
        public async Task TestSecureRequestHeaders()
        {
            // secure authenticated requests
            var minio = (DefaultMinioClient)MinioClient.Create("localhost:9000", "accesskey", "secretkey").WithSsl();
            var authenticator = new V4Authenticator(minio);
            Assert.True(authenticator.IsSecure);
            Assert.False(authenticator.IsAnonymous);

            IRestClient restClient = new RestClient("http://localhost:9000");
            IRestRequest request = new RestRequest("bucketname/objectname", Method.PUT);
            request.AddBody("body of request");
            await authenticator.PreAuthenticate(restClient, request, null);
            Assert.True(this.hasPayloadHeader(request, "x-amz-content-sha256"));
            Assert.True(this.hasPayloadHeader(request, "Content-Md5"));
            var match = this.GetHeaderKV(request, "x-amz-content-sha256");
            Assert.True(match != null && ((string)match.Item2).Equals("UNSIGNED-PAYLOAD"));
        }
    }
}