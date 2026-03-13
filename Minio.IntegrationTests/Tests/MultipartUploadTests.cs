using System.Net.Http.Headers;
using Minio.Model;
using Xunit;

namespace Minio.IntegrationTests.Tests;

public class MultipartUploadTests : MinioTest
{
    private const string BucketName = "multipart-test";
    private const string ObjectKey = "multipart/object.bin";

    private static readonly string[] s_contentEncoding = ["gzip", "identity"];

    [Fact]
    public async Task MultipartUploadSmallObject()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        // Small object that should fit in a single part
        var data = new byte[16 * 1024]; // 16KB
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);

        using var ms = new MemoryStream(data);
        await client.PutObjectAsync(BucketName, ObjectKey, ms).ConfigureAwait(true);

        var getResult = await client.GetObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await using (getResult.ConfigureAwait(true))
        {
            var readData = new byte[getResult.Length];
            await getResult.ReadExactlyAsync(readData).ConfigureAwait(true);
            Assert.Equal(data, readData);
        }

        // Cleanup
        await client.DeleteObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await client.DeleteBucketAsync(BucketName).ConfigureAwait(true);
    }

    [Fact]
    public async Task MultipartUploadLargeObject()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        // Large object that requires multiple parts (>16MB)
        var partSize = 16 * 1024 * 1024; // 16MB per part
        var numParts = 3;
        var totalSize = partSize * numParts;
        
        var buffer = new byte[partSize];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(i % 256);

        // Upload using multipart
        var createResult = await client.CreateMultipartUploadAsync(BucketName, ObjectKey).ConfigureAwait(true);
        var uploadId = createResult.UploadId;

        var partNumber = 1;
        var parts = new List<PartInfo>();

        // Upload parts
        for (var p = 0; p < numParts; p++)
        {
            using var partStream = new MemoryStream(buffer);
            var partResult = await client.UploadPartAsync(
                BucketName, ObjectKey, uploadId, partNumber, partStream
            ).ConfigureAwait(true);

            parts.Add(new PartInfo
            {
                PartNumber = partNumber,
                Etag = partResult.Etag
            });

            partNumber++;
        }

        // Complete multipart upload
        var completeResult = await client.CompleteMultipartUploadAsync(
            BucketName, ObjectKey, uploadId, parts
        ).ConfigureAwait(true);

        Assert.NotNull(completeResult.Etag);

        // Verify upload
        var headResult = await client.HeadObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        Assert.Equal(totalSize, headResult.ContentLength);

        // Cleanup
        await client.DeleteObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await client.DeleteBucketAsync(BucketName).ConfigureAwait(true);
    }

    [Fact]
    public async Task MultipartUploadWithContentEncoding()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        var data = new byte[32 * 1024]; // 32KB
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);

        var createResult = await client.CreateMultipartUploadAsync(
            BucketName, ObjectKey, 
            new CreateMultipartUploadOptions
            {
                ContentEncoding = s_contentEncoding
            }
        ).ConfigureAwait(true);

        using var ms = new MemoryStream(data);
        var partResult = await client.UploadPartAsync(
            BucketName, ObjectKey, createResult.UploadId, 1, ms
        ).ConfigureAwait(true);

        var completeResult = await client.CompleteMultipartUploadAsync(
            BucketName, ObjectKey, createResult.UploadId, 
            new[] { new PartInfo { PartNumber = 1, Etag = partResult.Etag } }
        ).ConfigureAwait(true);

        Assert.NotNull(completeResult.Etag);

        // Verify ContentEncoding was set
        var headResult = await client.HeadObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        Assert.Equal("gzip", headResult.Metadata["Content-Encoding"]);

        // Cleanup
        await client.DeleteObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await client.DeleteBucketAsync(BucketName).ConfigureAwait(true);
    }

    [Fact]
    public async Task MultipartUploadWithMetadata()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        var data = new byte[32 * 1024];

        var options = new CreateMultipartUploadOptions
        {
            ContentType = new MediaTypeHeaderValue("application/octet-stream"),
            UserTags = { { "custom-tag", "tag-value" } }
        };

        var createResult = await client.CreateMultipartUploadAsync(
            BucketName, ObjectKey, options
        ).ConfigureAwait(true);

        using var ms = new MemoryStream(data);
        var partResult = await client.UploadPartAsync(
            BucketName, ObjectKey, createResult.UploadId, 1, ms
        ).ConfigureAwait(true);

        var completeResult = await client.CompleteMultipartUploadAsync(
            BucketName, ObjectKey, createResult.UploadId,
            new[] { new PartInfo { PartNumber = 1, Etag = partResult.Etag } }
        ).ConfigureAwait(true);

        Assert.NotNull(completeResult.Etag);

        // Verify metadata using HeadObjectAsync
        var headResult = await client.HeadObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        Assert.Equal("application/octet-stream", headResult.ContentType.ToString());
        Assert.Contains("custom-tag", headResult.UserTags);
        Assert.Equal("tag-value", headResult.UserTags["custom-tag"]);

        // Cleanup
        await client.DeleteObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await client.DeleteBucketAsync(BucketName).ConfigureAwait(true);
    }

    [Fact]
    public async Task MultipartUploadWithCrc32Checksum()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        var data = new byte[32 * 1024]; // 32KB
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);

        // Compute CRC32 checksum
        var crc32 = ComputeCrc32(data);

        var createResult = await client.CreateMultipartUploadAsync(BucketName, ObjectKey).ConfigureAwait(true);

        using var ms = new MemoryStream(data);
        var partResult = await client.UploadPartAsync(
            BucketName, ObjectKey, createResult.UploadId, 1, ms,
            new UploadPartOptions
            {
                ChecksumAlgorithm = ChecksumAlgorithm.Crc32,
                Checksum = crc32
            }
        ).ConfigureAwait(true);

        var completeResult = await client.CompleteMultipartUploadAsync(
            BucketName, ObjectKey, createResult.UploadId,
            new[] { new PartInfo
                {
                    PartNumber = 1,
                    Etag = partResult.Etag,
                    ChecksumAlgorithm = ChecksumAlgorithm.Crc32,
                    Checksum = crc32
                }
            }
        ).ConfigureAwait(true);

        Assert.NotNull(completeResult.Etag);
        Assert.NotNull(completeResult.ChecksumCRC32);

        // Verify by downloading and ensuring no ChecksumVerificationException
        var getResult = await client.GetObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await using (getResult.ConfigureAwait(true))
        {
            var readData = new byte[getResult.Length];
            await getResult.ReadExactlyAsync(readData).ConfigureAwait(true);
            Assert.Equal(data, readData);
        }

        // Cleanup
        await client.DeleteObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await client.DeleteBucketAsync(BucketName).ConfigureAwait(true);
    }

    [Fact]
    public async Task MultipartUploadWithCrc32cChecksum()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        var data = new byte[32 * 1024]; // 32KB
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);

        // Compute CRC32C checksum
        var crc32c = ComputeCrc32c(data);

        var createResult = await client.CreateMultipartUploadAsync(BucketName, ObjectKey).ConfigureAwait(true);

        using var ms = new MemoryStream(data);
        var partResult = await client.UploadPartAsync(
            BucketName, ObjectKey, createResult.UploadId, 1, ms,
            new UploadPartOptions
            {
                ChecksumAlgorithm = ChecksumAlgorithm.Crc32c,
                Checksum = crc32c
            }
        ).ConfigureAwait(true);

        var completeResult = await client.CompleteMultipartUploadAsync(
            BucketName, ObjectKey, createResult.UploadId,
            new[] { new PartInfo
                {
                    PartNumber = 1,
                    Etag = partResult.Etag,
                    ChecksumAlgorithm = ChecksumAlgorithm.Crc32c,
                    Checksum = crc32c
                }
            }
        ).ConfigureAwait(true);

        Assert.NotNull(completeResult.Etag);
        Assert.NotNull(completeResult.ChecksumCRC32C);

        // Verify by downloading and ensuring no ChecksumVerificationException
        var getResult = await client.GetObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await using (getResult.ConfigureAwait(true))
        {
            var readData = new byte[getResult.Length];
            await getResult.ReadExactlyAsync(readData).ConfigureAwait(true);
            Assert.Equal(data, readData);
        }

        // Cleanup
        await client.DeleteObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await client.DeleteBucketAsync(BucketName).ConfigureAwait(true);
    }

    [Fact]
    public async Task GetObjectWithChecksumVerification()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        var data = new byte[32 * 1024]; // 32KB
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);

        // Compute CRC32C checksum
        var crc32c = ComputeCrc32c(data);

        // Upload with CRC32C checksum
        var createResult = await client.CreateMultipartUploadAsync(BucketName, ObjectKey).ConfigureAwait(true);

        using var ms = new MemoryStream(data);
        var partResult = await client.UploadPartAsync(
            BucketName, ObjectKey, createResult.UploadId, 1, ms,
            new UploadPartOptions
            {
                ChecksumAlgorithm = ChecksumAlgorithm.Crc32c,
                Checksum = crc32c
            }
        ).ConfigureAwait(true);

        var completeResult = await client.CompleteMultipartUploadAsync(
            BucketName, ObjectKey, createResult.UploadId,
            new[] { new PartInfo
                {
                    PartNumber = 1,
                    Etag = partResult.Etag,
                    ChecksumAlgorithm = ChecksumAlgorithm.Crc32c,
                    Checksum = crc32c
                }
            }
        ).ConfigureAwait(true);

        // Download and verify checksum is automatically verified
        // If verification fails, ChecksumVerificationException would be thrown
        var getResult = await client.GetObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await using (getResult.ConfigureAwait(true))
        {
            var readData = new byte[getResult.Length];
            await getResult.ReadExactlyAsync(readData).ConfigureAwait(true);
            Assert.Equal(data, readData);
        }

        // Cleanup
        await client.DeleteObjectAsync(BucketName, ObjectKey).ConfigureAwait(true);
        await client.DeleteBucketAsync(BucketName).ConfigureAwait(true);
    }

    [Fact]
    public async Task ListPartsAndListUploads()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        var createResult = await client.CreateMultipartUploadAsync(
            BucketName, ObjectKey + "-1"
        ).ConfigureAwait(true);

        var listUploads = await client.ListMultipartUploadsAsync(BucketName, prefix: ObjectKey + "-1").ToListAsync().ConfigureAwait(true);
        Assert.Contains(listUploads, u => u.UploadId == createResult.UploadId);

        await client.AbortMultipartUploadAsync(
            BucketName, ObjectKey + "-1", createResult.UploadId
        ).ConfigureAwait(true);

        // Cleanup
        await client.DeleteBucketAsync(BucketName).ConfigureAwait(true);
    }

    [Fact]
    public async Task AbortMultipartUpload()
    {
        var client = CreateClient();
        await client.CreateBucketAsync(BucketName).ConfigureAwait(true);

        var createResult = await client.CreateMultipartUploadAsync(
            BucketName, ObjectKey
        ).ConfigureAwait(true);

        // Upload one part
        using var ms = new MemoryStream(new byte[16 * 1024]);
        await client.UploadPartAsync(
            BucketName, ObjectKey, createResult.UploadId, 1, ms
        ).ConfigureAwait(true);

        // List parts
        var parts = await client.ListPartsAsync(
            BucketName, ObjectKey, createResult.UploadId
        ).ToListAsync().ConfigureAwait(true);
        Assert.Single(parts);

        // Abort the upload
        await client.AbortMultipartUploadAsync(
            BucketName, ObjectKey, createResult.UploadId
        ).ConfigureAwait(true);

        // Upload should be gone
        var uploadsAfterAbort = await client.ListMultipartUploadsAsync(BucketName, prefix: ObjectKey).ToListAsync().ConfigureAwait(true);
        Assert.Empty(uploadsAfterAbort);

        // Cleanup
        await client.DeleteBucketAsync(BucketName).ConfigureAwait(true);
    }

    private static byte[] GetRandomData(int size)
    {
#pragma warning disable CA5394
        var data = new byte[size];
        Random.Shared.NextBytes(data);
        return data;
#pragma warning restore CA5394
    }

    private static byte[] ComputeCrc32(byte[] data)
    {
        const uint defaultPolynomial = 0xEDB88320;
        uint hash = 0xFFFFFFFF;

        foreach (var b in data)
        {
            hash ^= b;
            for (var i = 0; i < 8; i++)
            {
                hash = (hash >> 1) ^ ((hash & 1) != 0 ? defaultPolynomial : 0);
            }
        }

        hash = ~hash;
        return BitConverter.GetBytes(hash);
    }

    private static byte[] ComputeCrc32c(byte[] data)
    {
        const uint defaultPolynomial = 0x82F63B78;
        uint hash = 0xFFFFFFFF;

        foreach (var b in data)
        {
            hash ^= b;
            for (var i = 0; i < 8; i++)
            {
                hash = (hash >> 1) ^ ((hash & 1) != 0 ? defaultPolynomial : 0);
            }
        }

        hash = ~hash;
        return BitConverter.GetBytes(hash);
    }
}
