using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio.CredentialProviders;
using Minio.Helpers;
using Minio.Implementation;
using Minio.Model;
using Minio.UnitTests.Services;
using Xunit;

namespace Minio.UnitTests.Tests;

public class V4RequestAuthenticatorTests
{
    [Fact]
    public async Task ValidateAuthentication()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost:9000/test?delimiter=%2F&encoding-type=url&list-type=2&prefix=");
        req.Headers.Add("X-Amz-Content-Sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        req.Headers.Add("X-Amz-Date", "20240411T153713Z");

        await AuthenticateRequestAsync(req).ConfigureAwait(true);

        Assert.NotNull(req.Headers.Authorization!.Scheme);
        Assert.Equal("AWS4-HMAC-SHA256", req.Headers.Authorization.Scheme);
        Assert.Equal("Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature=fbc9b67904568217c4dcdd438483fa7ff914a793e532d215eecddae7f78bdfe8", req.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ValidateAuthenticationWithComplexQuery()
    {
        var query = new QueryParams();
        query.Add("ping", "10");
        query.Add("events", EventType.ObjectCreatedAll);
        query.Add("events", EventType.ObjectAccessedAll);
        query.Add("events", EventType.ObjectRemovedAll);

        using var req = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:9000/test{query}");
        req.Headers.Add("X-Amz-Content-Sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        req.Headers.Add("X-Amz-Date", "20240411T153713Z");

        await AuthenticateRequestAsync(req).ConfigureAwait(true);

        Assert.NotNull(req.Headers.Authorization!.Scheme);
        Assert.Equal("AWS4-HMAC-SHA256", req.Headers.Authorization.Scheme);
        Assert.Equal("Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature=3c80a4a86d5c03a9978ff5f125b40fc75ed2fd27bf7c8ef26dec185b6ea63f44", req.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ValidateAuthenticationWithEmptyQueryValue()
    {
        var query = new QueryParams();
        query.Add("tagging", string.Empty);

        using var req = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:9000/test{query}");
        req.Headers.Add("X-Amz-Content-Sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        req.Headers.Add("X-Amz-Date", "20240411T153713Z");

        await AuthenticateRequestAsync(req).ConfigureAwait(true);

        Assert.NotNull(req.Headers.Authorization!.Scheme);
        Assert.Equal("AWS4-HMAC-SHA256", req.Headers.Authorization.Scheme);
        Assert.Equal("Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature=71b660a420c73e3b080c890af76dcf86019e09fa0a73a3b79526c8bf6ca340b7", req.Headers.Authorization.Parameter);
    }

    private static async Task AuthenticateRequestAsync(HttpRequestMessage req)
    {
        var credentialsProvider = new StaticCredentialsProvider(Options.Create(new StaticCredentialsOptions
        {
            AccessKey = "minioadmin",
            SecretKey = "minioadmin",
        }));
        var timeProvider = new StaticTimeProvider("20240411T153713Z");
        var logger = NullLoggerFactory.Instance.CreateLogger<V4RequestAuthenticator>();
        var authenticator = new V4RequestAuthenticator(credentialsProvider, timeProvider, logger);
        await authenticator.AuthenticateAsync(req, "us-east-1", "s3", default).ConfigureAwait(true);
    }
}