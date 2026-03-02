using System.Net.Http.Headers;
using Minio.Model;
using Xunit;

namespace Minio.IntegrationTests.Tests;

public class ObjectTests : MinioTest
{
    private const string BucketName = "test";
    private const string ObjectKey = "folder/testdata";

    [Fact]
    public async Task TestPutAndGetObject()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName, objectLocking: true).ConfigureAwait(true);
        var testData = GetRandomData(1024);

        var putObjectOptions = new PutObjectOptions
        {
            ContentType = new MediaTypeHeaderValue("application/x-minio-random-testdata"),
            UserMetadata =
            {
                { "TestMeta-1", "Meta=Value&1" },
                { "TestMeta-2", "Meta=Value&1" },
            },
            UserTags =
            {
                { "tag1", "abcd" },
                { "tag2", "efgh" },
            },
            StorageClass = "STANDARD",
            
            Mode = RetentionMode.Compliance,
            RetainUntilDate = DateTimeOffset.Now.AddHours(24),
        };
        
        using (var msUpload = new MemoryStream(testData))
        {
            await client.PutObjectAsync(BucketName, ObjectKey, msUpload, putObjectOptions).ConfigureAwait(true);
        }

        var stream = await client.GetObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        
        await using (stream.ConfigureAwait(true))
        {
            Assert.Equal(testData.Length, stream.Length);
            var readData = new byte[stream.Length];
            var readBytes = await stream.ReadAsync(readData).ConfigureAwait(true);
            Assert.Equal(stream.Length, readBytes);
            Assert.Equal(testData, readData);
        }
    }

    [Fact]
    public async Task TestPutZeroObject()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName, objectLocking: true).ConfigureAwait(true);
        await client.PutObjectAsync(BucketName, ObjectKey, Stream.Null).ConfigureAwait(true);

        var data = await client.GetObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await using (data.ConfigureAwait(true))
        {
            Assert.Equal(0, data.Length);
        }
    }

    [Fact]
    public async Task TestListObjects1()
    {
        const int objectSize = 256;
        const int parallelUploads = 100;

        var client = CreateClient();
        await client.CreateBucketAsync(BucketName, objectLocking: true).ConfigureAwait(true);

        // Write out 100 objects in parallel
        var buffer = new byte[objectSize];
        for (var i = 0; i < buffer.Length; ++i)
            buffer[i] = (byte)i;

        await Task.WhenAll(Enumerable.Range(0, parallelUploads).Select(async i =>
        {
            var ms = new MemoryStream(buffer, false);
            await using (ms.ConfigureAwait(false))
            {
                var opts = new PutObjectOptions
                {
                    UserMetadata =
                    {
                        { "FixedKey", "Minio-test" },
                        { "VariableKey", $"Value-{i}" },
                    },
                    UserTags =
                    {
                        { "VariableTag", $"Tag-{i}"}
                    }
                };
                await client.PutObjectAsync(BucketName, $"test-{i:D04}", ms, opts).ConfigureAwait(false);
            }
        })).ConfigureAwait(true);

        // Read an object file
        var stream = await client.GetObjectAsync(BucketName, "test-0000").ConfigureAwait(true);
        Assert.Equal(1, stream.Info.UserTagCount);
        Assert.Equal(objectSize, stream.Info.ContentLength);
        Assert.Equal("Minio-test", stream.Info.UserMetadata["FixedKey"]);
        Assert.Equal("Value-0", stream.Info.UserMetadata["VariableKey"]);
        await using (stream.ConfigureAwait(false))
        {
            var readBuffer = new byte[objectSize+10];
            var readBytes = await stream.ReadAsync(readBuffer).ConfigureAwait(true);
            Assert.Equal(objectSize, readBytes);
            for (var i = 0; i < readBytes; ++i)
                Assert.Equal((byte)i, readBuffer[i]);
        }

        // List all objects starting with "test-" in the test-bucket
        // (max 10 objects at a time)
        await foreach (var objItem in client.ListObjectsAsync(BucketName, prefix: "test-", delimiter: "/", includeMetadata: true, pageSize: 10))
        {
            Assert.StartsWith("test-", objItem.Key, StringComparison.Ordinal);
            if (!int.TryParse(objItem.Key[5..], out var i))
                Assert.Fail("Unable to parse index from key");
            Assert.Equal(objectSize, objItem.Size);
            Assert.Equal("Minio-test", objItem.UserMetadata["FixedKey"]);
            Assert.Equal($"Value-{i}", objItem.UserMetadata["VariableKey"]);
        }
    }

    public static readonly IEnumerable<object[]> Prefixes =
    [
        ["prefix"],
        ["字首"],
        ["!@#$%^&*()"],
        ["folder/prefix"],
        ["資料夾/字首"],
        ["!@#$%/^&*()"]
    ];

    [Theory]
    [MemberData(nameof(Prefixes))]
    public async Task TestListObjects2(string prefix)
    {
        const int objectSize = 256;

        var client = CreateClient();
        await client.CreateBucketAsync(BucketName, objectLocking: true).ConfigureAwait(true);

        var buffer = new byte[objectSize];
        for (var i = 0; i < buffer.Length; ++i)
            buffer[i] = (byte)i;

        var ms = new MemoryStream(buffer, false);
        await using (ms.ConfigureAwait(false))
        {
            await client.PutObjectAsync(BucketName, $"{prefix}-test", ms).ConfigureAwait(true);
        }

        await foreach (var objItem in client.ListObjectsAsync(BucketName, prefix: prefix, delimiter: "/")) 
            Assert.Equal($"{prefix}-test", objItem.Key);
    }

    [Fact]
    public async Task TestMultipartUpload()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        var createOpts = new CreateMultipartUploadOptions();
        var createResult = await client.CreateMultipartUploadAsync(BucketName, ObjectKey, createOpts).ConfigureAwait(true);

        var start = DateTimeOffset.Now.AddSeconds(-3);
        
        var totalParts = 10;
        var partSize = 5 * 1024 * 1024;  // should be at least 5MiB
        var parts = new PartInfo[totalParts];
        var parallelOpts = new ParallelOptions { MaxDegreeOfParallelism = 4 };
        
        await Parallel.ForEachAsync(Enumerable.Range(0, totalParts), parallelOpts, async (part, ct) =>
        {
            var data = GetRandomData(partSize);
            using (var ms = new MemoryStream(data))
            {
                long lastKnownPosition = -1;
                void Progress(long position, long length)
                {
                    Assert.NotEqual(lastKnownPosition, position);
                    Assert.Equal(data.Length, length);
                    Assert.InRange(position, Math.Max(0, lastKnownPosition), length);
                    lastKnownPosition = position;
                }

                var uploadOpts = new UploadPartOptions();
                var partResult = await client.UploadPartAsync(BucketName, ObjectKey, createResult.UploadId, part+1, ms, uploadOpts, Progress, ct).ConfigureAwait(true);
                parts[part] = new PartInfo
                {
                    Etag = partResult.Etag!
                };
                
                Assert.Equal(data.Length, lastKnownPosition);
            }
        }).ConfigureAwait(true);

        var end = DateTimeOffset.Now.AddSeconds(3);
        
        // List all parts
        var partItems = await client.ListPartsAsync(BucketName, ObjectKey, createResult.UploadId, pageSize: 5).ToListAsync().ConfigureAwait(true);
        Assert.Equal(totalParts, partItems.Count);
        
        // Verify part information
        for (var i = 0; i < totalParts; i++)
        {
            var partItem = partItems[i];
            Assert.Equal(i+1, partItem.PartNumber);
            Assert.Equal(partSize, partItem.Size);
            Assert.InRange(partItem.LastModified, start, end);
            Assert.Equal(parts[i].Etag, partItems[i].ETag);
        }
        
        // Complete the upload
        var completeOpts = new CompleteMultipartUploadOptions();
        var completeResult = await client.CompleteMultipartUploadAsync(BucketName, ObjectKey, createResult.UploadId, parts, completeOpts).ConfigureAwait(true);
        Assert.Equal(BucketName, completeResult.Bucket);
        Assert.Equal(ObjectKey, completeResult.Key);
        
        // Get the actual object
        var objectInfo = await client.HeadObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        Assert.Equal(completeResult.Etag, objectInfo.Etag.Tag);
        Assert.Equal(totalParts * partSize, objectInfo.ContentLength);
    }

    [Fact]
    public async Task TestListMultipartUpload()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        // Create multipart upload
        var abortUpload = await client.CreateMultipartUploadAsync(BucketName, ObjectKey).ConfigureAwait(true);

        // We should have 1 upload
        // IMPORTANT: In MinIO this call is only able to list when a prefix is set to an exact object-name
        var uploads1 = await client.ListMultipartUploadsAsync(BucketName, prefix: ObjectKey, pageSize: 10).ToListAsync().ConfigureAwait(true);
        Assert.Single(uploads1);

        // Abort all uploads
        await client.AbortMultipartUploadAsync(abortUpload.Bucket, abortUpload.Key, abortUpload.UploadId).ConfigureAwait(true);

        // We should have 0 uploads
        var uploads2 = await client.ListMultipartUploadsAsync(BucketName).ToListAsync().ConfigureAwait(true);
        Assert.Empty(uploads2);
    }

    [Fact]
    public async Task TestDeleteObjectSimple()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);
        using var ms = new MemoryStream(new byte[1024]);
        await client.PutObjectAsync(BucketName, ObjectKey, ms).ConfigureAwait(true);
        await client.DeleteObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
    }

    [Fact]
    public async Task TestDeleteObjectLotsOfFiles()
    {
        const int successFiles = 1015;  // Should be >1000 to batch deletes
        const int failedFiles = 10;
        
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);
        var data = new byte[1024];
        await Parallel.ForEachAsync(Enumerable.Range(0, successFiles), async (i, ct) =>
        {
            using var ms = new MemoryStream(data, false);
            await client.PutObjectAsync(BucketName, $"{ObjectKey}-{i:D04}", ms, cancellationToken: ct).ConfigureAwait(false);
        }).ConfigureAwait(true);

        var keys = Enumerable.Range(0, successFiles).Select(i => new ObjectIdentifier{ Key = $"{ObjectKey}-{i:D04}" });
        var failedKeys = Enumerable.Range(0, failedFiles).Select(i => new ObjectIdentifier
        {
            Key = $"InvalidETag-{i:D04}",
            ETag = "invalid",
        });
        var combinedKeys = keys.Concat(failedKeys);
        var failedKeySet = new HashSet<string>();
        var successKeySet = new HashSet<string>();
        await foreach (var result in client.DeleteObjectsVerboseAsync(BucketName, combinedKeys).ConfigureAwait(true))
        {
            if (result.ErrorCode != null)
            {
                Assert.StartsWith("InvalidETag-", result.Key, StringComparison.Ordinal);
                Assert.True(failedKeySet.Add(result.Key));
                Assert.Null(result.VersionId);
                Assert.NotNull(result.ErrorCode);
                Assert.NotNull(result.ErrorMessage);
            }
            else
            {
                Assert.StartsWith($"{ObjectKey}-", result.Key, StringComparison.Ordinal);
                Assert.True(successKeySet.Add(result.Key));
                Assert.Null(result.VersionId);
                Assert.Null(result.DeleteMarker);
                Assert.Null(result.DeleteMarkerVersionId);
            }
        }
        Assert.Equal(successFiles, successKeySet.Count);
        Assert.Equal(failedFiles, failedKeySet.Count);
    }

    [Fact]
    public async Task TestDeleteObjectLotsOfFilesQuiet()
    {
        const int successFiles = 1015;  // Should be >1000 to batch deletes
        const int failedFiles = 10;
        
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);
        var data = new byte[1024];
        await Parallel.ForEachAsync(Enumerable.Range(0, successFiles), async (i, ct) =>
        {
            using var ms = new MemoryStream(data, false);
            await client.PutObjectAsync(BucketName, $"{ObjectKey}-{i:D04}", ms, cancellationToken: ct).ConfigureAwait(false);
        }).ConfigureAwait(true);

        using (var ms = new MemoryStream(data, false))
        {
            await client.PutObjectAsync(BucketName, $"{ObjectKey}-empty", ms).ConfigureAwait(true);
        }

        var keys = Enumerable.Range(0, successFiles).Select(i => new ObjectIdentifier{Key = $"{ObjectKey}-{i:D04}"});
        var failedKeys = Enumerable.Range(0, failedFiles).Select(i => new ObjectIdentifier{Key = $"NonExistent-{i:D304}"});
        var combinedKeys = keys.Concat(failedKeys);
        await client.DeleteObjectsAsync(BucketName, combinedKeys).ConfigureAwait(true);

        // Only the file with "-empty" suffix should be there
        var objectCount = await client.ListObjectsAsync(BucketName).CountAsync().ConfigureAwait(true);
        Assert.Equal(1, objectCount);
    }

    [Fact]
    public async Task TestDeleteBucketVersioning()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        // Write without versioning
        var data = new byte[1024];
        using var ms1 = new MemoryStream(data, false);
        await client.PutObjectAsync(BucketName, ObjectKey, ms1).ConfigureAwait(true);
        var objInfo1 = await client.HeadObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        Assert.Null(objInfo1.VersionId);

        // Enable versioning
        await client.SetBucketVersioningAsync(BucketName, VersioningStatus.Enabled, true).ConfigureAwait(true);
        
        // Write with versioning
        using var ms2 = new MemoryStream(data, false);
        await client.PutObjectAsync(BucketName, ObjectKey, ms2).ConfigureAwait(true);
        var objInfo2 = await client.HeadObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        Assert.NotNull(objInfo2.VersionId);
        Assert.NotEqual(string.Empty, objInfo2.VersionId);

        // Check versioning setting
        var config1 = await client.GetBucketVersioningAsync(BucketName).ConfigureAwait(true);
        Assert.Equal(VersioningStatus.Enabled, config1.Status);
        Assert.True(config1.MfaDelete);

        // Disable versioning
        await client.SetBucketVersioningAsync(BucketName, VersioningStatus.Suspended).ConfigureAwait(true);

        // Check versioning setting
        var config2 = await client.GetBucketVersioningAsync(BucketName).ConfigureAwait(true);
        Assert.Equal(VersioningStatus.Suspended, config2.Status);
        Assert.False(config2.MfaDelete);
    }

    private static byte[] GetRandomData(int length)
    {
#pragma warning disable CA5394  // We explicitly want pseudo-random data
        var data = new byte[length];
        new Random(0).NextBytes(data);
        return data;
#pragma warning restore CA5394
    }
}