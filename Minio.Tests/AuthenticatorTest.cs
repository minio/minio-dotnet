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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using Minio.DataModel.Policy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using static Minio.Enum;
using RestSharp;
using System.Linq;

namespace Minio.Tests
{
    [TestClass]
    public class AuthenticatorTest
    {
        [TestMethod]
        public void TestAnonymousInsecureRequestHeaders()
        {
            //test anonymous insecure request headers
            V4Authenticator authenticator = new V4Authenticator(false, null, null);
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
            V4Authenticator authenticator = new V4Authenticator(true, null, null);
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
            V4Authenticator authenticator = new V4Authenticator(true, "accesskey", "secretkey");
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
        public void TestRequestWithHeaderParameters()
        {
            // secure authenticated requests
            V4Authenticator authenticator = new V4Authenticator(true, "accesskey", "secretkey");
            Assert.IsTrue(authenticator.isSecure);
            Assert.IsFalse(authenticator.isAnonymous);

            IRestClient restClient = new RestClient("http://localhost:9000");
            IRestRequest request = new RestRequest("bucketname/objectname", RestSharp.Method.PUT);
            request.AddHeader("response-content-disposition", "inline;filenameMyDocument.txt;");
            request.AddHeader("response-content-type", "application/json");

            request.AddBody("body of request");
            authenticator.Authenticate(restClient, request);
            var presignedUrl = authenticator.PresignURL(restClient, request, 5000);

            Assert.IsTrue(presignedUrl.Contains("&response-content-disposition"));
            Assert.IsTrue(presignedUrl.Contains("&response-content-type"));

            Assert.IsTrue(hasPayloadHeader(request, "x-amz-content-sha256"));
            Assert.IsTrue(hasPayloadHeader(request, "Content-Md5"));
            Assert.IsTrue(hasPayloadHeader(request, "response-content-disposition"));
            Tuple<string, object> match = GetHeaderKV(request, "x-amz-content-sha256");
            Assert.IsTrue(match != null && ((string)match.Item2).Equals("UNSIGNED-PAYLOAD"));
        }

        [TestMethod]
        public void TestInsecureRequestHeaders()
        {
            // insecure authenticated requests
            V4Authenticator authenticator = new V4Authenticator(false, "accesskey", "secretkey");
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