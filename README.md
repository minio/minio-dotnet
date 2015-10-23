# Minio .NET Library for Amazon S3 Compatible Cloud Storage [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/Minio/minio?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Install from NuGet [![Build Status](https://travis-ci.org/minio/minio-dotnet.svg?branch=master)](https://travis-ci.org/minio/minio-dotnet)

```powershell
To install Minio .NET package, run the following command in Nuget Package Manager Console

PM> Install-Package Minio
```

## Example
```cs
using Minio;

private static MinioClient client = new MinioClient("https://s3.amazonaws.com", "Access Key", "Secret Key");

var buckets = client.ListBuckets();
foreach (Bucket bucket in buckets)
{
    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDate);
}

```

### Additional Examples

#### Bucket Operations

* [ListBuckets.cs](./Minio.Examples/ListBuckets.cs)
* [MakeBucket.cs](./Minio.Examples/MakeBucket.cs)
* [RemoveBucket.cs](./Minio.Examples/RemoveBucket.cs)
* [BucketExists.cs](./Minio.Examples/BucketExists.cs)
* [ListObjects.cs](./Minio.Examples/ListObjects.cs)

#### Object Operations

* [PutObject.cs](./Minio.Examples/PutObject.cs)
* [GetObject.cs](./Minio.Examples/GetObject.cs)
* [GetPartialObject.cs](./Minio.Examples/GetPartialObject.cs)
* [RemoveObject.cs](./Minio.Examples/RemoveObject.cs)
* [StatObject.cs](./Minio.Examples/StatObject.cs)
* [RemoveIncompleteUpload.cs](./Minio.Examples/RemoveIncompleteUpload.cs)

### How to run these examples?

#### On Linux

Simply edit the example .Net program to include your access credentials and follow the steps below.

```bash
$ sudo apt-get install mono
$ git clone https://github.com/minio/minio-dotnet && cd minio-dotnet
$ wget http://www.nuget.org/nuget.exe
$ mono nuget.exe restore
$ xbuild
[edit Minio.Examples/ListBuckets.cs]
$ mcs /r:Minio/bin/Debug/Minio.dll Minio.Examples/ListBuckets.cs
$ export MONO_PATH=Minio/bin/Debug
$ mono Minio.Examples/ListBuckets.exe
....

```

## Contribute

[Contributors Guide](./CONTRIBUTING.md)
