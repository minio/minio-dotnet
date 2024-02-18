# Newtera Client SDK for .NET  

Newtera Client SDK provides higher level APIs for Newtera TDM services.For a complete list of APIs and examples, please take a look at the [Dotnet Client API Reference](https://min.io/docs/newtera/linux/developers/dotnet/API.html).This document assumes that you have a working VisualStudio development environment.

## Install from NuGet
To install [Newtera .NET package](https://www.nuget.org/packages/Newtera/), run the following command in Nuget Package Manager Console.

```powershell
PM> Install-Package Newtera
```

## Newtera Client Example for ASP.NET

When using `AddNewtera` to add Newtera to your ServiceCollection, Newtera will also use any custom Logging providers you've added, like Serilog to output traces when enabled.

```cs
using Newtera;
using Newtera.DataModel.Args;

public static class Program
{
    var endpoint = "play.min.io";
    var accessKey = "Q3AM3UQ867SPQQA43P2F";
    var secretKey = "zuf+tfteSlswRu7BJ86wtrueekitnifILbZam1KYY3TG";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder();

        // Add Newtera using the default endpoint
        builder.Services.AddNewtera(accessKey, secretKey);

        // Add Newtera using the custom endpoint and configure additional settings for default NewteraClient initialization
        builder.Services.AddNewtera(configureClient => configureClient
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey));

        // NOTE: SSL and Build are called by the build-in services already.

        var app = builder.Build();
        app.Run();
    }
}

[ApiController]
public class ExampleController : ControllerBase
{
    private readonly INewteraClient newteraClient;

    public ExampleController(INewteraClient newteraClient)
    {
        this.newteraClient = newteraClient;
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUrl(string bucketID)
    {
        return Ok(await newteraClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(bucketID)
            .ConfigureAwait(false));
    }
}

[ApiController]
public class ExampleFactoryController : ControllerBase
{
    private readonly INewteraClientFactory newteraClientFactory;

    public ExampleFactoryController(INewteraClientFactory newteraClientFactory)
    {
        this.newteraClientFactory = newteraClientFactory;
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUrl(string bucketID)
    {
        var newteraClient = newteraClientFactory.CreateClient(); //Has optional argument to configure specifics

        return Ok(await newteraClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(bucketID)
            .ConfigureAwait(false));
    }
}

```

## Newtera Client Example
To connect to an Amazon S3 compatible cloud storage service, you need the following information

| Variable name | Description                                                  |
|:--------------|:-------------------------------------------------------------|
| endpoint      | \<Domain-name\> or \<ip:port\> of your object storage        |
| accessKey     | User ID that uniquely identifies your account                |
| secretKey     | Password to your account                                     |
| secure        | boolean value to enable/disable HTTPS support (default=true) |

The following examples uses a freely hosted public Newtera service "play.min.io" for development purposes.

```cs
using Newtera;

var endpoint = "play.min.io";
var accessKey = "Q3AM3UQ867trueSPQQA43P2F";
var secretKey = "zuf+tfteSlswRu7BJ86wtrueekitnifILbZam1KYY3TG";
var secure = true;
// Initialize the client with access credentials.
private static INewteraClient newtera = new NewteraClient()
                                    .WithEndpoint(endpoint)
                                    .WithCredentials(accessKey, secretKey)
                                    .WithSSL(secure)
                                    .Build();

// Create an async task for listing buckets.
var getListBucketsTask = await newtera.ListBucketsAsync().ConfigureAwait(false);

// Iterate over the list of buckets.
foreach (var bucket in getListBucketsTask.Result.Buckets)
{
    Console.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
}

```

## Complete _File Uploader_ Example

This example program connects to an object storage server, creates a bucket and uploads a file to the bucket.
To run the following example, click on [Link] and start the project
```cs
using System;
using Newtera;
using Newtera.Exceptions;
using Newtera.DataModel;
using Newtera.Credentials;
using Newtera.DataModel.Args;
using System.Threading.Tasks;

namespace FileUploader
{
    class FileUpload
    {
        static void Main(string[] args)
        {
            var endpoint  = "play.min.io";
            var accessKey = "Q3AM3UQ867SPQQA43P2F";
            var secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            try
            {
                var newtera = new NewteraClient()
                                    .WithEndpoint(endpoint)
                                    .WithCredentials(accessKey, secretKey)
                                    .WithSSL()
                                    .Build();
                FileUpload.Run(newtera).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }

        // File uploader task.
        private async static Task Run(NewteraClient newtera)
        {
            var bucketName = "mymusic";
            var location   = "us-east-1";
            var objectName = "golden-oldies.zip";
            var filePath = "C:\\Users\\username\\Downloads\\golden_oldies.mp3";
            var contentType = "application/zip";

            try
            {
                // Make a bucket on the server, if not already present.
                var beArgs = new BucketExistsArgs()
                    .WithBucket(bucketName);
                bool found = await newtera.BucketExistsAsync(beArgs).ConfigureAwait(false);
                if (found)
                {
                      // Upload a file to bucket.
                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithFileName(filePath)
                        .WithContentType(contentType);
                    await newtera.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                    Console.WriteLine("Successfully uploaded " + objectName );
                }
            }
            catch (NewteraException e)
            {
                Console.WriteLine("File Upload Error: {0}", e.Message);
            }
        }
    }
}
```

## Running Newtera Client Examples
### On Windows
* Clone this repository and open the Newtera.Sln in Visual Studio 2017.

* Enter your credentials and bucket name, object name etc. in Newtera.Examples/Program.cs
* Uncomment the example test cases such as below in Program.cs to run an example.

* Run the Newtera.Client.Examples project from Visual Studio

### On Linux
#### Setting .NET SDK on Linux (Ubuntu 22.04)
<blockquote> NOTE: newtera-dotnet requires .NET 6.x SDK to build on Linux. </blockquote>

* Install [.Net SDK](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#2204)

```
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

```
sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-6.0
```

#### Running Newtera.Examples
* Clone this project.

```
$ git clone https://github.com/newtera/newtera-dotnet && cd newtera-dotnet
```

* Enter your credentials and bucket name, object name etc. in Newtera.Examples/Program.cs
  Uncomment the example test cases such as below in Program.cs to run an example.

```
dotnet build --configuration Release --no-restore
dotnet pack ./Newtera/Newtera.csproj --no-build --configuration Release --output ./artifacts
dotnet test ./Newtera.Tests/Newtera.Tests.csproj
```

#### Bucket Operations

* [ListBuckets.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/ListBuckets.cs)
* [BucketExists.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/BucketExists.cs)
* [ListObjects.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/ListObjects.cs)

#### File Object Operations
* [FGetObject.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/FGetObject.cs)
* [FPutObject.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/FPutObject.cs)

#### Object Operations
* [GetObject.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/GetObject.cs)
* [GetPartialObject.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/GetPartialObject.cs)

* [PutObject.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/PutObject.cs)
* [StatObject.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/StatObject.cs)
* [RemoveObject.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/RemoveObject.cs)
* [RemoveObjects.cs](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Cases/RemoveObjects.cs)

#### Client Custom Settings
* [SetAppInfo](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Program.cs)
* [SetTraceOn](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Program.cs)
* [SetTraceOff](https://github.com/newtera/newtera-dotnet/blob/master/Newtera.Examples/Program.cs)

## Explore Further
* [Complete Documentation](https://min.io/docs/newtera/kubernetes/upstream/index.html)
* [Newtera .NET SDK API Reference](https://min.io/docs/newtera/linux/developers/dotnet/API.html)
