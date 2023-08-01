# MinIO Client SDK for .NET  

MinIO Client SDK provides higher level APIs for MinIO and Amazon S3 compatible cloud storage services.For a complete list of APIs and examples, please take a look at the [Dotnet Client API Reference](https://min.io/docs/minio/linux/developers/dotnet/API.html).This document assumes that you have a working VisualStudio development environment.

[![Slack](https://slack.min.io/slack?type=svg)](https://slack.min.io) [![Github Actions](https://github.com/minio/minio-dotnet/actions/workflows/minio-dotnet.yml/badge.svg)](https://github.com/minio/minio-dotnet/actions) [![Nuget](https://img.shields.io/nuget/dt/Minio?logo=nuget&label=nuget&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FMinio)](https://www.nuget.org/packages/Minio/) [![GitHub tag (with filter)](https://img.shields.io/github/v/tag/minio/minio-dotnet?label=latest%20release)](https://github.com/minio/minio-dotnet/releases)

## Install from NuGet
To install [MinIO .NET package](https://www.nuget.org/packages/Minio/), run the following command in Nuget Package Manager Console.

```powershell
PM> Install-Package Minio
```
## MinIO Client Example
To connect to an Amazon S3 compatible cloud storage service, you need the following information

| Variable name | Description                                                  |
|:--------------|:-------------------------------------------------------------|
| endpoint      | \<Domain-name\> or \<ip:port\> of your object storage        |
| accessKey     | User ID that uniquely identifies your account                |
| secretKey     | Password to your account                                     |
| secure        | boolean value to enable/disable HTTPS support (default=true) |

The following examples uses a freely hosted public MinIO service "play.min.io" for development purposes.

```cs
using Minio;

var endpoint = "play.min.io";
var accessKey = "Q3AM3UQ867trueSPQQA43P2F";
var secretKey = "zuf+tfteSlswRu7BJ86wtrueekitnifILbZam1KYY3TG";
var secure = true;
// Initialize the client with access credentials.
private static MinioClient minio = new MinioClient()
                                    .WithEndpoint(endpoint)
                                    .WithCredentials(accessKey, secretKey)
                                    .WithSSL(secure)
                                    .Build();

// Create an async task for listing buckets.
var getListBucketsTask = await minio.ListBucketsAsync().ConfigureAwait(false);

// Iterate over the list of buckets.
foreach (var bucket in getListBucketsTask.Result.Buckets)
{
    Console.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
}

```
## Complete _File Uploader_ Example

This example program connects to an object storage server, creates a bucket and uploads a file to the bucket.
To run the following example, click on [Link] and start the project
```cs
using System;
using Minio;
using Minio.Exceptions;
using Minio.DataModel;
using System.Threading.Tasks;

namespace FileUploader
{
    class FileUpload
    {
        static void Main(string[] args)
        {
            var endpoint  = "play.min.io";
            var accessKey = "Q3AM3UQ867SPQQA43P2F";
            var secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            try
            {
                var minio = new MinioClient()
                                    .WithEndpoint(endpoint)
                                    .WithCredentials(accessKey, secretKey)
                                    .WithSSL()
                                    .Build();
                FileUpload.Run(minio).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }

        // File uploader task.
        private async static Task Run(MinioClient minio)
        {
            var bucketName = "mymusic";
            var location   = "us-east-1";
            var objectName = "golden-oldies.zip";
            var filePath = "C:\\Users\\username\\Downloads\\golden_oldies.mp3";
            var contentType = "application/zip";

            try
            {
                // Make a bucket on the server, if not already present.
                var beArgs = new BucketExistsArgs()
                    .WithBucket(bucketName);
                bool found = await minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
                if (!found)
                {
                    var mbArgs = new MakeBucketArgs()
                        .WithBucket(bucketName);
                    await minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
                }
                // Upload a file to bucket.
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithFileName(filePath)
                    .WithContentType(contentType);
                await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                Console.WriteLine("Successfully uploaded " + objectName );
            }
            catch (MinioException e)
            {
                Console.WriteLine("File Upload Error: {0}", e.Message);
            }
        }
    }
}
```

## Running MinIO Client Examples
### On Windows
* Clone this repository and open the Minio.Sln in Visual Studio 2017.

* Enter your credentials and bucket name, object name etc. in Minio.Examples/Program.cs
* Uncomment the example test cases such as below in Program.cs to run an example.
```cs
  //Cases.MakeBucket.Run(minioClient, bucketName).Wait();
```
* Run the Minio.Client.Examples project from Visual Studio

### On Linux
#### Setting .NET SDK on Linux (Ubuntu 22.04)
<blockquote> NOTE: minio-dotnet requires .NET 6.x SDK to build on Linux. </blockquote>

* Install [.Net SDK](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#2204)

```
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

```
sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-6.0
```

#### Running Minio.Examples
* Clone this project.

```
$ git clone https://github.com/minio/minio-dotnet && cd minio-dotnet
```

* Enter your credentials and bucket name, object name etc. in Minio.Examples/Program.cs
  Uncomment the example test cases such as below in Program.cs to run an example.
```cs
  //Cases.MakeBucket.Run(minioClient, bucketName).Wait();
```

```
dotnet build --configuration Release --no-restore
dotnet pack ./Minio/Minio.csproj --no-build --configuration Release --output ./artifacts
dotnet test ./Minio.Tests/Minio.Tests.csproj
```

#### Bucket Operations

* [MakeBucket.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/MakeBucket.cs)
* [ListBuckets.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListBuckets.cs)
* [BucketExists.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/BucketExists.cs)
* [RemoveBucket.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveBucket.cs)
* [ListObjects.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListObjects.cs)
* [ListIncompleteUploads.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListIncompleteUploads.cs)
* [ListenBucketNotifications.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListenBucketNotifications.cs)

#### Bucket policy Operations
* [GetBucketPolicy.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetBucketPolicy.cs)
* [SetBucketPolicy.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/SetBucketPolicy.cs)

#### Bucket notification Operations
* [GetBucketNotification.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetBucketNotification.cs)
* [SetBucketNotification.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/SetBucketNotification.cs)
* [RemoveAllBucketNotifications.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveAllBucketNotifications.cs)

#### File Object Operations
* [FGetObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/FGetObject.cs)
* [FPutObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/FPutObject.cs)

#### Object Operations
* [GetObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetObject.cs)
* [GetPartialObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetPartialObject.cs)
* [SelectObjectContent.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/SelectObjectContent.cs)

* [PutObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/PutObject.cs)
* [StatObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/StatObject.cs)
* [RemoveObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveObject.cs)
* [RemoveObjects.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveObjects.cs)
* [CopyObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/CopyObject.cs)
* [CopyObjectMetadata.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/CopyObjectMetadata.cs)
* [RemoveIncompleteUpload.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveIncompleteUpload.cs)

#### Presigned Operations
* [PresignedGetObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/PresignedGetObject.cs)
* [PresignedPutObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/PresignedPutObject.cs)
* [PresignedPostPolicy.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/PresignedPostPolicy.cs)

#### Client Custom Settings
* [SetAppInfo](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Program.cs)
* [SetTraceOn](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Program.cs)
* [SetTraceOff](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Program.cs)

## Explore Further
* [Complete Documentation](https://min.io/docs/minio/kubernetes/upstream/index.html)
* [MinIO .NET SDK API Reference](https://min.io/docs/minio/linux/developers/dotnet/API.html)
