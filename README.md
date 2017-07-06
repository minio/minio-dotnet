# Minio Client SDK for .NET  [![Slack](https://slack.minio.io/slack?type=svg)](https://slack.minio.io) [![Build status](https://ci.appveyor.com/api/projects/status/tvdpoypdmbuwg0me/branch/master?svg=true)](https://ci.appveyor.com/project/Harshavardhana/minio-dotnet/branch/master)
 
Minio Client SDK provides higher level APIs for Minio and Amazon S3 compatible cloud storage services.For a complete list of APIs and examples, please take a look at the [Dotnet Client API Reference](https://docs.minio.io/docs/dotnet-client-api-reference).This document assumes that you have a working VisualStudio development environment.  

## Minimum Requirements
 * .NET 4.5.2, .NetStandard1.6 or higher
 * Visual Studio 2017 RC 
  
## Install from NuGet

To install Minio .NET package for .NET Framework, run the following command in Nuget Package Manager Console.
```powershell
PM> Install-Package Minio 
```
To install Minio .NET package for .NetCore, run the following command in Nuget Package Manager Console.
```powershell
PM> Install-Package Minio.NetCore
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
* Clone this repository.

* Build the project in Visual Studio to produce the Minio.Examples console app.

* Move into Minio.Examples directory and enter your credentials and bucket name, object name etc.
  Uncomment the example test cases such as below in Program.cs to run an example.
```cs
  //Cases.MakeBucket.Run(minioClient, bucketName).Wait();
```
* Run the Minio.Client.Examples.NET452 or Minio.Client.Examples.NetCore project from Visual Studio
#### On Linux (Ubuntu 16.04 and above)
* Clone this repository.

* Move into Minio.Examples directory and enter your credentials and bucket name, object name etc.
  Uncomment the example test cases such as below in Program.cs to run an example.
```cs
  //Cases.MakeBucket.Run(minioClient, bucketName).Wait();
```
<blockquote> NOTE: minio-dotnet requires mono 5.0.1 stable release and .NET Core 1.0 SDK to build on Linux. </blockquote>
```
bash $ git clone https://github.com/minio/minio-dotnet && cd minio-dotnet 
$ sudo sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ yakkety main" > /etc/apt/sources.list.d/dotnetdev.list' 
$ sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 417A0893
$ sudo apt-get update
$ sudo apt-get install dotnet-dev-1.0.4
$ sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
$ echo "deb http://download.mono-project.com/repo/ubuntu xenial main" | sudo tee /etc/apt/sources.list.d/mono-official.list
$ sudo apt-get update
$ sudo apt-get install mono-complete 
$ sudo apt-get install ca-certificates-mono
$ sudo apt-get install mono-xsp4

$ msbuild /t:Clean 
$ msbuild /p:Configuration=.net4.5.2    # To compile .NET4.5.2 projects in the solution.
$ dotnet msbuild /p:Configuration=.netcore  # To compile .NetCore projects in the solution.
To run .NET4.5.2 example,
$ ./Minio.Examples/Minio.Client.Examples.Net452/bin/Debug/Minio.Client.Examples.Net452.exe 
To run .NetCore example,
$ cd Minio.Examples/Minio.Client.Examples.Core
$ dotnet restore
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
