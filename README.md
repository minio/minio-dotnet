# Minimal object storage library for the .NET Platform

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

TODO

## Join The Community
* Community hangout on Gitter    [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/Minio/minio?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
* Ask questions on Quora  [![Quora](http://upload.wikimedia.org/wikipedia/commons/thumb/5/57/Quora_logo.svg/55px-Quora_logo.svg.png)](http://www.quora.com/Minio)

## Contribute

[Contributors Guide](./CONTRIBUTING.md)
