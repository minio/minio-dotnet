namespace Minio.UnitTests.Tests;

public sealed class MockHttpMessageHandler : DelegatingHandler
{
    private readonly Action<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Action<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrEmpty(request.Headers.Host))
            request.Headers.Host = request.RequestUri?.Authority;
        var response = new HttpResponseMessage { RequestMessage = request };
        _handler(request, response);
        return Task.FromResult(response);
    }
}