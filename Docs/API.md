# .NET Client API Reference [![Slack](https://slack.min.io/slack?type=svg)](https://slack.min.io)

## Initialize MinIO Client object.

## MinIO

```cs
var minioClient = new MinioClient("play.min.io",
                                       "Q3AM3UQ867SPQQA43P2F",
                                       "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                                 ).WithSSL();
```

## AWS S3


```cs
var s3Client = new MinioClient("s3.amazonaws.com",
                                   "YOUR-ACCESSKEYID",
                                   "YOUR-SECRETACCESSKEY"
                               ).WithSSL();
```

| Bucket operations |  Object operations | Presigned operations  | Bucket Policy Operations
|:--- |:--- |:--- |:--- |
| [`makeBucket`](#makeBucket)  |[`getObject`](#getObject)   |[`presignedGetObject`](#presignedGetObject)   | [`getBucketPolicy`](#getBucketPolicy)   |
| [`listBuckets`](#listBuckets)  | [`putObject`](#putObject)  | [`presignedPutObject`](#presignedPutObject)  | [`setBucketPolicy`](#setBucketPolicy)   |
| [`bucketExists`](#bucketExists)  | [`copyObject`](#copyObject)  | [`presignedPostPolicy`](#presignedPostPolicy)  |[`setBucketNotification`](#setBucketNotification)  |
| [`removeBucket`](#removeBucket)  | [`statObject`](#statObject) |   | [`getBucketNotification`](#getBucketNotification)  |
| [`listObjects`](#listObjects)  | [`removeObject`](#removeObject) |   |  [`removeAllBucketNotification`](#removeAllBucketNotification) |
| [`listObjectVersions`](#listObjectVersions)  | [`removeObjects`](#removeObjects) |   |   |
| [`listIncompleteUploads`](#listIncompleteUploads) | [`removeIncompleteUpload`](#removeIncompleteUpload) |   |   |
| [`listenBucketNotifications`](#listenBucketNotifications)  | [`selectObjectContent`](#selectObjectContent) |   |   |
| [`setVersioning`](#setVersioning)  | [`setLegalHold`](#setLegalHold)  |   |   |
| [`getVersioning`](#getVersioning)  | [`getLegalHold`](#getLegalHold)  |   |   |
| [`setBucketEncryption`](#setBucketEncryption)  | [`setObjectTags`](#setObjectTags)  |   |   |
| [`getBucketEncryption`](#getBucketEncryption)  | [`getObjectTags`](#getObjectTags)  |   |   |
| [`removeBucketEncryption`](#removeBucketEncryption)  | [`removeObjectTags`](#removeObjectTags)  |   |   |
| [`setBucketTags`](#setBucketTags)  | [`setObjectRetention`](#setObjectRetention)  |   |   |
| [`getBucketTags`](#getBucketTags)  | [`getObjectRetention`](#getObjectRetention)  |   |   |
| [`removeBucketTags`](#removeBucketTags)  | [`clearObjectRetention`](#clearObjectRetention)  |   |   |
| [`setObjectLock`](#setObjectLock)  |   |   |   |
| [`getObjectLock`](#getObjectLock)  |   |   |   |
| [`removeObjectLock`](#removeObjectLock)  |   |   |   |
| [`setBucketLifecycle`](#setBucketLifecycle)  |   |   |   |
| [`getBucketLifecycle`](#getBucketLifecycle)  |   |   |   |
| [`removeBucketLifecycle`](#removeBucketLifecycle)  |   |   |   |



## 1. Constructors

<a name="constructors"></a>

|  |
|---|
|`public MinioClient(string endpoint, string accessKey = "", string secretKey = "", string region = "", string sessionToken="")`   |
| Creates MinIO client object with given endpoint.AccessKey, secretKey, region and sessionToken are optional parameters, and can be omitted for anonymous access.
  The client object uses Http access by default. To use Https, chain method WithSSL() to client object to use secure transfer protocol   |


__Parameters__

| Param  | Type  | Description  |
|---|---|---|
| `endpoint`  |  _string_ | endPoint is an URL, domain name, IPv4 address or IPv6 address.Valid endpoints are listed below: |
| | |s3.amazonaws.com |
| | |play.min.io |
| | |localhost |
| | |play.min.io|
| `accessKey`   | _string_   |accessKey is like user-id that uniquely identifies your account.This field is optional and can be omitted for anonymous access. |
|`secretKey`  |  _string_   | secretKey is the password to your account.This field is optional and can be omitted for anonymous access.|
|`region`  |  _string_   | region to which calls should be made.This field is optional and can be omitted.|
|`sessionToken`  |  _string_   | sessionToken needs to be set if temporary access credentials are used |

__Secure Access__

|  |
|---|
|`Chain .WithSSL() to MinIO Client object to use https instead of http. `   |

__Proxy__

|  |
|---|
|`Chain .WithProxy(proxyObject) to MinIO Client object to use proxy `   |

|  |
|---|
|`public MinioClient()`   |
| Creates MinIO client. This client gives an empty object that can be used with Chaining to populate only the member variables we need.
  The next important step is to connect to an endpoint. You can chain one of the overloaded method WithEndpoint() to client object to connect.
  This client object also uses Http access by default. To use Https, chain method WithSSL() to client object to use secure transfer protocol.
  To use non-anonymous access, chain method WithCredentials() to the client object along with the access key & secret key.
  Finally chain the method Build() to get the finally built client object.   |


__Parameters__

| None   |


__Secure Access__

|  |
|---|
|`Chain .WithSSL() to MinIO Client object to use https instead of http. `   |


__Endpoint__

|  |
|---|
|`Chain .WithEndpoint() to MinIO Client object to initialize the endpoint. `   |


__Parameters__
|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``endpoint``  | _string_ | Server Name (Resolvable) or IP address as the endpoint  |

| Return Type	  | Exceptions	  |
|:--- |:--- |
| ``MinioClient``  | Listed Exceptions: |
|        |  |



__Endpoint__

|  |
|---|
|`Chain .WithEndpoint() to MinIO Client object to initialize the endpoint. `   |


__Parameters__
|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``endpoint``  | _string_ | Server Name (Resolvable) or IP address as the endpoint  |
| ``port``  | _int_ | Port on which the server is listening  |
| ``secure``  | _bool_ | If true, use https; if not, use http  |

| Return Type	  | Exceptions	  |
|:--- |:--- |
| ``MinioClient``  | Listed Exceptions: |
|        |  |


__Proxy__

|  |
|---|
|`Chain .WithProxy(proxyObject) to MinIO Client object to use proxy `   |



__Examples__


### MinIO


```cs
// 1. public MinioClient(String endpoint)
MinioClient minioClient = new MinioClient("play.min.io");

// 2. public MinioClient(String endpoint, String accessKey, String secretKey)
MinioClient minioClient = new MinioClient("play.min.io",
                                          accessKey:"Q3AM3UQ867SPQQA43P2F",
                                          secretKey:"zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                                          ).WithSSL();

// 3. Initializing minio client with proxy
IWebProxy proxy = new WebProxy("192.168.0.1", 8000);
MinioClient minioClient = new MinioClient("my-ip-address:9000", "minio", "minio123").WithSSL().WithProxy(proxy);

// 4. Initializing minio client with temporary credentials
MinioClient minioClient = new MinioClient("my-ip-address:9000", "tempuserid", "temppasswd", sessionToken:"sessionToken");

// 5. Using Builder with public MinioClient(), Endpoint, Credentials & Secure connection
MinioClient minioClient = new MinioClient()
                                    .WithEndpoint("play.min.io")
                                    .WithCredentials("Q3AM3UQ867SPQQA43P2F", "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG")
                                    .WithSSL()
                                    .Build()
// 6. Using Builder with public MinioClient(), Endpoint, Credentials & Secure connection
MinioClient minioClient = new MinioClient()
                                    .WithEndpoint("play.min.io", 9000, true)
                                    .WithCredentials("Q3AM3UQ867SPQQA43P2F", "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG")
                                    .Build()
```



### AWS S3


```cs
// 1. public MinioClient(String endpoint)
MinioClient s3Client = new MinioClient("s3.amazonaws.com").WithSSL();

// 2. public MinioClient(String endpoint, String accessKey, String secretKey)
MinioClient s3Client = new MinioClient("s3.amazonaws.com",
                                       accessKey:"YOUR-ACCESSKEYID",
                                       secretKey:"YOUR-SECRETACCESSKEY").WithSSL();
// 3. Using Builder with public MinioClient(), Endpoint, Credentials & Secure connection
MinioClient minioClient = new MinioClient()
                                    .WithEndpoint("s3.amazonaws.com")
                                    .WithCredentials("YOUR-ACCESSKEYID", "YOUR-SECRETACCESSKEY")
                                    .WithSSL()
                                    .Build()
```

## 2. Bucket operations

<a name="makeBucket"></a>
### MakeBucketAsync(string bucketName, string location = "us-east-1")
`Task MakeBucketAsync(string bucketName, string location = "us-east-1", CancellationToken cancellationToken = default(CancellationToken))`

Creates a new bucket.


__Parameters__

|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_ | Name of the bucket  |
| ``region``  | _string_| Optional parameter. Defaults to us-east-1 for AWS requests  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |

| Return Type	  | Exceptions	  |
|:--- |:--- |
| ``Task``  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``RedirectionException`` : upon redirection by server |
|        | ``InternalClientException`` : upon internal library error        |


__Example__


```cs
try
{
   // Create bucket if it doesn't exist.
   bool found = await minioClient.BucketExistsAsync("mybucket");
   if (found)
   {
      Console.WriteLine("mybucket already exists");
   }
   else
   {
     // Create bucket 'my-bucketname'.
     await minioClient.MakeBucketAsync("mybucket");
     Console.WriteLine("mybucket is created successfully");
   }
}
catch (MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```

### MakeBucketAsync(MakeBucketArgs args)
`Task MakeBucketAsync(MakeBucketArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Creates a new bucket.


__Parameters__

|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _MakeBucketArgs_ | Arguments Object - name, location.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |

| Return Type	  | Exceptions	  |
|:--- |:--- |
| ``Task``  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``RedirectionException`` : upon redirection by server |
|        | ``InternalClientException`` : upon internal library error        |


__Example__


```cs
try
{
   // Create bucket if it doesn't exist.
   bool found = await minioClient.BucketExistsAsync(bktExistArgs);
   if (found)
   {
      Console.WriteLine(bktExistArgs.BucketName +" already exists");
   }
   else
   {
     // Create bucket 'my-bucketname'.
     await minioClient.MakeBucketAsync(mkBktArgs);
     Console.WriteLine(mkBktArgs.BucketName + " is created successfully");
   }
}
catch (MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="listBuckets"></a>
### ListBucketsAsync()

`Task<ListAllMyBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default(CancellationToken))`

Lists all buckets.

|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |

|Return Type	  | Exceptions	  |
|:--- |:--- |
| ``Task<ListAllMyBucketsResult>`` : Task with List of bucket type.  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``InvalidOperationException``: upon unsuccessful deserialization of xml data |
|        | ``ErrorResponseException`` : upon unsuccessful execution            |
|        | ``InternalClientException`` : upon internal library error        |


__Example__


```cs
try
{
    // List buckets that have read access.
    var list = await minioClient.ListBucketsAsync();
    foreach (Bucket bucket in list.Buckets)
    {
        Console.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
    }
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="bucketExists"></a>
### BucketExistsAsync(string bucketName)

`Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))`

Checks if a bucket exists.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<bool>`` : true if the bucket exists  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``ErrorResponseException`` : upon unsuccessful execution            |
|        | ``InternalClientException`` : upon internal library error        |



__Example__


```cs
try
{
   // Check whether 'my-bucketname' exists or not.
   bool found = await minioClient.BucketExistsAsync(bucketName);
   Console.WriteLine("bucket-name " + ((found == true) ? "exists" : "does not exist"));
}
catch (MinioException e)
{
   Console.WriteLine("[Bucket]  Exception: {0}", e);
}
```


### BucketExistsAsync(BucketExistsArgs)

`Task<bool> BucketExistsAsync(BucketExistsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Checks if a bucket exists.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _BucketExistsArgs_  | Argument object - bucket name.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<bool>`` : true if the bucket exists  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``ErrorResponseException`` : upon unsuccessful execution            |
|        | ``InternalClientException`` : upon internal library error        |



__Example__


```cs
try
{
   // Check whether 'my-bucketname' exists or not.
   bool found = await minioClient.BucketExistsAsync(args);
   Console.WriteLine(args.BucketName + " " + ((found == true) ? "exists" : "does not exist"));
}
catch (MinioException e)
{
   Console.WriteLine("[Bucket]  Exception: {0}", e);
}
```

<a name="removeBucket"></a>
### RemoveBucketAsync(string bucketName)

`Task RemoveBucketAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))`

Removes a bucket.


NOTE: -  removeBucket does not delete the objects inside the bucket. The objects need to be deleted using the removeObject API.


__Parameters__

 
|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  Task  | Listed Exceptions: |
|        | ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``ErrorResponseException`` : upon unsuccessful execution            |
|        | ``InternalClientException`` : upon internal library error        |
|        | ``BucketNotFoundException`` : upon missing bucket          |


__Example__


```cs
try
{
    // Check if my-bucket exists before removing it.
    bool found = await minioClient.BucketExistsAsync("mybucket");
    if (found)
    {
        // Remove bucket my-bucketname. This operation will succeed only if the bucket is empty.
        await minioClient.RemoveBucketAsync("mybucket");
        Console.WriteLine("mybucket is removed successfully");
    }
    else
    {
        Console.WriteLine("mybucket does not exist");
    }
}
catch(MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

### RemoveBucketAsync(RemoveBucketArgs args)

`Task RemoveBucketAsync(RemoveBucketArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Removes a bucket.


NOTE: -  removeBucket does not delete the objects inside the bucket. The objects need to be deleted using the removeObject API.


__Parameters__

 
|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveBucketArgs_  | Arguments Object - bucket name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  Task  | Listed Exceptions: |
|        | ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``ErrorResponseException`` : upon unsuccessful execution            |
|        | ``InternalClientException`` : upon internal library error        |
|        | ``BucketNotFoundException`` : upon missing bucket          |


__Example__


```cs
try
{
    // Check if my-bucket exists before removing it.
    bool found = await minioClient.BucketExistsAsync(bktExistsArgs);
    if (found)
    {
        // Remove bucket my-bucketname. This operation will succeed only if the bucket is empty.
        await minioClient.RemoveBucketAsync(rmBktArgs);
        Console.WriteLine(rmBktArgs.BucketName + " is removed successfully");
    }
    else
    {
        Console.WriteLine(bktExistsArgs.BucketName + " does not exist");
    }
}
catch(MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="getVersioning"></a>
### public async Task<VersioningConfiguration> GetVersioningAsync(GetVersioningArgs args)

`Task<VersioningConfiguration> GetVersioningAsync(GetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Get versioning information for a bucket.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetVersioningArgs_  | Arguments Object - bucket name. |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


|Return Type	  | Exceptions	  |
|:--- |:--- |
| ``VersioningConfiguration``:VersioningConfiguration with information populated from response.  | _None_  |


__Example__


```cs
try
{
    // Check whether 'mybucket' exists or not.
    bool found = minioClient.BucketExistsAsync(bktExistsArgs);
    if (found)
    {
        var args = new GetVersioningArgs("mybucket")
                                .WithSSL();
        VersioningConfiguration vc = await minio.GetVersioningInfoAsync(args);
    }
    else
    {
        Console.WriteLine(bktExistsArgs.BucketName + " does not exist");
    }
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```


<a name="setVersioning"></a>
### public async Task SetVersioningAsync(SetVersioningArgs args)

`Task SetVersioningAsync(SetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Set versioning to Enabled or Suspended for a bucket.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetVersioningArgs_  | Arguments Object - bucket name, versioning status. |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


|Return Type	  | Exceptions	  |
|:--- |:--- |
| ``Task``:  | _None_  |


__Example__


```cs
try
{
    // Check whether 'mybucket' exists or not.
    bool found = minioClient.BucketExistsAsync(bktExistsArgs);
    if (found)
    {
        var args = new SetVersioningArgs("mybucket")
                                .WithSSL()
                                .WithVersioningEnabled();

        await minio.SetVersioningAsync(setArgs);
    }
    else
    {
        Console.WriteLine(bktExistsArgs.BucketName + " does not exist");
    }
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```



<a name="setBucketEncryption"></a>
### SetBucketEncryptionAsync(SetBucketEncryptionArgs args)

`Task SetBucketEncryptionAsync(SetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken));`

Sets the Bucket Encryption Configuration of a bucket.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetBucketEncryptionArgs_  | SetBucketEncryptionArgs Argument Object with bucket, encryption configuration  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found   |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Set Encryption Configuration for the bucket
    SetBucketEncryptionArgs args = new SetBucketEncryptionArgs()
                                        .WithBucket(bucketName)
                                        .WithEncryptionConfig(config);
    await minio.SetBucketEncryptionAsync(args);
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```

<a name="getBucketEncryption"></a>
### GetBucketEncryptionAsync(GetBucketEncryptionArgs args)

`Task<ServerSideEncryptionConfiguration> GetBucketEncryptionAsync(GetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Gets the Bucket Encryption configuration of the bucket.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetBucketEncryptionArgs_  | GetBucketEncryptionArgs Argument Object with bucket name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<ServerSideEncryptionConfiguration>``: ServerSideEncryptionConfiguration object which contains the bucket encryption configuration. | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Get Bucket Encryption Configuration for the bucket
    var args = new GetBucketEncryptionArgs()
                        .WithBucket(bucketName);
    ServerSideEncryptionConfiguration config = await minio.GetBucketEncryptionAsync(args);
    Console.WriteLine($"Got encryption configuration for bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="removeBucketEncryption"></a>
### RemoveBucketEncryptionAsync(RemoveBucketEncryptionArgs args)

`Task RemoveBucketEncryptionAsync(RemoveBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Remove the Bucket Encryption configuration of an object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveBucketEncryptionArgs_  | RemoveBucketEncryptionArgs Argument Object with bucket name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Remove Bucket Encryption Configuration for the bucket
    var args = new RemoveBucketEncryptionArgs()
                                .WithBucket(bucketName);
    await minio.RemoveBucketEncryptionAsync(args);
    Console.WriteLine($"Removed encryption configuration for bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```

<a name="setBucketTags"></a>
### SetBucketTagsAsync(SetBucketTagsArgs args)

`Task SetBucketTagsAsync(SetBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Sets tags to a bucket.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetBucketTagsArgs_  | SetBucketTagsArgs Argument Object with bucket, tags to set  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found   |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Set Tags for the bucket
    SetBucketTagsArgs args = new SetBucketTagsArgs()
                                                .WithBucket(bucketName)
                                                .WithTagKeyValuePairs(tags);
    await minio.SetBucketTagsAsync(args);
    Console.WriteLine($"Set Tags for bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```

<a name="getBucketTags"></a>
### GetBucketTagsAsync(GetBucketTagsArgs args)

`Task<Tagging> GetBucketTagsAsync(GetBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Gets tags of a bucket.



__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetBucketTagsArgs_  | GetBucketTagsArgs Argument Object with bucket name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<Tagging>``: Tagging object which containing tag-value pairs. | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Get Bucket Tags for the bucket
    var args = new GetBucketTagsArgs()
                        .WithBucket(bucketName);
    var tags = await minio.GetBucketTagsAsync(args);
    Console.WriteLine($"Got tags for bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="removeBucketTags"></a>
### RemoveBucketTagsAsync(RemoveBucketTagsArgs args)

`Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Deletes tags of a bucket.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveBucketTagsArgs_  | RemoveBucketTagsArgs Argument Object with bucket name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Remove Bucket Encryption Configuration for the bucket
    var args = new RemoveBucketTagsArgs()
                                .WithBucket(bucketName);
    await minio.RemoveBucketTagsAsync(args);
    Console.WriteLine($"Removed tags for bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="setBucketLifecycle"></a>
### SetBucketLifecycleAsync(SetBucketLifecycleArgs args)

`Task SetBucketLifecycleAsync(SetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Sets Lifecycle configuration to a bucket.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetBucketLifecycleArgs_  | SetBucketLifecycleArgs Argument Object with bucket name, Lifecycle configuration to set  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found   |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Set Lifecycle configuration for the bucket
    SetBucketLifecycleArgs args = new SetBucketLifecycleArgs()
                                                .WithBucket(bucketName)
                                                .WithConfiguration(lfc);
    await minio.SetBucketLifecycleAsync(args);
    Console.WriteLine($"Set Lifecycle for bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```

<a name="getBucketLifecycle"></a>
### GetBucketLifecycleAsync(GetBucketLifecycleArgs args)

`Task<LifecycleConfiguration> GetBucketLifecycleAsync(GetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Gets Lifecycle configuration of a bucket.



__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetBucketLifecycleArgs_  | GetBucketLifecycleArgs Argument Object with bucket name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<LifecycleConfiguration>``: LifecycleConfiguration object which contains the Lifecycle configuration details. | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Get Bucket Lifecycle configuration for the bucket
    var args = new GetBucketLifecycleArgs()
                        .WithBucket(bucketName);
    var lfc = await minio.GetBucketLifecycleAsync(args);
    Console.WriteLine($"Got Lifecycle configuration for bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="removeBucketLifecycle"></a>
### RemoveBucketLifecycleAsync(RemoveBucketLifecycleArgs args)

`Task RemoveBucketLifecycleAsync(RemoveBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Deletes Lifecycle configuration of a bucket.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveBucketLifecycleArgs_  | RemoveBucketLifecycleArgs Argument Object with bucket name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Remove Bucket Lifecycle Configuration for the bucket
    var args = new RemoveBucketLifecycleArgs()
                                .WithBucket(bucketName);
    await minio.RemoveBucketLifecycleAsync(args);
    Console.WriteLine($"Removed Lifecycle configuration for bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```




<a name="listObjects"></a>
### ListObjectsAsync(ListObjectArgs args)

`IObservable<Item> ListObjectsAsync(ListObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Lists all objects in a bucket.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _ListObjectArgs_  | ListObjectArgs object - encapsulates bucket name, prefix, show recursively, show versions. |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


|Return Type	  | Exceptions	  |
|:--- |:--- |
| ``IObservable<Item>``:an Observable of Items.  | _None_  |


__Example__


```cs
try
{
    // Check whether 'mybucket' exists or not.
    bool found = minioClient.BucketExistsAsync("mybucket");
    if (found)
    {
        // List objects from 'my-bucketname'
        ListObjectArgs args = new ListObjectArgs()
                                        .WithBucket("mybucket")
                                        .WithPrefix("prefix")
                                        .WithRecursive(true);
        IObservable<Item> observable = minioClient.ListObjectsAsync(args);
        IDisposable subscription = observable.Subscribe(
				item => Console.WriteLine("OnNext: {0}", item.Key),
				ex => Console.WriteLine("OnError: {0}", ex.Message),
				() => Console.WriteLine("OnComplete: {0}"));
    }
    else
    {
        Console.WriteLine("mybucket does not exist");
    }
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```



<a name="setObjectLock"></a>
### SetObjectLockConfigurationAsync(SetObjectLockConfigurationArgs args)

`Task SetObjectLockConfigurationAsync(SetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Sets object-lock configuration in a bucket.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetObjectLockConfigurationArgs_  | SetObjectLockConfigurationArgs Argument Object with bucket, lock configuration to set  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found   |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    ObjectLockConfiguration config = = new ObjectLockConfiguration(RetentionMode.GOVERNANCE, 35);
    // Set Object Lock Configuration for the bucket
    SetObjectLockConfigurationArgs args = new SetObjectLockConfigurationArgs()
                                                        .WithBucket(bucketName)
                                                        .WithLockConfiguration(config);
    await minio.SetObjectLockConfigurationAsync(args);
    Console.WriteLine($"Set Object lock configuration to bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="getObjectLock"></a>
### GetObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args)

`Task<ObjectLockConfiguration> GetObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Gets object-lock configuration of a bucket.



__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetObjectLockConfigurationArgs_  | GetObjectLockConfigurationArgs Argument Object with bucket name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<ObjectLockConfiguration>``: ObjectLockConfiguration object which containing lock-enabled status & Object lock rule. | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Get the Object Lock Configuration for the bucket
    var args = new GetObjectLockConfigurationArgs()
                                     .WithBucket(bucketName);
    var config = await minio.GetObjectLockConfigurationAsync(args);
    Console.WriteLine($"Object lock configuration on bucket {bucketName} is : " + config.ObjectLockEnabled);
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="removeObjectLock"></a>
### RemoveObjectLockConfigurationAsync(RemoveObjectLockConfigurationArgs args)

`Task RemoveObjectLockConfigurationAsync(RemoveObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Removes object-lock configuration on a bucket.



__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveObjectLockConfigurationArgs_  | RemoveObjectLockConfigurationArgs Argument Object with bucket name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Remove Object Lock Configuration on the bucket
    var args = new RemoveObjectLockConfigurationArgs()
                                .WithBucket(bucketName);
    await minio.RemoveObjectLockConfigurationAsync(args);
    Console.WriteLine($"Removed Object lock configuration on bucket {bucketName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="listObjectVersions"></a>
### ListObjectVersionsAsync(ListObjectArgs args)

`IObservable<VersionItem> ListObjectVersionsAsync(ListObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Lists all objects along with multiple versions (if any) in a bucket.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _ListObjectArgs_  | ListObjectArgs object - encapsulates bucket name, prefix, show recursively, show versions. |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


|Return Type	  | Exceptions	  |
|:--- |:--- |
| ``IObservable<VersionItem>``: an Observable of Versioned Items.  | _None_  |


__Example__


```cs
try
{
    // Check whether 'mybucket' exists or not.
    bool found = minioClient.BucketExistsAsync("mybucket");
    if (found)
    {
        // List objects from 'my-bucketname'
        ListObjectArgs args = new ListObjectArgs()
                                        .WithBucket("mybucket")
                                        .WithPrefix("prefix")
                                        .WithRecursive(true)
                                        .WithVersions(true)
        IObservable<VersionItem> observable = minioClient.ListObjectVersionsAsync(args, true);
        IDisposable subscription = observable.Subscribe(
				item => Console.WriteLine("OnNext: {0} - {1}", item.Key, item.VersionId),
				ex => Console.WriteLine("OnError: {0}", ex.Message),
				() => Console.WriteLine("OnComplete: {0}"));
    }
    else
    {
        Console.WriteLine("mybucket does not exist");
    }
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```


<a name="listIncompleteUploads"></a>
### ListIncompleteUploads(ListIncompleteUploadsArgs args)

`IObservable<Upload> ListIncompleteUploads(ListIncompleteUploadsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Lists partially uploaded objects in a bucket.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _ListIncompleteUploadsArgs_  | ListIncompleteUploadsArgs object - encapsulates bucket name, prefix, show recursively.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


|Return Type	  | Exceptions	  |
|:--- |:--- |
| ``IObservable<Upload> ``: an Observable of Upload.  | _None_  |


__Example__


```cs
try
{
    // Check whether 'mybucket' exist or not.
    bool found = minioClient.BucketExistsAsync("mybucket");
    if (found)
    {
        // List all incomplete multipart upload of objects in 'mybucket'
        ListIncompleteUploadsArgs listArgs = new ListIncompleteUploadsArgs()
                                                            .WithBucket("mybucket")
                                                            .WithPrefix("prefix")
                                                            .WithRecursive(true);
        IObservable<Upload> observable = minioClient.ListIncompleteUploads(listArgs);
        IDisposable subscription = observable.Subscribe(
							item => Console.WriteLine("OnNext: {0}", item.Key),
							ex => Console.WriteLine("OnError: {0}", ex.Message),
							() => Console.WriteLine("OnComplete: {0}"));
    }
    else
    {
        Console.WriteLine("mybucket does not exist");
    }
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```


<a name="listenBucketNotifications"></a>
### ListenBucketNotificationsAsync(ListenBucketNotificationsArgs args)

`IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(ListenBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Subscribes to bucket change notifications (a Minio-only extension)

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _ListenBucketNotificationsArgs_  | ListenBucketNotificationsArgs object - encapsulates bucket name, list of events, prefix, suffix. |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


|Return Type	  | Exceptions	  |
|:--- |:--- |
| ``IObservable<MinioNotificationRaw>``: an Observable of _MinioNotificationRaw_, which contain the raw JSON notifications. Use the _MinioNotification_ class to deserialise using the JSON library of your choice. | _None_  |


__Example__


```cs
try
{
    var events = new List<EventType> { EventType.ObjectCreatedAll };
    var prefix = null;
    var suffix = null;
    ListenBucketNotificationsArgs args = new ListenBucketNotificationsArgs()
                                                            .WithBucket(bucketName)
                                                            .WithEvents(events)
                                                            .WithPrefix(prefix)
                                                            .WithSuffix(suffix);
    IObservable<MinioNotificationRaw> observable = minioClient.ListenBucketNotificationsAsync(args);

    IDisposable subscription = observable.Subscribe(
        notification => Console.WriteLine($"Notification: {notification.json}"),
        ex => Console.WriteLine($"OnError: {ex}"),
        () => Console.WriteLine($"Stopped listening for bucket notifications\n"));

}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="getBucketPolicy"></a>
### GetPolicyAsync(GetPolicyArgs args)
`Task<String> GetPolicyAsync(GetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Get bucket policy.


__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetPolicyArgs_  | GetPolicyArgs object encapsulating bucket name.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<String>``: The current bucket policy for given bucket as a json string.  | Listed Exceptions: |
|        | ``InvalidBucketNameException `` : upon invalid bucket name.       |
|        | ``InvalidObjectPrefixException`` : upon invalid object prefix.        |
|        | ``ConnectionException`` : upon connection error.            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``InternalClientException`` : upon internal library error.        |
|        | ``BucketNotFoundException`` : upon missing bucket          |


__Example__


```cs
try
{
    GetPolicyArgs args = new GetPolicyArgs()
                                    .WithBucket("myBucket");
    String policyJson = await minioClient.GetPolicyAsync(args);
    Console.WriteLine("Current policy: " + policyJson);
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="setBucketPolicy"></a>
### SetPolicyAsync(SetPolicyArgs args)
`Task SetPolicyAsync(SetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Set policy on bucket.

__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetPolicyArgs_  | SetPolicyArgs object encapsulating bucket name, Policy as a json string.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  Task  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |
|        | ``InvalidBucketNameException `` : upon invalid bucket name       |
|        | ``InvalidObjectPrefixException`` : upon invalid object prefix        |



__Example__

```cs
try
{
    string policyJson = $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}""],""Sid"":""""}},{{""Action"":[""s3:ListBucket""],""Condition"":{{""StringEquals"":{{""s3:prefix"":[""foo"",""prefix/""]}}}},""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}""],""Sid"":""""}},{{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}/foo*"",""arn:aws:s3:::{bucketName}/prefix/*""],""Sid"":""""}}]}}";
    SetPolicyArgs args = new SetPolicyArgs()
                                    .WithBucket("myBucket")
                                    .WithPolicy(policyJson);
    await minioClient.SetPolicyAsync(args);
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```


<a name="setBucketNotification"></a>
### SetBucketNotificationAsync(SetBucketNotificationsArgs args)
`Task SetBucketNotificationAsync(SetBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Sets notification configuration for a given bucket

__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetBucketNotificationsArgs_  | SetBucketNotificationsArgs object encapsulating bucket name, notification configuration object.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  Task  | Listed Exceptions: |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |
|        | ``InvalidBucketNameException `` : upon invalid bucket name       |
|        | ``InvalidOperationException``: upon unsuccessful serialization of notification object |



__Example__
```cs
try
{
    BucketNotification notification = new BucketNotification();
    Arn topicArn = new Arn("aws", "sns", "us-west-1", "412334153608", "topicminio");

    TopicConfig topicConfiguration = new TopicConfig(topicArn);
    List<EventType> events = new List<EventType>(){ EventType.ObjectCreatedPut , EventType.ObjectCreatedCopy };
    topicConfiguration.AddEvents(events);
    topicConfiguration.AddFilterPrefix("images");
    topicConfiguration.AddFilterSuffix("jpg");
    notification.AddTopic(topicConfiguration);

    QueueConfig queueConfiguration = new QueueConfig("arn:aws:sqs:us-west-1:482314153608:testminioqueue1");
    queueConfiguration.AddEvents(new List<EventType>() { EventType.ObjectCreatedCompleteMultipartUpload });
    notification.AddQueue(queueConfiguration);

    SetBucketNotificationsArgs args = new SetBucketNotificationsArgs()
                                                    .WithBucket(bucketName)
                                                    .WithBucketNotificationConfiguration(notification);
    await minio.SetBucketNotificationsAsync(args);
    Console.WriteLine("Notifications set for the bucket " + args.BucketName + " successfully");
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="getBucketNotification"></a>
### GetBucketNotificationAsync(GetBucketNotificationsArgs args)
`Task<BucketNotification> GetBucketNotificationAsync(GetBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Get bucket notification configuration


__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetBucketNotificationsArgs_  | GetBucketNotificationsArgs object encapsulating bucket name.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<BucketNotification>``: The current notification configuration for the bucket.  | Listed Exceptions: |
|        | ``InvalidBucketNameException `` : upon invalid bucket name.       |
|        | ``ConnectionException`` : upon connection error.            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``InternalClientException`` : upon internal library error.        |
|        | ``BucketNotFoundException`` : upon missing bucket          |
|        | ``InvalidOperationException``: upon unsuccessful deserialization of xml data |


__Example__


```cs
try
{
    GetBucketNotificationsArgs args = new GetBucketNotificationsArgs()
                                                    .WithBucket(bucketName);
    BucketNotification notifications = await minioClient.GetBucketNotificationAsync(args);
    Console.WriteLine("Notifications is " + notifications.ToXML());
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="removeAllBucketNotification"></a>
### RemoveAllBucketNotificationsAsync(RemoveAllBucketNotificationsArgs args)
`Task RemoveAllBucketNotificationsAsync(RemoveAllBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Remove all notification configurations set on the bucket


__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveAllBucketNotificationsArgs_  | RemoveAllBucketNotificationsArgs args encapsulating the bucket name.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task`:  | Listed Exceptions: |
|        | ``InvalidBucketNameException `` : upon invalid bucket name.       |
|        | ``ConnectionException`` : upon connection error.            |
|        | ``AccessDeniedException`` : upon access denial            |
|        | ``InternalClientException`` : upon internal library error.        |
|        | ``BucketNotFoundException`` : upon missing bucket          |
|        | ``InvalidOperationException``: upon unsuccessful serialization of xml data |


__Example__


```cs
try
{
    RemoveAllBucketNotificationsArgs args = new RemoveAllBucketNotificationsArgs()
                                                                .WithBucket(bucketName);
    await minioClient.RemoveAllBucketNotificationsAsync(args);
    Console.WriteLine("Notifications successfully removed from the bucket " + bucketName);
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

## 3. Object operations

<a name="getObject"></a>
### GetObjectAsync(GetObjectArgs args, ServerSideEncryption sse)

`Task GetObjectAsync(GetObjectArgs args, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))`

Downloads an object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetObjectArgs_ | GetObjectArgs Argument Object encapsulating bucket, object names, version Id, ServerSideEncryption object, offset, length |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``: Task callback returns an InputStream containing the object data.  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name. |
|        | ``ConnectionException`` : upon connection error.            |
|        | ``InternalClientException`` : upon internal library error.        |


__Examples__


```cs
//1. With Bucket & Object names.
try
{
   // Check whether the object exists using statObject().
   // If the object is not found, statObject() throws an exception,
   // else it means that the object exists.
   // Execution is successful.
   StatObjectArgs statObjectArgs = new StatObjectArgs()
                                            .WithBucket("mybucket")
                                            .WithObject("myobject");
   await minioClient.StatObjectAsync(statObjectArgs);

   // Get input stream to have content of 'my-objectname' from 'my-bucketname'
   GetObjectArgs getObjectArgs = new GetObjectArgs()
                                            .WithBucket("mybucket")
                                            .WithObject("myobject")
                                            .WithCallbackStream((stream) =>
                                                                {
                                                                    stream.CopyTo(Console.OpenStandardOutput());
                                                                });
   await minioClient.GetObjectAsync(getObjectArgs);
  }
  catch (MinioException e)
  {
      Console.WriteLine("Error occurred: " + e);
  }
  // 2. With Offset Length specifying a range of bytes & the object as a stream.
  try
  {
   // Check whether the object exists using statObject().
   // If the object is not found, statObject() throws an exception,
   // else it means that the object exists.
   // Execution is successful.
   StatObjectArgs statObjectArgs = new StatObjectArgs()
                                            .WithBucket("mybucket")
                                            .WithObject("myobject");
   await minioClient.StatObjectAsync(statObjectArgs);

   // Get input stream to have content of 'my-objectname' from 'my-bucketname'
   GetObjectArgs getObjectArgs = new GetObjectArgs()
                                            .WithBucket("mybucket")
                                            .WithObject("myobject")
                                            .WithOffset(1024L)
                                            .WithObjectSize(10L)
                                            .WithCallbackStream((stream) =>
                                                                {
                                                                    stream.CopyTo(Console.OpenStandardOutput());
                                                                });
   await minioClient.GetObjectAsync(getObjectArgs);
  }
  catch (MinioException e)
  {
      Console.WriteLine("Error occurred: " + e);
  }

  //3. Downloads and saves the object as a file in the local filesystem.
 try
 {
   // Check whether the object exists using statObjectAsync().
   // If the object is not found, statObjectAsync() throws an exception,
   // else it means that the object exists.
   // Execution is successful.
   StatObjectArgs statObjectArgs = new StatObjectArgs()
                                            .WithBucket("mybucket")
                                            .WithObject("myobject");
   await minioClient.StatObjectAsync(statObjectArgs);

   // Gets the object's data and stores it in photo.jpg
   GetObjectArgs getObjectArgs = new GetObjectArgs()
                                            .WithBucket("mybucket")
                                            .WithObject("myobject")
                                            .WithFileName("photo.jpg");
   await minioClient.GetObjectAsync(getObjectArgs);

}
catch (MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}

```

<a name="putObject"></a>
### PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType, ServerSideEncryption sse)

` Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))`


Uploads contents from a stream to objectName.



__Parameters__

|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``objectName``  | _string_  | Object name in the bucket |
| ``data``  | _Stream_  | Stream to upload |
| ``size``  | _long_    | size of stream   |
| ``contentType``  | _string_ | Content type of the file. Defaults to "application/octet-stream" |
| ``metaData``  | _Dictionary<string,string>_ | Dictionary of metadata headers. Defaults to null. |
| ``sse``    | _ServerSideEncryption_ | Server-side encryption option | Optional parameter. Defaults to null |

| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |
|        | ``EntityTooLargeException``: upon proposed upload size exceeding max allowed |
|        | ``UnexpectedShortReadException``: data read was shorter than size of input buffer |
|        | ``ArgumentNullException``: upon null input stream    |

__Example__


The maximum size of a single object is limited to 5TB. putObject transparently uploads objects larger than 5MiB in multiple parts. Uploaded data is carefully verified using MD5SUM signatures.


```cs
try
{
    byte[] bs = File.ReadAllBytes(fileName);
    System.IO.MemoryStream filestream = new System.IO.MemoryStream(bs);
    // Specify SSE-C encryption options
    Aes aesEncryption = Aes.Create();
    aesEncryption.KeySize = 256;
    aesEncryption.GenerateKey();
    var ssec = new SSEC(aesEncryption.Key);
    await minio.PutObjectAsync("mybucket",
                               "island.jpg",
                                filestream,
                                filestream.Length,
                               "application/octet-stream", ssec);
    Console.WriteLine("island.jpg is uploaded successfully");
}
catch(MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="putObject"></a>
### PutObjectAsync(string bucketName, string objectName, string filePath, string contentType=null, ServerSideEncryption sse)

` Task PutObjectAsync(string bucketName, string objectName, string filePath, string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))`


Uploads contents from a file to objectName.



__Parameters__

|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``objectName``  | _string_  | Object name in the bucket |
| ``fileName``  | _string_  | File to upload |
| ``contentType``  | _string_ | Content type of the file. Defaults to " |
| ``metadata``  | _Dictionary<string,string>_ | Dictionary of meta data headers and their values.Defaults to null.|
| ``sse``    | _ServerSideEncryption_ | Server-side encryption option | Optional parameter. Defaults to null |

| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |
|        | ``EntityTooLargeException``: upon proposed upload size exceeding max allowed |

__Example__


The maximum size of a single object is limited to 5TB. putObject transparently uploads objects larger than 5MiB in multiple parts. Uploaded data is carefully verified using MD5SUM signatures.


```cs
try
{
    await minio.PutObjectAsync("mybucket", "island.jpg", "/mnt/photos/island.jpg", contentType: "application/octet-stream");
    Console.WriteLine("island.jpg is uploaded successfully");
}
catch(MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```
<a name="statObject"></a>
### StatObjectAsync(StatObjectArgs args)

`Task<ObjectStat> StatObjectAsync(StatObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Gets metadata of an object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _StatObjectArgs_  | StatObjectArgs Argument Object with bucket, object names & server side encryption object  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<ObjectStat>``: Populated object meta data. | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |



__Example__


```cs
try
{
   // Get the metadata of the object.
   StatObjectArgs statObjectArgs = new StatObjectArgs()
                                            .WithBucket("mybucket")
                                            .WithObject("myobject");
   ObjectStat objectStat = await minioClient.StatObjectAsync(statObjectArgs);
   Console.WriteLine(objectStat);
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```

<a name="copyObject"></a>
### CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null)

*`Task<CopyObjectResult> CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null, CancellationToken cancellationToken = default(CancellationToken))`*

Copies content from objectName to destObjectName.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the source bucket  |
| ``objectName``  | _string_  | Object name in the source bucket to be copied |
| ``destBucketName``  | _string_  | Destination bucket name |
| ``destObjectName`` | _string_ | Destination object name to be created, if not provided defaults to source object name|
| ``copyConditions`` | _CopyConditions_ | Map of conditions useful for applying restrictions on copy operation|
| ``metadata``  | _Dictionary<string,string>_ | Dictionary of meta data headers and their values on the destination side.Defaults to null.|
| ``sseSrc``    | _ServerSideEncryption_ | Server-side encryption option for source object | Optional parameter. Defaults to null |
| ``sseDest``    | _ServerSideEncryption_ | Server-side encryption option for destination object| Optional parameter. Defaults to null |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |
|        | ``ArgumentException`` : upon missing bucket/object names |

__Example__


This API performs a Server-side copy operation from a given source object to destination object.

```cs
try
{
   CopyConditions copyConditions = new CopyConditions();
   copyConditions.setMatchETagNone("TestETag");
   ServerSideEncryption sseSrc, sseDst;
   // Uncomment to specify source and destination Server-side encryption options
   /*
    Aes aesEncryption = Aes.Create();
    aesEncryption.KeySize = 256;
    aesEncryption.GenerateKey();
    sseSrc = new SSEC(aesEncryption.Key);
    sseDst = new SSES3();
   */
   await minioClient.CopyObjectAsync("mybucket",  "island.jpg", "mydestbucket", "processed.png", copyConditions, sseSrc:sseSrc, sseDest:sseDst);
   Console.WriteLine("island.jpg is uploaded successfully");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="removeObject"></a>
### RemoveObjectAsync(RemoveObjectArgs args)

`Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Removes an object.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveObjectArgs_ | Arguments Object.  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |

| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |



__Example__


```cs
// 1. Remove object myobject from the bucket mybucket.
try
{
    RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                        .WithBucket("mybucket")
                                        .WithObject("myobject");
    await minioClient.RemoveObjectAsync(args);
    Console.WriteLine("successfully removed mybucket/myobject");
}
catch (MinioException e)
{
    Console.WriteLine("Error: " + e);
}

// 2. Remove one version of object myobject with versionID from mybucket.
try
{
    RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                        .WithBucket("mybucket")
                                        .WithObject("myobject")
                                        .WithVersionId("versionId");
    await minioClient.RemoveObjectAsync(args);
    Console.WriteLine("successfully removed mybucket/myobject{versionId}");
}
catch (MinioException e)
{
    Console.WriteLine("Error: " + e);
}

```
<a name="removeObjects"></a>
### RemoveObjectsAsync(RemoveObjectsArgs args)

`Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Removes a list of objects or object versions.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveObjectsArgs_ | Arguments Object - bucket name, List of Objects to be deleted or List of Tuples with Tuple(object name, List of version IDs).  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |

| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |



__Example__


```cs
// 1. Remove list of objects in objectNames from the bucket bucketName.
try
{
    string bucketName = "mybucket"
    List<String> objectNames = new LinkedList<String>();
    objectNames.add("my-objectname1");
    objectNames.add("my-objectname2");
    objectNames.add("my-objectname3");
    RemoveObjectsAsync rmArgs = new RemoveObjectsAsync()
                                            .WithBucket(bucketName)
                                            .WithObjects(objectNames);
    IObservable<DeleteError> observable = await minio.RemoveObjectAsync(rmArgs);
    IDisposable subscription = observable.Subscribe(
        deleteError => Console.WriteLine("Object: {0}", deleteError.Key),
        ex => Console.WriteLine("OnError: {0}", ex),
        () =>
        {
            Console.WriteLine("Removed objects from " + bucketName + "\n");
        });
}
catch (MinioException e)
{
    Console.WriteLine("Error: " + e);
}

// 2. Remove list of objects (only specific versions mentioned in Version ID list) from the bucket bucketName
try
{
    string bucketName = "mybucket";
    string objectName = "myobject1";
    List<string> versionIDs = new List<string>();
    versionIDs.Add("abcobject1version1dce");
    versionIDs.Add("abcobject1version2dce");
    versionIDs.Add("abcobject1version3dce");
    List<Tuple<string, string>> objectsVersions = new List<Tuple<string, string>>();
    objectsVersions.Add(new Tuple<string, List<string>>(objectName, versionIDs));
    objectsVersions.Add(new Tuple<string, string>("myobject2" "abcobject2version1dce"));
    objectsVersions.Add(new Tuple<string, string>("myobject2", "abcobject2version2dce"));
    objectsVersions.Add(new Tuple<string, string>("myobject2", "abcobject2version3dce"));
    RemoveObjectsAsync rmArgs = new RemoveObjectsAsync()
                                            .WithBucket(bucketName)
                                            .WithObjectsVersions(objectsVersions);
    IObservable<DeleteError> observable = await minio.RemoveObjectsAsync(rmArgs);
    IDisposable subscription = observable.Subscribe(
        deleteError => Console.WriteLine("Object: {0}", deleteError.Key),
        ex => Console.WriteLine("OnError: {0}", ex),
        () =>
        {
            Console.WriteLine("Listed all delete errors for remove objects on  " + bucketName + "\n");
        });
}
catch (MinioException e)
{
    Console.WriteLine("Error: " + e);
}

```


<a name="removeIncompleteUpload"></a>
### RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args)

`Task RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Removes a partially uploaded object.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveIncompleteUploadArgs_  | RemoveIncompleteUploadArgs object encapsulating the bucket, object names  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |


__Example__


```cs
try
{
    // Removes partially uploaded objects from buckets.
    RemoveIncompleteUploadArgs args = new RemoveIncompleteUploadArgs()
                                                    .WithBucket(bucketName)
                                                    .WithObject(objectName);
    await minioClient.RemoveIncompleteUploadAsync(args);
    Console.WriteLine("successfully removed all incomplete upload session of my-bucketname/my-objectname");
}
catch(MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="selectObjectContent"></a>
### SelectObjectContentAsync(SelectObjectContentArgs args)

`Task<SelectResponseStream> SelectObjectContentAsync(SelectObjectContentArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Downloads an object as a stream.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``    | _SelectObjectContentArgs_ | options for SelectObjectContent async | Required parameter. |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``: Task callback returns a SelectResponseStream containing select results.  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name. |
|        | ``ConnectionException`` : upon connection error.            |
|        | ``InternalClientException`` : upon internal library error.        |
|        | ``ArgumentException`` : upon invalid response format |
|        | ``IOException`` : insufficient data |


__Example__


```cs
try
{
    var opts = new SelectObjectOptions()
    {
        ExpressionType = QueryExpressionType.SQL,
        Expression = "select count(*) from s3object",
        InputSerialization = new SelectObjectInputSerialization()
        {
            CompressionType = SelectCompressionType.NONE,
            CSV = new CSVInputOptions()
            {
                FileHeaderInfo = CSVFileHeaderInfo.None,
                RecordDelimiter = "\n",
                FieldDelimiter = ",",
            }                    
        },
        OutputSerialization = new SelectObjectOutputSerialization()
        {
            CSV = new CSVOutputOptions()
            {
                RecordDelimiter = "\n",
                FieldDelimiter =  ",",
            }
        }
    };

    SelectObjectContentArgs args = SelectObjectContentArgs()
                                                .WithBucket(bucketName)
                                                .WithObject(objectName)
                                                .WithSelectObjectOptions(opts);
    var resp = await  minio.SelectObjectContentAsync(args);
    resp.Payload.CopyTo(Console.OpenStandardOutput());
    Console.WriteLine("Bytes scanned:" + resp.Stats.BytesScanned);
    Console.WriteLine("Bytes returned:" + resp.Stats.BytesReturned);
    Console.WriteLine("Bytes processed:" + resp.Stats.BytesProcessed);
    if (resp.Progress != null)
    {
        Console.WriteLine("Progress :" + resp.Progress.BytesProcessed);
    }
}
catch (MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```


<a name="setLegalHold"></a>
### SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args)

`Task SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Sets the Legal Hold status of an object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetObjectLegalHoldArgs_  | SetObjectLegalHoldArgs Argument Object with bucket, object names, version id(optional)  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``InvalidObjectNameException`` : upon invalid object name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found   |
|        |  ``ObjectNotFoundException`` : upon object with name not found |
|        |  ``MissingObjectLockConfiguration`` : upon bucket created with object lock not enabled |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |



__Example__


```cs
try
{
    // Setting WithLegalHold true, sets Legal hold status to ON.
    SetObjectLegalHoldArgs args = new SetObjectLegalHoldArgs()
                                                .WithBucket(bucketName)
                                                .WithObject(objectName)
                                                .WithVersionId(versionId)
                                                .WithLegalHold(true);
    await minio.SetObjectLegalHoldAsync(args);
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```

<a name="getLegalHold"></a>
### GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args)

`Task<bool> GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Gets the Legal Hold status of an object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetObjectLegalHoldArgs_  | GetObjectLegalHoldArgs Argument Object with bucket, object names, version id(optional)  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<bool>``: True if LegalHold is enabled, false otherwise. | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``InvalidObjectNameException`` : upon invalid object name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``ObjectNotFoundException`` : upon object with name not found |
|        |  ``MissingObjectLockConfiguration`` : upon bucket created with object lock not enabled |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |


__Example__


```cs
try
{
    // Get Legal Hold status a object
    var args = new GetObjectLegalHoldArgs()
                            .WithBucket(bucketName)
                            .WithObject(objectName)
                            .WithVersionId(versionId);
    bool enabled = await minio.GetObjectLegalHoldAsync(args);
    Console.WriteLine("LegalHold Configuration STATUS for " + bucketName + "/" + objectName +
                                        (!string.IsNullOrEmpty(versionId)?" with Version ID " + versionId: " ") +
                                        " : " + (enabled?"ON":"OFF"));
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```
<a name="setObjectTags"></a>
### SetObjectTagsAsync(SetObjectTagsArgs args)

`Task SetObjectTagsAsync(SetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Sets tags to a object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetObjectTagsArgs_  | SetObjectTagsArgs Argument Object with object, tags to set  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``InvalidObjectNameException`` : upon invalid object name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found   |
|        |  ``ObjectNotFoundException`` : upon object with name not found   |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Set Tags for the object
    SetObjectTagsArgs args = new new SetObjectTagsArgs()
                                                .WithBucket(bucketName)
                                                .WithObject(objectName)
                                                .WithTagKeyValuePairs(tags);
    await minio.SetObjectTagsAsync(args);
    Console.WriteLine($"Set tags for object {bucketName}/{objectName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```

<a name="getObjectTags"></a>
### GetObjectTagsAsync(GetObjectTagsArgs args)

`Task<Tagging> GetObjectTagsAsync(GetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Gets tags of a object.



__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetObjectTagsArgs_  | GetObjectTagsArgs Argument Object with object name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<Tagging>``: Task<Tagging> object which containing tag-value pairs. | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``InvalidObjectNameException`` : upon invalid object name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``ObjectNotFoundException`` : upon object with name not found   |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Get Object Tags for the object
    var args = new GetObjectTagsArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName);
    var tags = await minio.GetObjectTagsAsync(args);
    Console.WriteLine($"Got tags for object {bucketName}/{objectName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="removeObjectTags"></a>
### RemoveObjectTagsAsync(RemoveObjectTagsArgs args)

`Task RemoveObjectTagsAsync(RemoveObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Deletes tags of a object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _RemoveObjectTagsArgs_  | RemoveObjectTagsArgs Argument Object with object name  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``InvalidObjectNameException`` : upon invalid object name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``ObjectNotFoundException`` : upon object with name not found   |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |
|        |  ``UnexpectedMinioException`` : upon internal errors encountered during the operation |



__Example__


```cs
try
{
    // Remove Tags for the object
    var args = new RemoveObjectTagsArgs()
                                .WithBucket(bucketName)
                                .WithObject(objectName);
    await minio.RemoveObjectTagsAsync(args);
    Console.WriteLine($"Removed tags for object {bucketName}/{objectName}.");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="setObjectRetention"></a>
### SetObjectRetentionAsync(SetObjectRetentionArgs args)

`Task SetObjectRetentionAsync(SetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Sets retention configuration to an object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _SetObjectRetentionArgs_  | SetObjectRetentionArgs Argument Object with bucket, object names, version id(optional)  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``InvalidObjectNameException`` : upon invalid object name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found |
|        |  ``ObjectNotFoundException`` : upon object with name not found |
|        |  ``MissingObjectLockConfiguration`` : upon bucket created with object lock not enabled |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |



__Example__


```cs
try
{
    // Setting Retention Configuration of the object.
    SetObjectRetentionArgs args = new SetObjectRetentionArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithRetentionValidDays(numOfDays);
    await minio.SetObjectRetentionAsync(args);
    Console.WriteLine($"Assigned retention configuration to object {bucketName}/{objectName}");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="getObjectRetention"></a>
### GetObjectRetentionAsync(GetObjectRetentionArgs args)

`Task<ObjectRetentionConfiguration> GetObjectRetentionAsync(GetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Gets retention configuration of an object.



__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _GetObjectRetentionArgs_  | GetObjectRetentionArgs Argument Object with bucket, object names, version id(optional)  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<ObjectRetentionConfiguration>``: ObjectRetentionConfiguration object with configuration data. | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``InvalidObjectNameException`` : upon invalid object name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found  |
|        |  ``ObjectNotFoundException`` : upon object with name not found |
|        |  ``MissingObjectLockConfiguration`` : upon bucket created with object lock not enabled |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |


__Example__


```cs
try
{
    // Get Retention configuration of an object
    var args = new GetObjectRetentionArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName);
    ObjectRetentionConfiguration config = await minio.GetObjectRetentionAsync(args);
    Console.WriteLine($"Got retention configuration for object {bucketName}/{objectName}");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```


<a name="clearObjectRetention"></a>
### ClearObjectRetentionAsync(ClearObjectRetentionArgs args)

`Task ClearObjectRetentionAsync(ClearObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))`

Clears retention configuration to an object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _ClearObjectRetentionArgs_  | ClearObjectRetentionArgs Argument Object with bucket, object names, version id(optional)  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``  | Listed Exceptions: |
|        |  ``AuthorizationException`` : upon access or secret key wrong or not found |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        |  ``InvalidObjectNameException`` : upon invalid object name |
|        |  ``BucketNotFoundException`` : upon bucket with name not found   |
|        |  ``ObjectNotFoundException`` : upon object with name not found |
|        |  ``MissingObjectLockConfiguration`` : upon bucket created with object lock not enabled |
|        |  ``MalFormedXMLException`` : upon configuration XML in http request validation failure |



__Example__


```cs
try
{
    // Clearing the Retention Configuration of the object.
    ClearObjectRetentionArgs args = new ClearObjectRetentionArgs()
                                                .WithBucket(bucketName)
                                                .WithObject(objectName);
    await minio.ClearObjectRetentionAsync(args);
    Console.WriteLine($"Clears retention configuration to object {bucketName}/{objectName}");
}
catch(MinioException e)
{
   Console.WriteLine("Error occurred: " + e);
}
```



## 4. Presigned operations
<a name="presignedGetObject"></a>

### PresignedGetObjectAsync(PresignedGetObjectArgs args);
`Task<string> PresignedGetObjectAsync(PresignedGetObjectArgs args)`

Generates a presigned URL for HTTP GET operations. Browsers/Mobile clients may point to this URL to directly download objects even if the bucket is private. This presigned URL can have an associated expiration time in seconds after which it is no longer operational. The default expiry is set to 7 days.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _PresignedGetObjectArgs_  | PresignedGetObjectArgs encapsulating bucket, object names, expiry, response headers & request date |

| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<string>`` : string contains URL to download the object | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InvalidExpiryRangeException`` : upon invalid expiry range.            |


__Example__


```cs
try
{
    PresignedGetObjectArgs args = new PresignedGetObjectArgs()
                                                .WithBucket("mybucket")
                                                .WithObject("myobject")
                                                .WithExpiry(60 * 60 * 24);
    String url = await minioClient.PresignedGetObjectAsync(args);
    Console.WriteLine(url);
}
catch(MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="presignedPutObject"></a>
### PresignedPutObjectAsync(PresignedPutObjectArgs args)

`Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args)`

Generates a presigned URL for HTTP PUT operations. Browsers/Mobile clients may point to this URL to upload objects directly to a bucket even if it is private. This presigned URL can have an associated expiration time in seconds after which it is no longer operational. The default expiry is set to 7 days.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _PresignedPutObjectArgs_  | PresignedPutObjectArgs arguments object with bucket, object names & expiry  |

| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task<string>`` : string contains URL to upload the object | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``InvalidKeyException`` : upon an invalid access key or secret key           |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InvalidExpiryRangeException`` : upon invalid expiry range.            |


__Example__

```cs
try
{
    PresignedPutObjectArgs args = PresignedPutObjectArgs()
                                            .WithBucket("mybucket")
                                            .WithObject("myobject")
                                            .WithExpiry(60 * 60 * 24);
    String url = await minioClient.PresignedPutObjectAsync(args);
    Console.WriteLine(url);
}
catch(MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="presignedPostPolicy"></a>
### PresignedPostPolicy(PresignedPostPolicyArgs args)

`Task<Dictionary<string, string>> PresignedPostPolicyAsync(PresignedPostPolicyArgs args)`

Allows setting policy conditions to a presigned URL for POST operations. Policies such as bucket name to receive object uploads, key name prefixes, expiry policy may be set.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``args``  | _PresignedPostPolicyArgs_  | PresignedPostPolicyArgs Arguments object includes bucket, object names & Post policy of an object.  |


| Return Type	  | Exceptions	  |
|:--- |:--- |
| ``Task<Dictionary<string, string>>``: Map of strings to construct form-data. | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``NoSuchAlgorithmException`` : upon requested algorithm was not found during signature calculation.           |



__Example__


```cs
try
{
    PostPolicy policy = new PostPolicy();
    policy.SetContentType("image/png");
    policy.SetUserMetadata("custom", "user");
    DateTime expiration = DateTime.UtcNow;
    policy.SetExpires(expiration.AddDays(10));
    policy.SetKey("my-objectname");
    policy.SetBucket("my-bucketname");
    PresignedPostPolicyArgs args = PresignedPostPolicyArgs()
                                            .WithBucket("my-bucketname")
                                            .WithObject("my-objectname")
                                            .WithPolicy(policy);

    Dictionary<string, string> formData = minioClient.Api.PresignedPostPolicy(args);
    string curlCommand = "curl ";
    foreach (KeyValuePair<string, string> pair in formData)
    {
        curlCommand = curlCommand + " -F " + pair.Key + "=" + pair.Value;
    }
    curlCommand = curlCommand + " -F file=@/etc/bashrc https://s3.amazonaws.com/my-bucketname";
    Console.WriteLine(curlCommand);
}
catch(MinioException e)
{
  Console.WriteLine("Error occurred: " + e);
}
```
## Client Custom Settings
<a name="SetAppInfo"></a>
### SetAppInfo(string appName, string appVersion)
Adds application details to User-Agent.

__Parameters__

| Param  | Type  | Description  |
|---|---|---|
|`appName`  | _string_  | Name of the application performing the API requests |
| `appVersion`| _string_ | Version of the application performing the API requests |


__Example__


```cs
// Set Application name and version to be used in subsequent API requests.
minioClient.SetAppInfo("myCloudApp", "1.0.0")
```
<a name="SetTraceOn"></a>
### SetTraceOn(IRequestLogger logger = null)
Enables HTTP tracing. The trace is written to the stdout.

__Parameters__

| Param  | Type  | Description  |
|---|---|---|
|`logger`  | _IRequestLogger_  | Implementation of interface `Minio.IRequestLogger` for serialization models for trace HTTP |

__Example__

```cs
// Set HTTP tracing on with default trace logger.
minioClient.SetTraceOn()

// Set custom logger for HTTP trace
minioClient.SetTraceOn(new JsonNetLogger())
```


<a name="SetTraceOff"></a>
### SetTraceOff()
Disables HTTP tracing.


__Example__
```cs
// Sets HTTP tracing off.
minioClient.SetTraceOff()
```
