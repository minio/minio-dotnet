# 适用于与Amazon S3兼容的云存储的MinIO .NET SDK  [![Slack](https://slack.min.io/slack?type=svg)](https://slack.min.io) [![Build status](https://ci.appveyor.com/api/projects/status/tvdpoypdmbuwg0me/branch/master?svg=true)](https://ci.appveyor.com/project/Harshavardhana/minio-dotnet/branch/master)

MinIO .NET Client SDK提供了简单的API来访问MinIO以及任何与Amazon S3兼容的对象存储服务。有关API和示例的完整列表，请查看[Dotnet Client API Reference](https://docs.min.io/docs/dotnet-client-api-reference)文档。本文假设你已经有VisualStudio开发环境。

## 最低需求
 * .NET 4.5.2，.NetStandard2.0或更高版本
 * Visual Studio 2017

## 使用NuGet安装

为了安装.NET Framework的MinIO .NET包，你可以在Nuget Package Manager控制台运行下面的命令。
```powershell
PM> Install-Package Minio
```
## MinIO Client示例
MinIO client需要以下4个参数来连接与Amazon S3兼容的对象存储服务。

| 参数  | 描述|
| :---         |     :---     |
| endpoint   | 对象存储服务的URL   |
| accessKey | Access key是唯一标识你的账户的用户ID。 |
| secretKey | Secret key是你账户的密码。 |
| secure | true代表使用HTTPS。 |

下面示例中使用运行在 [https://play.min.io:9000](https://play.min.io:9000) 上的MinIO服务，你可以用这个服务来开发和测试。示例中的访问凭据是公开的。

```cs
using Minio;

// Initialize the client with access credentials.
private static MinioClient minio = new MinioClient("play.min.io:9000",
                "Q3AM3UQ867SPQQA43P2F",
                "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                ).WithSSL();

// Create an async task for listing buckets.
var getListBucketsTask = minio.ListBucketsAsync();

// Iterate over the list of buckets.
foreach (Bucket bucket in getListBucketsTask.Result.Buckets)
{
    Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
}

```
## 完整的_File Uploader_示例

本示例程序连接到一个对象存储服务，创建一个存储桶，并且上传一个文件到该存储桶中。为了运行下面的示例，请点击[Link]启动该项目。

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
            var endpoint  = "play.min.io:9000";
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

## 运行MinIO Client示例
####  Windows
* clone这个项目，并在Visual Studio 2017中打开Minio.Sln。
```
$ git clone https://github.com/minio/minio-dotnet && cd minio-dotnet
```
* 在Minio.Examples/Program.cs中输入你的认证信息、存储桶名称、对象名称等。
  在Program.cs中取消注释以下类似的测试用例来运行示例。
```cs
  //Cases.MakeBucket.Run(minioClient, bucketName).Wait();
```
* 从Visual Studio运行Minio.Client.Examples或
#### Linux (Ubuntu 16.04)

##### 在Linux上设置Mono和.NETCore
<blockquote> 注意：minio-dotnet需要mono 5.0.1稳定版本和.NET Core 2.0 SDK。</blockquote>

* 为你的发行版发装[.NETCore](https://www.microsoft.com/net/core#linuxredhat)和[Mono](http://www.mono-project.com/download/#download-lin) 。请参阅示例脚本Ubuntu Xenial [mono_install.sh](https://github.com/minio/minio-dotnet/blob/master/mono_install.sh)安装.NETCore和Mono。

```
$ ./mono_install.sh
```
##### 运行Minio.Examples
```
$ cd Minio.Examples
$ dotnet build -c Release 
$ dotnet run
```
#### 操作存储桶

* [MakeBucket.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/MakeBucket.cs)
* [ListBuckets.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListBuckets.cs)
* [BucketExists.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/BucketExists.cs)
* [RemoveBucket.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveBucket.cs)
* [ListObjects.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListObjects.cs)
* [ListIncompleteUploads.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/ListIncompleteUploads.cs)

#### 存储桶策略
* [GetPolicy.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetBucketPolicy.cs)
* [SetPolicy.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/SetBucketPolicy.cs)

#### 存储桶通知
* [GetBucketNotification.cs](./Minio.Examples/Cases/GetBucketNotification.cs)
* [SetBucketNotification.cs](./Minio.Examples/Cases/SetBucketNotification.cs)
* [RemoveAllBucketNotifications.cs](./Minio.Examples/Cases/RemoveAllBucketNotifications.cs)

#### 操作文件对象
* [FGetObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/FGetObject.cs)
* [FPutObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/FPutObject.cs)

#### 操作对象
* [GetObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetObject.cs)
* [GetPartialObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/GetPartialObject.cs)
* [PutObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/PutObject.cs)
* [StatObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/StatObject.cs)
* [RemoveObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveObject.cs)
* [RemoveObjects.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveObjects.cs)
* [CopyObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/CopyObject.cs)
* [RemoveIncompleteUpload.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/RemoveIncompleteUpload.cs)

#### Presigned操作
* [PresignedGetObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/PresignedGetObject.cs)
* [PresignedPutObject.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/PresignedPutObject.cs)
* [PresignedPostPolicy.cs](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Cases/PresignedPostPolicy.cs)

#### 客户端自定义设置
* [SetAppInfo](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Program.cs)
* [SetTraceOn](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Program.cs)
* [SetTraceOff](https://github.com/minio/minio-dotnet/blob/master/Minio.Examples/Program.cs)

## 了解更多
* [完整文档](https://docs.min.io)
* [MinIO .NET SDK API文档](https://docs.min.io/docs/dotnet-client-api-reference)
