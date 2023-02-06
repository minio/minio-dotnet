using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;

namespace Minio.Tests;

[TestClass]
public class GenericResponseTest
{
    /// <summary>
    /// Test to ensure the constructor parameters are set on the response properties.
    /// </summary>
    /// <param name="expectedStatusCode">The expected status code</param>
    [DataTestMethod]
    [DataRow(HttpStatusCode.OK)]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public void TestConstructor(HttpStatusCode expectedStatusCode)
    {
        // arrange
        string expectedResponseContent = Guid.NewGuid().ToString(); // random string

        // act
        GenericResponse response = new GenericResponse(expectedStatusCode, expectedResponseContent);

        // assert
        Assert.AreEqual(expectedStatusCode, response.ResponseStatusCode);
        Assert.AreEqual(expectedResponseContent, response.ResponseContent);
    }
}
