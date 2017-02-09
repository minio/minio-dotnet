
# Minio Client SDK for .NET  [![Slack](https://slack.minio.io/slack?type=svg)](https://slack.minio.io) [![Build Status](https://travis-ci.org/minio/minio-dotnet.svg?branch=master)](https://travis-ci.org/minio/minio-dotnet)

Minio Client SDK provides higher level APIs for Minio and Amazon S3 compatible cloud storage services. 

For a complete list of APIs and examples, please take a look at the [Dotnet Client API Reference](https://docs.minio.io/docs/dotnet-client-api-reference).

This document assumes that you have a working VisualStudio development environment.  

## Minimum Requirements
  .NET 4.5 or higher
  Visual Studio 10 or higher
  
## Install from NuGet

To install Minio .NET package, run the following command in Nuget Package Manager Console.
```powershell

PM> Install-Package Minio
```

## Minio Client Example
To connect to an Amazon S3 compatible cloud storage service, you will need to specify the following parameters.

| Parameter  | Description| 
| :---         |     :---     |
| endpoint   | URL to object storage service.   | 
| accessKeyID | Access key is the user ID that uniquely identifies your account. |   
| secretAccessKey | Secret key is the password to your account. |
| secure | Enable/Disable HTTPS support. |

The following examples uses a freely hosted public Minio service 'play.minio.io' for development purposes.
```cs
using Minio;

private static MinioClient minio = new MinioClient("play.minio.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                ).WithSSL();

// List buckets on the play.minio.io server
var getListBucketsTask = minio.Api.ListBucketsAsync();
Task.WaitAll(getListBucketsTask); // block while the task completes
var list = getListBucketsTask.Result;

foreach (Bucket bucket in list.Buckets)          
{                
    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDate.DateTime);
}

```

```cs
using Minio;

// Initialize the client with access credentials.
private static MinioClient minio = new MinioClient("play.minio.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                ).WithSSL();

// Create an async task for listing buckets.
var getListBucketsTask = minio.Api.ListBucketsAsync();

// Iterate over the list of buckets.
foreach (Bucket bucket in getListBucketsTask.Result.Buckets)          
{                
    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDate.DateTime);
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
            var endpoint  = "play.minio.io:9000";
            var accessKey = "Q3AM3UQ867SPQQA43P2F";
            var secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            try
            { 
                var minio = new MinioClient(endpoint, accessKey, secretKey).WithSSL();
                FileUpload.Run(minio).Wait();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }
        
        // File uploader task.
        private async static Task Run(MinioClient minio)
        {
            var bucketName = "mymusic";
            var location   = "us-east-1";
            var objectName = "golden-oldies.zip";
            var filePath = "/tmp/golden-oldies.zip";
            var contentType = "application/zip";

            try
            {
                // Make a bucket on the server.
                await minio.Api.MakeBucketAsync(bucketName, location);
                
                // Upload a file to bucket.
                await minio.Api.PutObjectAsync(bucketName, objectName, filePath, contentType);  
                
                Console.Out.WriteLine("Successfully uploaded " + objectName);
            }
            catch (MinioException e)
            {
                Console.WriteLine("File Upload Error: {0}", e.Message);
            }
        }
    }
}
```


## Running Minio Client Examples

* Download from Github.. Build Minio solution in Visual Studio

* Move into Minio.Examples directory and run the project. Uncomment cases that you want to run 
 in Program.cs to play with it.
#### Bucket Operations

* [MakeBucket.cs](./Minio.Examples/Cases/MakeBucket.cs)
* [ListBuckets.cs](./Minio.Examples/Cases/ListBuckets.cs)
* [BucketExists.cs](./Minio.Examples/Cases/BucketExists.cs)
* [RemoveBucket.cs](./Minio.Examples/Cases/RemoveBucket.cs)
* [Listobjects.cs](./Minio.Examples/Cases/Listobjects.cs)
* [ListIncompleteUploads.cs](./Minio.Examples/Cases/ListIncompleteUploads.cs)

#### Bucket policy Operations
* [GetPolicy.cs](./Minio.Examples/Cases/GetPolicy.cs)
* [SetPolicy.cs](./Minio.Examples/Cases/SetPolicy.cs)

#### File Object Operations
* [FGetObject.cs](./Minio.Examples/Cases/FGetObject.cs)
* [FPutObject.cs](./Minio.Examples/Cases/FPutObject.cs)

#### Object Operations
* [GetObject.cs](./Minio.Examples/Cases/GetObject.cs)
* [PutObject.cs](./Minio.Examples/Cases/PutObject.cs)
* [StatObject.cs](./Minio.Examples/Cases/StatObject.cs)
* [RemoveObject.cs](./Minio.Examples/Cases/RemoveObject.cs)
* [CopyObject.cs](./Minio.Examples/Cases/CopyObject.cs)
* [RemoveIncompleteUpload.cs](./Minio.Examples/Cases/RemoveIncompleteUpload.cs)

#### Presigned Operations
* [PresignedGetObject.cs](./Minio.Examples/Cases/PresignedGetObject.cs)
* [PresignedPutObject.cs](./Minio.Examples/Cases/PresignedPutObject.cs)
* [PresignedPostPolicy.cs](./Minio.Examples/Cases/PresignedPostPolicy.cs)

#### Client Custom Settings
* [SetAppInfo](./Minio.Examples/Program.cs)
* [SetTraceOn](./Minio.Examples/Program.cs)
* [SetTraceOff](./Minio.Examples/Program.cs)




## Explore Further
* [Complete Documentation](https://docs.minio.io)

## Contribute

[Contributors Guide](https://github.com/minio/minio-go/blob/master/CONTRIBUTING.md)

