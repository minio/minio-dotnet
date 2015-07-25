# Minio .NET Library for Amazon S3 compatible cloud storage [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/Minio/minio?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Install from NuGet [![Build Status](https://travis-ci.org/minio/minio-dotnet.svg?branch=master)](https://travis-ci.org/minio/minio-dotnet)

```powershell
To install Json.NET, run the following command in the Package Manager Console

PM> Install-Package Minio
```

## Example
```cs
using Minio;

private static Client client = Client.Create("https://s3-us-west-2.amazonaws.com", "Access Key", "Secret Key");

var buckets = client.ListBuckets();
foreach (Bucket bucket in buckets)
{
    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDate);
}

```

### Additional Examples

* [ExamplePutObject.cs](./Minio.Tests/Examples/ExamplePutObject.cs)
* [ExampleGetObject.cs](./Minio.Tests/Examples/ExampleGetObject.cs)
* [ExampleGetPartialObject.cs](./Minio.Tests/Examples/ExampleGetPartialObject.cs)
* [ExampleListBuckets.cs](./Minio.Tests/Examples/ExampleListBuckets.cs)
* [ExampleListObjects.cs](./Minio.Tests/Examples/ExampleListObjects.cs)
* [ExampleMakeBucket.cs](./Minio.Tests/Examples/ExampleMakeBucket.cs)
* [ExampleRemoveBucket.cs](./Minio.Tests/Examples/ExampleRemoveBucket.cs)

## Contribute

[Contributors Guide](./CONTRIBUTING.md)
