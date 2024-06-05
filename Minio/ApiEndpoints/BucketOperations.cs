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

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Xml.Linq;
using CommunityToolkit.HighPerformance;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Encryption;
using Minio.DataModel.ILM;
using Minio.DataModel.Notification;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Replication;
using Minio.DataModel.Response;
using Minio.DataModel.Result;
using Minio.DataModel.Tags;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Split up in partial classes")]
public partial class MinioClient : IBucketOperations
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
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        var bucketList = new ListAllMyBucketsResult();
        if (HttpStatusCode.OK == response.StatusCode)
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
                await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            return response is not null &&
                   (response.Exception is null ||
                    response.Exception.GetType() != typeof(BucketNotFoundException));
        }
        catch (InternalClientException ice)
        {
            return (ice.ServerResponse is null ||
                    HttpStatusCode.NotFound != ice.ServerResponse.StatusCode) &&
                   ice.ServerResponse is not null;
        }
        catch (Exception ex)
        {
            if (ex.GetType() == typeof(BucketNotFoundException)) return false;
            throw;
        }
    }

    /// <summary>
    ///     Remove the bucket with the given name.
    /// </summary>
    /// <param name="args">RemoveBucketArgs Arguments Object which has bucket identifier information like bucket name .etc.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucketName is not found</exception>
    /// <exception cref="InvalidBucketNameException">When bucketName is null</exception>
    public async Task RemoveBucketAsync(RemoveBucketArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response = await this.ExecuteTaskAsync(ResponseErrorHandlers,
            requestMessageBuilder, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Create a bucket with the given name.
    /// </summary>
    /// <param name="args">MakeBucketArgs Arguments Object that has bucket info like name, location. etc</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="NotImplementedException">When object-lock or another extension is not implemented</exception>
    public async Task MakeBucketAsync(MakeBucketArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        if (string.IsNullOrEmpty(args.Location))
            args.Location = Config.Region;

        if (string.Equals(args.Location, "us-east-1", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(Config.Region))
            args.Location = Config.Region;

        args.IsBucketCreationRequest = true;
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Get Versioning information on the bucket with given bucket name
    /// </summary>
    /// <param name="args">GetVersioningArgs takes bucket as argument. </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> GetVersioningResponse with information populated from REST response </returns>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    public async Task<VersioningConfiguration> GetVersioningAsync(GetVersioningArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();

        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var responseResult =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        var versioningResponse = new GetVersioningResponse(responseResult.StatusCode, responseResult.Content);
        return versioningResponse.VersioningConfig;
    }

    /// <summary>
    ///     Set Versioning as specified on the bucket with given bucket name
    /// </summary>
    /// <param name="args">SetVersioningArgs Arguments Object with information like Bucket name, Versioning configuration</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetVersioningAsync(SetVersioningArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response = await this.ExecuteTaskAsync(ResponseErrorHandlers,
            requestMessageBuilder, cancellationToken: cancellationToken).ConfigureAwait(false);
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
    public async IAsyncEnumerable<Item> ListObjectsEnumAsync(ListObjectsArgs args,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (args == null) throw new ArgumentNullException(nameof(args));

        args.Validate();

        var goArgs = new GetObjectListArgs()
            .WithBucket(args.BucketName)
            .WithPrefix(args.Prefix)
            .WithDelimiter(args.Recursive ? string.Empty : "/")
            .WithVersions(args.Versions)
            .WithIncludeUserMetadata(args.IncludeUserMetadata)
            .WithMarker(string.Empty)
            .WithListObjectsV1(!args.UseV2)
            .WithHeaders(args.Headers)
            .WithVersionIdMarker(string.Empty);

        XNamespace ns = "http://s3.amazonaws.com/doc/2006-03-01/";
        var tag = ns + (args.Versions ? "Version" : "Contents");

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var requestMessageBuilder = await this.CreateRequest(goArgs).ConfigureAwait(false);
            using var responseResult = await this
                .ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (responseResult.StatusCode != HttpStatusCode.OK)
                throw new ErrorResponseException(
                    $"HTTP status-code {responseResult.StatusCode:D}: {responseResult.StatusCode}", responseResult);

#if NET2_0_OR_GREATER
    var root = await XDocument.LoadAsync(responseResult.ContentStream, LoadOptions.None, ct).ConfigureAwait(false);
#else
            var root = XDocument.Load(responseResult.ContentStream);
#endif

            var items = root.Root.Descendants(tag).Select(t =>
            {
                string contentType = null;
                string expires = null;
                Dictionary<string, string> userMetaData = null;

                if (args.IncludeUserMetadata)
                {
                    var xUserMetadata = t.Element(ns + "UserMetadata");
                    if (xUserMetadata == null)
                        throw new InvalidOperationException(
                            "Client doesn't support metadata while listing objects (MinIO specific feature)");

                    contentType = xUserMetadata.Element(ns + "content-type")?.Value;
                    expires = xUserMetadata.Element(ns + "expires")?.Value;
                    const string metaElementPrefix = "X-Amz-Meta-";
                    userMetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var xHeader in xUserMetadata.Elements().Where(x =>
                                 x.Name.Namespace == ns && x.Name.LocalName.StartsWith(metaElementPrefix,
                                     StringComparison.OrdinalIgnoreCase)))
                    {
                        var key = xHeader.Name.LocalName[metaElementPrefix.Length..];
                        userMetaData[key] = xHeader.Value;
                    }
                }

                var objectKey = t.Element(ns + "Key")?.Value;
                if (objectKey != null)
                    objectKey = HttpUtility.UrlDecode(objectKey);

                return new Item
                {
                    Key = objectKey,
                    LastModified = t.Element(ns + "LastModified")?.Value,
                    ETag = t.Element(ns + "ETag")?.Value,
                    Size = ulong.TryParse(t.Element(ns + "Size")?.Value, out var size) ? size : 0,
                    VersionId = t.Element(ns + "VersionId")?.Value,
                    ContentType = contentType,
                    Expires = expires,
                    UserMetadata = userMetaData,
                    IsDir = false
                };
            });
            foreach (var item in items)
                yield return item;

            var prefixes = from c in root.Root.Descendants(ns + "CommonPrefixes")
                select new Item { Key = c.Element(ns + "Prefix")?.Value, IsDir = true };
            foreach (var item in prefixes)
                yield return item;

            var nextContinuationToken = root.Root.Element(ns + "NextContinuationToken")?.Value;
            if (string.IsNullOrEmpty(nextContinuationToken)) break;

            goArgs.WithContinuationToken(nextContinuationToken);
        }
    }

    /// <summary>
    ///     Gets notification configuration for this bucket
    /// </summary>
    /// <param name="args">GetBucketNotificationsArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    public async Task<BucketNotification> GetBucketNotificationsAsync(GetBucketNotificationsArgs args,
        CancellationToken cancellationToken = default)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var responseResult =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var getBucketNotificationsResponse =
            new GetBucketNotificationsResponse(responseResult.StatusCode, responseResult.Content);
        return getBucketNotificationsResponse.BucketNotificationConfiguration;
    }

    /// <summary>
    ///     Sets the notification configuration for this bucket
    /// </summary>
    /// <param name="args">
    ///     SetBucketNotificationsArgs Arguments Object with information like Bucket name, notification object
    ///     with configuration to set
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetBucketNotificationsAsync(SetBucketNotificationsArgs args,
        CancellationToken cancellationToken = default)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes all bucket notification configurations stored on the server.
    /// </summary>
    /// <param name="args">RemoveAllBucketNotificationsArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task RemoveAllBucketNotificationsAsync(RemoveAllBucketNotificationsArgs args,
        CancellationToken cancellationToken = default)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Subscribes to bucket change notifications (a Minio-only extension)
    /// </summary>
    /// <param name="args">
    ///     ListenBucketNotificationsArgs Arguments Object with information like Bucket name, listen events,
    ///     prefix filter keys, suffix filter keys
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>An observable of JSON-based notification events</returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(ListenBucketNotificationsArgs args,
        CancellationToken cancellationToken = default)
    {
        if (S3utils.IsAmazonEndPoint(Config.BaseUrl))
            // Amazon AWS does not support bucket notifications
            throw new ConnectionException(
                "Listening for bucket notification is specific only to `minio` server endpoints");

        return Observable.Create<MinioNotificationRaw>(
            async (obs, ct) =>
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                var requestMessageBuilder =
                    await this.CreateRequest(args).ConfigureAwait(false);
                args = args.WithNotificationObserver(obs)
                    .WithEnableTrace(Config.TraceHttp);
                using var response =
                    await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                cts.Token.ThrowIfCancellationRequested();
            });
    }

    /// <summary>
    ///     Gets Tagging values set for this bucket
    /// </summary>
    /// <param name="args">GetBucketTagsArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Tagging Object with key-value tag pairs</returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    public async Task<Tagging> GetBucketTagsAsync(GetBucketTagsArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var responseResult =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var getBucketNotificationsResponse =
            new GetBucketTagsResponse(responseResult.StatusCode, responseResult.Content);
        return getBucketNotificationsResponse.BucketTags;
    }

    /// <summary>
    ///     Sets the Encryption Configuration for the mentioned bucket.
    /// </summary>
    /// <param name="args">SetBucketEncryptionArgs Arguments Object with information like Bucket name, encryption config</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetBucketEncryptionAsync(SetBucketEncryptionArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns the Encryption Configuration for the mentioned bucket.
    /// </summary>
    /// <param name="args">GetBucketEncryptionArgs Arguments Object encapsulating information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> An object of type ServerSideEncryptionConfiguration </returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    public async Task<ServerSideEncryptionConfiguration> GetBucketEncryptionAsync(GetBucketEncryptionArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var responseResult =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var getBucketEncryptionResponse =
            new GetBucketEncryptionResponse(responseResult.StatusCode, responseResult.Content);
        return getBucketEncryptionResponse.BucketEncryptionConfiguration;
    }

    /// <summary>
    ///     Removes the Encryption Configuration for the mentioned bucket.
    /// </summary>
    /// <param name="args">RemoveBucketEncryptionArgs Arguments Object encapsulating information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task RemoveBucketEncryptionAsync(RemoveBucketEncryptionArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Sets the Tagging values for this bucket
    /// </summary>
    /// <param name="args">SetBucketTagsArgs Arguments Object with information like Bucket name, tag key-value pairs</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetBucketTagsAsync(SetBucketTagsArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes Tagging values stored for the bucket.
    /// </summary>
    /// <param name="args">RemoveBucketTagsArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Sets the Object Lock Configuration on this bucket
    /// </summary>
    /// <param name="args">
    ///     SetObjectLockConfigurationArgs Arguments Object with information like Bucket name, object lock
    ///     configuration to set
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetObjectLockConfigurationAsync(SetObjectLockConfigurationArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the Object Lock Configuration on this bucket
    /// </summary>
    /// <param name="args">GetObjectLockConfigurationArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>ObjectLockConfiguration object</returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
    public async Task<ObjectLockConfiguration> GetObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var responseResult =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var resp = new GetObjectLockConfigurationResponse(responseResult.StatusCode, responseResult.Content);
        return resp.LockConfiguration;
    }

    /// <summary>
    ///     Removes the Object Lock Configuration on this bucket
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
    public async Task RemoveObjectLockConfigurationAsync(RemoveObjectLockConfigurationArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Sets the Lifecycle configuration for this bucket
    /// </summary>
    /// <param name="args">
    ///     SetBucketLifecycleArgs Arguments Object with information like Bucket name, Lifecycle configuration
    ///     object
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetBucketLifecycleAsync(SetBucketLifecycleArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets Lifecycle configuration set for this bucket returned in an object
    /// </summary>
    /// <param name="args">GetBucketLifecycleArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>LifecycleConfiguration Object with the lifecycle configuration</returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    public async Task<LifecycleConfiguration> GetBucketLifecycleAsync(GetBucketLifecycleArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var responseResult =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var response = new GetBucketLifecycleResponse(responseResult.StatusCode, responseResult.Content);
        return response.BucketLifecycle;
    }

    /// <summary>
    ///     Removes Lifecycle configuration stored for the bucket.
    /// </summary>
    /// <param name="args">RemoveBucketLifecycleArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task RemoveBucketLifecycleAsync(RemoveBucketLifecycleArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Get Replication configuration for the bucket
    /// </summary>
    /// <param name="args">GetBucketReplicationArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Replication configuration object</returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="MissingBucketReplicationConfigurationException">When bucket replication configuration is not set</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    public async Task<ReplicationConfiguration> GetBucketReplicationAsync(GetBucketReplicationArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var responseResult =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var response = new GetBucketReplicationResponse(responseResult.StatusCode, responseResult.Content);
        return response.Config;
    }

    /// <summary>
    ///     Set the Replication configuration for the bucket
    /// </summary>
    /// <param name="args">
    ///     SetBucketReplicationArgs Arguments Object with information like Bucket name, Replication
    ///     Configuration object
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="MissingBucketReplicationConfigurationException">When bucket replication configuration is not set</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    public async Task SetBucketReplicationAsync(SetBucketReplicationArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Remove Replication configuration for the bucket.
    /// </summary>
    /// <param name="args">RemoveBucketReplicationArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="MissingBucketReplicationConfigurationException">When bucket replication configuration is not set</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    public async Task RemoveBucketReplicationAsync(RemoveBucketReplicationArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Subscribes to bucket change notifications (a Minio-only extension)
    /// </summary>
    /// <param name="bucketName">Bucket to get notifications from</param>
    /// <param name="events">Events to listen for</param>
    /// <param name="prefix">Filter keys starting with this prefix</param>
    /// <param name="suffix">Filter keys ending with this suffix</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>An observable of JSON-based notification events</returns>
    public IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(
        string bucketName,
        IList<EventType> events,
        string prefix = "",
        string suffix = "",
        CancellationToken cancellationToken = default)
    {
        var eventList = new List<EventType>(events);
        var args = new ListenBucketNotificationsArgs()
            .WithBucket(bucketName)
            .WithEvents(eventList)
            .WithPrefix(prefix)
            .WithSuffix(suffix);
        return ListenBucketNotificationsAsync(args, cancellationToken);
    }

    /// <summary>
    ///     Returns current policy stored on the server for this bucket
    /// </summary>
    /// <param name="args">GetPolicyArgs object has information like Bucket name.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task that returns the Bucket policy as a json string</returns>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="UnexpectedMinioException">When a policy is not set</exception>
    public async Task<string> GetPolicyAsync(GetPolicyArgs args, CancellationToken cancellationToken = default)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var responseResult =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var getPolicyResponse = new GetPolicyResponse(responseResult.StatusCode, responseResult.Content);
        return getPolicyResponse.PolicyJsonString;
    }

    /// <summary>
    ///     Sets the current bucket policy
    /// </summary>
    /// <param name="args">SetPolicyArgs object has information like Bucket name and the policy to set in Json format</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="UnexpectedMinioException">When a policy is not set</exception>
    /// <returns>Task to set a policy</returns>
    public async Task SetPolicyAsync(SetPolicyArgs args, CancellationToken cancellationToken = default)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes the current bucket policy
    /// </summary>
    /// <param name="args">RemovePolicyArgs object has information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task to set a policy</returns>
    /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="UnexpectedMinioException">When a policy is not set</exception>
    public async Task RemovePolicyAsync(RemovePolicyArgs args, CancellationToken cancellationToken = default)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }
}
