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

namespace Minio.Tests;

/// <summary>
///     HttpRequestMessageBuilder Tests
/// </summary>
[TestClass]
public class HttpRequestMessageBuilderTests
{
    [TestMethod]
    public void RequestGetter_NoQueryParametersAdded_ReturnsUrisQueryParameters()
    {
        var requestBuilder = new HttpRequestMessageBuilder(HttpMethod.Get,
            "http://localhost:9000/bucketname/objectname?query1=one&query2=two");

        Assert.AreEqual("?query1=one&query2=two", requestBuilder.Request.RequestUri.Query);
    }

    [TestMethod]
    public void RequestGetter_AditionalQueryParameters_ReturnsAllQueryParameters()
    {
        var requestBuilder = new HttpRequestMessageBuilder(HttpMethod.Get,
            "http://localhost:9000/bucketname/objectname?query1=one&query2=two");
        requestBuilder.QueryParameters.Add("query3", "three");

        Assert.AreEqual("?query1=one&query2=two&query3=three", requestBuilder.Request.RequestUri.Query);
    }

    [TestMethod]
    public void RequestGetter_DuplicatedQueryParameters_ReturnsAllQueryParameters()
    {
        var requestBuilder = new HttpRequestMessageBuilder(HttpMethod.Get,
            "http://localhost:9000/bucketname/objectname?query1=one&query2=two");
        requestBuilder.QueryParameters.Add("query2", "new_value");

        Assert.AreEqual("?query1=one&query2=two&query2=new_value", requestBuilder.Request.RequestUri.Query);
    }
}
