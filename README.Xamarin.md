## Minio.Xamarin

[![NuGet](https://img.shields.io/nuget/v/nlog.svg)](https://nuget.com)
[![Build Status](https://travis-ci.org/kvandake/minio-dotnet.svg?branch=xamarin)](https://travis-ci.org/kvandake/minio-dotnet)

## Where can I use it?
Minio is currently compatible with:

* Xamarin.iOS / Xamarin.Mac
* Xamarin.Android
* ~~.NET 4.5~~
* ~~Windows 10 (Universal Windows Platform)~~

## Setup
To use, simply reference the nuget package in each of your platform projects!

## Getting Started
To connect to an Amazon S3 compatible cloud storage service, you will need to specify the following parameters.

| Parameter  | Description| 
| :---         |     :---     |
| endpoint   | URL to object storage service.   | 
| accessKey | Access key is the user ID that uniquely identifies your account. |   
| secretKey | Secret key is the password to your account. |
| secure | Enable/Disable HTTPS support. |

The following examples uses a freely hosted public Minio service [play.minio.io](https://play.minio.io) for development purposes.

```cs
using Minio;

// Initialize the client with access credentials.
var minio = MinioClient.Create(
            "play.minio.io:9000",
            "Q3AM3UQ867SPQQA43P2F",
            "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
            ).WithSSL();

// Create an async task for listing buckets.
var getListBucketsTask = await minio.ListBucketsAsync();

// Iterate over the list of buckets.
foreach (Bucket bucket in getListBucketsTask.Buckets)          
{                
    Console.WriteLine(bucket.Name + " " + bucket.CreationDate.DateTime);
}
```

### MvvmCross
``` cs
// from your PCL App.cs
Mvx.RegisterSingleton<IMinioClient>(() => MinioClient.Create(endpoint, accessKey, secretKey));
```

### ModernHttpClient
You can connect the [ModernHttpClient](https://github.com/paulcbetts/ModernHttpClient) to the Minio.

``` cs
var minioSettings = new MinioSettings(endpoint, accessKey, secretKey)
{
	CreateHttpClientHandlerFunc = () => new ModernHttpClient.NativeMessageHandler()
};
var minio = MinioClient.Create(minioSettings));
```

## Basic Method Documentation
```cs
// Create a private bucket with the given name.
Task MakeBucketAsync(string bucketName, string location = "us-east-1", CancellationToken cancellationToken = default(CancellationToken));

// List all objects in a bucket
Task<ListAllMyBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default(CancellationToken));

// Returns true if the specified bucketName exists, otherwise returns false.
Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken));

// Remove a bucket
Task RemoveBucketAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken));

// List all objects non-recursively in a bucket with a given prefix, optionally emulating a directory
Task<IList<Item>> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = true, CancellationToken cancellationToken = default(CancellationToken));

// Get an object. The object will be streamed to the callback given by the user.
Task<byte[]> GetObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken));

// Get an object. The object will be streamed to the callback given by the user.
Task<byte[]> GetObjectAsync(string bucketName, string objectName, long offset, long length, CancellationToken cancellationToken = default(CancellationToken));

// Creates an object from file input stream
Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType = null, Dictionary<string, string> metaData = null,CancellationToken cancellationToken = default(CancellationToken));

// Creates an object from file input stream
Task PutObjectAsync(string bucketName, string objectName, Stream data, string contentType = null, Dictionary<string, string> metaData = null,CancellationToken cancellationToken = default(CancellationToken));
        
// Removes an object with given name in specific bucket
Task RemoveObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken));

// Lists all incomplete uploads in a given bucket and prefix recursively
Task<IList<Upload>> ListIncompleteUploads(string bucketName, string prefix, bool recursive, CancellationToken cancellationToken = default(CancellationToken));

// Remove incomplete uploads from a given bucket and objectName
Task RemoveIncompleteUploadAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken));

// Copy a source object into a new destination object.
Task CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, CancellationToken cancellationToken = default(CancellationToken));

// Presigned Get url.
Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt);

// Presigned Put url.
Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt);
```

### TODO
* Fix method [SetPolicyAsync](https://github.com/kvandake/minio-dotnet/blob/xamarin/Xamarin/Minio.Tests/Tests/SetBucketPolicyTests.cs#L26)
