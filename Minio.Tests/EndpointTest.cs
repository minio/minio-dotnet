using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Configuration;
using Minio.Exceptions;
using Minio;

namespace Minio.Tests
{
    [TestClass]
    public class UnitTest1
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

            object[][] Jray = new object[][] { parameterValuesArray, parameterValuesArray1 };
            object[][] jointList =
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
            for (int i = 0; i < jointList.Length; i++)
            {
                Object[] testdata = jointList[i];
                Object[] testCase = (Object[])testdata[0];
                Object[] expectedValues = (Object[]) testdata[1];
                try
                {
                    Uri endPointURL = RequestUtil.getEndpointURL((string)testCase[0], (bool)testCase[1]);
                    Assert.AreEqual(endPointURL.OriginalString, expectedValues[0]);
                    Console.Out.WriteLine(endPointURL.OriginalString);
                }
                catch(InvalidEndpointException ex)
                {
                    Assert.AreEqual(ex.Message, ((InvalidEndpointException)expectedValues[1]).Message);
                }
            }
        }
    }
}
