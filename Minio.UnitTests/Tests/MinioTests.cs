using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio.CredentialProviders;
using Minio.Implementation;
using Minio.UnitTests.Services;

namespace Minio.UnitTests.Tests;

public abstract class MinioUnitTests
{
    // ReSharper disable MemberCanBePrivate.Global
    protected string MinioEndPoint { get; set; } = "http://localhost:9000";
    protected string AccessKey { get; set; } = "minioadmin";
    protected string SecretKey { get; set; } = "minioadmin";
    protected string CurrentTime { get; set; } = "20240411T153713Z";
    // ReSharper restore MemberCanBePrivate.Global

    protected async Task RunWithMinioClientAsync(Action<HttpRequestMessage, HttpResponseMessage> handler, Func<IMinioClient, Task> func, bool withPolicy = false)
    {
        ArgumentNullException.ThrowIfNull(func);
        
        var options = Options.Create(new ClientOptions
        {
            EndPoint = new Uri(MinioEndPoint)
        });
        var credentialsProvider = new StaticCredentialsProvider(Options.Create(new StaticCredentialsOptions
        {
            AccessKey = AccessKey,
            SecretKey = SecretKey,
        }));
        var timeProvider = new StaticTimeProvider(CurrentTime);
        var authLogger = NullLoggerFactory.Instance.CreateLogger<V4RequestAuthenticator>();
        var authenticator = new V4RequestAuthenticator(credentialsProvider, timeProvider, authLogger);
        DelegatingHandler messageHandler = new MockHttpMessageHandler(handler);
        if (withPolicy)
        {
            messageHandler = new PolicyHttpMessageHandler(MinioClientBuilder.GetRetryPolicy())
            {
                InnerHandler = messageHandler
            };
        }

        using (messageHandler)
        {
            using var httpClientFactory = new TestHttpClientFactory(messageHandler);
            var logger = NullLoggerFactory.Instance.CreateLogger<MinioClient>();
            var client = new MinioClient(options, timeProvider, authenticator, httpClientFactory, logger);
            await func(client).ConfigureAwait(false);
        }
    }
}