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
| [`listObjectVersions`](#listObjectVersions)  |  |   |   |
| [`listIncompleteUploads`](#listIncompleteUploads)  | [`removeObjects`](#removeObjects) |   |   |
| [`listenBucketNotifications`](#listenBucketNotifications) | [`removeIncompleteUpload`](#removeIncompleteUpload) |   |   |
| [`setVersioning`](#setVersioning)  | [`selectObjectContent`](#selectObjectContent) |   |   |
| [`getVersioning`](#getVersioning)  |  |   |   |

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
### ListIncompleteUploads(string bucketName, string prefix, bool recursive)

`IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix, bool recursive, CancellationToken cancellationToken = default(CancellationToken))`

Lists partially uploaded objects in a bucket.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``prefix``  | _string_  | Prefix string. List objects whose name starts with ``prefix`` |
| ``recursive``  | _bool_  | when false, emulates a directory structure where each listing returned is either a full object or part of the object's key up to the first '/'. All objects with the same prefix up to the first '/' will be merged into one entry |
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
        IObservable<Upload> observable = minioClient.ListIncompleteUploads("mybucket", "prefix", true);
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
### GetObjectAsync(string bucketName, string objectName, Action<Stream> callback, ServerSideEncryption sse)

`Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))`

Downloads an object as a stream.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_ | Name of the bucket  |
| ``objectName``  | _string_  | Object name in the bucket |
| ``callback``    | _Action<Stream>_ | Call back to process stream |
| ``sse``    | _ServerSideEncryption_ | Server-side encryption option | Optional parameter. Defaults to null |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``: Task callback returns an InputStream containing the object data.  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name. |
|        | ``ConnectionException`` : upon connection error.            |
|        | ``InternalClientException`` : upon internal library error.        |


__Example__


```cs
try
{
   // Check whether the object exists using statObject().
   // If the object is not found, statObject() throws an exception,
   // else it means that the object exists.
   // Execution is successful.
   await minioClient.StatObjectAsync("mybucket", "myobject");

   // Get input stream to have content of 'my-objectname' from 'my-bucketname'
   await minioClient.GetObjectAsync("mybucket", "myobject",
                                    (stream) =>
                                    {
                                        stream.CopyTo(Console.OpenStandardOutput());
                                    });
  }
  catch (MinioException e)
  {
      Console.WriteLine("Error occurred: " + e);
  }
```

<a name="getObject"></a>
### GetObjectAsync(string bucketName, string objectName, long offset, long length, Action<Stream> callback, ServerSideEncryption sse)

`Task GetObjectAsync(string bucketName, string objectName, long offset, long length, Action<Stream> callback, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))`

Downloads the specified range bytes of an object as a stream.Both offset and length are required.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_ | Name of the bucket.  |
| ``objectName``  | _string_  | Object name in the bucket. |
| ``offset``| _long_ | Offset of the object from where stream will start |
| ``length``| _long_| Length of the object to read in from the stream |
| ``callback``    | _Action<Stream>_ | Call back to process stream |
| ``sse``    | _ServerSideEncryption_ | Server-side encryption option | Optional parameter. Defaults to null |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task``: Task callback returns an InputStream containing the object data.  | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name. |
|        | ``ConnectionException`` : upon connection error.            |
|        | ``InternalClientException`` : upon internal library error.        |


__Example__


```cs
try
{
   // Check whether the object exists using statObject().
   // If the object is not found, statObject() throws an exception,
   // else it means that the object exists.
   // Execution is successful.
   await minioClient.StatObjectAsync("mybucket", "myobject");

   // Get input stream to have content of 'my-objectname' from 'my-bucketname'
   await minioClient.GetObjectAsync("mybucket", "myobject", 1024L, 10L,
                                    (stream) =>
                                    {
                                        stream.CopyTo(Console.OpenStandardOutput());
                                    });
  }
  catch (MinioException e)
  {
      Console.WriteLine("Error occurred: " + e);
  }
```

<a name="getObject"></a>
### GetObjectAsync(String bucketName, String objectName, String fileName, ServerSideEncryption sse)

`Task GetObjectAsync(string bucketName, string objectName, string fileName, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))`

Downloads and saves the object as a file in the local filesystem.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _String_  | Name of the bucket  |
| ``objectName``  | _String_  | Object name in the bucket |
| ``fileName``  | _String_  | File name |
| ``sse``    | _ServerSideEncryption_ | Server-side encryption option | Optional parameter. Defaults to null |
| ``cancellationToken``| _System.Threading.CancellationToken_ | Optional parameter. Defaults to default(CancellationToken) |


| Return Type	  | Exceptions	  |
|:--- |:--- |
|  ``Task `` | Listed Exceptions: |
|        |  ``InvalidBucketNameException`` : upon invalid bucket name |
|        | ``ConnectionException`` : upon connection error            |
|        | ``InternalClientException`` : upon internal library error        |

__Example__

```cs
try
{
   // Check whether the object exists using statObjectAsync().
   // If the object is not found, statObjectAsync() throws an exception,
   // else it means that the object exists.
   // Execution is successful.
   await minioClient.StatObjectAsync("mybucket", "myobject");

   // Gets the object's data and stores it in photo.jpg
   await minioClient.GetObjectAsync("mybucket", "myobject", "photo.jpg");

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
### StatObjectAsync(string bucketName, string objectName, ServerSideEncryption sse)

`Task<ObjectStat> StatObjectAsync(string bucketName, string objectName, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))`

Gets metadata of an object.


__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``objectName``  | _string_  | Object name in the bucket |
| ``sse``    | _ServerSideEncryption_ | Server-side encryption option | Optional parameter. Defaults to null |
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
   ObjectStat objectStat = await minioClient.StatObjectAsync("mybucket", "myobject");
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
| ``args``  | _RemoveObjectArgs_ | Arguments Object - name.  |
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

Removes a list of objects or a list of specified versionIDs of each object in the list.

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
            Console.WriteLine("Listed all delete errors for remove objects on  " + bucketName + "\n");
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
    List<Tuple<string, List<string>>> objectsVersions = new List<Tuple<string, List<string>>>();
    objectsVersions.Add(new Tuple<string, List<string>>(objectName, versionIDs));
    objectName = "myobject2";
    versionIDs = new List<string>();
    versionIDs.Add("abcobject2version1dce");
    versionIDs.Add("abcobject2version2dce");
    versionIDs.Add("abcobject2version3dce");
    objectsVersions.Add(new Tuple<string, List<string>>(objectName, versionIDs));
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
### RemoveIncompleteUploadAsync(string bucketName, string objectName)

`Task RemoveIncompleteUploadAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))`

Removes a partially uploaded object.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``objectName``  | _string_  | Object name in the bucket |
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
    await minioClient.RemoveIncompleteUploadAsync("mybucket", "myobject");
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

## 4. Presigned operations
<a name="presignedGetObject"></a>

### PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string, string> reqParams = null, DateTime? reqDate = null);
`Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string, string> reqParams = null, DateTime? reqDate = null)`

Generates a presigned URL for HTTP GET operations. Browsers/Mobile clients may point to this URL to directly download objects even if the bucket is private. This presigned URL can have an associated expiration time in seconds after which it is no longer operational. The default expiry is set to 7 days.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _String_ | Name of the bucket  |
| ``objectName``  | _String_  | Object name in the bucket |
| ``expiresInt``  | _Integer_  | Expiry in seconds. Default expiry is set to 7 days. |
| ``reqParams``   | _Dictionary<string,string>_ | Additional response header overrides supports response-expires, response-content-type, response-cache-control, response-content-disposition.|
| ``reqDate``   | _DateTime?_ | Optional request date and time. Defaults to DateTime.UtcNow if unset.|

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
    String url = await minioClient.PresignedGetObjectAsync("mybucket", "myobject", 60 * 60 * 24);
    Console.WriteLine(url);
}
catch(MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="presignedPutObject"></a>
### PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)

`Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)`

Generates a presigned URL for HTTP PUT operations. Browsers/Mobile clients may point to this URL to upload objects directly to a bucket even if it is private. This presigned URL can have an associated expiration time in seconds after which it is no longer operational. The default expiry is set to 7 days.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``objectName``  | _string_  | Object name in the bucket |
| ``expiresInt``  | _int_  | Expiry in seconds. Default expiry is set to 7 days. |

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
    String url = await minioClient.PresignedPutObjectAsync("mybucket", "myobject", 60 * 60 * 24);
    Console.WriteLine(url);
}
catch(MinioException e)
{
    Console.WriteLine("Error occurred: " + e);
}
```

<a name="presignedPostPolicy"></a>
### PresignedPostPolicy(PostPolicy policy)

`Task<Dictionary<string, string>> PresignedPostPolicyAsync(PostPolicy policy)`

Allows setting policy conditions to a presigned URL for POST operations. Policies such as bucket name to receive object uploads, key name prefixes, expiry policy may be set.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``PostPolicy``  | _PostPolicy_  | Post policy of an object.  |


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

    Dictionary<string, string> formData = minioClient.Api.PresignedPostPolicy(policy);
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
