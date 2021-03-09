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

using Minio.DataModel;
using Minio.DataModel.ILM;
using Minio.DataModel.Replication;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Minio
{
    internal class GetVersioningResponse : GenericResponse
    {
        internal VersioningConfiguration VersioningConfig { get; set; }
        internal GetVersioningResponse(HttpStatusCode statusCode, string responseContent)
                    : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.VersioningConfig = (VersioningConfiguration)new XmlSerializer(typeof(VersioningConfiguration)).Deserialize(stream);
            }
        }
    }

    internal class ListBucketsResponse : GenericResponse
    {
        internal ListAllMyBucketsResult BucketsResult;
        internal ListBucketsResponse(HttpStatusCode statusCode, string responseContent)
                    : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.BucketsResult = (ListAllMyBucketsResult)new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream);
            }
        }
    }

    internal class ListObjectsItemResponse
    {
        internal Item BucketObjectsLastItem;
        internal IObserver<Item> ItemObservable;
        internal string NextMarker { get; private set; }

        internal ListObjectsItemResponse(ListObjectsArgs args, Tuple<ListBucketResult, List<Item>> objectList, IObserver<Item> obs)
        {
            this.ItemObservable = obs;
            string marker = string.Empty;
            foreach (Item item in objectList.Item2)
            {
                this.BucketObjectsLastItem = item;
                if (objectList.Item1.EncodingType == "url")
                {
                    item.Key = HttpUtility.UrlDecode(item.Key);
                }
                this.ItemObservable.OnNext(item);
            }
            if (objectList.Item1.NextMarker != null)
            {
                if (objectList.Item1.EncodingType == "url")
                {
                    NextMarker = HttpUtility.UrlDecode(objectList.Item1.NextMarker);
                }
                else
                {
                    NextMarker = objectList.Item1.NextMarker;
                }
            }
            else if (this.BucketObjectsLastItem != null)
            {
                if (objectList.Item1.EncodingType == "url")
                {
                    NextMarker = HttpUtility.UrlDecode(this.BucketObjectsLastItem.Key);
                }
                else
                {
                    NextMarker = this.BucketObjectsLastItem.Key;
                }
            }
        }
    }

    internal class ListObjectVersionResponse
    {
        internal VersionItem BucketObjectsLastItem;
        internal IObserver<VersionItem> ItemObservable;
        internal string NextMarker { get; private set; }

        internal ListObjectVersionResponse(ListObjectsArgs args, Tuple<ListVersionsResult, List<VersionItem>> objectList, IObserver<VersionItem> obs)
        {
            this.ItemObservable = obs;
            string marker = string.Empty;
            foreach (VersionItem item in objectList.Item2)
            {
                this.BucketObjectsLastItem = item;
                if (objectList.Item1.EncodingType == "url")
                {
                    item.Key = HttpUtility.UrlDecode(item.Key);
                }
                this.ItemObservable.OnNext(item);
            }
            if (objectList.Item1.NextMarker != null)
            {
                if (objectList.Item1.EncodingType == "url")
                {
                    NextMarker = HttpUtility.UrlDecode(objectList.Item1.NextMarker);
                }
                else
                {
                    NextMarker = objectList.Item1.NextMarker;
                }
            }
            else if (this.BucketObjectsLastItem != null)
            {
                if (objectList.Item1.EncodingType == "url")
                {
                    NextMarker = HttpUtility.UrlDecode(this.BucketObjectsLastItem.Key);
                }
                else
                {
                    NextMarker = this.BucketObjectsLastItem.Key;
                }
            }
        }
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
            {
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.BucketResult = (ListBucketResult)new XmlSerializer(typeof(ListBucketResult)).Deserialize(stream);
            }
            XDocument root = XDocument.Parse(responseContent);
            var items = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Contents")
                        select new Item
                        {
                            Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                            LastModified = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}LastModified").Value,
                            ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value,
                            Size = ulong.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value, CultureInfo.CurrentCulture),
                            IsDir = false
                        };
            var prefixes = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}CommonPrefixes")
                           select new Item
                           {
                               Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Prefix").Value,
                               IsDir = true
                           };
            items = items.Concat(prefixes);
            this.ObjectsTuple = Tuple.Create(this.BucketResult, items.ToList());
        }
    }

    internal class GetObjectsVersionsListResponse : GenericResponse
    {
        internal ListVersionsResult BucketResult;
        internal Tuple<ListVersionsResult, List<VersionItem>> ObjectsTuple;
        internal GetObjectsVersionsListResponse(HttpStatusCode statusCode, string responseContent)
                    : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.BucketResult = (ListVersionsResult)new XmlSerializer(typeof(ListVersionsResult)).Deserialize(stream);
            }
            XDocument root = XDocument.Parse(responseContent);
            var items = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Version")
                        select new VersionItem
                        {
                            Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                            LastModified = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}LastModified").Value,
                            ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value,
                            VersionId = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}VersionId").Value,
                            Size = ulong.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value, CultureInfo.CurrentCulture),
                            IsDir = false
                        };
            var prefixes = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}CommonPrefixes")
                           select new VersionItem
                           {
                               Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Prefix").Value,
                               IsDir = true
                           };
            items = items.Concat(prefixes);
            this.ObjectsTuple = Tuple.Create(this.BucketResult, items.ToList());
        }
    }
    internal class GetPolicyResponse : GenericResponse
    {
        internal string PolicyJsonString { get; private set; }

        private async Task Initialize()
        {
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ResponseContent)))
            using (var streamReader = new StreamReader(stream))
            {
                this.PolicyJsonString =  await streamReader.ReadToEndAsync()
                                                    .ConfigureAwait(false);
            }
        }

        internal GetPolicyResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                return;
            }
            Initialize().Wait();
        }
    }
    internal class GetBucketNotificationsResponse : GenericResponse
    {
        internal BucketNotification BucketNotificationConfiguration { set; get; }
        internal GetBucketNotificationsResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                this.BucketNotificationConfiguration = new BucketNotification();
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.BucketNotificationConfiguration = (BucketNotification)new XmlSerializer(typeof(BucketNotification)).Deserialize(stream);
            }
        }
    }

    internal class GetBucketEncryptionResponse : GenericResponse
    {
        internal ServerSideEncryptionConfiguration BucketEncryptionConfiguration { get; set; }

        internal GetBucketEncryptionResponse(HttpStatusCode statusCode, string responseContent)
                    : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) || !HttpStatusCode.OK.Equals(statusCode))
            {
                this.BucketEncryptionConfiguration = null;
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                BucketEncryptionConfiguration = (ServerSideEncryptionConfiguration)new XmlSerializer(typeof(ServerSideEncryptionConfiguration)).Deserialize(stream);
            }
        }
    }

    internal class GetBucketTagsResponse : GenericResponse
    {
        internal Tagging BucketTags { set; get; }
        internal GetBucketTagsResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                this.BucketTags = null;
                return;
            }
            // Remove namespace from response content, if present.
            responseContent = utils.RemoveNamespaceInXML(responseContent);
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.BucketTags = (Tagging)new XmlSerializer(typeof(Tagging)).Deserialize(stream);
            }
        }
    }

    internal class GetObjectLockConfigurationResponse : GenericResponse
    {
        internal ObjectLockConfiguration LockConfiguration { get; set; }

        internal GetObjectLockConfigurationResponse(HttpStatusCode statusCode, string responseContent)
                    : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) || !HttpStatusCode.OK.Equals(statusCode))
            {
                this.LockConfiguration = null;
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.LockConfiguration = (ObjectLockConfiguration)new XmlSerializer(typeof(ObjectLockConfiguration)).Deserialize(stream);
            }
        }
    }

    internal class GetBucketLifecycleResponse : GenericResponse
    {
        internal LifecycleConfiguration BucketLifecycle { set; get; }
        internal GetBucketLifecycleResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                this.BucketLifecycle = null;
                return;
            }
            //Remove xmlns content for config serialization
            responseContent = utils.RemoveNamespaceInXML(responseContent);
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.BucketLifecycle = (LifecycleConfiguration)new XmlSerializer(typeof(LifecycleConfiguration)).Deserialize(stream);
            }
        }
    }

    internal class GetBucketReplicationResponse : GenericResponse
    {
        internal ReplicationConfiguration Config { set; get; }
        internal GetBucketReplicationResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                this.Config = null;
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.Config = (ReplicationConfiguration)new XmlSerializer(typeof(ReplicationConfiguration)).Deserialize(stream);
            }
        }
    }
}