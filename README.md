# Minio Client SDK for .NET  [![Slack](https://slack.minio.io/slack?type=svg)](https://slack.minio.io) [![Build status](https://ci.appveyor.com/api/projects/status/tvdpoypdmbuwg0me/branch/master?svg=true)](https://ci.appveyor.com/project/Harshavardhana/minio-dotnet/branch/master)

Minio Client SDK provides higher level APIs for Minio and Amazon S3 compatible cloud storage services.For a complete list of APIs and examples, please take a look at the [Dotnet Client API Reference](https://docs.minio.io/docs/dotnet-client-api-reference).This document assumes that you have a working VisualStudio development environment.

## Minimum Requirements
 * .NET 4.6, .NetStandard 2.0 or higher
 * Visual Studio 2017

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
| accessKey | Access key is the user ID that uniquely identifies your account. |
| secretKey | Secret key is the password to your account. |
| secure | Enable/Disable HTTPS support. |

The following examples uses a freely hosted public Minio service 'play.minio.io' for development purposes.

```cs
using Minio;

// Initialize the client with access credentials.
private static MinioClient minio = new MinioClient("play.minio.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                ).WithSSL();

// Create an async task for listing buckets.
var getListBucketsTask = minio.ListBucketsAsync();

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
            var filePath = "C:\\Users\\username\\Downloads\\golden_oldies.mp3";
            var contentType = "application/zip";

            try
            {
                // Make a bucket on the server, if not already present.
                bool found = await minio.BucketExistsAsync(bucketName);
                if (!found)
                {
                    await minio.MakeBucketAsync(bucketName, location);
                }
                // Upload a file to bucket.
                await minio.PutObjectAsync(bucketName, objectName, filePath, contentType);
                Console.Out.WriteLine("Successfully uploaded " + objectName );
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
#### On Windows
* Clone this repository and open the Minio.Sln in Visual Studio 2017.

* Enter your credentials and bucket name, object name etc.in Minio.Examples/Program.cs
  Uncomment the example test cases such as below in Program.cs to run an example.
```cs
  //Cases.MakeBucket.Run(minioClient, bucketName).Wait();
```
* Run the Minio.Client.Examples project from Visual Studio
#### On Linux (Ubuntu 16.04)

##### Setting up .NETCore on Linux
<blockquote> NOTE: minio-dotnet requires .NET Core 2.0 SDK to build on Linux. </blockquote>

* Install [.NETCore](https://www.microsoft.com/net/core#linuxredhat)) for your distro. See sample script  to install .NETCore for Ubuntu Xenial [netcore_install_linux.sh](https://github.com/minio/minio-dotnet/blob/master/netcore_install_linux.sh)

```
$ ./netcore_install_linux.sh
```
##### Running Minio.Examples
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
$ cd Minio.Examples
$ dotnet build -c Release
$ dotnet run
```
#### Bucket Operations

* [MakeBucket.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/MakeBucket.cs)
* [ListBuckets.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListBuckets.cs)
* [BucketExists.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/BucketExists.cs)
* [RemoveBucket.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveBucket.cs)
* [ListObjects.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListObjects.cs)
* [ListIncompleteUploads.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListIncompleteUploads.cs)

#### Bucket policy Operations
* [GetPolicy.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetBucketPolicy.cs)
* [SetPolicy.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/SetBucketPolicy.cs)

#### Bucket notification Operations
* [GetBucketNotification.cs](./Minio.Examples/Cases/GetBucketNotification.cs)
* [SetBucketNotification.cs](./Minio.Examples/Cases/SetBucketNotification.cs)
* [RemoveAllBucketNotifications.cs](./Minio.Examples/Cases/RemoveAllBucketNotifications.cs)

#### File Object Operations
* [FGetObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/FGetObject.cs)
* [FPutObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/FPutObject.cs)

#### Object Operations
* [GetObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetObject.cs)
* [GetPartialObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetPartialObject.cs)
* [PutObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/PutObject.cs)
* [StatObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/StatObject.cs)
* [RemoveObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveObject.cs)
* [RemoveObjects.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveObjects.cs)
* [CopyObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/CopyObject.cs)
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
* [Complete Documentation](https://docs.minio.io)
* [Minio .NET SDK API Reference](https://docs.minio.io/docs/dotnet-client-api-reference)
