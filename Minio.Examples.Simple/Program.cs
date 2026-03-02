using Minio;
using Minio.Model;

// Ensure that Minio is running:
//   docker run --rm -p 9000:9000 quay.io/minio/minio:latest server /data

// Create Minio client
var minioClient = new MinioClientBuilder("http://localhost:9000")
    .WithStaticCredentials("minioadmin", "minioadmin")
    .Build();

// Create the test-bucket (if it doesn't exist)
const string testBucket = "testbucket";
var hasBucket = await minioClient.BucketExistsAsync(testBucket).ConfigureAwait(false);
if (!hasBucket)
    await minioClient.CreateBucketAsync(testBucket).ConfigureAwait(false);

Console.WriteLine($"Bucket '{testBucket}' is ready.");
