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
using Minio.Exceptions;
using Minio.Helper;

namespace Minio.Tests;

[TestClass]
public class EndpointTest
{
    [TestMethod]
    public void TestGetEndpointURL()
    {
        _ = RequestUtil.GetEndpointURL("s3.amazonaws.com", true);
        object[] parameterValuesArray =
        {
            new object[] { "s3.amazonaws.com", true, "testbucket", null, false },
            new object[] { "testbucket.s3.amazonaws.com", true }
        };

        object[] parameterValuesArray1 = { "s3.amazonaws.com", true, "testbucket", "testobject", false };

        object[][] testCases =
        {
            new object[]
            {
                new object[] { "s3.cn-north-1.amazonaws.com.cn", true },
                new object[] { "https://s3.cn-north-1.amazonaws.com.cn", null, true }
            },
            new object[]
            {
                new object[] { "s3.amazonaws.com:443", true },
                new object[] { "https://s3.amazonaws.com:443", null, true }
            },
            new object[]
            {
                new object[] { "s3.amazonaws.com", true }, new object[] { "https://s3.amazonaws.com", null, true }
            },
            new object[]
            {
                new object[] { "s3.amazonaws.com", false }, new object[] { "http://s3.amazonaws.com", null, true }
            },
            new object[]
            {
                new object[] { "192.168.1.1:9000", false }, new object[] { "http://192.168.1.1:9000", null, true }
            },
            new object[]
            {
                new object[] { "192.168.1.1:9000", true }, new object[] { "https://192.168.1.1:9000", null, true }
            },
            new object[]
            {
                new object[] { "13333.123123.-", true }, new object[]
                {
                    "", new InvalidEndpointException(
                        "Endpoint: 13333.123123.- does not follow ip address or domain name standards."),
                    false
                }
            },
            new object[]
            {
                new object[] { "s3.aamzza.-", true }, new object[]
                {
                    "", new InvalidEndpointException(
                        "Endpoint: s3.aamzza.- does not follow ip address or domain name standards."),
                    false
                }
            },
            new object[]
            {
                new object[] { "", true }, new object[]
                {
                    "", new InvalidEndpointException(
                        "Endpoint:  does not follow ip address or domain name standards."),
                    false
                }
            }
        };

        for (var i = 0; i < testCases.Length; i++)
        {
            var testdata = testCases[i];
            var testCase = (object[])testdata[0];
            var expectedValues = (object[])testdata[1];
            try
            {
                var endPointURL = RequestUtil.GetEndpointURL((string)testCase[0], (bool)testCase[1]);
                Assert.AreEqual(endPointURL.OriginalString, expectedValues[0]);
            }
            catch (InvalidEndpointException ex)
            {
                Assert.AreEqual(ex.Message, ((InvalidEndpointException)expectedValues[1]).Message);
            }
        }
    }

    [TestMethod]
    public void TestIfIPIsValid()
    {
        var testIPDict = new Dictionary<string, bool>
            (StringComparer.Ordinal)
            {
                { "192.168.1", false },
                { "192.168.1.1", true },
                { "192.168.1.1.1", false },
                { "-192.168.1.1", false },
                { "260.192.1.1", false }
            };

        foreach (var testCase in testIPDict) Assert.AreEqual(S3utils.IsValidIP(testCase.Key), testCase.Value);
    }

    [TestMethod]
    public void TestIfDomainIsValid()
    {
        var testDomainDict = new Dictionary<string, bool>
            (StringComparer.Ordinal)
            {
                { "%$$$", false },
                { "s3.amazonaws.com", true },
                { "s3.cn-north-1.amazonaws.com.cn", true },
                { "s3.amazonaws.com_", false },
                { "s3.amz.test.com", true },
                { "s3.%%", false },
                { "localhost", true },
                { "-localhost", false },
                { "", false },
                { "\n \t", false },
                { "   ", false }
            };

        foreach (var testCase in testDomainDict)
            Assert.AreEqual(RequestUtil.IsValidEndpoint(testCase.Key), testCase.Value);
    }

    [TestMethod]
    public void TestIsAmazonEndpoint()
    {
        var testAmazonDict = new Dictionary<string, bool>
            (StringComparer.Ordinal)
            {
                { "192.168.1.1", false },
                { "storage.googleapis.com", false },
                { "s3.amazonaws.com", true },
                { "amazons3.amazonaws.com", false },
                { "-192.168.1.1", false },
                { "260.192.1.1", false },
                { "https://s3.amazonaws.com", false },
                { "s3.cn-north-1.amazonaws.com.cn", true }
            };

        foreach (var testCase in testAmazonDict)
        {
            var value = S3utils.IsAmazonEndPoint(testCase.Key);
            Assert.AreEqual(S3utils.IsAmazonEndPoint(testCase.Key), testCase.Value);
        }
    }

    [TestMethod]
    public void TestIsAmazonChinaEndpoint()
    {
        var testAmazonDict = new Dictionary<string, bool>
            (StringComparer.Ordinal)
            {
                { "192.168.1.1", false },
                { "storage.googleapis.com", false },
                { "s3.amazonaws.com", false },
                { "amazons3.amazonaws.com", false },
                { "-192.168.1.1", false },
                { "260.192.1.1", false },
                { "https://s3.amazonaws.com", false },
                { "s3.cn-north-1.amazonaws.com.cn", true }
            };

        foreach (var testCase in testAmazonDict)
        {
            var value = S3utils.IsAmazonChinaEndPoint(testCase.Key);
            Assert.AreEqual(S3utils.IsAmazonChinaEndPoint(testCase.Key), testCase.Value);
        }
    }
}
