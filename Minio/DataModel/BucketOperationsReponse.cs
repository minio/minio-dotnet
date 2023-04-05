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
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
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

        VersioningConfig = Utils.DeserializeXml<VersioningConfiguration>(Encoding.UTF8
                .GetBytes(responseContent).AsMemory().AsStream());
    }

    internal VersioningConfiguration VersioningConfig { get; set; }
}

internal class ListBucketsResponse : GenericResponse
{
    internal ListAllMyBucketsResult BucketsResult;

    internal ListBucketsResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) || !HttpStatusCode.OK.Equals(statusCode))
            return;

        BucketsResult = Utils.DeserializeXml<ListAllMyBucketsResult>(Encoding.UTF8
                .GetBytes(responseContent).AsMemory().AsStream());
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

        BucketResult = Utils.DeserializeXml<ListBucketResult>(Encoding.UTF8
                .GetBytes(responseContent).AsMemory().AsStream());

        var root = XDocument.Parse(responseContent);
        XNamespace ns = Utils.DetermineNamespace(root);

        var items = from c in root.Root.Descendants(ns + "Contents")
            select new Item
            {
                Key = c.Element(ns + "Key").Value,
                LastModified = c.Element(ns + "LastModified").Value,
                ETag = c.Element(ns + "ETag").Value,
                Size = ulong.Parse(c.Element(ns + "Size").Value,
                    CultureInfo.CurrentCulture),
                IsDir = false
            };
        var prefixes = from c in root.Root.Descendants(ns + "CommonPrefixes")
            select new Item
            {
                Key = c.Element(ns + "Prefix").Value,
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

        BucketResult = Utils.DeserializeXml<ListVersionsResult>(Encoding.UTF8
                .GetBytes(responseContent).AsMemory().AsStream());

        var root = XDocument.Parse(responseContent);
        XNamespace ns = Utils.DetermineNamespace(root);

        var items = from c in root.Root.Descendants(ns + "Version")
            select new Item
            {
                Key = c.Element(ns + "Key").Value,
                LastModified = c.Element(ns + "LastModified").Value,
                ETag = c.Element(ns + "ETag").Value,
                VersionId = c.Element(ns + "VersionId").Value,
                Size = ulong.Parse(c.Element(ns + "Size").Value,
                    CultureInfo.CurrentCulture),
                IsDir = false
            };
        var prefixes = from c in root.Root.Descendants(ns + "CommonPrefixes")
            select new Item
            {
                Key = c.Element(ns + "Prefix").Value,
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
        Memory<byte> content = Encoding.UTF8.GetBytes(ResponseContent);
        using var streamReader = new StreamReader(content.AsStream());
        PolicyJsonString = await streamReader.ReadToEndAsync()
            .ConfigureAwait(false);
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
        BucketNotificationConfiguration = Utils.DeserializeXml<BucketNotification>(Encoding.UTF8
                .GetBytes(responseContent).AsMemory().AsStream());
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
        BucketEncryptionConfiguration = Utils.DeserializeXml<ServerSideEncryptionConfiguration>(Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream());
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
        responseContent = Utils.RemoveNamespaceInXML(responseContent);
        BucketTags = Utils.DeserializeXml<Tagging>(Encoding.UTF8.GetBytes(responseContent).AsMemory()
                .AsStream());
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
        LockConfiguration = Utils.DeserializeXml<ObjectLockConfiguration>(Encoding.UTF8
                .GetBytes(responseContent).AsMemory().AsStream());
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
        responseContent = Utils.RemoveNamespaceInXML(responseContent);
        BucketLifecycle = Utils.DeserializeXml<LifecycleConfiguration>(Encoding.UTF8
                .GetBytes(responseContent).AsMemory().AsStream());
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

        Config = Utils.DeserializeXml<ReplicationConfiguration>(Encoding.UTF8
                .GetBytes(responseContent).AsMemory().AsStream());
    }

    internal ReplicationConfiguration Config { set; get; }
}