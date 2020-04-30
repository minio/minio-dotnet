/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017, 2018, 2019 MinIO, Inc.
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
using Minio.Exceptions;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Web;

namespace Minio
{
    public partial class MinioClient : IBucketOperations
    {
        /// <summary>
        /// List all objects in a bucket
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Task with an iterator lazily populated with objects</returns>
        public async Task<ListAllMyBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = await this.CreateRequest(Method.GET, resourcePath: "/").ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            ListAllMyBucketsResult bucketList = new ListAllMyBucketsResult();
            if (HttpStatusCode.OK.Equals(response.StatusCode))
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                using (var stream = new MemoryStream(contentBytes))
                    bucketList = (ListAllMyBucketsResult)new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream);
                return bucketList;
            }
            return bucketList;
        }

        /// <summary>
        /// Create a private bucket with the given name.
        /// </summary>
        /// <param name="bucketName">Name of the new bucket</param>
        /// <param name="location">Region</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns> Task </returns>
        /// <exception cref="InvalidBucketNameException">When bucketName is null</exception>
        public async Task MakeBucketAsync(string bucketName, string location = "us-east-1", CancellationToken cancellationToken = default(CancellationToken))
        {
            if (bucketName == null)
            {
                throw new InvalidBucketNameException(bucketName, "bucketName cannot be null");
            }

            if (location == "us-east-1")
            {
                if (this.Region != string.Empty)
                {
                    location = this.Region;
                }
            }

            // Set Target URL
            Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, location);
            SetTargetURL(requestUrl);

            var request = new RestRequest("/" + bucketName, Method.PUT)
            {
                XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer(),
                RequestFormat = DataFormat.Xml
            };
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(location);
                request.AddBody(config);
            }

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns true if the specified bucketName exists, otherwise returns false.
        /// </summary>
        /// <param name="bucketName">Bucket to test existence of</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Task that returns true if exists and user has access</returns>
        public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (bucketName == null)
                {
                    throw new InvalidBucketNameException(bucketName, "bucketName cannot be null");
                }
                var request = await this.CreateRequest(Method.HEAD, bucketName).ConfigureAwait(false);
                var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(BucketNotFoundException))
                {
                    return false;
                }
                throw;
            }
            return true;
        }

        /// <summary>
        /// Remove a bucket
        /// </summary>
        /// <param name="bucketName">Name of bucket to remove</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Task</returns>
        public async Task RemoveBucketAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = await this.CreateRequest(Method.DELETE, bucketName).ConfigureAwait(false);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// List all objects non-recursively in a bucket with a given prefix, optionally emulating a directory
        /// </summary>
        /// <param name="bucketName">Bucket to list objects from</param>
        /// <param name="prefix">Filters all objects beginning with a given prefix</param>
        /// <param name="recursive">Set to true to recursively list all objects</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>An observable of items that client can subscribe to</returns>
        public IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Observable.Create<Item>(
              async (obs, ct) =>
              {
                  bool isRunning = true;
                  string marker = null;

                  var delimiter = "/";
                  if (recursive)
                  {
                      delimiter = string.Empty;
                  }

                  using(var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct)) {
                    while (isRunning)
                    {
                        Tuple<ListBucketResult, List<Item>> result = await GetObjectListAsync(bucketName, prefix, delimiter, marker, cts.Token).ConfigureAwait(false);
                        Item lastItem = null;
                        foreach (Item item in result.Item2)
                        {
                            lastItem = item;
                            if (result.Item1.EncodingType == "url")
                            {
                                item.Key = HttpUtility.UrlDecode(item.Key);
                            }
                            obs.OnNext(item);
                        }
                        if (result.Item1.NextMarker != null)
                        {
                            if (result.Item1.EncodingType == "url")
                            {
                                marker = HttpUtility.UrlDecode(result.Item1.NextMarker);
                            }
                            else
                            {
                                marker = result.Item1.NextMarker;
                            }
                        }
                        else if (lastItem != null)
                        {
                            if (result.Item1.EncodingType == "url")
                            {
                                marker = HttpUtility.UrlDecode(lastItem.Key);
                            }
                            else
                            {
                                marker = lastItem.Key;
                            }
                        }
                        isRunning = result.Item1.IsTruncated;
                        cts.Token.ThrowIfCancellationRequested();
                    }
                  }

              });
        }

        /// <summary>
        /// Gets the list of objects in the bucket filtered by prefix
        /// </summary>
        /// <param name="bucketName">Bucket to list objects from</param>
        /// <param name="prefix">Filters all objects starting with a given prefix</param>
        /// <param name="delimiter">Delimit the output upto this character</param>
        /// <param name="marker">marks location in the iterator sequence</param>
        /// <returns>Task with a tuple populated with objects</returns>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        private async Task<Tuple<ListBucketResult, List<Item>>> GetObjectListAsync(string bucketName, string prefix, string delimiter, string marker, CancellationToken cancellationToken = default(CancellationToken))
        {
            var queryMap = new Dictionary<string,string>();
            // null values are treated as empty strings.
            if (delimiter == null)
            {
                delimiter = string.Empty;
            }

            if (prefix == null)
            {
                prefix = string.Empty;
            }

            if (marker == null)
            {
                marker = string.Empty;
            }
            
            var request = await this.CreateRequest(Method.GET,
                                                     bucketName)
                                        .ConfigureAwait(false);
            request.AddQueryParameter("delimiter",delimiter);
            request.AddQueryParameter("prefix",prefix);
            request.AddQueryParameter("max-keys", "1000");
            request.AddQueryParameter("marker",marker);
            request.AddQueryParameter("encoding-type","url");
  
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            ListBucketResult listBucketResult = null;
            using (var stream = new MemoryStream(contentBytes))
            {
                listBucketResult = (ListBucketResult)new XmlSerializer(typeof(ListBucketResult)).Deserialize(stream);
            }

            XDocument root = XDocument.Parse(response.Content);

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

            return Tuple.Create(listBucketResult, items.ToList());
        }

        /// <summary>
        /// Returns current policy stored on the server for this bucket
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Task that returns the Bucket policy as a json string</returns>
        public async Task<string> GetPolicyAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
        {
            IRestResponse response = null;

            var request = await this.CreateRequest(Method.GET, bucketName,
                                 contentType: "application/json")
                            .ConfigureAwait(false);
            request.AddQueryParameter("policy","");
            string policyString = null;
            response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);

            using (var stream = new MemoryStream(contentBytes))
            using (var streamReader = new StreamReader(stream))
            {
                policyString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            }
            return policyString;
        }

        /// <summary>
        /// Sets the current bucket policy
        /// </summary>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="policyJson">Policy json as string </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Task to set a policy</returns>
        public async Task SetPolicyAsync(string bucketName, string policyJson, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = await this.CreateRequest(Method.PUT, bucketName,
                                           contentType: "application/json")
                                .ConfigureAwait(false);
            request.AddQueryParameter("policy","");
            request.AddJsonBody(policyJson);
            IRestResponse response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets notification configuration for this bucket
        /// </summary>
        /// <param name="bucketName">Bucket name</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task<BucketNotification> GetBucketNotificationsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
        {
            utils.ValidateBucketName(bucketName);
            var request = await this.CreateRequest(Method.GET,
                                               bucketName)
                                    .ConfigureAwait(false);
            request.AddQueryParameter("notification","");

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            using (var stream = new MemoryStream(contentBytes))
            {
                return (BucketNotification)new XmlSerializer(typeof(BucketNotification)).Deserialize(stream);
            }
        }

        /// <summary>
        /// Sets the notification configuration for this bucket
        /// </summary>
        /// <param name="bucketName">Bucket name</param>
        /// <param name="notification">Notification object with configuration to be set on the server</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task SetBucketNotificationsAsync(string bucketName, BucketNotification notification, CancellationToken cancellationToken = default(CancellationToken))
        {
            utils.ValidateBucketName(bucketName);
            var request = await this.CreateRequest(Method.PUT, bucketName)
                                .ConfigureAwait(false);
            request.AddQueryParameter("notification","");

            var bodyString = notification.ToString();

            var body = System.Text.Encoding.UTF8.GetBytes(bodyString);
            request.AddParameter("application/xml", body, RestSharp.ParameterType.RequestBody);

            IRestResponse response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes all bucket notification configurations stored on the server.
        /// </summary>
        /// <param name="bucketName">Bucket name</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public Task RemoveAllBucketNotificationsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
        {
            utils.ValidateBucketName(bucketName);
            BucketNotification notification = new BucketNotification();
            return SetBucketNotificationsAsync(bucketName, notification, cancellationToken);
        }

        /// <summary>
        /// Subscribes to bucket change notifications (a Minio-only extension)
        /// </summary>
        /// <param name="bucketName">Bucket to get notifications from</param>
        /// <param name="events">Events to listen for</param>
        /// <param name="prefix">Filter keys starting with this prefix</param>
        /// <param name="suffix">Filter keys ending with this suffix</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>An observable of JSON-based notification events</returns>
        public IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(string bucketName, IList<EventType> events, string prefix = "", string suffix = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            return Observable.Create<MinioNotificationRaw>(
                async (obs, ct) =>
                {
                    bool isRunning = true;

                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct))
                    {
                        while (isRunning)
                        {
                            var request = await this.CreateRequest(Method.GET,
                                                                    bucketName)
                                                        .ConfigureAwait(false);
                            request.AddQueryParameter("prefix",prefix);
                            request.AddQueryParameter("sufffix",suffix);
                            foreach (var eventType in events)
                            {
                                request.AddQueryParameter("events",eventType.value);
                            }

                            request.ResponseWriter = responseStream =>
                            {
                                using (responseStream)
                                {
                                    var sr = new StreamReader(responseStream);
                                    while (true)
                                    {
                                        string line = sr.ReadLine();
                                        if (this.trace)
                                        {
                                            Console.WriteLine("== ListenBucketNotificationsAsync read line ==");
                                            Console.WriteLine(line);
                                            Console.WriteLine("==============================================");
                                        }
                                        if (line == null)
                                        {
                                            break;
                                        }
                                        string trimmed = line.Trim();
                                        if (trimmed.Length > 2)
                                        {
                                            obs.OnNext(new MinioNotificationRaw(trimmed));
                                        }
                                    }
                                }
                            };

                            await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

                            cts.Token.ThrowIfCancellationRequested();
                        }
                    }

              });

        }
    }
}
