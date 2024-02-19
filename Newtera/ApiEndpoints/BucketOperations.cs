﻿/*
 * Newtera .NET Library for Newtera TDM,
 * (C) 2017-2021 Newtera, Inc.
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

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reactive.Linq;
using CommunityToolkit.HighPerformance;
using Newtera.ApiEndpoints;
using Newtera.DataModel;
using Newtera.DataModel.Args;
using Newtera.DataModel.Response;
using Newtera.DataModel.Result;
using Newtera.Exceptions;
using Newtera.Helper;

namespace Newtera;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Split up in partial classes")]
public partial class NewteraClient : IBucketOperations
{
    /// <summary>
    ///     List all the buckets for the current Endpoint URL
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task with an iterator lazily populated with objects</returns>
    public async Task<ListAllMyBucketsResult> ListBucketsAsync(
        CancellationToken cancellationToken = default)
    {
        var requestMessageBuilder = await this.CreateRequest(HttpMethod.Get).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        var bucketList = new ListAllMyBucketsResult();
        if (HttpStatusCode.OK.Equals(response.StatusCode))
        {
            using var stream = response.ContentBytes.AsStream();
            bucketList = Utils.DeserializeXml<ListAllMyBucketsResult>(stream);
        }

        return bucketList;
    }

    /// <summary>
    ///     Check if a private bucket with the given name exists.
    /// </summary>
    /// <param name="args">BucketExistsArgs Arguments Object which has bucket identifier information - bucket name, region</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    public async Task<bool> BucketExistsAsync(BucketExistsArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        try
        {
            var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            using var response =
                await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            return response is not null &&
                   (response.Exception is null ||
                    response.Exception.GetType() != typeof(BucketNotFoundException));
        }
        catch (InternalClientException ice)
        {
            return (ice.ServerResponse is null ||
                    !HttpStatusCode.NotFound.Equals(ice.ServerResponse.StatusCode)) &&
                   ice.ServerResponse is not null;
        }
        catch (Exception ex)
        {
            if (ex.GetType() == typeof(BucketNotFoundException)) return false;
            throw;
        }
    }

    /// <summary>
    ///     List all objects along with versions non-recursively in a bucket with a given prefix, optionally emulating a
    ///     directory
    /// </summary>
    /// <param name="args">
    ///     ListObjectsArgs Arguments Object with information like Bucket name, prefix, recursive listing,
    ///     versioning
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>An observable of items that client can subscribe to</returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">If a functionality or extension (like versioning) is not implemented</exception>
    /// <exception cref="InvalidOperationException">
    ///     For example, if you call ListObjectsAsync on a bucket with versioning
    ///     enabled or object lock enabled
    /// </exception>
    public IObservable<Item> ListObjectsAsync(ListObjectsArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        return Observable.Create<Item>(
            async (obs, ct) =>
            {
                var isRunning = true;
                var marker = string.Empty;
                uint count = 0;
                var versionIdMarker = string.Empty;
                var nextContinuationToken = string.Empty;
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                while (isRunning)
                {
                    var goArgs = new GetObjectListArgs()
                        .WithBucket(args.BucketName)
                        .WithPrefix(args.Prefix)
                        .WithContinuationToken(nextContinuationToken)
                        .WithHeaders(args.Headers);
  
                    var objectList = await GetObjectListAsync(goArgs, cts.Token).ConfigureAwait(false);
                    if (objectList.Item2.Count == 0 &&
                        objectList.Item1.KeyCount.Equals("0", StringComparison.OrdinalIgnoreCase) && count == 0)
                        return;

                    var listObjectsItemResponse = new ListObjectsItemResponse(args, objectList, obs);
                    marker = listObjectsItemResponse.NextMarker;
                    isRunning = objectList.Item1.IsTruncated;
                    nextContinuationToken = objectList.Item1.IsTruncated
                        ? objectList.Item1.NextContinuationToken
                        : string.Empty;

                    cts.Token.ThrowIfCancellationRequested();
                    count++;
                }
            }
        );
    }

    /// <summary>
    ///     Gets the list of objects in the bucket filtered by prefix
    /// </summary>
    /// <param name="args">
    ///     GetObjectListArgs Arguments Object with information like Bucket name, prefix, delimiter, marker,
    ///     versions(get version IDs of the objects)
    /// </param>
    /// <returns>Task with a tuple populated with objects</returns>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    private async Task<Tuple<ListBucketResult, List<Item>>> GetObjectListAsync(GetObjectListArgs args,
        CancellationToken cancellationToken = default)
    {
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var responseResult =
            await this.ExecuteTaskAsync(requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var getObjectsListResponse = new GetObjectsListResponse(responseResult.StatusCode, responseResult.Content);
        return getObjectsListResponse.ObjectsTuple;
    }

    /// <summary>
    ///     Gets the list of objects in the bucket filtered by prefix
    /// </summary>
    /// <param name="bucketName">Bucket to list objects from</param>
    /// <param name="prefix">Filters all objects starting with a given prefix</param>
    /// <returns>Task with a tuple populated with objects</returns>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    private Task<Tuple<ListBucketResult, List<Item>>> GetObjectListAsync(string bucketName, string prefix,
        CancellationToken cancellationToken = default)
    {
        // null values are treated as empty strings.
        var args = new GetObjectListArgs()
            .WithBucket(bucketName)
            .WithPrefix(prefix);
        return GetObjectListAsync(args, cancellationToken);
    }
}
