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
using System.Globalization;
using System.Net;
using System.Reactive.Linq;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Encryption;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Response;
using Minio.DataModel.Result;
using Minio.DataModel.Select;
using Minio.DataModel.Tags;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Split up in partial classes")]
public partial class MinioClient : IObjectOperations
{
    /// <summary>
    ///     Get an object. The object will be streamed to the callback given by the user.
    /// </summary>
    /// <param name="args">
    ///     GetObjectArgs Arguments Object encapsulates information like - bucket name, object name, server-side
    ///     encryption object, action stream, length, offset
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="DirectoryNotFoundException">If the directory to copy to is not found</exception>
    public Task<ObjectStat> GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default)
    {
        return GetObjectHelper(args, cancellationToken);
    }

    /// <summary>
    ///     Select an object's content. The object will be streamed to the callback given by the user.
    /// </summary>
    /// <param name="args">
    ///     SelectObjectContentArgs Arguments Object which encapsulates bucket name, object name, Select Object
    ///     Options
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    public async Task<SelectResponseStream> SelectObjectContentAsync(SelectObjectContentArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var selectObjectContentResponse =
            new SelectObjectContentResponse(response.StatusCode, response.Content, response.ContentBytes);
        return selectObjectContentResponse.ResponseStream;
    }

    /// <summary>
    ///     Lists all incomplete uploads in a given bucket and prefix recursively
    /// </summary>
    /// <param name="args">ListIncompleteUploadsArgs Arguments Object which encapsulates bucket name, prefix, recursive</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A lazily populated list of incomplete uploads</returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    public IObservable<Upload> ListIncompleteUploads(ListIncompleteUploadsArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        return Observable.Create<Upload>(
            async obs =>
            {
                string nextKeyMarker = null;
                string nextUploadIdMarker = null;
                var isRunning = true;

                while (isRunning)
                {
                    var getArgs = new GetMultipartUploadsListArgs()
                        .WithBucket(args.BucketName)
                        .WithDelimiter(args.Delimiter)
                        .WithPrefix(args.Prefix)
                        .WithKeyMarker(nextKeyMarker)
                        .WithUploadIdMarker(nextUploadIdMarker);
                    var uploads = await GetMultipartUploadsListAsync(getArgs, cancellationToken).ConfigureAwait(false);
                    if (uploads is null)
                    {
                        isRunning = false;
                        continue;
                    }

                    foreach (var upload in uploads.Item2) obs.OnNext(upload);
                    nextKeyMarker = uploads.Item1.NextKeyMarker;
                    nextUploadIdMarker = uploads.Item1.NextUploadIdMarker;
                    isRunning = uploads.Item1.IsTruncated;
                }
            });
    }

    /// <summary>
    ///     Remove incomplete uploads from a given bucket and objectName
    /// </summary>
    /// <param name="args">RemoveIncompleteUploadArgs Arguments Object which encapsulates bucket, object names</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var listUploadArgs = new ListIncompleteUploadsArgs()
            .WithBucket(args.BucketName)
            .WithPrefix(args.ObjectName);

        Upload[] uploads;
        try
        {
            uploads = await ListIncompleteUploads(listUploadArgs, cancellationToken)?.ToArray();
        }
        catch (Exception ex) when (ex.GetType() == typeof(BucketNotFoundException))
        {
            throw;
        }

        if (uploads is null) return;
        foreach (var upload in uploads)
            if (upload.Key.Equals(args.ObjectName, StringComparison.OrdinalIgnoreCase))
            {
                var rmArgs = new RemoveUploadArgs()
                    .WithBucket(args.BucketName)
                    .WithObject(args.ObjectName)
                    .WithUploadId(upload.UploadId);
                await RemoveUploadAsync(rmArgs, cancellationToken).ConfigureAwait(false);
            }
    }

    /// <summary>
    ///     Presigned get url - returns a presigned url to access an object's data without credentials.URL can have a maximum
    ///     expiry of
    ///     up to 7 days or a minimum of 1 second.Additionally, you can override a set of response headers using reqParams.
    /// </summary>
    /// <param name="args">
    ///     PresignedGetObjectArgs Arguments object encapsulating bucket and object names, expiry time, response
    ///     headers, request date
    /// </param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    public async Task<string> PresignedGetObjectAsync(PresignedGetObjectArgs args)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        var authenticator = new V4Authenticator(Config.Secure, Config.AccessKey, Config.SecretKey, Config.Region,
            Config.SessionToken);
        return authenticator.PresignURL(requestMessageBuilder, args.Expiry, Config.Region, Config.SessionToken,
            args.RequestDate);
    }

    /// <summary>
    ///     Presigned post policy
    /// </summary>
    /// <param name="args">PresignedPostPolicyArgs Arguments object encapsulating Policy, Expiry, Region, </param>
    /// <returns>Tuple of URI and Policy Form data</returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task<(Uri, IDictionary<string, string>)> PresignedPostPolicyAsync(PresignedPostPolicyArgs args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        // string region = string.Empty;
        var region = await this.GetRegion(args.BucketName).ConfigureAwait(false);
        args.Validate();
        // Presigned operations are not allowed for anonymous users
        if (string.IsNullOrEmpty(Config.AccessKey) && string.IsNullOrEmpty(Config.SecretKey))
            throw new MinioException("Presigned operations are not supported for anonymous credentials");

        var authenticator = new V4Authenticator(Config.Secure, Config.AccessKey, Config.SecretKey,
            region, Config.SessionToken);

        // Get base64 encoded policy.
        var policyBase64 = args.Policy.Base64();

        var t = DateTime.UtcNow;
        const string signV4Algorithm = "AWS4-HMAC-SHA256";
        var credential = authenticator.GetCredentialString(t, region);
        var signature = authenticator.PresignPostSignature(region, t, policyBase64);
        args = args.WithDate(t)
            .WithAlgorithm(signV4Algorithm)
            .WithSessionToken(Config.SessionToken)
            .WithCredential(credential)
            .WithRegion(region);

        // Fill in the form data.
        args.Policy.FormData["bucket"] = args.BucketName;
        // args.Policy.formData["key"] = "\\\"" + args.ObjectName + "\\\"";

        args.Policy.FormData["key"] = args.ObjectName;

        args.Policy.FormData["policy"] = policyBase64;
        args.Policy.FormData["x-amz-algorithm"] = signV4Algorithm;
        args.Policy.FormData["x-amz-credential"] = credential;
        args.Policy.FormData["x-amz-date"] = t.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(Config.SessionToken))
            args.Policy.FormData["x-amz-security-token"] = Config.SessionToken;
        args.Policy.FormData["x-amz-signature"] = signature;

        Config.Uri = RequestUtil.MakeTargetURL(Config.BaseUrl, Config.Secure, args.BucketName, region, false);
        return (Config.Uri, args.Policy.FormData);
    }

    /// <summary>
    ///     Presigned Put url -returns a presigned url to upload an object without credentials.URL can have a maximum expiry of
    ///     upto 7 days or a minimum of 1 second.
    /// </summary>
    /// <param name="args">PresignedPutObjectArgs Arguments Object which encapsulates bucket, object names, expiry</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(HttpMethod.Put, args.BucketName,
            args.ObjectName,
            args.Headers, // contentType
            Convert.ToString(args.GetType(), CultureInfo.InvariantCulture), // metaData
            Utils.ObjectToByteArray(args.RequestBody)).ConfigureAwait(false);
        var authenticator = new V4Authenticator(Config.Secure, Config.AccessKey, Config.SecretKey, Config.Region,
            Config.SessionToken);
        return authenticator.PresignURL(requestMessageBuilder, args.Expiry, Config.Region, Config.SessionToken);
    }

    /// <summary>
    ///     Get the configuration object for Legal Hold Status
    /// </summary>
    /// <param name="args">
    ///     GetObjectLegalHoldArgs Arguments Object which has object identifier information - bucket name,
    ///     object name, version ID
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation </param>
    /// <returns> True if Legal Hold is ON, false otherwise  </returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task<bool> GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var legalHoldConfig = new GetLegalHoldResponse(response.StatusCode, response.Content);
        return legalHoldConfig.CurrentLegalHoldConfiguration?.Status.Equals("on", StringComparison.OrdinalIgnoreCase) ==
               true;
    }

    /// <summary>
    ///     Set the Legal Hold Status using the related configuration
    /// </summary>
    /// <param name="args">
    ///     SetObjectLegalHoldArgs Arguments Object which has object identifier information - bucket name,
    ///     object name, version ID
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets Tagging values set for this object
    /// </summary>
    /// <param name="args"> GetObjectTagsArgs Arguments Object with information like Bucket, Object name, (optional)version Id</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Tagging Object with key-value tag pairs</returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    public async Task<Tagging> GetObjectTagsAsync(GetObjectTagsArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var getObjectTagsResponse = new GetObjectTagsResponse(response.StatusCode, response.Content);
        return getObjectTagsResponse.ObjectTags;
    }

    /// <summary>
    ///     Removes an object with given name in specific bucket
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectArgs Arguments Object encapsulates information like - bucket name, object name, optional
    ///     list of versions to be deleted
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Task</returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var restResponse =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes list of objects from bucket
    /// </summary>
    /// <param name="args">
    ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
    ///     optional list of versions (for each object) to be deleted
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Observable that returns delete error while deleting objects if any</returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        IList<DeleteError> errs = new List<DeleteError>();
        errs = args.ObjectNamesVersions.Count > 0
            ? await RemoveObjectVersionsHelper(args, errs.ToList(), cancellationToken).ConfigureAwait(false)
            : await RemoveObjectsHelper(args, errs, cancellationToken).ConfigureAwait(false);

        return Observable.Create<DeleteError>( // From Current change
            async obs =>
            {
                await Task.Yield();
                foreach (var error in errs) obs.OnNext(error);
            }
        );
    }

    /// <summary>
    ///     Sets the Tagging values for this object
    /// </summary>
    /// <param name="args">
    ///     SetObjectTagsArgs Arguments Object with information like Bucket name,Object name, (optional)version
    ///     Id, tag key-value pairs
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetObjectTagsAsync(SetObjectTagsArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes Tagging values stored for the object
    /// </summary>
    /// <param name="args">RemoveObjectTagsArgs Arguments Object with information like Bucket name</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task RemoveObjectTagsAsync(RemoveObjectTagsArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Set the Retention using the configuration object
    /// </summary>
    /// <param name="args">
    ///     SetObjectRetentionArgs Arguments Object which has object identifier information - bucket name,
    ///     object name, version ID
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task SetObjectRetentionAsync(SetObjectRetentionArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Get the Retention configuration for the object
    /// </summary>
    /// <param name="args">
    ///     GetObjectRetentionArgs Arguments Object which has object identifier information - bucket name,
    ///     object name, version ID
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
    public async Task<ObjectRetentionConfiguration> GetObjectRetentionAsync(GetObjectRetentionArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var retentionResponse = new GetRetentionResponse(response.StatusCode, response.Content);
        return retentionResponse.CurrentRetentionConfiguration;
    }

    /// <summary>
    ///     Clears the Retention configuration for the object
    /// </summary>
    /// <param name="args">
    ///     ClearObjectRetentionArgs Arguments Object which has object identifier information - bucket name,
    ///     object name, version ID
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns> Task </returns>
    /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
    /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
    /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
    public async Task ClearObjectRetentionAsync(ClearObjectRetentionArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates object in a bucket fom input stream or filename.
    /// </summary>
    /// <param name="args">
    ///     PutObjectArgs Arguments object encapsulating bucket name, object name, file name, object data
    ///     stream, object size, content type.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="FileNotFoundException">If the file to copy from not found</exception>
    /// <exception cref="ObjectDisposedException">The file stream has been disposed</exception>
    /// <exception cref="NotSupportedException">The file stream cannot be read from</exception>
    /// <exception cref="InvalidOperationException">The file stream is currently in a read operation</exception>
    /// <exception cref="AccessDeniedException">For encrypted PUT operation, Access is denied if the key is wrong</exception>
    public async Task<PutObjectResponse> PutObjectAsync(PutObjectArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        args.SSE?.Marshal(args.Headers);

        var isSnowball = args.Headers.ContainsKey("X-Amz-Meta-Snowball-Auto-Extract") &&
                         Convert.ToBoolean(args.Headers["X-Amz-Meta-Snowball-Auto-Extract"],
                             CultureInfo.InvariantCulture);

        // Upload object in single part if size falls under restricted part size
        // or the request has snowball objects
        if ((args.ObjectSize < Constants.MinimumPartSize || isSnowball) && args.ObjectSize >= 0 &&
            args.ObjectStreamData is not null)
        {
            var bytes = await ReadFullAsync(args.ObjectStreamData, (int)args.ObjectSize).ConfigureAwait(false);
            var bytesRead = bytes.Length;
            if (bytesRead != (int)args.ObjectSize)
                throw new UnexpectedShortReadException(
                    $"Data read {bytesRead.ToString(CultureInfo.InvariantCulture)} is shorter than the size {args.ObjectSize.ToString(CultureInfo.InvariantCulture)} of input buffer.");

            args = args.WithRequestBody(bytes)
                .WithStreamData(null)
                .WithObjectSize(bytesRead);
            return await PutObjectSinglePartAsync(args, cancellationToken, true).ConfigureAwait(false);
        }

        // For all sizes greater than 5MiB do multipart.
        var multipartUploadArgs = new NewMultipartUploadPutArgs()
            .WithBucket(args.BucketName)
            .WithObject(args.ObjectName)
            .WithVersionId(args.VersionId)
            .WithHeaders(args.Headers)
            .WithContentType(args.ContentType)
            .WithTagging(args.ObjectTags)
            .WithLegalHold(args.LegalHoldEnabled)
            .WithRetentionConfiguration(args.Retention)
            .WithServerSideEncryption(args.SSE);
        // Get upload Id after creating new multi-part upload operation to
        // be used in putobject part, complete multipart upload operations.
        var uploadId = await NewMultipartUploadAsync(multipartUploadArgs, cancellationToken).ConfigureAwait(false);
        // Remove SSE-S3 and KMS headers during PutObjectPart operations.
        var putObjectPartArgs = new PutObjectPartArgs()
            .WithBucket(args.BucketName)
            .WithObject(args.ObjectName)
            .WithObjectSize(args.ObjectSize)
            .WithContentType(args.ContentType)
            .WithUploadId(uploadId)
            .WithStreamData(args.ObjectStreamData)
            .WithProgress(args.Progress)
            .WithRequestBody(args.RequestBody)
            .WithHeaders(args.Headers);
        IDictionary<int, string> etags = null;
        // Upload file contents.
        if (!string.IsNullOrEmpty(args.FileName))
        {
            using var fileStream = new FileStream(args.FileName, FileMode.Open, FileAccess.Read);
            putObjectPartArgs = putObjectPartArgs
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithRequestBody(null);
            etags = await PutObjectPartAsync(putObjectPartArgs, cancellationToken).ConfigureAwait(false);
        }
        // Upload stream contents
        else
        {
            etags = await PutObjectPartAsync(putObjectPartArgs, cancellationToken).ConfigureAwait(false);
        }

        var completeMultipartUploadArgs = new CompleteMultipartUploadArgs()
            .WithBucket(args.BucketName)
            .WithObject(args.ObjectName)
            .WithUploadId(uploadId)
            .WithETags(etags);
        var putObjectResponse = await CompleteMultipartUploadAsync(completeMultipartUploadArgs, cancellationToken)
            .ConfigureAwait(false);
        putObjectResponse.Size = args.ObjectSize;
        return putObjectResponse;
    }

    /// <summary>
    ///     Copy a source object into a new destination object.
    /// </summary>
    /// <param name="args">
    ///     CopyObjectArgs Arguments Object which encapsulates bucket name, object name, destination bucket,
    ///     destination object names, Copy conditions object, metadata, SSE source, destination objects
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
    public async Task CopyObjectAsync(CopyObjectArgs args, CancellationToken cancellationToken = default)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        IServerSideEncryption sseGet = null;
        if (args.SourceObject.SSE is SSECopy sSECopy) sseGet = sSECopy.CloneToSSEC();

        var statArgs = new StatObjectArgs()
            .WithBucket(args.SourceObject.BucketName)
            .WithObject(args.SourceObject.ObjectName)
            .WithVersionId(args.SourceObject.VersionId)
            .WithServerSideEncryption(sseGet);
        var stat = await StatObjectAsync(statArgs, cancellationToken).ConfigureAwait(false);
        _ = args.WithCopyObjectSourceStats(stat);
        if (stat.TaggingCount > 0 && !args.ReplaceTagsDirective)
        {
            var getTagArgs = new GetObjectTagsArgs()
                .WithBucket(args.SourceObject.BucketName)
                .WithObject(args.SourceObject.ObjectName)
                .WithVersionId(args.SourceObject.VersionId)
                .WithServerSideEncryption(sseGet);
            var tag = await GetObjectTagsAsync(getTagArgs, cancellationToken).ConfigureAwait(false);
            _ = args.WithTagging(tag);
        }

        args.Validate();
        var srcByteRangeSize = args.SourceObject.CopyOperationConditions?.ByteRange ?? 0L;
        var copySize = srcByteRangeSize == 0 ? args.SourceObjectInfo.Size : srcByteRangeSize;

        if (srcByteRangeSize > args.SourceObjectInfo.Size ||
            (srcByteRangeSize > 0 &&
             args.SourceObject.CopyOperationConditions.byteRangeEnd >=
             args.SourceObjectInfo.Size))
            throw new InvalidDataException($"Specified byte range ({args.SourceObject
                .CopyOperationConditions
                .byteRangeStart.ToString(CultureInfo.InvariantCulture)}-{args.SourceObject
                .CopyOperationConditions.byteRangeEnd.ToString(CultureInfo.InvariantCulture)
            }) does not fit within source object (size={args.SourceObjectInfo.Size
                .ToString(CultureInfo.InvariantCulture)})");

        if (copySize > Constants.MaxSingleCopyObjectSize ||
            (srcByteRangeSize > 0 &&
             srcByteRangeSize != args.SourceObjectInfo.Size))
        {
            var multiArgs = new MultipartCopyUploadArgs(args)
                .WithCopySize(copySize);
            await MultipartCopyUploadAsync(multiArgs, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var sourceObject = new CopySourceObjectArgs()
                .WithBucket(args.SourceObject.BucketName)
                .WithObject(args.SourceObject.ObjectName)
                .WithVersionId(args.SourceObject.VersionId)
                .WithCopyConditions(args.SourceObject.CopyOperationConditions);

            var cpReqArgs = new CopyObjectRequestArgs()
                .WithBucket(args.BucketName)
                .WithObject(args.ObjectName)
                .WithVersionId(args.VersionId)
                .WithHeaders(args.Headers)
                .WithCopyObjectSource(sourceObject)
                .WithSourceObjectInfo(args.SourceObjectInfo)
                .WithCopyOperationObjectType(typeof(CopyObjectResult))
                .WithReplaceMetadataDirective(args.ReplaceMetadataDirective)
                .WithReplaceTagsDirective(args.ReplaceTagsDirective)
                .WithTagging(args.ObjectTags);
            cpReqArgs.Validate();
            var newMeta = args.ReplaceMetadataDirective
                ? new Dictionary<string, string>(args.Headers, StringComparer.Ordinal)
                : new Dictionary<string, string>(args.SourceObjectInfo.MetaData, StringComparer.Ordinal);
            if (args.SourceObject.SSE is not null and SSECopy)
                args.SourceObject.SSE.Marshal(newMeta);
            args.SSE?.Marshal(newMeta);
            _ = cpReqArgs.WithHeaders(newMeta);
            _ = await CopyObjectRequestAsync(cpReqArgs, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Presigned post policy
    /// </summary>
    /// <param name="policy"></param>
    /// <returns></returns>
    public Task<(Uri, IDictionary<string, string>)> PresignedPostPolicyAsync(PostPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        var args = new PresignedPostPolicyArgs()
            .WithBucket(policy.Bucket)
            .WithObject(policy.Key)
            .WithPolicy(policy);
        return PresignedPostPolicyAsync(args);
    }

    /// <summary>
    ///     Tests the object's existence and returns metadata about existing objects.
    /// </summary>
    /// <param name="args">
    ///     StatObjectArgs Arguments Object encapsulates information like - bucket name, object name,
    ///     server-side encryption object
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Facts about the object</returns>
    public async Task<ObjectStat> StatObjectAsync(StatObjectArgs args, CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var responseHeaders = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var param in response.Headers.ToList()) responseHeaders.Add(param.Key, param.Value);
        var statResponse = new StatObjectResponse(response.StatusCode, response.Content, response.Headers, args);

        return statResponse.ObjectInfo;
    }

    /// <summary>
    ///     Get list of multi-part uploads matching particular uploadIdMarker
    /// </summary>
    /// <param name="args">GetMultipartUploadsListArgs Arguments Object which encapsulates bucket name, prefix, recursive</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task<Tuple<ListMultipartUploadsResult, List<Upload>>> GetMultipartUploadsListAsync(
        GetMultipartUploadsListArgs args,
        CancellationToken cancellationToken)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var getUploadResponse = new GetMultipartUploadsListResponse(response.StatusCode, response.Content);

        return getUploadResponse.UploadResult;
    }

    /// <summary>
    ///     Remove object with matching uploadId from bucket
    /// </summary>
    /// <param name="args">RemoveUploadArgs Arguments Object which encapsulates bucket, object names, upload Id</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task RemoveUploadAsync(RemoveUploadArgs args, CancellationToken cancellationToken)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
    }

    /// <summary>
    ///     Upload object part to bucket for particular uploadId
    /// </summary>
    /// <param name="args">
    ///     PutObjectArgs encapsulates bucket name, object name, upload id, part number, object data(body),
    ///     Headers, SSE Headers
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <param name="singleFile">
    ///     This boolean parameter differentiates single part file upload and
    ///     multi part file upload as this function is shared by both.
    /// </param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectDisposedException">The file stream has been disposed</exception>
    /// <exception cref="NotSupportedException">The file stream cannot be read from</exception>
    /// <exception cref="InvalidOperationException">The file stream is currently in a read operation</exception>
    /// <exception cref="AccessDeniedException">For encrypted PUT operation, Access is denied if the key is wrong</exception>
    private async Task<PutObjectResponse> PutObjectSinglePartAsync(PutObjectArgs args,
        CancellationToken cancellationToken = default,
        bool singleFile = false)
    {
        //Skipping validate as we need the case where stream sends 0 bytes
        var progressReport = new ProgressReport();
        if (singleFile) args.Progress?.Report(progressReport);
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        if (singleFile && args.Progress is not null)
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(args.BucketName)
                .WithObject(args.ObjectName);
            var stat = await StatObjectAsync(statArgs, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                progressReport.Percentage = 100;
                progressReport.TotalBytesTransferred = stat.Size;
            }

            args.Progress.Report(progressReport);
        }

        return new PutObjectResponse(response.StatusCode, response.Content, response.Headers,
            args.ObjectSize, args.ObjectName);
    }

    /// <summary>
    ///     Upload object in multiple parts. Private Helper function
    /// </summary>
    /// <param name="args">PutObjectPartArgs encapsulates bucket name, object name, upload id, part number, object data(body)</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectDisposedException">The file stream has been disposed</exception>
    /// <exception cref="NotSupportedException">The file stream cannot be read from</exception>
    /// <exception cref="InvalidOperationException">The file stream is currently in a read operation</exception>
    /// <exception cref="AccessDeniedException">For encrypted PUT operation, Access is denied if the key is wrong</exception>
    private async Task<IDictionary<int, string>> PutObjectPartAsync(PutObjectPartArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var multiPartInfo = Utils.CalculateMultiPartSize(args.ObjectSize);
        var partSize = multiPartInfo.PartSize;
        var partCount = multiPartInfo.PartCount;
        var lastPartSize = multiPartInfo.LastPartSize;
        var totalParts = new Part[(int)partCount];

        var expectedReadSize = partSize;
        int partNumber;
        var numPartsUploaded = 0;
        var etags = new Dictionary<int, string>();
        var progressReport = new ProgressReport();
        args.Progress?.Report(progressReport);
        for (partNumber = 1; partNumber <= partCount; partNumber++)
        {
            var dataToCopy = await ReadFullAsync(args.ObjectStreamData, (int)partSize).ConfigureAwait(false);
            if (dataToCopy.IsEmpty && numPartsUploaded > 0) break;
            if (partNumber == partCount) expectedReadSize = lastPartSize;
            var putObjectArgs = new PutObjectArgs(args)
                .WithRequestBody(dataToCopy)
                .WithUploadId(args.UploadId)
                .WithPartNumber(partNumber);
            var putObjectResponse =
                await PutObjectSinglePartAsync(putObjectArgs, cancellationToken).ConfigureAwait(false);
            var etag = putObjectResponse.Etag;

            numPartsUploaded++;
            totalParts[partNumber - 1] = new Part
            {
                PartNumber = partNumber, ETag = etag, Size = (long)expectedReadSize
            };
            etags[partNumber] = etag;
            if (!dataToCopy.IsEmpty) progressReport.TotalBytesTransferred += dataToCopy.Length;
            if (args.ObjectSize != -1) progressReport.Percentage = (int)(100 * partNumber / partCount);
            args.Progress?.Report(progressReport);
        }

        // This shouldn't happen where stream size is known.
        if (args.ObjectSize != -1)
        {
            var removeUploadArgs = new RemoveUploadArgs()
                .WithBucket(args.BucketName)
                .WithObject(args.ObjectName)
                .WithUploadId(args.UploadId);
            await RemoveUploadAsync(removeUploadArgs, cancellationToken).ConfigureAwait(false);
            return null;
        }

        return etags;
    }

    /// <summary>
    ///     Make a multi part copy upload for objects larger than 5GB or if CopyCondition specifies a byte range.
    /// </summary>
    /// <param name="args">
    ///     MultipartCopyUploadArgs Arguments object encapsulating destination and source bucket, object names,
    ///     copy conditions, size, metadata, SSE
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
    private async Task MultipartCopyUploadAsync(MultipartCopyUploadArgs args,
        CancellationToken cancellationToken = default)
    {
        var multiPartInfo = Utils.CalculateMultiPartSize(args.CopySize, true);
        var partSize = multiPartInfo.PartSize;
        var partCount = multiPartInfo.PartCount;
        var lastPartSize = multiPartInfo.LastPartSize;
        var totalParts = new Part[(int)partCount];

        var nmuArgs = new NewMultipartUploadCopyArgs()
            .WithBucket(args.BucketName)
            .WithObject(args.ObjectName ?? args.SourceObject.ObjectName)
            .WithHeaders(args.Headers)
            .WithCopyObjectSource(args.SourceObject)
            .WithSourceObjectInfo(args.SourceObjectInfo)
            .WithReplaceMetadataDirective(args.ReplaceMetadataDirective)
            .WithReplaceTagsDirective(args.ReplaceTagsDirective);
        nmuArgs.Validate();
        // No need to resume upload since this is a Server-side copy. Just initiate a new upload.
        var uploadId = await NewMultipartUploadAsync(nmuArgs, cancellationToken).ConfigureAwait(false);
        var expectedReadSize = partSize;
        int partNumber;
        for (partNumber = 1; partNumber <= partCount; partNumber++)
        {
            var partCondition = args.SourceObject.CopyOperationConditions.Clone();
            partCondition.byteRangeStart = ((long)partSize * (partNumber - 1)) + partCondition.byteRangeStart;
            partCondition.byteRangeEnd = partNumber < partCount
                ? partCondition.byteRangeStart + (long)partSize - 1
                : partCondition.byteRangeStart + (long)lastPartSize - 1;
            var queryMap = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                queryMap.Add("uploadId", uploadId);
                queryMap.Add("partNumber", partNumber.ToString(CultureInfo.InvariantCulture));
            }

            if (args.SourceObject.SSE is not null and SSECopy)
                args.SourceObject.SSE.Marshal(args.Headers);
            args.SSE?.Marshal(args.Headers);
            var cpPartArgs = new CopyObjectRequestArgs()
                .WithBucket(args.BucketName)
                .WithObject(args.ObjectName)
                .WithVersionId(args.VersionId)
                .WithHeaders(args.Headers)
                .WithCopyOperationObjectType(typeof(CopyPartResult))
                .WithPartCondition(partCondition)
                .WithQueryMap(queryMap)
                .WithCopyObjectSource(args.SourceObject)
                .WithSourceObjectInfo(args.SourceObjectInfo)
                .WithReplaceMetadataDirective(args.ReplaceMetadataDirective)
                .WithReplaceTagsDirective(args.ReplaceTagsDirective)
                .WithTagging(args.ObjectTags);
            var cpPartResult =
                (CopyPartResult)await CopyObjectRequestAsync(cpPartArgs, cancellationToken).ConfigureAwait(false);

            totalParts[partNumber - 1] = new Part
            {
                PartNumber = partNumber, ETag = cpPartResult.ETag, Size = (long)expectedReadSize
            };
        }

        var etags = new Dictionary<int, string>();
        for (partNumber = 1; partNumber <= partCount; partNumber++) etags[partNumber] = totalParts[partNumber - 1].ETag;
        var completeMultipartUploadArgs = new CompleteMultipartUploadArgs(args)
            .WithUploadId(uploadId)
            .WithETags(etags);
        // Complete multi part upload
        _ = await CompleteMultipartUploadAsync(completeMultipartUploadArgs, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Start a new multi-part upload request
    /// </summary>
    /// <param name="args">
    ///     NewMultipartUploadPutArgs arguments object encapsulating bucket name, object name, Headers, SSE
    ///     Headers
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
    private async Task<string> NewMultipartUploadAsync(NewMultipartUploadPutArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var uploadResponse = new NewMultipartUploadResponse(response.StatusCode, response.Content);
        return uploadResponse.UploadId;
    }

    /// <summary>
    ///     Start a new multi-part copy upload request
    /// </summary>
    /// <param name="args">
    ///     NewMultipartUploadCopyArgs arguments object encapsulating bucket name, object name, Headers, SSE
    ///     Headers
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
    private async Task<string> NewMultipartUploadAsync(NewMultipartUploadCopyArgs args,
        CancellationToken cancellationToken = default)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var uploadResponse = new NewMultipartUploadResponse(response.StatusCode, response.Content);
        return uploadResponse.UploadId;
    }

    /// <summary>
    ///     Create the copy request, execute it and return the copy result.
    /// </summary>
    /// <param name="args"> CopyObjectRequestArgs Arguments Object encapsulating </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    private async Task<object> CopyObjectRequestAsync(CopyObjectRequestArgs args, CancellationToken cancellationToken)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        var copyObjectResponse =
            new CopyObjectResponse(response.StatusCode, response.Content, args.CopyOperationObjectType);
        return copyObjectResponse.CopyPartRequestResult;
    }

    /// <summary>
    ///     Internal method to complete multi part upload of object to server.
    /// </summary>
    /// <param name="args">CompleteMultipartUploadArgs Arguments object with bucket name, object name, upload id, Etags</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectNotFoundException">When object is not found</exception>
    /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
    private async Task<PutObjectResponse> CompleteMultipartUploadAsync(CompleteMultipartUploadArgs args,
        CancellationToken cancellationToken)
    {
        args?.Validate();
        var requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
        using var response =
            await this.ExecuteTaskAsync(ResponseErrorHandlers, requestMessageBuilder,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        return new PutObjectResponse(response.StatusCode, response.Content, response.Headers, -1,
            args.ObjectName);
    }

    /// <summary>
    ///     Advances in the stream upto currentPartSize or End of Stream
    /// </summary>
    /// <param name="data"></param>
    /// <param name="currentPartSize"></param>
    /// <returns>bytes read in a byte array</returns>
    internal async Task<ReadOnlyMemory<byte>> ReadFullAsync(Stream data, int currentPartSize)
    {
        Memory<byte> result = new byte[currentPartSize];
        var totalRead = 0;
        while (totalRead < currentPartSize)
        {
            Memory<byte> curData = new byte[currentPartSize - totalRead];
            var curRead = await data.ReadAsync(curData[..(currentPartSize - totalRead)]).ConfigureAwait(false);
            if (curRead == 0) break;
            for (var i = 0; i < curRead; i++)
                curData.Slice(i, 1).CopyTo(result[(totalRead + i)..]);
            totalRead += curRead;
        }

        if (totalRead == 0) return null;

        if (totalRead == currentPartSize) return result;

        Memory<byte> truncatedResult = new byte[totalRead];
        for (var i = 0; i < totalRead; i++)
            result.Slice(i, 1).CopyTo(truncatedResult[i..]);
        return truncatedResult;
    }
}
