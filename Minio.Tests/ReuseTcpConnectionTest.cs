using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Exceptions;

namespace Minio.Tests;

[TestClass]
public class ReuseTcpConnectionTest
{
    public ReuseTcpConnectionTest()
    {
        _minioClient = new MinioClient()
            .WithEndpoint(TestHelper.Endpoint)
            .WithCredentials(TestHelper.AccessKey, TestHelper.SecretKey)
            .WithSSL()
            .Build();
    }

    private MinioClient _minioClient { get; }

    private async Task<bool> ObjectExistsAsync(MinioClient client, string bucket, string objectName)
    {
        try
        {
            var getObjectArgs = new GetObjectArgs()
                .WithBucket("bucket")
                .WithObject(objectName)
                .WithFile("testfile");
            await client.GetObjectAsync(getObjectArgs).ConfigureAwait(false);

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    [TestMethod]
    public async Task ReuseTcpTest()
    {
        var bucket = "bucket";
        var objectName = "object-name";

        var bktExistArgs = new BucketExistsArgs()
            .WithBucket(bucket);
        var found = await _minioClient.BucketExistsAsync(bktExistArgs).ConfigureAwait(false);
        if (!found)
        {
            var mkBktArgs = new MakeBucketArgs()
                .WithBucket(bucket);
            await _minioClient.MakeBucketAsync(mkBktArgs).ConfigureAwait(false);
        }

        if (!await ObjectExistsAsync(_minioClient, bucket, objectName).ConfigureAwait(false))
        {
            var helloData = Encoding.UTF8.GetBytes("hello world");
            using var helloStream = new MemoryStream();
            helloStream.Write(helloData);
            helloStream.Seek(0, SeekOrigin.Begin);
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(helloStream)
                .WithObjectSize(helloData.Length);
            await _minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
        }

        await GetObjectLength(bucket, objectName).ConfigureAwait(false);

        for (var i = 0; i < 100; i++)
            // sequential execution, produce one tcp connection, check by netstat -an | grep 9000
            await GetObjectLength(bucket, objectName).ConfigureAwait(false);

        Parallel.ForEach(Enumerable.Range(0, 500),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 8
            },
            async _ =>
            {
                // concurrent execution, produce eight tcp connections.
                await GetObjectLength(bucket, objectName).ConfigureAwait(false);
            });
    }

    private async Task<double> GetObjectLength(string bucket, string objectName)
    {
        long objectLength = 0;
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.Dispose());
        await _minioClient.GetObjectAsync(getObjectArgs).ConfigureAwait(false);

        return objectLength;
    }
}