/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017-2021 MinIO, Inc.
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
using Minio.DataModel.Tags;
using Minio.Exceptions;
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
using Minio.Helper;
using Minio.DataModel.ILM;
using Minio.DataModel.Replication;
using Minio.DataModel.ObjectLock;

namespace Minio
{
    public partial class MinioClient : IBucketOperations
    {
        /// <summary>
        /// Check if a private bucket with the given name exists.
        /// </summary>
        /// <param name="args">BucketExistsArgs Arguments Object which has bucket identifier information - bucket name, region</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns> Task </returns>
        public async Task<bool> BucketExistsAsync(BucketExistsArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            try
            {
                RestRequest request = await this.CreateRequest(.Head, args.BucketName).ConfigureAwait(false);
                var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            }
                HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(HttpMethod.Head, args.BucketName).ConfigureAwait(false);
            await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
            // var request = await this.CreateRequest(args.RequestMethod, args.BucketName).ConfigureAwait(false);
            // HttpRequestMessageBuilder requestMessageBuilder = new HttpRequestMessageBuilder(
            //     request.Method, request.RequestUri, request.RequestUri.AbsolutePath);
            // var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
            //    .ConfigureAwait(false);
            //     // RestRequest request = await this.CreateRequest(Method.HEAD, args.BucketName).ConfigureAwait(false);
            //     // var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
            // }
            catch (InternalClientException ice)
            {
                if ((ice.ServerResponse != null && HttpStatusCode.NotFound.Equals(ice.ServerResponse.StatusCode))
                        || ice.ServerResponse == null)
                {
                    return false;
                }
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
        /// Remove the bucket with the given name.
        /// </summary>
        /// <param name="args">RemoveBucketArgs Arguments Object which has bucket identifier information like bucket name .etc.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns> Task </returns>
        /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucketName is not found</exception>
        /// <exception cref="InvalidBucketNameException">When bucketName is null</exception>
        public async Task RemoveBucketAsync(RemoveBucketArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            RestRequest request = await this.CreateRequest(Method.DELETE, args.BucketName).ConfigureAwait(false);
            await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken);
        }
        HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(HttpMethod.Delete, args.BucketName).ConfigureAwait(false);
        var responseResult = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
          .ConfigureAwait(false);
    }

    /// <summary>
    /// Create a bucket with the given name.
    /// </summary>
    /// <param name="args">MakeBucketArgs Arguments Object that has bucket info like name, location. etc</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="NotImplementedException">When object-lock or another extension is not implemented</exception>
    public async Task MakeBucketAsync(MakeBucketArgs args, CancellationToken cancellationToken = default(CancellationToken))
    {
        args.Validate();
        RestRequest request = new RestRequest("/" + args.BucketName, Method.PUT);
        if (string.IsNullOrEmpty(args.Location))
        {
            args.Location = this.Region;
        }
        // Set Target URL for MakeBucket
        Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, region: args.Location);
        SetTargetURL(requestUrl);
        // Set Authenticator, if necessary.
        if (string.IsNullOrEmpty(this.Region) && !s3utils.IsAmazonEndPoint(this.BaseUrl) && args.Location != "us-east-1" && this.restClient != null)
        {
            this.restClient.Authenticator = new V4Authenticator(this.Secure, this.AccessKey, this.SecretKey, region: args.Location, sessionToken: this.SessionToken);
        }
        var response = await this.ExecuteAsync(this.NoErrorHandlers, args.BuildRequest(request), cancellationToken);
        Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, args.Location);

        HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken);
    }


    /// <summary>
    /// Get Versioning information on the bucket with given bucket name
    /// </summary>
    /// <param name="args">GetVersioningArgs takes bucket as argument. </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> GetVersioningResponse with information populated from REST response </returns>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    public async Task<VersioningConfiguration> GetVersioningAsync(GetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))
    {
        args.Validate();
        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
        IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        GetVersioningResponse versioningResponse = new GetVersioningResponse(response.StatusCode, response.Content);

        HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        ResponseResult responseResult = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
        GetVersioningResponse versioningResponse = new GetVersioningResponse(responseResult.StatusCode, responseResult.Content);
        return versioningResponse.VersioningConfig;
    }


    /// <summary>
    /// Set Versioning as specified on the bucket with given bucket name
    /// </summary>
    /// <param name="args">SetVersioningArgs Arguments Object with information like Bucket name, Versioning configuration</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetVersioningAsync(SetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))
    {
        args.Validate();
        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
        var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
        HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Returns current policy stored on the server for this bucket
    /// </summary>
    /// <param name="args">GetPolicyArgs object has information like Bucket name.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task that returns the Bucket policy as a json string</returns>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="UnexpectedMinioException">When a policy is not set</exception>
    public async Task<string> GetPolicyAsync(GetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))
    {
        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
        IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        // *******************************
        var request = await this.CreateRequest(args.RequestMethod, args.BucketName).ConfigureAwait(false);
        HttpRequestMessageBuilder requestMessageBuilder = new HttpRequestMessageBuilder(
            request.Method, request.RequestUri, request.RequestUri.AbsolutePath);
        var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
        GetPolicyResponse getPolicyResponse = new GetPolicyResponse(response.StatusCode, response.Content);
        return getPolicyResponse.PolicyJsonString;
    }


    /// <summary>
    /// Sets the current bucket policy
    /// </summary>
    /// <param name="args">SetPolicyArgs object has information like Bucket name and the policy to set in Json format</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="UnexpectedMinioException">When a policy is not set</exception>
    /// <returns>Task to set a policy</returns>
    public async Task SetPolicyAsync(SetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))
    {
        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
        await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Removes the current bucket policy
    /// </summary>
    /// <param name="args">RemovePolicyArgs object has information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task to set a policy</returns>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="UnexpectedMinioException">When a policy is not set</exception>
    public async Task RemovePolicyAsync(RemovePolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))
    {
        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
        var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        var request = await this.CreateRequest(args.RequestMethod, args.BucketName).ConfigureAwait(false);
        HttpRequestMessageBuilder requestMessageBuilder = new HttpRequestMessageBuilder(
            request.Method, request.RequestUri, request.RequestUri.AbsolutePath);
        var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// List all objects in a bucket
    /// List all the buckets for the current Endpoint URL
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task with an iterator lazily populated with objects</returns>
    public async Task<ListAllMyBucketsResult> ListBucketsAsync(
        CancellationToken cancellationToken = default(CancellationToken))
    {
        var request = await this.CreateRequest(Method.GET, resourcePath: "/").ConfigureAwait(false);
        var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        ListBucketsResponse listBucketsResponse = new ListBucketsResponse(response.StatusCode, response.Content);
        return listBucketsResponse.BucketsResult;
    }


    /// <summary>
    /// List all objects along with versions non-recursively in a bucket with a given prefix, optionally emulating a directory
    /// </summary>
    /// <param name="args">ListObjectsArgs Arguments Object with information like Bucket name, prefix, recursive listing, versioning</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>An observable of items that client can subscribe to</returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">If a functionality or extension (like versioning) is not implemented</exception>
    /// <exception cref="InvalidOperationException">For example, if you call ListObjectsAsync on a bucket with versioning enabled or object lock enabled</exception>
    public IObservable<Item> ListObjectsAsync(ListObjectsArgs args, CancellationToken cancellationToken = default(CancellationToken))
    {
        args.Validate();
        BucketExistsArgs bucketExistsArgs = new BucketExistsArgs()
                                                        .WithBucket(args.BucketName);
        // Check if the bucket exists.
        var bucketExistTask = this.BucketExistsAsync(bucketExistsArgs, cancellationToken);
        Task.WaitAll(bucketExistTask);
        var found = bucketExistTask.Result;
        if (!found)
            var request = await this.CreateRequest(HttpMethod.Get, resourcePath: "/").ConfigureAwait(false);
        HttpRequestMessageBuilder requestMessageBuilder = new HttpRequestMessageBuilder(
            request.Method, request.RequestUri, request.RequestUri.AbsolutePath);
        var responseResult = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);


        ListAllMyBucketsResult bucketList;
        if (HttpStatusCode.OK.Equals(responseResult.StatusCode))
            var request = await this.CreateRequest(HttpMethod.Get, resourcePath: "/").ConfigureAwait(false);
        HttpRequestMessageBuilder requestMessageBuilder = new HttpRequestMessageBuilder(
            request.Method, request.RequestUri, request.RequestUri.AbsolutePath);
        var responseResult = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);


        ListAllMyBucketsResult bucketList;
        if (HttpStatusCode.OK.Equals(responseResult.StatusCode))
        {
            using (var stream = new MemoryStream(responseResult.ContentBytes))
                bucketList =
                    (ListAllMyBucketsResult)new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream);


            /// <summary>
            /// Gets the list of objects in the bucket filtered by prefix
            /// </summary>
            /// <param name="args">GetObjectListArgs Arguments Object with information like Bucket name, prefix, delimiter, marker, versions(get version IDs of the objects)</param>
            /// <returns>Task with a tuple populated with objects</returns>
            /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
            private async Task<Tuple<ListBucketResult, List<Item>>> GetObjectListAsync(GetObjectListArgs args, CancellationToken cancellationToken = default(CancellationToken))
            {
                RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
                IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
                GetObjectsListResponse getObjectsListResponse = new GetObjectsListResponse(response.StatusCode, response.Content);
                return getObjectsListResponse.ObjectsTuple;
            }


            /// <summary>
            /// Gets the list of objects along with version IDs in the bucket filtered by prefix
            /// </summary>
            /// <param name="args">GetObjectListArgs Arguments Object with information like Bucket name, prefix, delimiter, marker, versions(get version IDs of the objects)</param>
            /// <returns>Task with a tuple populated with objects</returns>
            /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
            private async Task<Tuple<ListVersionsResult, List<Item>>> GetObjectVersionsListAsync(GetObjectListArgs args, CancellationToken cancellationToken = default(CancellationToken))
            {
                RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
                IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
                GetObjectsVersionsListResponse getObjectsListResponse = new GetObjectsVersionsListResponse(response.StatusCode, response.Content);
                return getObjectsListResponse.ObjectsTuple;
                return bucketList;
            }
            return bucketList;
        }
    }

    /// <summary>
    /// Create a private bucket with the given name.
    /// </summary>
    /// <param name="bucketName">Name of the new bucket</param>
    /// <param name="location">Region</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="InvalidBucketNameException">When bucketName is null</exception>
    [Obsolete("Use MakeBucketAsync method with MakeBucketArgs object. Refer MakeBucket example code.")]
    public async Task MakeBucketAsync(string bucketName, string location = "us-east-1", CancellationToken cancellationToken = default(CancellationToken))
    {
        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
        IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        GetBucketNotificationsResponse getBucketNotificationsResponse = new GetBucketNotificationsResponse(response.StatusCode, response.Content);
        return getBucketNotificationsResponse.BucketNotificationConfiguration;
        MakeBucketArgs args = new MakeBucketArgs()
                                        .WithBucket(bucketName)
                                        .WithLocation(location);
        await this.MakeBucketAsync(args, cancellationToken);
    }

    /// <summary>
    /// Returns true if the specified bucketName exists, otherwise returns false.
    /// </summary>
    /// <param name="bucketName">Bucket to test existence of</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task that returns true if exists and user has access</returns>
    [Obsolete("Use BucketExistsAsync method with BucketExistsArgs object. Refer BucketExists example code.")]
    public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
    {
        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
        var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        BucketExistsArgs args = new BucketExistsArgs()
                                            .WithBucket(bucketName);
        return await BucketExistsAsync(args, cancellationToken);
        BucketExistsArgs args = new BucketExistsArgs()
                                            .WithBucket(bucketName);
        return await BucketExistsAsync(args, cancellationToken);
    }

    /// <summary>
    /// Remove a bucket
    /// </summary>
    /// <param name="bucketName">Name of bucket to remove</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task</returns>
    [Obsolete("Use RemoveBucketAsync method with RemoveBucketArgs object. Refer RemoveBucket example code.")]
    public async Task RemoveBucketAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
    {
        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
        var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        RemoveBucketArgs args = new RemoveBucketArgs()
                                            .WithBucket(bucketName);
        await RemoveBucketAsync(args, cancellationToken);
        RemoveBucketArgs args = new RemoveBucketArgs()
                                            .WithBucket(bucketName);
        await RemoveBucketAsync(args, cancellationToken);
    }

    /// <summary>
    /// List all objects non-recursively in a bucket with a given prefix, optionally emulating a directory
    /// </summary>
    /// <param name="bucketName">Bucket to list objects from</param>
    /// <param name="prefix">Filters all objects beginning with a given prefix</param>
    /// <param name="recursive">Set to true to recursively list all objects</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>An observable of items that client can subscribe to</returns>
    public IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = false,
        CancellationToken cancellationToken = default(CancellationToken))
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

                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct))
                {
                    while (isRunning)
                    {
                        args = args.WithNotificationObserver(obs)
                                   .WithEnableTrace(this.trace);
                        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
                        await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
                        Tuple<ListBucketResult, List<Item>> result =
                            await GetObjectListAsync(bucketName, prefix, delimiter, marker, cts.Token)
                                .ConfigureAwait(false);
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
    private async Task<Tuple<ListBucketResult, List<Item>>> GetObjectListAsync(string bucketName, string prefix,
        string delimiter, string marker, CancellationToken cancellationToken = default(CancellationToken))
    {
        args.Validate();
        RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
        IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        GetBucketTagsResponse getBucketNotificationsResponse = new GetBucketTagsResponse(response.StatusCode, response.Content);
        return getBucketNotificationsResponse.BucketTags;
    }
//     var queryMap = new Dictionary<string, string>();
//             // null values are treated as empty strings.
//             if (delimiter == null)
//             {
//                 delimiter = string.Empty;
//             }

// if (prefix == null)
// {
//     prefix = string.Empty;
// }

// if (marker == null)
// {
//     marker = string.Empty;
// }

// var requestMessageBuilder = await this.CreateRequest(HttpMethod.Get,
//         bucketName)
//     .ConfigureAwait(false);
// requestMessageBuilder.AddQueryParameter("delimiter", delimiter);
// requestMessageBuilder.AddQueryParameter("prefix", prefix);
// requestMessageBuilder.AddQueryParameter("max-keys", "1000");
// requestMessageBuilder.AddQueryParameter("marker", marker);
// requestMessageBuilder.AddQueryParameter("encoding-type", "url");

// var responseResult = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
//     .ConfigureAwait(false);

// ListBucketResult listBucketResult = null;
// using (var stream = new MemoryStream(responseResult.ContentBytes))
// {
//     listBucketResult = (ListBucketResult)new XmlSerializer(typeof(ListBucketResult)).Deserialize(stream);
// }

// XDocument root = XDocument.Parse(responseResult.Content);

/// <summary>
/// Sets the Encryption Configuration for the mentioned bucket.
/// </summary>
/// <param name="args">SetBucketEncryptionArgs Arguments Object with information like Bucket name, encryption config</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns> Task </returns>
/// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
public async Task SetBucketEncryptionAsync(SetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}

// var responseResult = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
//     .ConfigureAwait(false);

// ListBucketResult listBucketResult = null;
// using (var stream = new MemoryStream(responseResult.ContentBytes))
// {
//     listBucketResult = (ListBucketResult)new XmlSerializer(typeof(ListBucketResult)).Deserialize(stream);
// }

// XDocument root = XDocument.Parse(responseResult.Content);

// var items = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Contents")
//             select new Item
//             {
//                 Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
//                 LastModified = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}LastModified").Value,
//                 ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value,
//                 Size = ulong.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value,
//                     CultureInfo.CurrentCulture),
//                 IsDir = false
//             };

// var prefixes = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}CommonPrefixes")
//                select new Item
//                {
//                    Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Prefix").Value,
//                    IsDir = true
//                };

// items = items.Concat(prefixes);

/// <summary>
/// Returns the Encryption Configuration for the mentioned bucket.
/// </summary>
/// <param name="args">GetBucketEncryptionArgs Arguments Object encapsulating information like Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns> An object of type ServerSideEncryptionConfiguration </returns>
/// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
public async Task<ServerSideEncryptionConfiguration> GetBucketEncryptionAsync(GetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
    GetBucketEncryptionResponse getBucketEncryptionResponse = new GetBucketEncryptionResponse(response.StatusCode, response.Content);
    return getBucketEncryptionResponse.BucketEncryptionConfiguration;
    return Tuple.Create(listBucketResult, items.ToList());

    items = items.Concat(prefixes);

    return Tuple.Create(listBucketResult, items.ToList());
}


/// <summary>
/// Returns current policy stored on the server for this bucket
/// </summary>
/// <param name="bucketName">Bucket name.</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns>Task that returns the Bucket policy as a json string</returns>
public async Task<string> GetPolicyAsync(string bucketName,
    CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}
// var requestMessageBuilder = await this.CreateRequest(HttpMethod.Get, bucketName,
//         contentType: "application/json")
//     .ConfigureAwait(false);
// requestMessageBuilder.AddQueryParameter("policy", "");
// string policyString = null;
// var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
//     .ConfigureAwait(false);
// using (var contentStream = new MemoryStream(response.ContentBytes))
// using (var streamReader = new StreamReader(contentStream))
// {
//     policyString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
// }

// return policyString;
GetPolicyArgs args = new GetPolicyArgs()
                                .WithBucket(bucketName);
return await this.GetPolicyAsync(args, cancellationToken);
        }

            // return policyString;
            GetPolicyArgs args = new GetPolicyArgs()
                                            .WithBucket(bucketName);
return await this.GetPolicyAsync(args, cancellationToken);
        }


        /// <summary>
        /// Sets the current bucket policy
        /// </summary>
        /// <param name="args">SetBucketTagsArgs Arguments Object with information like Bucket name, tag key-value pairs</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        public async Task SetBucketTagsAsync(SetBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}


/// <summary>
/// Removes Tagging values stored for the bucket.
/// </summary>
/// <param name="args">RemoveBucketTagsArgs Arguments Object with information like Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
/// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
public async Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}


/// <summary>
/// Sets the Object Lock Configuration on this bucket
/// </summary>
/// <param name="args">SetObjectLockConfigurationArgs Arguments Object with information like Bucket name, object lock configuration to set</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
/// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
/// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
public async Task SetObjectLockConfigurationAsync(SetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}


/// <summary>
/// Gets the Object Lock Configuration on this bucket
/// </summary>
/// <param name="args">GetObjectLockConfigurationArgs Arguments Object with information like Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns>ObjectLockConfiguration object</returns>
/// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
public async Task<ObjectLockConfiguration> GetObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
    GetObjectLockConfigurationResponse resp = new GetObjectLockConfigurationResponse(response.StatusCode, response.Content);
    return resp.LockConfiguration;
}


/// <summary>
/// Removes the Object Lock Configuration on this bucket
/// </summary>
/// <param name="args">RemoveObjectLockConfigurationArgs Arguments Object with information like Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
/// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
/// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
public async Task RemoveObjectLockConfigurationAsync(RemoveObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}


/// <summary>
/// Sets the Lifecycle configuration for this bucket
/// </summary>
/// <param name="args">SetBucketLifecycleArgs Arguments Object with information like Bucket name, Lifecycle configuration object</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
/// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
public async Task SetBucketLifecycleAsync(SetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}


/// <summary>
/// Gets Lifecycle configuration set for this bucket returned in an object
/// </summary>
/// <param name="args">GetBucketLifecycleArgs Arguments Object with information like Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns>LifecycleConfiguration Object with the lifecycle configuration</returns>
/// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
public async Task<LifecycleConfiguration> GetBucketLifecycleAsync(GetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    IRestResponse restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
    GetBucketLifecycleResponse response = new GetBucketLifecycleResponse(restResponse.StatusCode, restResponse.Content);
    return response.BucketLifecycle;
}


/// <summary>
/// Removes Lifecycle configuration stored for the bucket.
/// </summary>
/// <param name="args">RemoveBucketLifecycleArgs Arguments Object with information like Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
/// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
public async Task RemoveBucketLifecycleAsync(RemoveBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}


/// <summary>
/// Get Replication configuration for the bucket
/// </summary>
/// <param name="args">GetBucketReplicationArgs Arguments Object with information like Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns>Replication configuration object</returns>
/// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="MissingBucketReplicationConfigurationException">When bucket replication configuration is not set</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
public async Task<ReplicationConfiguration> GetBucketReplicationAsync(GetBucketReplicationArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    IRestResponse restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
    GetBucketReplicationResponse response = new GetBucketReplicationResponse(restResponse.StatusCode, restResponse.Content);
    return response.Config;
}


/// <summary>
/// Set the Replication configuration for the bucket
/// </summary>
/// <param name="args">SetBucketReplicationArgs Arguments Object with information like Bucket name, Replication Configuration object</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
/// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="MissingBucketReplicationConfigurationException">When bucket replication configuration is not set</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception> 
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
public async Task SetBucketReplicationAsync(SetBucketReplicationArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}


/// <summary>
/// Remove Replication configuration for the bucket.
/// </summary>
/// <param name="args">RemoveBucketReplicationArgs Arguments Object with information like Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
/// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
/// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
/// <exception cref="MissingBucketReplicationConfigurationException">When bucket replication configuration is not set</exception>
/// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception> 
/// <exception cref="BucketNotFoundException">When bucket is not found</exception>
public async Task RemoveBucketReplicationAsync(RemoveBucketReplicationArgs args, CancellationToken cancellationToken = default(CancellationToken))
{
    args.Validate();
    RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
    var restResponse = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
}


/// <summary>
/// Create a private bucket with the given name.
/// </summary>
/// <param name="bucketName">Name of the new bucket</param>
/// <param name="location">Region</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns> Task </returns>
/// <exception cref="InvalidBucketNameException">When bucketName is null</exception>
[Obsolete("Use MakeBucketAsync method with MakeBucketArgs object. Refer MakeBucket example code.")]
public Task MakeBucketAsync(string bucketName, string location = "us-east-1", CancellationToken cancellationToken = default(CancellationToken))
{
    MakeBucketArgs args = new MakeBucketArgs()
                                    .WithBucket(bucketName)
                                    .WithLocation(location);
    return this.MakeBucketAsync(args, cancellationToken);
}

/// <summary>
/// Returns true if the specified bucketName exists, otherwise returns false.
/// </summary>
/// <param name="bucketName">Bucket to test existence of</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns>Task that returns true if exists and user has access</returns>
[Obsolete("Use BucketExistsAsync method with BucketExistsArgs object. Refer BucketExists example code.")]
public Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
{
    BucketExistsArgs args = new BucketExistsArgs()
                                        .WithBucket(bucketName);
    return BucketExistsAsync(args, cancellationToken);
}

/// <summary>
/// Remove a bucket
/// </summary>
/// <param name="bucketName">Name of bucket to remove</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns>Task</returns>
[Obsolete("Use RemoveBucketAsync method with RemoveBucketArgs object. Refer RemoveBucket example code.")]
public Task RemoveBucketAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
{
    RemoveBucketArgs args = new RemoveBucketArgs()
                                        .WithBucket(bucketName);
    return RemoveBucketAsync(args, cancellationToken);
}

/// <summary>
/// List all objects non-recursively in a bucket with a given prefix, optionally emulating a directory
/// </summary>
/// <param name="bucketName">Bucket to list objects from</param>
/// <param name="prefix">Filters all objects beginning with a given prefix</param>
/// <param name="recursive">Set to true to recursively list all objects</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns>An observable of items that client can subscribe to</returns>
[Obsolete("Use ListObjectsAsync method with ListObjectsArgs object. Refer ListObjects example code.")]
public IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = false, CancellationToken cancellationToken = default(CancellationToken))
{
    ListObjectsArgs args = new ListObjectsArgs()
                                    .WithBucket(bucketName)
                                    .WithPrefix(prefix)
                                    .WithRecursive(recursive);
    return this.ListObjectsAsync(args, cancellationToken);
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
private Task<Tuple<ListBucketResult, List<Item>>> GetObjectListAsync(string bucketName, string prefix, string delimiter, string marker, CancellationToken cancellationToken = default(CancellationToken))
{
    var queryMap = new Dictionary<string, string>();
    // null values are treated as empty strings.
    GetObjectListArgs args = new GetObjectListArgs()
                                    .WithBucket(bucketName)
                                    .WithPrefix(prefix)
                                    .WithDelimiter(delimiter)
                                    .WithMarker(marker);
    return this.GetObjectListAsync(args, cancellationToken);
}


/// <summary>
/// Returns current policy stored on the server for this bucket
/// </summary>
/// <param name="bucketName">Bucket name.</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns>Task that returns the Bucket policy as a json string</returns>
[Obsolete("Use GetPolicyAsync method with GetPolicyArgs object. Refer GetBucketPolicy example code.")]
public Task<string> GetPolicyAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
{
    GetPolicyArgs args = new GetPolicyArgs()
                                    .WithBucket(bucketName);
    return this.GetPolicyAsync(args, cancellationToken);
}


/// <summary>
/// Sets the current bucket policy
/// </summary>
/// <param name="bucketName">Bucket Name</param>
/// <param name="policyJson">Policy json as string </param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns>Task to set a policy</returns>
public async Task SetPolicyAsync(string bucketName, string policyJson,
    CancellationToken cancellationToken = default(CancellationToken))
{
    // var requestBuilder = await this.CreateRequest(HttpMethod.Put, bucketName,
    //         contentType: "application/json")
    //     .ConfigureAwait(false);
    // requestBuilder.AddQueryParameter("policy", "");
    // requestBuilder.AddJsonBody(policyJson);
    // _ = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestBuilder, cancellationToken)
    //     .ConfigureAwait(false);
    SetPolicyArgs args = new SetPolicyArgs()
                                    .WithBucket(bucketName)
                                    .WithPolicy(policyJson);
    return this.SetPolicyAsync(args, cancellationToken);
    await this.SetPolicyAsync(args, cancellationToken);
    await this.SetPolicyAsync(args, cancellationToken);
}

/// <summary>
/// Gets notification configuration for this bucket
/// </summary>
/// <param name="bucketName">Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
public async Task<BucketNotification> GetBucketNotificationsAsync(string bucketName,
    CancellationToken cancellationToken = default(CancellationToken))
{
    GetBucketNotificationsArgs args = new GetBucketNotificationsArgs()
                                                    .WithBucket(bucketName);
    return this.GetBucketNotificationsAsync(args, cancellationToken);
    utils.ValidateBucketName(bucketName);
    var requestMessageBuilder = await this.CreateRequest(HttpMethod.Get,
            bucketName)
        .ConfigureAwait(false);
    requestMessageBuilder.AddQueryParameter("notification", "");

    var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
        .ConfigureAwait(false);
    using (var contentStream = new MemoryStream(response.ContentBytes))
    {
        return (BucketNotification)new XmlSerializer(typeof(BucketNotification)).Deserialize(contentStream);
    }
}

/// <summary>
/// Sets the notification configuration for this bucket
/// </summary>
/// <param name="bucketName">Bucket name</param>
/// <param name="notification">Notification object with configuration to be set on the server</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
public async Task SetBucketNotificationsAsync(string bucketName, BucketNotification notification,
    CancellationToken cancellationToken = default(CancellationToken))
{
    SetBucketNotificationsArgs args = new SetBucketNotificationsArgs()
                                                        .WithBucket(bucketName)
                                                        .WithBucketNotificationConfiguration(notification);
    return this.SetBucketNotificationsAsync(args, cancellationToken);
    utils.ValidateBucketName(bucketName);
    var requestMessageBuilder = await this.CreateRequest(HttpMethod.Put, bucketName)
        .ConfigureAwait(false);
    requestMessageBuilder.AddQueryParameter("notification", "");

    var bodyString = notification.ToString();
    requestMessageBuilder.AddXmlBody(bodyString);

    _ = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
}

/// <summary>
/// Removes all bucket notification configurations stored on the server.
/// </summary>
/// <param name="bucketName">Bucket name</param>
/// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
/// <returns></returns>
[Obsolete("Use RemoveAllBucketNotificationsAsync method with RemoveAllBucketNotificationsArgs object. Refer RemoveAllBucketNotification example code.")]
public Task RemoveAllBucketNotificationsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))
        public Task RemoveAllBucketNotificationsAsync(string bucketName,
            CancellationToken cancellationToken = default(CancellationToken))
        public Task RemoveAllBucketNotificationsAsync(string bucketName,
            CancellationToken cancellationToken = default(CancellationToken))
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
    List<EventType> eventList = new List<EventType>(events);
    ListenBucketNotificationsArgs args = new ListenBucketNotificationsArgs()
                                                            .WithBucket(bucketName)
                                                            .WithEvents(eventList)
                                                            .WithPrefix(prefix)
                                                            .WithSuffix(suffix);
    return this.ListenBucketNotificationsAsync(args, cancellationToken);
    return Observable.Create<MinioNotificationRaw>(
        async (obs, ct) =>
        {
            bool isRunning = true;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct))
            {
                while (isRunning)
                {
                    var requestMessageBuilder = await this.CreateRequest(HttpMethod.Get,
                            bucketName)
                        .ConfigureAwait(false);
                    requestMessageBuilder.AddQueryParameter("prefix", prefix);
                    requestMessageBuilder.AddQueryParameter("sufffix", suffix);
                    foreach (var eventType in events)
                    {
                        requestMessageBuilder.AddQueryParameter("events", eventType.value);
                    }

                    var response =
                        await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken);

                    using (response.ContentStream)
                    {
                        var sr = new StreamReader(response.ContentStream);
                        while (true)
                        {
                            string line = sr.ReadLine();
                            if (this.trace)
                            {
                                Console.WriteLine("== ListenBucketNotificationsAsync read line ==");
                                Console.WriteLine(line);
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

                    cts.Token.ThrowIfCancellationRequested();
                }
            }

        }
    );
}
    }
}
