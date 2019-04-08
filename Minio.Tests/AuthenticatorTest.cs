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
using System;
using System.Collections.Generic;
using System.Linq;
using RestSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        private Tuple<string,object> GetHeaderKV(IRestRequest request, string headername)
        {
            var headers = request.Parameters.Where(p => p.Type.Equals(ParameterType.HttpHeader)).ToList();
            List<string> headerKeys = new List<string>();
            foreach (Parameter header in headers)
            {
                string headerName = header.Name.ToLower();
                if (headerName.Contains(headername.ToLower()))
                {
                    return new Tuple<string, object>(headerName, header.Value);
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