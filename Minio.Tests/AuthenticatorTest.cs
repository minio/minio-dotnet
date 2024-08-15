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

[TestClass]
public class AuthenticatorTest
{
    [TestMethod]
    public void TestAnonymousInsecureRequestHeaders()
    {
        //test anonymous insecure request headers
        var authenticator = new V4Authenticator(false, "", "");
        Assert.IsTrue(authenticator.IsAnonymous);

        var request = new HttpRequestMessageBuilder(HttpMethod.Put, "http://localhost:9000/bucketname/objectname");
        request.AddJsonBody("[]");

        var authenticatorInsecure = new V4Authenticator(false, "a", "b");
        Assert.IsFalse(authenticatorInsecure.IsAnonymous);

        _ = authenticatorInsecure.Authenticate(request);
        Assert.IsTrue(HasPayloadHeader(request, "x-amz-content-sha256"));
    }

    [TestMethod]
    public void TestAnonymousSecureRequestHeaders()
    {
        //test anonymous secure request headers
        var authenticator = new V4Authenticator(true, "", "");
        Assert.IsTrue(authenticator.IsAnonymous);

        var request = new HttpRequestMessageBuilder(HttpMethod.Put, "http://localhost:9000/bucketname/objectname");
        request.AddJsonBody("[]");

        var authenticatorSecure = new V4Authenticator(true, "a", "b");
        Assert.IsFalse(authenticatorSecure.IsAnonymous);

        _ = authenticatorSecure.Authenticate(request);
        Assert.IsTrue(HasPayloadHeader(request, "x-amz-content-sha256"));
    }

    [TestMethod]
    public void TestSecureRequestHeaders()
    {
        // secure authenticated requests
        var authenticator = new V4Authenticator(true, "accesskey", "secretkey");
        Assert.IsTrue(authenticator.IsSecure);
        Assert.IsFalse(authenticator.IsAnonymous);

        var request = new HttpRequestMessageBuilder(HttpMethod.Put, "http://localhost:9000/bucketname/objectname");
        request.AddJsonBody("[]");
        _ = authenticator.Authenticate(request);
        Assert.IsTrue(HasPayloadHeader(request, "x-amz-content-sha256"));
        var match = GetHeaderKV(request, "x-amz-content-sha256");
        Assert.IsTrue(match?.Item2.Equals("UNSIGNED-PAYLOAD", StringComparison.Ordinal) == true);
    }

    [TestMethod]
    public void TestInsecureRequestHeaders()
    {
        // insecure authenticated requests
        var authenticator = new V4Authenticator(false, "accesskey", "secretkey");
        Assert.IsFalse(authenticator.IsSecure);
        Assert.IsFalse(authenticator.IsAnonymous);
        var request = new HttpRequestMessageBuilder(HttpMethod.Put, "http://localhost:9000/bucketname/objectname");
        request.AddJsonBody("[]");
        _ = authenticator.Authenticate(request);
        Assert.IsTrue(HasPayloadHeader(request, "x-amz-content-sha256"));
        Assert.IsFalse(HasPayloadHeader(request, "Content-Md5"));
    }

    // [TestMethod]
    // public void TestPresignedPostPolicy()
    // {
    //     DateTime requestDate = new DateTime(2020, 05, 01, 15, 45, 33, DateTimeKind.Utc);
    //     var authenticator = new V4Authenticator(false, "my-access-key", "secretkey");

    //     var policy = new PostPolicy();
    //     policy.SetBucket("bucket-name");
    //     policy.SetKey("object-name");
    //     policy.SetAlgorithm("AWS4-HMAC-SHA256");
    //     policy.SetCredential(authenticator.GetCredentialString(requestDate, region));
    //     policy.SetDate(requestDate);
    //     policy.SetSessionToken("");

    //     var region = "mock-location";
    //     string policyBase64 = policy.Base64();
    //     string signature = authenticator.PresignPostSignature(region, requestDate, policyBase64);

    //     policy.SetPolicy(policyBase64);
    //     policy.SetSignature(signature);

    //     var headers = new Dictionary<string, string>
    //     {
    //         {"bucket", "bucket-name"},
    //         {"key", "object-name"},
    //         {"x-amz-algorithm", "AWS4-HMAC-SHA256"},
    //         {"x-amz-credential", "my-access-key/20200501/mock-location/s3/aws4_request"},
    //         {"x-amz-date", "20200501T154533Z"},
    //         {"policy", "eyJleHBpcmF0aW9uIjoiMDAwMS0wMS0wMVQwMDowMDowMC4wMDBaIiwiY29uZGl0aW9ucyI6W1siZXEiLCIkYnVja2V0IiwiYnVja2V0LW5hbWUiXSxbImVxIiwiJGtleSIsIm9iamVjdC1uYW1lIl0sWyJlcSIsIiR4LWFtei1hbGdvcml0aG0iLCJBV1M0LUhNQUMtU0hBMjU2Il0sWyJlcSIsIiR4LWFtei1jcmVkZW50aWFsIiwibXktYWNjZXNzLWtleS8yMDIwMDUwMS9tb2NrLWxvY2F0aW9uL3MzL2F3czRfcmVxdWVzdCJdLFsiZXEiLCIkeC1hbXotZGF0ZSIsIjIwMjAwNTAxVDE1NDUzM1oiXV19"},
    //         {"x-amz-signature", "ec6dad862909ee905cfab3ef87ede0e666eebd6b8f00d28e5df104a8fcbd4027"},
    //     };

    //     CollectionAssert.AreEquivalent(headers, policy.GetFormData());
    // }

    [TestMethod]
    public void GetPresignCanonicalRequestTest()
    {
        var authenticator = new V4Authenticator(false, "my-access-key", "my-secret-key");

        var request = new Uri(
            "https://localhost:9000/bucket/object-name?X-Amz-Expires=43200&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20240815%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20240815T153925Z&X-Amz-SignedHeaders=content-language%3Bhost%3Bx-special");
        var headersToSign = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "X-Special".ToLowerInvariant(), "special" }, { "Content-Language".ToLowerInvariant(), "en" },
            { "host","localhost:9000"}
        };

        var canonicalQueryString = "X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20240815%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20240815T153925Z&X-Amz-Expires=43200&X-Amz-SignedHeaders=content-language%3Bhost%3Bx-special";

        var canonicalRequest = authenticator.GetPresignCanonicalRequest(HttpMethod.Put, request, headersToSign, canonicalQueryString);
        Assert.AreEqual("PUT\n/bucket/object-name\nX-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20240815%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20240815T153925Z&X-Amz-Expires=43200&X-Amz-SignedHeaders=content-language%3Bhost%3Bx-special\ncontent-language:en\nhost:localhost:9000\nx-special:special\n\ncontent-language;host;x-special\nUNSIGNED-PAYLOAD",
            canonicalRequest);
    }

    [TestMethod]
    public void GetPresignCanonicalRequestWithParametersTest()
    {
        var authenticator = new V4Authenticator(false, "my-access-key", "my-secret-key");

        var request = new Uri(
            "https://localhost:9000/bucket/object-name?X-Amz-Expires=43200&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20240815%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20240815T153925Z&X-Amz-SignedHeaders=content-language%3Bhost%3Bx-special");
        var headersToSign = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "X-Special".ToLowerInvariant(), "special" }, { "Content-Language".ToLowerInvariant(), "en" },{ "host","localhost:9000"}
        };

        var canonicalQueryString = "X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20240815%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20240815T153925Z&X-Amz-Expires=43200&X-Amz-SignedHeaders=content-language%3Bhost%3Bx-special";

        var canonicalRequest = authenticator.GetPresignCanonicalRequest(HttpMethod.Put, request, headersToSign, canonicalQueryString);
        Assert.AreEqual("PUT\n/bucket/object-name\nX-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=my-access-key%2F20240815%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20240815T153925Z&X-Amz-Expires=43200&X-Amz-SignedHeaders=content-language%3Bhost%3Bx-special\ncontent-language:en\nhost:localhost:9000\nx-special:special\n\ncontent-language;host;x-special\nUNSIGNED-PAYLOAD",
            canonicalRequest);
    }

    private Tuple<string, string> GetHeaderKV(HttpRequestMessageBuilder request, string headername)
    {
        var key = request.HeaderParameters.Keys.FirstOrDefault(o =>
            string.Equals(o, headername, StringComparison.OrdinalIgnoreCase));
        return key is not null ? Tuple.Create(key, request.HeaderParameters[key]) : null;
    }

    private bool HasPayloadHeader(HttpRequestMessageBuilder request, string headerName)
    {
        var match = GetHeaderKV(request, headerName);
        return match is not null;
    }
}
