using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.Exceptions;

namespace Minio.Tests;

[TestClass]
public class OperationsTest
{
    private readonly DateTime _requestDate = new(2020, 05, 01, 15, 45, 33, DateTimeKind.Utc);

    private static bool IsLocationRequest(HttpRequestMessageBuilder httpRequest)
    {
        // todo how to test this with mock client.
        var resource = httpRequest.RequestUri.LocalPath;
        return resource.Contains("?") == false &&
               httpRequest.QueryParameters.ContainsKey("location");
    }


    private async Task<bool> ObjectExistsAsync(IMinioClient client, string bucket, string objectName)
    {
        try
        {
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithCallbackStream(stream => { });
            await client.GetObjectAsync(getObjectArgs);

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    [TestMethod]
    public async Task PresignedGetObject()
    {
        // todo how to test this with mock client.
        var client = new MinioClient()
            .WithEndpoint("play.min.io")
            .WithCredentials("Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG")
            .Build();

        var bucket = "bucket";
        var objectName = "object-name";

        var bktExistArgs = new BucketExistsArgs()
            .WithBucket(bucket);
        var found = await client.BucketExistsAsync(bktExistArgs);
        if (!found)
        {
            var mkBktArgs = new MakeBucketArgs()
                .WithBucket(bucket);
            await client.MakeBucketAsync(mkBktArgs);
        }

        if (!await ObjectExistsAsync(client, bucket, objectName))
        {
            var helloData = Encoding.UTF8.GetBytes("hello world");
            var helloStream = new MemoryStream();
            helloStream.Write(helloData);
            helloStream.Seek(0, SeekOrigin.Begin);
            var PutObjectArgs = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(helloStream)
                .WithObjectSize(helloData.Length);
            await client.PutObjectAsync(PutObjectArgs);
        }

        var presignedGetObjectArgs = new PresignedGetObjectArgs()
            .WithBucket("bucket")
            .WithObject("object-name")
            .WithExpiry(3600)
            .WithRequestDate(_requestDate);

        var signedUrl = await client.PresignedGetObjectAsync(presignedGetObjectArgs);
        Assert.AreEqual(
            "http://play.min.io/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=Q3AM3UQ867SPQQA43P2F%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&X-Amz-Signature=d4202da690618f77142d6f0557c97839f0773b2c718082e745cd9b199aa6b28f",
            signedUrl);
    }

    [TestMethod]
    public async Task PresignedGetObjectWithHeaders()
    {
        // todo how to test this with mock client.
        var client = new MinioClient()
            .WithEndpoint("play.min.io")
            .WithCredentials("Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG")
            .Build();

        var bucket = "bucket";
        var objectName = "object-name";

        var reqParams = new Dictionary<string, string>
        {
            { "Response-Content-Disposition", "attachment; filename=\"filename.jpg\"" }
        };

        var bktExistArgs = new BucketExistsArgs()
            .WithBucket(bucket);
        var found = await client.BucketExistsAsync(bktExistArgs);
        if (!found)
        {
            var mkBktArgs = new MakeBucketArgs()
                .WithBucket(bucket);
            await client.MakeBucketAsync(mkBktArgs);
        }

        if (!await ObjectExistsAsync(client, bucket, objectName))
        {
            var helloData = Encoding.UTF8.GetBytes("hello world");
            var helloStream = new MemoryStream();
            helloStream.Write(helloData);
            var PutObjectArgs = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(helloStream)
                .WithObjectSize(helloData.Length);
            await client.PutObjectAsync(PutObjectArgs);
        }

        var presignedGetObjectArgs = new PresignedGetObjectArgs()
            .WithBucket("bucket")
            .WithObject("object-name")
            .WithExpiry(3600)
            .WithHeaders(reqParams)
            .WithRequestDate(_requestDate);

        var signedUrl = await client.PresignedGetObjectAsync(presignedGetObjectArgs);

        Assert.AreEqual(
            "http://play.min.io/bucket/object-name?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=Q3AM3UQ867SPQQA43P2F%2F20200501%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20200501T154533Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&response-content-disposition=attachment%3B%20filename%3D%22filename.jpg%22&X-Amz-Signature=de66f04dd4ac35838b9e83d669f7b5a70b452c6468e2b4a9e9c29f42e7fa102d",
            signedUrl);
    }
}