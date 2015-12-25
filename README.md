B0;95;c# Minio .NET Library for Amazon S3 Compatible Cloud Storage [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/Minio/minio?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

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
* [ListIncompleteUploads.cs](./Minio.Examples/ListIncompleteUploads.cs)

#### Object Operations

* [PutObject.cs](./Minio.Examples/PutObject.cs)
* [GetObject.cs](./Minio.Examples/GetObject.cs)
* [GetPartialObject.cs](./Minio.Examples/GetPartialObject.cs)
* [RemoveObject.cs](./Minio.Examples/RemoveObject.cs)
* [StatObject.cs](./Minio.Examples/StatObject.cs)
* [RemoveIncompleteUpload.cs](./Minio.Examples/RemoveIncompleteUpload.cs)

#### Pesigned Operations

* [PresignedGetObject.cs](./Minio.Examples/PresignedGetObject.cs)
* [PresignedPutObject.cs](./Minio.Examples/PresignedPutObject.cs)
* [PresignedPostPolicy.cs](./Minio.Examples/PresignedPostPolicy.cs)

### How to run these examples?

#### On Linux (Ubuntu 14.04)

Simply edit the example .Net program to include your access credentials and follow the steps below.

<blockquote>
NOTE: minio-dotnet requires mono 4.2 stable release to build on Linux.
</blockquote>

```bash
$ git clone https://github.com/minio/minio-dotnet && cd minio-dotnet
$ sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
$ echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
$ sudo apt-get update
$ sudo apt-get install mono-xbuild mono-complete
$ sudo mozroots --import --machine --sync 
$ sudo certmgr -ssl -m https://go.microsoft.com
$ sudo certmgr -ssl -m https://nugetgallery.blob.core.windows.net
$ sudo certmgr -ssl -m https://nuget.org
$ wget https://www.nuget.org/nuget.exe
$ mono nuget.exe update -self
$ mono nuget.exe restore
$ xbuild /t:Clean
$ xbuild /t:Rebuild /p:Configuration=Release
[edit Minio.Examples/ListBuckets.cs]
$ mcs /r:Minio/bin/Release/Minio.dll Minio.Examples/ListBuckets.cs
$ export MONO_PATH=Minio/bin/Release
$ mono Minio.Examples/ListBuckets.exe
....

```

## Contribute

[Contributors Guide](./CONTRIBUTING.md)
