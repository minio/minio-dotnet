using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio.CredentialProviders;
using Minio.Implementation;
using Minio.UnitTests.Services;
using Polly;

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
            var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3, 
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                })
                .AddTimeout(TimeSpan.FromSeconds(10)) 
                .Build();
            
            #pragma warning disable EXTEXP0001
            messageHandler = new ResilienceHandler(pipeline)
            {
                InnerHandler = messageHandler
            };
            #pragma warning restore EXTEXP0001
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