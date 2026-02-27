using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Minio.IntegrationTests.Helpers;
using Minio.Model;
using Minio.Model.Notification;
using NATS.Client.Core;
using Testcontainers.Minio;
using Testcontainers.Nats;
using Xunit;

namespace Minio.IntegrationTests.Tests;

public class BucketNotificationTests
{
    private sealed class BucketNotificationEventDeserializer : INatsDeserialize<BucketNotificationEvent>
    {
        #pragma warning disable CA1822  // interface implementation cannot be static
        public BucketNotificationEvent? Deserialize(in ReadOnlySequence<byte> buffer)
            => JsonSerializer.Deserialize<BucketNotificationEvent>(buffer.FirstSpan);
        #pragma warning restore CA1822
    }

    [Fact]
    public async Task TestBucketNotificationsViaNats()
    {
        // Start NATS container
        await using var natsContainer = new NatsBuilder(Images.Nats)
            .WithExposedPort(4222)
            .Build();
        await natsContainer.StartAsync().ConfigureAwait(true);

        const string natsIdentifier = "test";
        const string natsSubject = "test-subject";

        // Subscribe to NATS
        var nats = new NatsConnection(new NatsOpts { Url = natsContainer.GetConnectionString() });
        await using (nats.ConfigureAwait(true))
        {
            var tsc = new TaskCompletionSource<BucketNotificationEvent>();
            var natsObservable = nats.SubscribeAsync(subject: natsSubject, serializer: new BucketNotificationEventDeserializer()).ToObservable();
            using var natsSubscription = natsObservable.Subscribe(e => tsc.TrySetResult(e.Data!));

            // Start MinIO container with proper NATS configuration
            await using var minioContainer = new MinioBuilder(Images.AIStor)
                .WithEnvironment(new Dictionary<string, string>
                {
                    ["MINIO_LICENSE"] = License.Minio,
                    [$"MINIO_NOTIFY_NATS_ENABLE_{natsIdentifier}"] = "on",
                    [$"MINIO_NOTIFY_NATS_ADDRESS_{natsIdentifier}"] = $"{natsContainer.IpAddress}:4222",
                    [$"MINIO_NOTIFY_NATS_SUBJECT_{natsIdentifier}"] = natsSubject,
                })
                .Build();
            await minioContainer.StartAsync().ConfigureAwait(true);

            var client = new MinioClientBuilder(minioContainer.GetConnectionString())
                .WithStaticCredentials(minioContainer.GetAccessKey(), minioContainer.GetSecretKey())
                .Build();

            const string bucketName = "testbucket";
            const string key = "test-nats";
            const string contentType = "test/plain";

            await client.CreateBucketAsync(bucketName).ConfigureAwait(true);
            var bucketNotification = new BucketNotification
            {
                QueueConfigs =
                {
                    new QueueConfig
                    {
                        Queue = $"arn:minio:sqs::{natsIdentifier}:nats",
                        Events = { EventType.ObjectCreatedAll }
                    }
                }
            };
            await client.SetBucketNotificationsAsync(bucketName, bucketNotification).ConfigureAwait(true);

            // Write the object
            var testData = "Hello world"u8.ToArray();
            using var ms = new MemoryStream(testData, false);
            await client.PutObjectAsync(bucketName, key, ms, new PutObjectOptions
            {
                ContentType = new MediaTypeHeaderValue(contentType)
            }).ConfigureAwait(true);

            // Wait until the NATS notification comes in
            var bucketNotificationEvent = await tsc.Task.ConfigureAwait(true);

            // Check the result of the event
            // (also checks JSON deserialization)
            Assert.Equal(EventType.ObjectCreatedPut, bucketNotificationEvent.EventName);
            Assert.Equal($"{bucketName}/{key}", bucketNotificationEvent.Key);
            Assert.Single(bucketNotificationEvent.Records);
            var notificationEvent = bucketNotificationEvent.Records[0];
            Assert.Equal("2.0", notificationEvent.EventVersion);
            Assert.Equal("minio:s3", notificationEvent.EventSource);
            Assert.Equal("", notificationEvent.AwsRegion);
            Assert.Equal(EventType.ObjectCreatedPut, notificationEvent.EventName);
            Assert.Equal(minioContainer.GetAccessKey(), notificationEvent.UserIdentity.PrincipalId);
            Assert.Equal(minioContainer.GetAccessKey(), notificationEvent.RequestParameters["principalId"]);
            Assert.Equal("", notificationEvent.RequestParameters["region"]);
            Assert.True(IPAddress.TryParse(notificationEvent.RequestParameters["sourceIPAddress"], out _));
            Assert.NotEmpty(notificationEvent.ResponseElements["x-amz-id-2"]);
            Assert.NotEmpty(notificationEvent.ResponseElements["x-amz-request-id"]);
            Assert.NotEmpty(notificationEvent.ResponseElements["x-minio-deployment-id"]);
            Assert.Equal($"http://{minioContainer.IpAddress}:9000", notificationEvent.ResponseElements["x-minio-origin-endpoint"]);
            Assert.Equal("1.0", notificationEvent.S3.SchemaVersion);
            Assert.Equal("Config", notificationEvent.S3.ConfigurationId);
            Assert.Equal(bucketName, notificationEvent.S3.Bucket.Name);
            Assert.Equal(minioContainer.GetAccessKey(), notificationEvent.S3.Bucket.OwnerIdentity.PrincipalId);
            Assert.Equal($"arn:aws:s3:::{bucketName}", notificationEvent.S3.Bucket.Arn);
            Assert.Equal(key, notificationEvent.S3.Object.Key);
            Assert.Equal((ulong)testData.Length, notificationEvent.S3.Object.Size);
            Assert.Equal("3e25960a79dbc69b674cd4ec67a72c62", notificationEvent.S3.Object.Etag);
            Assert.Equal(contentType, notificationEvent.S3.Object.ContentType);
            Assert.Equal(contentType, notificationEvent.S3.Object.UserMetadata["content-type"]);
            Assert.NotEmpty(notificationEvent.S3.Object.Sequencer);
            Assert.True(IPAddress.TryParse(notificationEvent.Source.Host, out _));

            // Verify the bucket notification
            var returnedBucketNotification = await client.GetBucketNotificationsAsync(bucketName).ConfigureAwait(true);
            Assert.True(XmlHelpers.DeepEqualsWithNormalization(bucketNotification.Serialize(), returnedBucketNotification.Serialize()));
        }
    }

    [Fact]
    public async Task TestMinioBucketNotifications()
    {
        // Start MinIO container
        await using var minioContainer = new MinioBuilder(Images.AIStor)
            .WithEnvironment("MINIO_LICENSE", License.Minio)
            .Build();
        await minioContainer.StartAsync().ConfigureAwait(true);

        var client = new MinioClientBuilder(minioContainer.GetConnectionString())
            .WithStaticCredentials(minioContainer.GetAccessKey(), minioContainer.GetSecretKey())
            .Build();

        const string bucketName = "testbucket";
        const string key = "test-events";
        const string contentType = "test/plain";

        await client.CreateBucketAsync(bucketName).ConfigureAwait(true);

        var tsc = new TaskCompletionSource<NotificationEvent>();
        var bucketNotifications = await client.ListenBucketNotificationsAsync(bucketName, EventType.ObjectCreatedAll).ConfigureAwait(true); 
        using var subscription = bucketNotifications.Subscribe(e => tsc.TrySetResult(e));

        // Write the object
        var testData = "Hello world"u8.ToArray();
        using var ms = new MemoryStream(testData, false);
        await client.PutObjectAsync(bucketName, key, ms, new PutObjectOptions
        {
            ContentType = new MediaTypeHeaderValue(contentType)
        }).ConfigureAwait(true);

        // Wait until the NATS notification comes in
        var notificationEvent = await tsc.Task.ConfigureAwait(true);

        // Check the result of the event
        // (also checks JSON deserialization)
        Assert.Equal("2.0", notificationEvent.EventVersion);
        Assert.Equal("minio:s3", notificationEvent.EventSource);
        Assert.Equal("", notificationEvent.AwsRegion);
        Assert.Equal(EventType.ObjectCreatedPut, notificationEvent.EventName);
        Assert.Equal(minioContainer.GetAccessKey(), notificationEvent.UserIdentity.PrincipalId);
        Assert.Equal(minioContainer.GetAccessKey(), notificationEvent.RequestParameters["principalId"]);
        Assert.Equal("", notificationEvent.RequestParameters["region"]);
        Assert.True(IPAddress.TryParse(notificationEvent.RequestParameters["sourceIPAddress"], out _));
        Assert.NotEmpty(notificationEvent.ResponseElements["x-amz-id-2"]);
        Assert.NotEmpty(notificationEvent.ResponseElements["x-amz-request-id"]);
        Assert.NotEmpty(notificationEvent.ResponseElements["x-minio-deployment-id"]);
        Assert.Equal($"http://{minioContainer.IpAddress}:9000", notificationEvent.ResponseElements["x-minio-origin-endpoint"]);
        Assert.Equal("1.0", notificationEvent.S3.SchemaVersion);
        Assert.Equal("Config", notificationEvent.S3.ConfigurationId);
        Assert.Equal(bucketName, notificationEvent.S3.Bucket.Name);
        Assert.Equal(minioContainer.GetAccessKey(), notificationEvent.S3.Bucket.OwnerIdentity.PrincipalId);
        Assert.Equal($"arn:aws:s3:::{bucketName}", notificationEvent.S3.Bucket.Arn);
        Assert.Equal(key, notificationEvent.S3.Object.Key);
        Assert.Equal((ulong)testData.Length, notificationEvent.S3.Object.Size);
        Assert.Equal("3e25960a79dbc69b674cd4ec67a72c62", notificationEvent.S3.Object.Etag);
        Assert.Equal(contentType, notificationEvent.S3.Object.ContentType);
        Assert.Equal(contentType, notificationEvent.S3.Object.UserMetadata["content-type"]);
        Assert.NotEmpty(notificationEvent.S3.Object.Sequencer);
        Assert.True(IPAddress.TryParse(notificationEvent.Source.Host, out _));
    }
}