# Minimal object storage library for the .NET Platform [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/Minio/minio?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Install from NuGet

```powershell
To install Json.NET, run the following command in the Package Manager Console

PM> Install-Package Minio
```

## Example
```cs
private static ObjectStorageClient client = ObjectStorageClient.GetClient("https://s3-us-west-2.amazonaws.com", "Access Key", "Secret Key");

var buckets = client.ListBuckets();
foreach (Bucket bucket in buckets)
{
    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDate);
}

```

### Additional Examples

### Additional Examples

* [ExamplePutObject.cs](./Minio.ClientTests/Examples/ExamplePutObject.cs)
* [ExampleGetObject.cs](./Minio.ClientTests/Examples/ExampleGetObject.cs)
* [ExampleGetPartialObject.cs](./Minio.ClientTests/Examples/ExampleGetPartialObject.cs)
* [ExampleListBuckets.cs](./Minio.ClientTests/Examples/ExampleListBuckets.cs)
* [ExampleListObjects.cs](./Minio.ClientTests/Examples/ExampleListObjects.cs)
* [ExampleMakeBucket.cs](./Minio.ClientTests/Examples/ExampleMakeBucket.cs)
* [ExampleRemoveBucket.cs](./Minio.ClientTests/Examples/ExampleRemoveBucket.cs)

## Contribute

[Contributors Guide](./CONTRIBUTING.md)
