using Microsoft.AspNetCore.Mvc;
using Minio;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddScoped<MinioClient>((sp) => {

    string endPoint = "MINIO_HOST";
    int port = 443;
    string accessKey = "MINIO_ACCESSKEY";
    string secretKey = "MINIO_SECRET_KEY";

    IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();

    MinioClient minioClient = new MinioClient(httpClient)
                                        .WithEndpoint(endPoint, port)
                                        .WithCredentials(accessKey, secretKey)
                                        .WithSSL()
                                        .Build();

    return minioClient;
});
var app = builder.Build();


app.MapGet("/", () => "Hello World!");
app.MapGet("/test-bucket-exists/{bucketName}", async ([FromServices] MinioClient minio, [FromRoute] string bucketName) => 
{
        try
            {
                Console.WriteLine("Running example for API: BucketExistsAsync");
                BucketExistsArgs args = new BucketExistsArgs()
                                                    .WithBucket(bucketName);
                bool found = await minio.BucketExistsAsync(args);
                Console.WriteLine((found ? "Found" : "Couldn't find ") + "bucket " + bucketName);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }finally 
            {
                minio.Dispose();
            }

});

app.Run();
