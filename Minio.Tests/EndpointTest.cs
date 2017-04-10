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
    public class EndpointTest
    {
        [TestMethod]
        public void TestGetEndpointURL()
        {
            Minio.RequestUtil.getEndpointURL("s3.amazonaws.com", true);
            object[] parameterValuesArray =
                        {
                          new Object[]{ "s3.amazonaws.com",true,"testbucket",null,false },
                          new object[] {"testbucket.s3.amazonaws.com", true}

                        };
            object[] parameterValuesArray1 =
                       {
                           "s3.amazonaws.com",true,"testbucket","testobject",false

                        };

            object[][] testCases =
            {
                new Object[] {
                          new Object[]{ "s3.cn-north-1.amazonaws.com.cn", true},
                          new Object[] { "https://s3.cn-north-1.amazonaws.com.cn", null,true}
                },
               new Object[] {
                            new Object[]{ "s3.amazonaws.com:443",true },
                            new Object[] {"https://s3.amazonaws.com:443",null, true}
                },
               new Object[] {
                          new Object[]{ "s3.amazonaws.com",true },
                          new Object[] {"https://s3.amazonaws.com",null, true}
                },


                new Object[] {
                            new Object[]{ "s3.amazonaws.com",false },
                            new Object[] {"http://s3.amazonaws.com",null, true}
                },

                 new Object[] {
                          new Object[]{ "192.168.1.1:9000", false},
                          new object[] { "http://192.168.1.1:9000", null,true}
                },
                 new Object[] {
                          new Object[]{ "192.168.1.1:9000", true},
                          new object[] { "https://192.168.1.1:9000", null,true}
                },
                 new Object[] {
                          new Object[]{ "13333.123123.-", true},
                          new object[] { "",new InvalidEndpointException("Endpoint: 13333.123123.- does not follow ip address or domain name standards."),false}
                },

                     new Object[] {
                          new Object[]{ "s3.aamzza.-", true},
                          new object[] { "",new InvalidEndpointException("Endpoint: s3.aamzza.- does not follow ip address or domain name standards."),false}
                },
                         new Object[] {
                          new Object[]{ "", true},
                          new object[] { "",new InvalidEndpointException("Endpoint:  does not follow ip address or domain name standards."),false}
                },
            };
            for (int i = 0; i < testCases.Length; i++)
            {
                Object[] testdata = testCases[i];
                Object[] testCase = (Object[])testdata[0];
                Object[] expectedValues = (Object[])testdata[1];
                try
                {
                    Uri endPointURL = RequestUtil.getEndpointURL((string)testCase[0], (bool)testCase[1]);
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
            Dictionary<string, bool> testIPDict = new Dictionary<string, bool>
            {
                {"192.168.1",false },
                {"192.168.1.1", true},
                {"192.168.1.1.1", false},
                {"-192.168.1.1", false},
                {"260.192.1.1", false},
            };
            foreach (KeyValuePair<string,bool> testCase in testIPDict)
            {
                Assert.AreEqual(s3utils.IsValidIP(testCase.Key), testCase.Value);
            }
        }

        [TestMethod]
        public void TestIfDomainIsValid()
        {
            Dictionary<string, bool> testDomainDict = new Dictionary<string, bool>
            {
                {"%$$$", false},
                {"s3.amazonaws.com", true},
                {"s3.cn-north-1.amazonaws.com.cn", true},
                {"s3.amazonaws.com_", false},
                {"s3.amz.test.com", true},
                {"s3.%%", false},
                {"localhost", true},
                {"-localhost", false},
                {"", false},
                {"\n \t", false},
                {"   ", false},
            };
            foreach (KeyValuePair<string, bool> testCase in testDomainDict)
            {
                Assert.AreEqual(s3utils.IsValidDomain(testCase.Key), testCase.Value);
            }
        }

        [TestMethod]
        public void TestIsAmazonEndpoint()
        {
            Dictionary<string, bool> testAmazonDict = new Dictionary<string, bool>
            {
                {"192.168.1.1", false},
                {"storage.googleapis.com", false},
                {"s3.amazonaws.com", true},
                {"amazons3.amazonaws.com", false},
                {"-192.168.1.1", false},
                {"260.192.1.1", false},
		        {"https://s3.amazonaws.com", false},
                {"s3.cn-north-1.amazonaws.com.cn", true},
            };
            foreach (KeyValuePair<string, bool> testCase in testAmazonDict)
            {
                bool value = s3utils.IsAmazonEndPoint(testCase.Key);
                Assert.AreEqual(s3utils.IsAmazonEndPoint(testCase.Key), testCase.Value);
            }
        }

        [TestMethod]
        public void TestIsAmazonChinaEndpoint()
        {
            Dictionary<string, bool> testAmazonDict = new Dictionary<string, bool>
            {
                {"192.168.1.1", false},
                {"storage.googleapis.com", false},
                {"s3.amazonaws.com", false},
                {"amazons3.amazonaws.com", false},
                {"-192.168.1.1", false},
                {"260.192.1.1", false},
                {"https://s3.amazonaws.com", false},
                {"s3.cn-north-1.amazonaws.com.cn", true},
            };
            foreach (KeyValuePair<string, bool> testCase in testAmazonDict)
            {
                bool value = s3utils.IsAmazonChinaEndPoint(testCase.Key);
                Assert.AreEqual(s3utils.IsAmazonChinaEndPoint(testCase.Key), testCase.Value);
            }
        }
    }
  
}
