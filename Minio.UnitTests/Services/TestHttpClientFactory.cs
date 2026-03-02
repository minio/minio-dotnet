namespace Minio.UnitTests.Services;

public sealed class TestHttpClientFactory : IHttpClientFactory, IDisposable
{
    private readonly DelegatingHandler _messageHandler;

    public TestHttpClientFactory(DelegatingHandler handler)
    {
        _messageHandler = handler;
    }
    
    public HttpClient CreateClient(string name) => new(_messageHandler);

    public void Dispose()
    {
        _messageHandler.Dispose();
    }
}