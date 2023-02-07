/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.DataModel;
using Minio.DataModel.ILM;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Replication;
using Minio.DataModel.Tags;

namespace Minio;

internal class GetVersioningResponse : GenericResponse
{
    internal GetVersioningResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
            return;

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            stream.Position = 0;

            VersioningConfig =
                (VersioningConfiguration)new XmlSerializer(typeof(VersioningConfiguration)).Deserialize(stream);
        }
    }

    internal VersioningConfiguration VersioningConfig { get; set; }
}

internal class ListBucketsResponse : GenericResponse
{
    internal ListAllMyBucketsResult BucketsResult;

    internal ListBucketsResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
            return;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            BucketsResult =
                (ListAllMyBucketsResult)new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream);
        }
    }
}

internal class ListObjectsItemResponse
{
    internal Item BucketObjectsLastItem;
    internal IObserver<Item> ItemObservable;

    internal ListObjectsItemResponse(ListObjectsArgs args, Tuple<ListBucketResult, List<Item>> objectList,
        IObserver<Item> obs)
    {
        ItemObservable = obs;
        var marker = string.Empty;
        NextMarker = string.Empty;
        foreach (var item in objectList.Item2)
        {
            BucketObjectsLastItem = item;
            if (objectList.Item1.EncodingType == "url") item.Key = HttpUtility.UrlDecode(item.Key);
            ItemObservable.OnNext(item);
        }

        if (objectList.Item1.NextMarker != null)
        {
            if (objectList.Item1.EncodingType == "url")
                NextMarker = HttpUtility.UrlDecode(objectList.Item1.NextMarker);
            else
                NextMarker = objectList.Item1.NextMarker;
        }
        else if (BucketObjectsLastItem != null)
        {
            if (objectList.Item1.EncodingType == "url")
                NextMarker = HttpUtility.UrlDecode(BucketObjectsLastItem.Key);
            else
                NextMarker = BucketObjectsLastItem.Key;
        }
    }

    internal string NextMarker { get; }
}

internal class ListObjectVersionResponse
{
    internal Item BucketObjectsLastItem;
    internal IObserver<Item> ItemObservable;

    internal ListObjectVersionResponse(ListObjectsArgs args, Tuple<ListVersionsResult, List<Item>> objectList,
        IObserver<Item> obs)
    {
        ItemObservable = obs;
        var marker = string.Empty;
        foreach (var item in objectList.Item2)
        {
            BucketObjectsLastItem = item;
            if (objectList.Item1.EncodingType == "url") item.Key = HttpUtility.UrlDecode(item.Key);
            ItemObservable.OnNext(item);
        }

        if (objectList.Item1.NextMarker != null)
        {
            if (objectList.Item1.EncodingType == "url")
            {
                NextMarker = HttpUtility.UrlDecode(objectList.Item1.NextMarker);
                NextKeyMarker = HttpUtility.UrlDecode(objectList.Item1.NextKeyMarker);
                NextVerMarker = HttpUtility.UrlDecode(objectList.Item1.NextVersionIdMarker);
            }
            else
            {
                NextMarker = objectList.Item1.NextMarker;
                NextKeyMarker = objectList.Item1.NextKeyMarker;
                NextVerMarker = objectList.Item1.NextVersionIdMarker;
            }
        }
        else if (BucketObjectsLastItem != null)
        {
            if (objectList.Item1.EncodingType == "url")
            {
                NextMarker = HttpUtility.UrlDecode(BucketObjectsLastItem.Key);
                NextKeyMarker = HttpUtility.UrlDecode(BucketObjectsLastItem.Key);
                NextVerMarker = HttpUtility.UrlDecode(BucketObjectsLastItem.VersionId);
            }
            else
            {
                NextMarker = BucketObjectsLastItem.Key;
                NextKeyMarker = BucketObjectsLastItem.Key;
                NextVerMarker = BucketObjectsLastItem.VersionId;
            }
        }
    }

    internal string NextMarker { get; }
    internal string NextKeyMarker { get; }
    internal string NextVerMarker { get; }
}

internal class GetObjectsListResponse : GenericResponse
{
    internal ListBucketResult BucketResult;
    internal Tuple<ListBucketResult, List<Item>> ObjectsTuple;

    internal GetObjectsListResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
            return;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            BucketResult = (ListBucketResult)new XmlSerializer(typeof(ListBucketResult)).Deserialize(stream);
        }

        var root = XDocument.Parse(responseContent);
        var items = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Contents")
            select new Item
            {
                Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                LastModified = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}LastModified").Value,
                ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value,
                Size = ulong.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value,
                    CultureInfo.CurrentCulture),
                IsDir = false
            };
        var prefixes = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}CommonPrefixes")
            select new Item
            {
                Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Prefix").Value,
                IsDir = true
            };
        items = items.Concat(prefixes);
        ObjectsTuple = Tuple.Create(BucketResult, items.ToList());
    }
}

internal class GetObjectsVersionsListResponse : GenericResponse
{
    internal ListVersionsResult BucketResult;
    internal Tuple<ListVersionsResult, List<Item>> ObjectsTuple;

    internal GetObjectsVersionsListResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
            return;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            BucketResult = (ListVersionsResult)new XmlSerializer(typeof(ListVersionsResult)).Deserialize(stream);
        }

        var root = XDocument.Parse(responseContent);
        var items = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Version")
            select new Item
            {
                Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                LastModified = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}LastModified").Value,
                ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value,
                VersionId = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}VersionId").Value,
                Size = ulong.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value,
                    CultureInfo.CurrentCulture),
                IsDir = false
            };
        var prefixes = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}CommonPrefixes")
            select new Item
            {
                Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Prefix").Value,
                IsDir = true
            };
        items = items.Concat(prefixes);
        ObjectsTuple = Tuple.Create(BucketResult, items.ToList());
    }
}

internal class GetPolicyResponse : GenericResponse
{
    internal GetPolicyResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
            return;
        Initialize().Wait();
    }

    internal string PolicyJsonString { get; private set; }

    private async Task Initialize()
    {
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(ResponseContent)))
        using (var streamReader = new StreamReader(stream))
        {
            PolicyJsonString = await streamReader.ReadToEndAsync()
                .ConfigureAwait(false);
        }
    }
}

internal class GetBucketNotificationsResponse : GenericResponse
{
    internal GetBucketNotificationsResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
        {
            BucketNotificationConfiguration = new BucketNotification();
            return;
        }

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            BucketNotificationConfiguration =
                (BucketNotification)new XmlSerializer(typeof(BucketNotification)).Deserialize(stream);
        }
    }

    internal BucketNotification BucketNotificationConfiguration { set; get; }
}

internal class GetBucketEncryptionResponse : GenericResponse
{
    internal GetBucketEncryptionResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) || !HttpStatusCode.OK.Equals(statusCode))
        {
            BucketEncryptionConfiguration = null;
            return;
        }

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            BucketEncryptionConfiguration =
                (ServerSideEncryptionConfiguration)new XmlSerializer(typeof(ServerSideEncryptionConfiguration))
                    .Deserialize(stream);
        }
    }

    internal ServerSideEncryptionConfiguration BucketEncryptionConfiguration { get; set; }
}

internal class GetBucketTagsResponse : GenericResponse
{
    internal GetBucketTagsResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
        {
            BucketTags = null;
            return;
        }

        // Remove namespace from response content, if present.
        responseContent = utils.RemoveNamespaceInXML(responseContent);
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            BucketTags = (Tagging)new XmlSerializer(typeof(Tagging)).Deserialize(stream);
        }
    }

    internal Tagging BucketTags { set; get; }
}

internal class GetObjectLockConfigurationResponse : GenericResponse
{
    internal GetObjectLockConfigurationResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) || !HttpStatusCode.OK.Equals(statusCode))
        {
            LockConfiguration = null;
            return;
        }

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            LockConfiguration =
                (ObjectLockConfiguration)new XmlSerializer(typeof(ObjectLockConfiguration)).Deserialize(stream);
        }
    }

    internal ObjectLockConfiguration LockConfiguration { get; set; }
}

internal class GetBucketLifecycleResponse : GenericResponse
{
    internal GetBucketLifecycleResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
        {
            BucketLifecycle = null;
            return;
        }

        //Remove xmlns content for config serialization
        responseContent = utils.RemoveNamespaceInXML(responseContent);
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            BucketLifecycle =
                (LifecycleConfiguration)new XmlSerializer(typeof(LifecycleConfiguration)).Deserialize(stream);
        }
    }

    internal LifecycleConfiguration BucketLifecycle { set; get; }
}

internal class GetBucketReplicationResponse : GenericResponse
{
    internal GetBucketReplicationResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) ||
            !HttpStatusCode.OK.Equals(statusCode))
        {
            Config = null;
            return;
        }

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
        {
            Config = (ReplicationConfiguration)new XmlSerializer(typeof(ReplicationConfiguration)).Deserialize(stream);
        }
    }

    internal ReplicationConfiguration Config { set; get; }
}