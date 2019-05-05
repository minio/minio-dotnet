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
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Configuration;
using Minio.Exceptions;
using Minio;
using Minio.Helper;
namespace Minio.Tests
{
    [TestClass]
    public class TestRegion
    {
        [TestMethod]
        public void TestGetRegion()
        {
            Dictionary<string, string> endpoint2Region = new Dictionary<string, string>
            {
                {"s3.us-east-2.amazonaws.com", "us-east-2"},
                {"s3.amazonaws.com", ""},
                {"testbucket.s3-ca-central-1.amazonaws.com", "ca-central-1"},
                {"mybucket-s3-us-east-2.amazonaws.com", "us-east-2"},
                {"s3.us-west-1.amazonaws.com", "us-west-1"},
                {"mybucket-s3-us-west-1.amazonaws.com", "us-west-1"},
                {"wests3iss.s3-us-west-1.amazonaws.com", "us-west-1"},
            };
            foreach (KeyValuePair<string, string> testCase in endpoint2Region)
            {
                Assert.AreEqual(Regions.GetRegionFromEndpoint(testCase.Key), testCase.Value);
            }
        }
    }
   
}
