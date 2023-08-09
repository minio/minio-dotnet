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
using Minio.Helper;

namespace Minio.Tests;

[TestClass]
public class RegionTest
{
    [DataTestMethod]
    [DataRow("s3.us-east-2.amazonaws.com", "us-east-2")]
    [DataRow("s3.amazonaws.com", "")]
    [DataRow("testbucket.s3-ca-central-1.amazonaws.com", "ca-central-1")]
    [DataRow("mybucket-s3-us-east-2.amazonaws.com", "us-east-2")]
    [DataRow("s3.us-west-1.amazonaws.com", "us-west-1")]
    [DataRow("mybucket-s3-us-west-1.amazonaws.com", "us-west-1")]
    [DataRow("wests3iss.s3-us-west-1.amazonaws.com", "us-west-1")]
    [DataRow("test.s3-s3.bucket.s3-us-west-1.amazonaws.com", "us-west-1")]
    [DataRow("test-s3.s3-bucket.s3-us-west-1.amazonaws.com", "us-west-1")]
    [DataRow("minio.mydomain.com", "")]
    [DataRow("minio.mydomain.com:9000", "")]
    [DataRow("subdomain.minio.mydomain.com:9000", "")]
    [DataRow("localhost:9000", "")]
    public void TestGetRegion(string endpoint, string expectedRegion)
    {
        Assert.AreEqual(expectedRegion, RegionHelper.GetRegionFromEndpoint(endpoint));
    }
}
