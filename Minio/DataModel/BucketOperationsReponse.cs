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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.DataModel;
using Minio.DataModel.ILM;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Replication;
using Minio.DataModel.Tags;

namespace Minio;

internal class GetVersioningResponse : GenericXmlResponse<VersioningConfiguration>
{
    internal GetVersioningResponse(ResponseResult result)
        : base(result)
    {
    }

    internal VersioningConfiguration VersioningConfig => _result;
}

internal class ListBucketsResponse : GenericXmlResponse<ListAllMyBucketsResult>
{
    internal ListAllMyBucketsResult BucketsResult => _result;

    internal ListBucketsResponse(ResponseResult result)
        : base(result)
    {
    }
}

internal class ListObjectsItemResponse
{
    internal Item BucketObjectsLastItem;
    internal IObserver<Item> ItemObservable;

    internal ListObjectsItemResponse(ListObjectsArgs args, Tuple<ListBucketResult, List<Item>> objectList, IObserver<Item> obs)
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

internal class GetObjectsListResponse : GenericXmlResponse<ListBucketResult>
{
    internal ListBucketResult BucketResult => _result;

    internal Tuple<ListBucketResult, List<Item>> ObjectsTuple;

    internal GetObjectsListResponse(ResponseResult result)
        : base(result)
    {
        if (IsOkWithContent)
            return;

        var root = XDocument.Parse(result.Content);
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

internal class GetObjectsVersionsListResponse : GenericXmlResponse<ListVersionsResult>
{
    internal ListVersionsResult BucketResult => _result;
    internal Tuple<ListVersionsResult, List<Item>> ObjectsTuple;

    internal GetObjectsVersionsListResponse(ResponseResult result)
        : base(result)
    {
        if (IsOkWithContent)
            return;

        var root = XDocument.Parse(result.Content);
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
    internal GetPolicyResponse(ResponseResult result)
        : base(result)
    {
    }

    internal string PolicyJsonString => ResponseContent;
}

internal class GetBucketNotificationsResponse : GenericXmlResponse<BucketNotification>
{
    internal GetBucketNotificationsResponse(ResponseResult result)
        : base(result)
    {
    }

    internal BucketNotification BucketNotificationConfiguration => _result;
}

internal class GetBucketEncryptionResponse : GenericXmlResponse<ServerSideEncryptionConfiguration>
{
    internal GetBucketEncryptionResponse(ResponseResult result)
        : base(result)
    {
    }

    internal ServerSideEncryptionConfiguration BucketEncryptionConfiguration => _result;
}

internal class GetBucketTagsResponse : GenericXmlResponse<Tagging>
{
    internal GetBucketTagsResponse(ResponseResult result)
        : base(result)
    {
    }

    internal Tagging BucketTags => _result;

    protected override string ConvertContent(string content)
    {
        // Remove xmlns content for config serialization
        return utils.RemoveNamespaceInXML(content);
    }
}

internal class GetObjectLockConfigurationResponse : GenericXmlResponse<ObjectLockConfiguration>
{
    internal GetObjectLockConfigurationResponse(ResponseResult result)
        : base(result)
    {
    }

    internal ObjectLockConfiguration LockConfiguration => _result;
}

internal class GetBucketLifecycleResponse : GenericXmlResponse<LifecycleConfiguration>
{
    internal GetBucketLifecycleResponse(ResponseResult result)
        : base(result)
    {
    }

    protected override string ConvertContent(string content)
    {
        // Remove xmlns content for config serialization
        return utils.RemoveNamespaceInXML(content);
    }

    internal LifecycleConfiguration BucketLifecycle => _result;
}

internal class GetBucketReplicationResponse : GenericXmlResponse<ReplicationConfiguration>
{
    internal GetBucketReplicationResponse(ResponseResult result)
        : base(result)
    {
    }

    internal ReplicationConfiguration Config => _result;
}