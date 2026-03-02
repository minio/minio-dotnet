using Minio.UnitTests.Helpers;
using Minio.UnitTests.Services;
using Xunit;

namespace Minio.UnitTests.Tests;

public class HttpAssertsTests
{
    [Fact]
    public async Task CheckHeaders()
    {
        var messageHandler = new MockHttpMessageHandler((req, resp) =>
        {
            req.AssertHeaders("host: example.com");
            resp.Headers.SetRawHeader("Content-Type", "text/plain");
        });
        using var httpClientFactory = new TestHttpClientFactory(messageHandler);
        using var httpClient = httpClientFactory.CreateClient("");
        var resp = await httpClient.GetAsync(new Uri("https://example.com")).ConfigureAwait(true);
        resp.AssertHeaders("content-type: text/plain");
    }
}
