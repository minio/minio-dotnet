# .NET Client API参考文档 [![Slack](https://slack.minio.io/slack?type=svg)](https://slack.minio.io)

## 初始化Minio Client object。

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

| 操作存储桶 |  操作对象 | Presigned操作  | 存储桶策略
|:--- |:--- |:--- |:--- |
| [`makeBucket`](#makeBucket)  |[`getObject`](#getObject)   |[`presignedGetObject`](#presignedGetObject)   | [`getBucketPolicy`](#getBucketPolicy)   |
| [`listBuckets`](#listBuckets)  | [`putObject`](#putObject)  | [`presignedPutObject`](#presignedPutObject)  | [`setBucketPolicy`](#setBucketPolicy)   |
| [`bucketExists`](#bucketExists)  | [`copyObject`](#copyObject)  | [`presignedPostPolicy`](#presignedPostPolicy)  |[`setBucketNotification`](#setBucketNotification)  |
| [`removeBucket`](#removeBucket)  | [`statObject`](#statObject) |   | [`getBucketNotification`](#getBucketNotification)  |
| [`listObjects`](#listObjects)  | [`removeObject`](#removeObject) |   |  [`removeAllBucketNotification`](#removeAllBucketNotification) |
| [`listIncompleteUploads`](#listIncompleteUploads)  | [`removeObjects`](#removeObjects) |   |   |
| | [`removeIncompleteUpload`](#removeIncompleteUpload) |   |   |


## 1. 构造函数

<a name="constructors"></a>

|  |
|---|
|`public MinioClient(string endpoint, string accessKey = "", string secretKey = "")`   |
| 使用给定的endpoint创建个一个MinioClient对象。AccessKey、secretKey和region是可选参数，如果为空的话代表匿名访问。该client对象默认使用HTTP进行访问，如果想使用HTTPS，针对client对象链式调用WithSSL()可启用安全的传输协议。   |


__参数__

| 参数    | 类型    | 描述    |
|---|---|---|
| `endpoint`  |  _string_ | endPoint是一个URL，域名，IPv4或者IPv6地址。以下是合法的endpoints: |
| | |s3.amazonaws.com |
| | |play.minio.io:9000 |
| | |localhost |
| | |play.minio.io|
| `accessKey`   | _string_   |accessKey类似于用户ID，用于唯一标识你的账户。可选，为空代表匿名访问。 |
|`secretKey`  |  _string_   | secretKey是你账户的密码。可选，为空代表匿名访问。 |
|`region`  |  _string_   | 对象存储的region。可选。|

__安全访问__

|  |
|---|
|`client对象链式调用.WithSSL()，可以启用https。 `   |


__示例__


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

## 2. 操作存储桶

<a name="makeBucket"></a>
### MakeBucketAsync(string bucketName, string location="us-east-1")
`Task MakeBucketAsync(string bucketName, string location = "us-east-1", CancellationToken cancellationToken = default(CancellationToken))`

创建一个存储桶。


__参数__

| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_ | 存储桶名称。  |
| ``region``  | _string_| 可选参数。默认是us-east-1。  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。 默认是default(CancellationToken) |

| 返回值类型	  | 异常    |
|:--- |:--- |
| ``Task``  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``AccessDeniedException`` : 拒绝访问。            |
|        | ``RedirectionException`` : 服务器重定向。 |
|        | ``InternalClientException`` : 内部错误。        |


__示例__


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

列出所有的存储桶。

| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |

|返回值类型	  | 异常    |
|:--- |:--- |
| ``Task<ListAllMyBucketsResult>`` : 包含存储桶类型列表的Task。  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``AccessDeniedException`` : 拒绝访问。           |
|        | ``InvalidOperationException``: xml数据反序列化异常。 |
|        | ``ErrorResponseException`` : 执行异常。            |
|        | ``InternalClientException`` : 内部错误。        |


__示例__


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

检查存储桶是否存在。


__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。 默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task<bool>`` ： 如果存储桶存在的话则是true。  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``AccessDeniedException`` : 拒绝访问。          |
|        | ``ErrorResponseException`` : 执行异常。         |
|        | ``InternalClientException`` : 内部错误。        |



__示例__


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

删除一个存储桶


注意： -  removeBucket不会删除存储桶中的对象，你需要调用removeObject API清空存储桶内的对象。


__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  Task  | 列出的异常： |
|        | ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``AccessDeniedException`` : 拒绝访问。            |
|        | ``ErrorResponseException`` : 执行异常。            |
|        | ``InternalClientException`` : 内部错误。        |
|        | ``BucketNotFoundException`` : 存储桶不存在。          |


__示例__


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

列出存储桶里的对象。

__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``prefix``  | _string_  | 对象的前缀。 |
| ``recursive``  | _bool_  | `true`代表递归查找，`false`代表类似文件夹查找，以'/'分隔，不查子文件夹。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


|返回值类型	  | 异常    |
|:--- |:--- |
| ``IObservable<Item>``:an Observable of Items.  | _None_  |


__示例__


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

列出存储桶中未完整上传的对象。


__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``prefix``  | _string_  | 对象的前缀。 |
| ``recursive``  | _bool_  | `true`代表递归查找，`false`代表类似文件夹查找，以'/'分隔，不查子文件夹。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


|返回值类型	  | 异常    |
|:--- |:--- |
| ``IObservable<Upload> ``  | _None_  |


__示例__


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
### GetPolicyAsync(string bucketName, string objectPrefix)
`Task<PolicyType> GetPolicyAsync(string bucketName, string objectPrefix, CancellationToken cancellationToken = default(CancellationToken))`

获取存储桶或者对象前缀的访问权限。


__参数__

| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``objectPrefix``  | _string_  | 该存储桶下的对象前缀 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task<PolicyType>``: 指定存储桶和对象前缀的存储桶策略。  | 列出的异常： |
|        | ``InvalidBucketNameException `` : 无效的存储桶名称。       |
|        | ``InvalidObjectPrefixException`` : 无效的对象前缀。        |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``AccessDeniedException`` : 拒绝访问。            |
|        | ``InternalClientException`` : 内部错误。        |
|        | ``BucketNotFoundException`` : 存储桶不存在。          |


__示例__


```cs
try
{
    PolicyType policy = await minioClient.GetPolicyAsync("myBucket", objectPrefix:"downloads");
    Console.Out.WriteLine("Current policy: " + policy.GetType().ToString());
}
catch (MinioException e)
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="setBucketPolicy"></a>
### SetPolicyAsync(string bucketName, string objectPrefix, PolicyType policyType)
`Task SetPolicyAsync(string bucketName, string objectPrefix, PolicyType policyType, CancellationToken cancellationToken = default(CancellationToken))`

针对存储桶和对象前缀设置访问策略。

__参数__

| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``objectPrefix``  | _string_  | 对象前缀。 |
| ``PolicyType``  | _PolicyType_  | 要设置的策略。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  Task  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |
|        | ``InvalidBucketNameException `` : 无效的存储桶名称。       |
|        | ``InvalidObjectPrefixException`` : 无效的对象前缀。        |



__示例__

```cs
try
{
    await minioClient.SetPolicyAsync("myBucket", "uploads",PolicyType.WRITE_ONLY);
}
catch (MinioException e)
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```


<a name="setBucketNotification"></a>
### SetBucketNotificationAsync(string bucketName,BucketNotification notification)
`Task SetBucketNotificationAsync(string bucketName, BucketNotification notification, CancellationToken cancellationToken = default(CancellationToken))`

给存储桶设置通知。

__参数__

| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``notification``  | _BucketNotification_  | 要设置的通知。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  Task  | 列出的异常： |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |
|        | ``InvalidBucketNameException `` : 无效的存储桶名称。       |
|        | ``InvalidOperationException``: 通知对象序列化异常。 |



__示例__
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

获取存储桶的通知配置。


__参数__

| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task<BucketNotification>``: 存储桶的当前通知配置。  | 列出的异常： |
|        | ``InvalidBucketNameException `` : 无效的存储桶名称。       |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``AccessDeniedException`` : 拒绝访问。            |
|        | ``InternalClientException`` : 内部错误。        |
|        | ``BucketNotFoundException`` : 存储桶不存在。          |
|        | ``InvalidOperationException``: xml数据反序列化异常。 |


__示例__


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

删除存储桶上所有配置的通知。


__参数__

| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task`:  | 列出的异常： |
|        | ``InvalidBucketNameException `` : 无效的存储桶名称。       |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``AccessDeniedException`` : 拒绝访问。            |
|        | ``InternalClientException`` : 内部错误。        |
|        | ``BucketNotFoundException`` : 存储桶不存在。          |
|        | ``InvalidOperationException``: xml数据序列化异常。 |


__示例__


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

## 3. 操作对象

<a name="getObject"></a>
### GetObjectAsync(string bucketName, string objectName, Action<Stream> callback)

`Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback, CancellationToken cancellationToken = default(CancellationToken))`

返回对象数据的流。


__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_ | 存储桶名称。  |
| ``objectName``  | _string_  | 存储桶里的对象名称。 |
| ``callback``    | _Action<Stream>_ | 处理流的回调函数。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task``: Task回调，返回含有对象数据的InputStream。  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |


__示例__


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
### GetObjectAsync(string bucketName, string objectName, long offset,long length, Action<Stream> callback)

`Task GetObjectAsync(string bucketName, string objectName, long offset, long length, Action<Stream> callback, CancellationToken cancellationToken = default(CancellationToken))`

下载对象指定区域的字节数组做为流。offset和length都必须传。


__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_ | 存储桶名称。  |
| ``objectName``  | _string_  | 存储桶里的对象名称。 |
| ``offset``| _long_ | ``offset`` 是起始字节的位置。 |
| ``length``| _long_| ``length``是要读取的长度。 |
| ``callback``    | _Action<Stream>_ | 处理流的回调函数。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task``: Task回调，返回含有对象数据的InputStream。  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |


__示例__


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
### GetObjectAsync(String bucketName, String objectName, String fileName)

`Task GetObjectAsync(string bucketName, string objectName, string fileName, CancellationToken cancellationToken = default(CancellationToken))`

下载并将文件保存到本地文件系统。


__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _String_  | 存储桶名称。  |
| ``objectName``  | _String_  | 存储桶里的对象名称。 |
| ``fileName``  | _String_  | 本地文件路径。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task `` | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |

__示例__

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
### PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType)

` Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType,Dictionary<string,string> metaData=null, CancellationToken cancellationToken = default(CancellationToken))`


通过Stream上传对象。



__参数__

| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``objectName``  | _string_  | 存储桶里的对象名称。 |
| ``data``  | _Stream_  | 要上传的Stream对象。 |
| ``size``  | _long_    | 流的大小。   |
| ``contentType``  | _string_ | 文件的Content type，默认是"application/octet-stream"。 |
| ``metaData``  | _Dictionary<string,string>_ | 元数据头信息的Dictionary对象，默认是null。 |

| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task``  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |
|        | ``EntityTooLargeException``: 要上传的大小超过最大允许值。 |
|        | ``UnexpectedShortReadException``: 读取的数据大小比指定的size要小。 |
|        | ``ArgumentNullException``: Stream为null。    |

__示例__


单个对象的最大大小限制在5TB。putObject在对象大于5MiB时，自动使用multiple parts方式上传。这样，当上传失败时，客户端只需要上传未成功的部分即可（类似断点上传）。上传的对象使用MD5SUM签名进行完整性验证。


```cs
try
{
    byte[] bs = File.ReadAllBytes(fileName);
    System.IO.MemoryStream filestream = new System.IO.MemoryStream(bs);

    await minio.PutObjectAsync("mybucket",
                               "island.jpg",
                                filestream,
                                filestream.Length,
                               "application/octet-stream");
    Console.Out.WriteLine("island.jpg is uploaded successfully");
}
catch(MinioException e)
{
    Console.Out.WriteLine("Error occurred: " + e);
}
```

<a name="putObject"></a>
### PutObjectAsync(string bucketName, string objectName, string filePath, string contentType=null)

` Task PutObjectAsync(string bucketName, string objectName, string filePath, string contentType=null,Dictionary<string,string> metaData=null, CancellationToken cancellationToken = default(CancellationToken))`


通过文件上传到对象中。



__参数__

| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``objectName``  | _string_  | 存储桶里的对象名称。 |
| ``fileName``  | _string_  | 要上传的本地文件名。 |
| ``contentType``  | _string_ | 文件的Content type，默认是"application/octet-stream"。 |
| ``metadata``  | _Dictionary<string,string>_ | 元数据头信息的Dictionary对象，默认是null。|

| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task``  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |
|        | ``EntityTooLargeException``: 要上传的大小超过最大允许值。 |

__示例__


单个对象的最大大小限制在5TB。putObject在对象大于5MiB时，自动使用multiple parts方式上传。这样，当上传失败时，客户端只需要上传未成功的部分即可（类似断点上传）。上传的对象使用MD5SUM签名进行完整性验证。


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
### StatObjectAsync(string bucketName, string objectName)

`Task<ObjectStat> StatObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))`

获取对象的元数据。


__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``objectName``  | _string_  | 存储桶里的对象名称。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task<ObjectStat>``: Populated object meta data. | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |



__示例__


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
### CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null)

*`Task<CopyObjectResult> CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, CancellationToken cancellationToken = default(CancellationToken))`*

从objectName指定的对象中将数据拷贝到destObjectName指定的对象。


__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 源存储桶名称。  |
| ``objectName``  | _string_  | 源存储桶中的源对象名称。 |
| ``destBucketName``  | _string_  | 目标存储桶名称。 |
| ``destObjectName`` | _string_ | 要创建的目标对象名称,如果为空，默认为源对象名称。|
| ``copyConditions`` | _CopyConditions_ | 拷贝操作的一些条件Map。|
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task``  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |
|        | ``ArgumentException`` : 存储桶不存在。 |

__示例__


本API执行了一个服务端的拷贝操作。

```cs
try
{
   CopyConditions copyConditions = new CopyConditions();
   copyConditions.setMatchETagNone("TestETag");

   await minioClient.CopyObjectAsync("mybucket",  "island.jpg", "mydestbucket", "processed.png", copyConditions);
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

删除一个对象。

__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``objectName``  | _string_  | 存储桶里的对象名称。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |

| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task``  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |



__示例__


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

删除多个对象。

__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``objectsList``  | _IEnumerable<string>_  | 含有多个对象名称的IEnumerable。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |

| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task``  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |



__示例__


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

删除一个未完整上传的对象。

__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``objectName``  | _string_  | 存储桶里的对象名称。 |
| ``cancellationToken``| _System.Threading.CancellationToken_ | 可选参数。默认是default(CancellationToken) |


| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task``  | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InternalClientException`` : 内部错误。        |


__示例__


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

## 4. Presigned操作
<a name="presignedGetObject"></a>

### PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string,string> reqParams = null);
`Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string,string> reqParams = null)`

生成一个给HTTP GET请求用的presigned URL。浏览器/移动端的客户端可以用这个URL进行下载，即使其所在的存储桶是私有的。这个presigned URL可以设置一个失效时间，默认值是7天。

__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _String_ | 存储桶名称。  |
| ``objectName``  | _String_  | 存储桶里的对象名称。 |
| ``expiresInt``  | _Integer_  | 失效时间（以秒为单位），默认是7天，不得大于七天。 |
| ``reqParams``   | _Dictionary<string,string>_ | 额外的响应头信息，支持response-expires、response-content-type、response-cache-control、response-content-disposition。|

| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task<string>`` : string包含可下载该对象的URL。 | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InvalidExpiryRangeException`` : 无效的失效时间。            |


__示例__


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

生成一个给HTTP PUT请求用的presigned URL。浏览器/移动端的客户端可以用这个URL进行上传，即使其所在的存储桶是私有的。这个presigned URL可以设置一个失效时间，默认值是7天。


__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``bucketName``  | _string_  | 存储桶名称。  |
| ``objectName``  | _string_  | 存储桶里的对象名称。 |
| ``expiresInt``  | _int_  | 失效时间（以秒为单位），默认是7天，不得大于七天。 |

| 返回值类型	  | 异常    |
|:--- |:--- |
|  ``Task<string>`` : string包含可下载该对象的URL。 | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``InvalidKeyException`` : 无效的access key或secret key。          |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``InvalidExpiryRangeException`` : 无效的失效时间。            |


__示例__

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

允许给POST请求的presigned URL设置策略，比如接收对象上传的存储桶名称的策略，key名称前缀，过期策略。

__参数__


| 参数    | 类型    | 描述    |
|:--- |:--- |:--- |
| ``PostPolicy``  | _PostPolicy_  | 对象的post策略。  |


| 返回值类型	  | 异常    |
|:--- |:--- |
| ``Task<Dictionary<string,string>>``: string的键值对，用于构造表单数据。 | 列出的异常： |
|        |  ``InvalidBucketNameException`` : 无效的存储桶名称。 |
|        | ``ConnectionException`` : 连接异常。            |
|        | ``NoSuchAlgorithmException`` : 在做签名计算时找不到指定的算法。           |



__示例__


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
给User-Agent添加应用信息。

__参数__

| 参数    | 类型    | 描述    |
|---|---|---|
|`appName`  | _string_  | 执行API请求的应用名称。 |
| `appVersion`| _string_ | 执行API请求的应用版本。 |


__示例__


```cs
// Set Application name and version to be used in subsequent API requests.
minioClient.SetAppInfo("myCloudApp", "1.0.0")
```
<a name="SetTraceOn"></a>
### SetTraceOn()
开启HTTP tracing，trace日志会输出到stdout。


__示例__


```cs
// Set HTTP tracing on.
minioClient.SetTraceOn()
```
<a name="SetTraceOff"></a>
### SetTraceOff()
Disables HTTP tracing.


__示例__
```cs
// Sets HTTP tracing off.
minioClient.SetTraceOff()
```
