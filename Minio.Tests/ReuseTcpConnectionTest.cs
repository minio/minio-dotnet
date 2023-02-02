using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Exceptions;

namespace Minio.Tests;

[TestClass]
public class ReuseTcpConnectionTest
{
    public ReuseTcpConnectionTest()
    {
        MinioClient = new MinioClient()
            .WithEndpoint("play.min.io")
            .WithCredentials("Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG")
            .WithSSL()
            .Build();
    }

    private MinioClient MinioClient { get; }

    private async Task<bool> ObjectExistsAsync(MinioClient client, string bucket, string objectName)
    {
        try
        {
            var getObjectArgs = new GetObjectArgs()
                .WithBucket("bucket")
                .WithObject(objectName)
                .WithFile("testfile");
            await client.GetObjectAsync(getObjectArgs);

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
        var found = await MinioClient.BucketExistsAsync(bktExistArgs);
        if (!found)
        {
            var mkBktArgs = new MakeBucketArgs()
                .WithBucket(bucket);
            await MinioClient.MakeBucketAsync(mkBktArgs);
        }

        if (!await ObjectExistsAsync(MinioClient, bucket, objectName))
        {
            var helloData = Encoding.UTF8.GetBytes("hello world");
            var helloStream = new MemoryStream();
            helloStream.Write(helloData);
            helloStream.Seek(0, SeekOrigin.Begin);
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(helloStream)
                .WithObjectSize(helloData.Length);
            await MinioClient.PutObjectAsync(putObjectArgs);
        }

        await GetObjectLength(bucket, objectName);

        for (var i = 0; i < 100; i++)
            // sequential execution, produce one tcp connection, check by netstat -an | grep 9000
            await GetObjectLength(bucket, objectName);

        Parallel.ForEach(Enumerable.Range(0, 500),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 8
            },
            async i =>
            {
                // concurrent execution, produce eight tcp connections.
                await GetObjectLength(bucket, objectName);
            });
    }

    private async Task<double> GetObjectLength(string bucket, string objectName)
    {
        long objectLength = 0;
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithCallbackStream(stream => { stream.Dispose(); });
        await MinioClient.GetObjectAsync(getObjectArgs);

        return objectLength;
    }
}