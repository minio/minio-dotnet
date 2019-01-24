# .NET Client API Reference [![Slack](https://slack.minio.io/slack?type=svg)](https://slack.minio.io)
 
## Initialize Minio Client object.

## Minio

```cs
var minioClient = new MinioClient("play.minio.io:9000",
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
| [`listIncompleteUploads`](#listIncompleteUploads)  | [`removeObjects`](#removeObjects) |   |   |
| | [`removeIncompleteUpload`](#removeIncompleteUpload) |   |   |


## 1. Constructors

<a name="constructors"></a>

|  |
|---|
|`public MinioClient(string endpoint, string accessKey = "", string secretKey = "")`   |
| Creates Minio client object with given endpoint.AccessKey,secretKey and region are optional parameters,and can be omitted for anonymous access. 
  The client object uses Http access by default. To use Https, chain method WithSSL() to client object to use secure transfer protocol   |


__Parameters__

| Param  | Type  | Description  |
|---|---|---|
| `endpoint`  |  _string_ | endPoint is an URL, domain name, IPv4 address or IPv6 address.Valid endpoints are listed below: |
| | |s3.amazonaws.com |
| | |play.minio.io:9000 |
| | |localhost |
| | |play.minio.io|
| `accessKey`   | _string_   |accessKey is like user-id that uniquely identifies your account.This field is optional and can be omitted for anonymous access. |
|`secretKey`  |  _string_   | secretKey is the password to your account.This field is optional and can be omitted for anonymous access.|
|`region`  |  _string_   | region to which calls should be made.This field is optional and can be omitted.|

__Secure Access__

|  |
|---|
|`Chain .WithSSL() to Minio Client object to use https instead of http. `   |


__Example__


### Minio


```cs
// 1. public MinioClient(String endpoint)
MinioClient minioClient = new MinioClient("play.minio.io:9000");

// 2. public MinioClient(String endpoint, String accessKey, String secretKey)
MinioClient minioClient = new MinioClient("play.minio.io:9000", 
                                          accessKey:"Q3AM3UQ867SPQQA43P2F", 
                                          secretKey:"zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG"
                                          ).WithSSL();
```


### AWS S3


```cs
// 1. public MinioClient(String endpoint)
MinioClient s3Client = new MinioClient("s3.amazonaws.com").WithSSL();

// 2. public MinioClient(String endpoint, String accessKey, String secretKey)
MinioClient s3Client = new MinioClient("s3.amazonaws.com:80", 
                                       accessKey:"YOUR-ACCESSKEYID", 
                                       secretKey:"YOUR-SECRETACCESSKEY").WithSSL();
```

## 2. Bucket operations

<a name="makeBucket"></a>
### MakeBucketAsync(string bucketName, string location="us-east-1")
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
      Console.Out.WriteLine("mybucket already exists");
   } 
   else 
   {
     // Create bucket 'my-bucketname'.
     await minioClient.MakeBucketAsync("mybucket");
     Console.Out.WriteLine("mybucket is created successfully");
   }
} 
catch (MinioException e) 
{
   Console.Out.WriteLine("Error occurred: " + e);
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
        Console.Out.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
    }
} 
catch (MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
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
   Console.Out.WriteLine("bucket-name " + ((found == true) ? "exists" : "does not exist"));
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
        Console.Out.WriteLine("mybucket is removed successfully");
    }
    else 
    {
        Console.Out.WriteLine("mybucket does not exist");
    }
} 
catch(MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="listObjects"></a>
### ListObjectsAsync(string bucketName, string prefix = null, bool recursive = true)

`IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = true, CancellationToken cancellationToken = default(CancellationToken))`

Lists all objects in a bucket. 

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``prefix``  | _string_  | Prefix string. List objects whose name starts with ``prefix`` |
| ``recursive``  | _bool_  | when false, emulates a directory structure where each listing returned is either a full object or part of the object's key up to the first '/'. All objects with the same prefix up to the first '/' will be merged into one entry |
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
        IObservable<Item> observable = minioClient.ListObjectsAsync("mybucket", "prefix", true);
        IDisposable subscription = observable.Subscribe(
				item => Console.WriteLine("OnNext: {0}", item.Key),
				ex => Console.WriteLine("OnError: {0}", ex.Message),
				() => Console.WriteLine("OnComplete: {0}"));    
    } 
    else 
    {
        Console.Out.WriteLine("mybucket does not exist");
    }
} 
catch (MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
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
        Console.Out.WriteLine("mybucket does not exist");
    }
} 
catch (MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="getBucketPolicy"></a>
### GetPolicyAsync(string bucketName)
`Task<String> GetPolicyAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))`

Get bucket policy.


__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket.  |
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
    String policyJson = await minioClient.GetPolicyAsync("myBucket");
    Console.Out.WriteLine("Current policy: " + policy.GetType().ToString());
} 
catch (MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="setBucketPolicy"></a>
### SetPolicyAsync(string bucketName, string policyJson)
`Task SetPolicyAsync(string bucketName, string policyJson, CancellationToken cancellationToken = default(CancellationToken))`

Set policy on bucket.

__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``policyJson``  | _string_  | Policy as a json string |
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
    await minioClient.SetPolicyAsync("myBucket");
}
catch (MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```


<a name="setBucketNotification"></a>
### SetBucketNotificationAsync(string bucketName,BucketNotification notification)
`Task SetBucketNotificationAsync(string bucketName, BucketNotification notification, CancellationToken cancellationToken = default(CancellationToken))`

Sets notification configuration for a given bucket

__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``notification``  | _BucketNotification_  | Notifications to apply |
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
        
    await minio.SetBucketNotificationsAsync(bucketName, 
                                        notification);
    Console.Out.WriteLine("Notifications set for the bucket " + bucketName + " successfully");
}
catch (MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="getBucketNotification"></a>
### GetBucketNotificationAsync(string bucketName)
`Task<BucketNotification> GetBucketNotificationAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))`

Get bucket notification configuration


__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket.  |
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
    BucketNotification notifications = await minioClient.GetBucketNotificationAsync(bucketName);
    Console.Out.WriteLine("Notifications is " + notifications.ToXML());
} 
catch (MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="removeAllBucketNotification"></a>
### RemoveAllBucketNotificationsAsync(string bucketName)
`Task RemoveAllBucketNotificationsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))`

Remove all notification configurations set on the bucket


__Parameters__

|Param   | Type   | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket.  |
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
    await minioClient.RemoveAllBucketNotificationsAsync(bucketName);
    Console.Out.WriteLine("Notifications successfully removed from the bucket " + bucketName);
} 
catch (MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
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
      Console.Out.WriteLine("Error occurred: " + e);
  }
```

<a name="getObject"></a>
### GetObjectAsync(string bucketName, string objectName, long offset,long length, Action<Stream> callback, ServerSideEncryption sse)

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
      Console.Out.WriteLine("Error occurred: " + e);
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
   Console.Out.WriteLine("Error occurred: " + e);
}
```
<a name="putObject"></a>
### PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType,ServerSideEncryption sse)

` Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType,Dictionary<string,string> metaData=null,ServerSideEncryption sse = null,CancellationToken cancellationToken = default(CancellationToken))`


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
                               "application/octet-stream",ssec);
    Console.Out.WriteLine("island.jpg is uploaded successfully");
} 
catch(MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="putObject"></a>
### PutObjectAsync(string bucketName, string objectName, string filePath, string contentType=null,ServerSideEncryption sse)

` Task PutObjectAsync(string bucketName, string objectName, string filePath, string contentType=null,Dictionary<string,string> metaData=null, ServerSideEncryption sse=null,CancellationToken cancellationToken = default(CancellationToken))`


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
    await minio.PutObjectAsync("mybucket", "island.jpg", "/mnt/photos/island.jpg",contentType: "application/octet-stream");
    Console.Out.WriteLine("island.jpg is uploaded successfully");
} 
catch(MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```
<a name="statObject"></a>
### StatObjectAsync(string bucketName, string objectName,ServerSideEncryption sse)

`Task<ObjectStat> StatObjectAsync(string bucketName, string objectName,ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))`

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
   Console.Out.WriteLine(objectStat);
} 
catch(MinioException e) 
{
   Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="copyObject"></a>
### CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null,Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null)

*`Task<CopyObjectResult> CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, Dictionary<string, string> metadata = null,ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null,CancellationToken cancellationToken = default(CancellationToken))`*

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
   ServerSideEncryption sseSrc,sseDst;
   // Uncomment to specify source and destination Server-side encryption options
   /*
    Aes aesEncryption = Aes.Create();
    aesEncryption.KeySize = 256;
    aesEncryption.GenerateKey();
    sseSrc = new SSEC(aesEncryption.Key);
    sseDst = new SSES3();
   */
   await minioClient.CopyObjectAsync("mybucket",  "island.jpg", "mydestbucket", "processed.png", copyConditions,sseSrc:sseSrc, sseDest:sseDst);
   Console.Out.WriteLine("island.jpg is uploaded successfully");
} 
catch(MinioException e) 
{
   Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="removeObject"></a>
### RemoveObjectAsync(string bucketName, string objectName)

`Task RemoveObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))`

Removes an object.

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
    // Remove objectname from the bucket my-bucketname.
    await minioClient.RemoveObjectAsync("mybucket", "myobject");
    Console.Out.WriteLine("successfully removed mybucket/myobject");
} 
catch (MinioException e) 
{
    Console.Out.WriteLine("Error: " + e);
}
```
<a name="removeObjects"></a>
### RemoveObjectAsync(string bucketName, IEnumerable<string> objectsList)

`Task<IObservable<DeleteError>> RemoveObjectAsync(string bucketName, IEnumerable<string> objectsList, CancellationToken cancellationToken = default(CancellationToken))`

Removes a list of objects.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | Name of the bucket  |
| ``objectsList``  | _IEnumerable<string>_  | IEnumerable of Object names |
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
    List<String> objectNames = new LinkedList<String>();
    objectNames.add("my-objectname1");
    objectNames.add("my-objectname2");
    objectNames.add("my-objectname3");
    // Remove list of objects in objectNames from the bucket bucketName.
    IObservable<DeleteError> observable = await minio.RemoveObjectAsync(bucketName, objectNames);
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
    Console.Out.WriteLine("Error: " + e);
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
    Console.Out.WriteLine("successfully removed all incomplete upload session of my-bucketname/my-objectname");
} 
catch(MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```

## 4. Presigned operations
<a name="presignedGetObject"></a>

### PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string,string> reqParams = null);
`Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string,string> reqParams = null)`

Generates a presigned URL for HTTP GET operations. Browsers/Mobile clients may point to this URL to directly download objects even if the bucket is private. This presigned URL can have an associated expiration time in seconds after which it is no longer operational. The default expiry is set to 7 days.

__Parameters__


|Param   | Type	  | Description  |
|:--- |:--- |:--- |
| ``bucketName``  | _String_ | Name of the bucket  |
| ``objectName``  | _String_  | Object name in the bucket |
| ``expiresInt``  | _Integer_  | Expiry in seconds. Default expiry is set to 7 days. |
| ``reqParams``   | _Dictionary<string,string>_ | Additional response header overrides supports response-expires, response-content-type, response-cache-control, response-content-disposition.|
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
    Console.Out.WriteLine(url);
} 
catch(MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
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
    Console.Out.WriteLine(url);
}
catch(MinioException e) 
{
    Console.Out.WriteLine("Error occurred: " + e);
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
| ``Task<Dictionary<string,string>>``: Map of strings to construct form-data. | Listed Exceptions: |
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
    Console.Out.WriteLine(curlCommand);
} 
catch(MinioException e) 
{
  Console.Out.WriteLine("Error occurred: " + e);
}
```
## Client Custom Settings
<a name="SetAppInfo"></a>
### SetAppInfo(string appName, tring appVersion)
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
