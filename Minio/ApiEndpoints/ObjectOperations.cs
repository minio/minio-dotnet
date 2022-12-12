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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.DataModel;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Tags;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio;

public partial class MinioClient : IObjectOperations
{
    private readonly List<string> supportedHeaders = new()
        { "cache-control", "content-encoding", "content-type", "x-amz-acl", "content-disposition" };

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
        return getObjectHelper(args, cancellationToken);
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
        args.Validate();
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
                    Tuple<ListMultipartUploadsResult, List<Upload>> uploads = null;
                    uploads = await GetMultipartUploadsListAsync(getArgs, cancellationToken).ConfigureAwait(false);
                    if (uploads == null)
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
        args.Validate();
        var listUploadArgs = new ListIncompleteUploadsArgs()
            .WithBucket(args.BucketName)
            .WithPrefix(args.ObjectName);

        Upload[] uploads = null;
        try
        {
            uploads = await ListIncompleteUploads(listUploadArgs, cancellationToken)?.ToArray();
        }
        catch (Exception ex)
        {
            //Bucket Not found. So, incomplete uploads are removed.
            if (ex.GetType() != typeof(BucketNotFoundException)) throw;
        }

        if (uploads == null) return;
        foreach (var upload in uploads)
            if (upload.Key.ToLower().Equals(args.ObjectName.ToLower()))
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        var authenticator = new V4Authenticator(Secure, AccessKey, SecretKey, Region,
            SessionToken);
        return authenticator.PresignURL(requestMessageBuilder, args.Expiry, Region, SessionToken, args.RequestDate);
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
    public async Task<(Uri, Dictionary<string, string>)> PresignedPostPolicyAsync(PresignedPostPolicyArgs args)
    {
        // string region = string.Empty;
        var region = await GetRegion(args.BucketName);
        args.Validate();
        // Presigned operations are not allowed for anonymous users
        if (string.IsNullOrEmpty(AccessKey) && string.IsNullOrEmpty(SecretKey))
            throw new MinioException("Presigned operations are not supported for anonymous credentials");

        var authenticator = new V4Authenticator(Secure, AccessKey, SecretKey,
            region, SessionToken);

        // Get base64 encoded policy.
        var policyBase64 = args.Policy.Base64();

        var t = DateTime.UtcNow;
        var signV4Algorithm = "AWS4-HMAC-SHA256";
        var credential = authenticator.GetCredentialString(t, region);
        var signature = authenticator.PresignPostSignature(region, t, policyBase64);
        args = args.WithDate(t)
            .WithAlgorithm(signV4Algorithm)
            .WithSessionToken(SessionToken)
            .WithCredential(credential)
            .WithRegion(region);


        // Fill in the form data.
        args.Policy.formData["bucket"] = args.BucketName;
        // args.Policy.formData["key"] = "\\\"" + args.ObjectName + "\\\"";

        args.Policy.formData["key"] = args.ObjectName;

        args.Policy.formData["policy"] = policyBase64;
        args.Policy.formData["x-amz-algorithm"] = signV4Algorithm;
        args.Policy.formData["x-amz-credential"] = credential;
        args.Policy.formData["x-amz-date"] = t.ToString("yyyyMMddTHHmmssZ");
        if (SessionToken != "") args.Policy.formData["x-amz-security-token"] = SessionToken;
        args.Policy.formData["x-amz-signature"] = signature;

        uri = RequestUtil.MakeTargetURL(BaseUrl, Secure, args.BucketName, region, false);
        return (uri, args.Policy.formData);
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(HttpMethod.Put, args.BucketName,
            args.ObjectName,
            contentType: Convert.ToString(args.GetType()), // contentType
            headerMap: args.Headers, // metaData
            body: utils.ObjectToByteArray(args.RequestBody));
        var authenticator = new V4Authenticator(Secure, AccessKey, SecretKey, Region,
            SessionToken);
        return authenticator.PresignURL(requestMessageBuilder, args.Expiry, Region, SessionToken);
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);
        var legalHoldConfig = new GetLegalHoldResponse(response.StatusCode, response.Content);
        return legalHoldConfig.CurrentLegalHoldConfiguration == null
            ? false
            : legalHoldConfig.CurrentLegalHoldConfiguration.Status.ToLower().Equals("on");
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var restResponse = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken);
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
        args.Validate();
        var errs = new List<DeleteError>();
        if (args.ObjectNamesVersions.Count > 0)
            errs = await removeObjectVersionsHelper(args, errs, cancellationToken);
        else
            errs = await removeObjectsHelper(args, errs, cancellationToken);

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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
    public async Task PutObjectAsync(PutObjectArgs args, CancellationToken cancellationToken = default)
    {
        args.Validate();
        if (args.SSE != null) args.SSE.Marshal(args.Headers);

        // Upload object in single part if size falls under restricted part size.
        if (args.ObjectSize < Constants.MinimumPartSize && args.ObjectSize >= 0 && args.ObjectStreamData != null)
        {
            var bytes = await ReadFullAsync(args.ObjectStreamData, (int)args.ObjectSize).ConfigureAwait(false);
            var bytesRead = bytes == null ? 0 : bytes.Length;
            if (bytesRead != (int)args.ObjectSize)
                throw new UnexpectedShortReadException(
                    $"Data read {bytesRead} is shorter than the size {args.ObjectSize} of input buffer.");
            args = args.WithRequestBody(bytes)
                .WithStreamData(null)
                .WithObjectSize(bytesRead);
            await PutObjectSinglePartAsync(args, cancellationToken).ConfigureAwait(false);
            return;
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
            .WithRequestBody(args.RequestBody)
            .WithHeaders(args.Headers);
        Dictionary<int, string> etags = null;
        // Upload file contents.
        if (!string.IsNullOrEmpty(args.FileName))
        {
            var fileInfo = new FileInfo(args.FileName);
            var size = fileInfo.Length;
            using (var fileStream = new FileStream(args.FileName, FileMode.Open, FileAccess.Read))
            {
                putObjectPartArgs = putObjectPartArgs
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithRequestBody(null);
                etags = await PutObjectPartAsync(putObjectPartArgs, cancellationToken).ConfigureAwait(false);
            }
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
        await CompleteMultipartUploadAsync(completeMultipartUploadArgs, cancellationToken).ConfigureAwait(false);
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
        ServerSideEncryption sseGet = null;
        if (args.SourceObject.SSE is SSECopy sSECopy) sseGet = sSECopy.CloneToSSEC();

        var statArgs = new StatObjectArgs()
            .WithBucket(args.SourceObject.BucketName)
            .WithObject(args.SourceObject.ObjectName)
            .WithVersionId(args.SourceObject.VersionId)
            .WithServerSideEncryption(sseGet);
        var stat = await StatObjectAsync(statArgs, cancellationToken).ConfigureAwait(false);
        args.WithCopyObjectSourceStats(stat);
        if (stat.TaggingCount > 0 && !args.ReplaceTagsDirective)
        {
            var getTagArgs = new GetObjectTagsArgs()
                .WithBucket(args.SourceObject.BucketName)
                .WithObject(args.SourceObject.ObjectName)
                .WithVersionId(args.SourceObject.VersionId)
                .WithServerSideEncryption(sseGet);
            var tag = await GetObjectTagsAsync(getTagArgs, cancellationToken).ConfigureAwait(false);
            args.WithTagging(tag);
        }

        args.Validate();
        var srcByteRangeSize = args.SourceObject.CopyOperationConditions != null
            ? args.SourceObject.CopyOperationConditions.GetByteRange()
            : 0L;
        var copySize = srcByteRangeSize == 0 ? args.SourceObjectInfo.Size : srcByteRangeSize;

        if (srcByteRangeSize > args.SourceObjectInfo.Size ||
            (srcByteRangeSize > 0 &&
             args.SourceObject.CopyOperationConditions.byteRangeEnd >= args.SourceObjectInfo.Size))
            throw new ArgumentException("Specified byte range (" +
                                        args.SourceObject.CopyOperationConditions.byteRangeStart +
                                        "-" + args.SourceObject.CopyOperationConditions.byteRangeEnd +
                                        ") does not fit within source object (size=" +
                                        args.SourceObjectInfo.Size + ")");

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
            Dictionary<string, string> newMeta = null;
            if (args.ReplaceMetadataDirective)
                newMeta = new Dictionary<string, string>(args.Headers);
            else
                newMeta = new Dictionary<string, string>(args.SourceObjectInfo.MetaData);
            if (args.SourceObject.SSE != null && args.SourceObject.SSE is SSECopy)
                args.SourceObject.SSE.Marshal(newMeta);
            if (args.SSE != null) args.SSE.Marshal(newMeta);
            cpReqArgs.WithHeaders(newMeta);
            await CopyObjectRequestAsync(cpReqArgs, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Presigned post policy
    /// </summary>
    /// <param name="policy"></param>
    /// <returns></returns>
    public Task<(Uri, Dictionary<string, string>)> PresignedPostPolicyAsync(PostPolicy policy)
    {
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);
        var responseHeaders = new Dictionary<string, string>();
        foreach (var param in response.Headers.ToList()) responseHeaders.Add(param.Key, param.Value);
        var statResponse = new StatObjectResponse(response.StatusCode, response.Content, response.Headers, args);

        return statResponse.ObjectInfo;
    }

    /// <summary>
    ///     Get an object. The object will be streamed to the callback given by the user.
    /// </summary>
    /// <param name="bucketName">Bucket to retrieve object from</param>
    /// <param name="objectName">Name of object to retrieve</param>
    /// <param name="cb">A stream will be passed to the callback</param>
    /// <param name="sse">Server-side encryption option. Defaults to null.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    [Obsolete(
        "Use GetObjectAsync method with GetObjectArgs object. Refer GetObject, GetObjectVersion & GetObjectQuery example code.")]
    public Task GetObjectAsync(string bucketName, string objectName, Action<Stream> cb,
        ServerSideEncryption sse = null, CancellationToken cancellationToken = default)
    {
        var args = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream(cb)
            .WithServerSideEncryption(sse);
        return GetObjectAsync(args, cancellationToken);
    }


    /// <summary>
    ///     Get an object. The object will be streamed to the callback given by the user.
    /// </summary>
    /// <param name="bucketName">Bucket to retrieve object from</param>
    /// <param name="objectName">Name of object to retrieve</param>
    /// <param name="offset"> Offset of the object from where stream will start</param>
    /// <param name="length">length of the object that will be read in the stream </param>
    /// <param name="cb">A stream will be passed to the callback</param>
    /// <param name="sse">Server-side encryption option. Defaults to null.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    [Obsolete(
        "Use GetObjectAsync method with GetObjectArgs object. Refer GetObject, GetObjectVersion & GetObjectQuery example code.")]
    public Task GetObjectAsync(string bucketName, string objectName, long offset, long length, Action<Stream> cb,
        ServerSideEncryption sse = null, CancellationToken cancellationToken = default)
    {
        var args = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream(cb)
            .WithOffsetAndLength(offset, length)
            .WithServerSideEncryption(sse);
        return GetObjectAsync(args);
    }

    /// <summary>
    ///     Get an object. The object will be streamed to the callback given by the user.
    /// </summary>
    /// <param name="bucketName">Bucket to retrieve object from</param>
    /// <param name="objectName">Name of object to retrieve</param>
    /// <param name="fileName">string with file path</param>
    /// <param name="sse">Server-side encryption option. Defaults to null.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    [Obsolete(
        "Use GetObjectAsync method with GetObjectArgs object. Refer GetObject, GetObjectVersion & GetObjectQuery example code.")]
    public Task GetObjectAsync(string bucketName, string objectName, string fileName, ServerSideEncryption sse = null,
        CancellationToken cancellationToken = default)
    {
        var args = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithFile(fileName)
            .WithServerSideEncryption(sse);
        return GetObjectAsync(args, cancellationToken);
    }


    /// <summary>
    ///     Select an object's content. The object will be streamed to the callback given by the user.
    /// </summary>
    /// <param name="bucketName">Bucket to retrieve object from</param>
    /// <param name="objectName">Name of object to retrieve</param>
    /// <param name="opts">Select Object options</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    [Obsolete(
        "Use SelectObjectContentAsync method with SelectObjectContentsArgs object. Refer SelectObjectContent example code.")]
    public Task<SelectResponseStream> SelectObjectContentAsync(string bucketName, string objectName,
        SelectObjectOptions opts, CancellationToken cancellationToken = default)
    {
        var args = new SelectObjectContentArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithExpressionType(opts.ExpressionType)
            .WithInputSerialization(opts.InputSerialization)
            .WithOutputSerialization(opts.OutputSerialization)
            .WithQueryExpression(opts.Expression)
            .WithServerSideEncryption(opts.SSE)
            .WithRequestProgress(opts.RequestProgress);
        return SelectObjectContentAsync(args, cancellationToken);
    }

    /// <summary>
    ///     Creates an object from file
    /// </summary>
    /// <param name="bucketName">Bucket to create object in</param>
    /// <param name="objectName">Key of the new object</param>
    /// <param name="fileName">Path of file to upload</param>
    /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
    /// <param name="metaData">Object metadata to be stored. Defaults to null.</param>
    /// <param name="sse">Server-side encryption option. Defaults to null.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    [Obsolete("Use PutObjectAsync method with PutObjectArgs object. Refer to PutObject example code.")]
    public Task PutObjectAsync(string bucketName, string objectName, string fileName, string contentType = null,
        Dictionary<string, string> metaData = null, ServerSideEncryption sse = null,
        CancellationToken cancellationToken = default)
    {
        var args = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithFileName(fileName)
            .WithContentType(contentType)
            .WithHeaders(metaData)
            .WithServerSideEncryption(sse);
        return PutObjectAsync(args, cancellationToken);
    }

    /// <summary>
    ///     Creates an object from inputstream
    /// </summary>
    /// <param name="bucketName">Bucket to create object in</param>
    /// <param name="objectName">Key of the new object</param>
    /// <param name="data">Stream of bytes to send</param>
    /// <param name="size">Total size of bytes to be written, must match with data's length</param>
    /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
    /// <param name="metaData">Object metadata to be stored. Defaults to null.</param>
    /// <param name="sse">Server-side encryption option. Defaults to null.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    [Obsolete("Use PutObjectAsync method with PutObjectArgs object. Refer PutObject example code.")]
    public Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType = null,
        Dictionary<string, string> metaData = null, ServerSideEncryption sse = null,
        CancellationToken cancellationToken = default)
    {
        var args = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType)
            .WithHeaders(metaData)
            .WithServerSideEncryption(sse);
        return PutObjectAsync(args, cancellationToken);
    }


    /// <summary>
    ///     Lists all incomplete uploads in a given bucket and prefix recursively
    /// </summary>
    /// <param name="bucketName">Bucket to list all incomplete uploads from</param>
    /// <param name="prefix">Filter all incomplete uploads starting with this prefix</param>
    /// <param name="recursive">Set to true to recursively list all incomplete uploads</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A lazily populated list of incomplete uploads</returns>
    [Obsolete(
        "Use ListIncompleteUploads method with ListIncompleteUploadsArgs object. Refer ListIncompleteUploads example code.")]
    public IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix = null, bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var args = new ListIncompleteUploadsArgs()
            .WithBucket(bucketName)
            .WithPrefix(prefix)
            .WithDelimiter("/");
        if (recursive)
        {
            args = args.WithDelimiter(null);
            return ListIncompleteUploads(args, cancellationToken);
        }

        return ListIncompleteUploads(args, cancellationToken);
    }

    /// <summary>
    ///     Remove incomplete uploads from a given bucket and objectName
    /// </summary>
    /// <param name="bucketName">Bucket to remove incomplete uploads from</param>
    /// <param name="objectName">Key to remove incomplete uploads from</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    [Obsolete(
        "Use RemoveIncompleteUploadAsync method with RemoveIncompleteUploadArgs object. Refer RemoveIncompleteUpload example code.")]
    public Task RemoveIncompleteUploadAsync(string bucketName, string objectName,
        CancellationToken cancellationToken = default)
    {
        var args = new RemoveIncompleteUploadArgs()
            .WithBucket(bucketName)
            .WithObject(objectName);
        return RemoveIncompleteUploadAsync(args);
    }

    /// <summary>
    ///     Removes an object with given name in specific bucket
    /// </summary>
    /// <param name="bucketName">Bucket to remove object from</param>
    /// <param name="objectName">Key of object to remove</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    [Obsolete("Use RemoveObjectAsync method with RemoveObjectArgs object. Refer RemoveObject example code.")]
    public Task RemoveObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName);
        return RemoveObjectAsync(args, cancellationToken);
    }


    /// <summary>
    ///     Removes multiple objects from a specific bucket
    /// </summary>
    /// <param name="bucketName">Bucket to remove objects from</param>
    /// <param name="objectNames">List of object keys to remove.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    [Obsolete("Use RemoveObjectsAsync method with RemoveObjectsArgs object. Refer RemoveObjects example code.")]
    public Task<IObservable<DeleteError>> RemoveObjectAsync(string bucketName, IEnumerable<string> objectNames,
        CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectsArgs()
            .WithBucket(bucketName)
            .WithObjects(new List<string>(objectNames));
        return RemoveObjectsAsync(args, cancellationToken);
    }

    /// <summary>
    ///     Tests the object's existence and returns metadata about existing objects.
    /// </summary>
    /// <param name="bucketName">Bucket to test object in</param>
    /// <param name="objectName">Name of the object to stat</param>
    /// <param name="sse"> Server-side encryption option.Defaults to null</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>Facts about the object</returns>
    [Obsolete(
        "Use StatObjectAsync method with StatObjectArgs object. Refer StatObject & StatObjectQuery example code.")]
    public Task<ObjectStat> StatObjectAsync(string bucketName, string objectName,
        ServerSideEncryption sse = null, CancellationToken cancellationToken = default)
    {
        var args = new StatObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithServerSideEncryption(sse);
        return StatObjectAsync(args, cancellationToken);
    }

    /// <summary>
    ///     Copy a source object into a new destination object.
    /// </summary>
    /// <param name="bucketName">Bucket name where the object to be copied exists.</param>
    /// <param name="objectName">Object name source to be copied.</param>
    /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
    /// <param name="destObjectName">
    ///     Object name to be created, if not provided uses source object name as destination object
    ///     name.
    /// </param>
    /// <param name="copyConditions">
    ///     Optionally can take a key value CopyConditions as well for conditionally attempting
    ///     copyObject.
    /// </param>
    /// <param name="metadata">Optional Object metadata to be stored. Defaults to null.</param>
    /// <param name="sseSrc">Optional source encryption options.Defaults to null. </param>
    /// <param name="sseDest">Optional destination encryption options.Defaults to null.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    [Obsolete("Use CopyObjectAsync method with CopyObjectArgs object. Refer CopyObject example code.")]
    public Task CopyObjectAsync(string bucketName, string objectName, string destBucketName,
        string destObjectName = null, CopyConditions copyConditions = null, Dictionary<string, string> metadata = null,
        ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null,
        CancellationToken cancellationToken = default)
    {
        var cpSrcArgs = new CopySourceObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCopyConditions(copyConditions)
            .WithServerSideEncryption(sseSrc);
        var args = new CopyObjectArgs()
            .WithBucket(destBucketName)
            .WithObject(destObjectName)
            .WithCopyObjectSource(cpSrcArgs)
            .WithHeaders(metadata)
            .WithServerSideEncryption(sseDest);
        return CopyObjectAsync(args, cancellationToken);
    }

    /// <summary>
    ///     Presigned get url - returns a presigned url to access an object's data without credentials.URL can have a maximum
    ///     expiry of
    ///     upto 7 days or a minimum of 1 second.Additionally, you can override a set of response headers using reqParams.
    /// </summary>
    /// <param name="bucketName">Bucket to retrieve object from</param>
    /// <param name="objectName">Key of object to retrieve</param>
    /// <param name="expiresInt">Expiration time in seconds</param>
    /// <param name="reqParams">optional override response headers</param>
    /// <param name="reqDate">optional request date and time in UTC</param>
    /// <returns></returns>
    [Obsolete("Use PresignedGetObjectAsync method with PresignedGetObjectArgs object.")]
    public Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt,
        Dictionary<string, string> reqParams = null, DateTime? reqDate = null)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithHeaders(reqParams)
            .WithExpiry(expiresInt)
            .WithRequestDate(reqDate);

        return PresignedGetObjectAsync(args);
    }

    /// <summary>
    ///     Presigned Put url -returns a presigned url to upload an object without credentials.URL can have a maximum expiry of
    ///     upto 7 days or a minimum of 1 second.
    /// </summary>
    /// <param name="bucketName">Bucket to retrieve object from</param>
    /// <param name="objectName">Key of object to retrieve</param>
    /// <param name="expiresInt">Expiration time in seconds</param>
    /// <returns></returns>
    [Obsolete("Use PresignedPutObjectAsync method with PresignedPutObjectArgs object.")]
    public Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)
    {
        var args = new PresignedPutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithExpiry(expiresInt);
        return PresignedPutObjectAsync(args);
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
        args.Validate();
        ResponseResult response = null;
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);
        var getUploadResponse = new GetMultipartUploadsListResponse(response.StatusCode, response.Content);
        response.Dispose();

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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
    /// <returns></returns>
    /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
    /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
    /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
    /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
    /// <exception cref="ObjectDisposedException">The file stream has been disposed</exception>
    /// <exception cref="NotSupportedException">The file stream cannot be read from</exception>
    /// <exception cref="InvalidOperationException">The file stream is currently in a read operation</exception>
    /// <exception cref="AccessDeniedException">For encrypted PUT operation, Access is denied if the key is wrong</exception>
    private async Task<string> PutObjectSinglePartAsync(PutObjectArgs args,
        CancellationToken cancellationToken = default)
    {
        //Skipping validate as we need the case where stream sends 0 bytes
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken);
        var putObjectResponse = new PutObjectResponse(response.StatusCode, response.Content, response.Headers);
        return putObjectResponse.Etag;
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
    private async Task<Dictionary<int, string>> PutObjectPartAsync(PutObjectPartArgs args,
        CancellationToken cancellationToken = default)
    {
        args.Validate();
        dynamic multiPartInfo = utils.CalculateMultiPartSize(args.ObjectSize);
        double partSize = multiPartInfo.partSize;
        double partCount = multiPartInfo.partCount;
        double lastPartSize = multiPartInfo.lastPartSize;
        var totalParts = new Part[(int)partCount];

        var expectedReadSize = partSize;
        int partNumber;
        var numPartsUploaded = 0;
        var etags = new Dictionary<int, string>();
        for (partNumber = 1; partNumber <= partCount; partNumber++)
        {
            var dataToCopy = await ReadFullAsync(args.ObjectStreamData, (int)partSize).ConfigureAwait(false);
            if (dataToCopy == null && numPartsUploaded > 0) break;
            if (partNumber == partCount) expectedReadSize = lastPartSize;
            var putObjectArgs = new PutObjectArgs(args)
                .WithRequestBody(dataToCopy)
                .WithUploadId(args.UploadId)
                .WithPartNumber(partNumber);
            var etag = await PutObjectSinglePartAsync(putObjectArgs, cancellationToken).ConfigureAwait(false);
            numPartsUploaded += 1;
            totalParts[partNumber - 1] = new Part
                { PartNumber = partNumber, ETag = etag, Size = (long)expectedReadSize };
            etags[partNumber] = etag;
        }

        // This shouldn't happen where stream size is known.
        if (partCount != numPartsUploaded && args.ObjectSize != -1)
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
        dynamic multiPartInfo = utils.CalculateMultiPartSize(args.CopySize, true);
        double partSize = multiPartInfo.partSize;
        double partCount = multiPartInfo.partCount;
        double lastPartSize = multiPartInfo.lastPartSize;
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
            partCondition.byteRangeStart = (long)partSize * (partNumber - 1) + partCondition.byteRangeStart;
            if (partNumber < partCount)
                partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)partSize - 1;
            else
                partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)lastPartSize - 1;
            var queryMap = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                queryMap.Add("uploadId", uploadId);
                queryMap.Add("partNumber", partNumber.ToString());
            }

            if (args.SourceObject.SSE != null && args.SourceObject.SSE is SSECopy)
                args.SourceObject.SSE.Marshal(args.Headers);
            if (args.SSE != null) args.SSE.Marshal(args.Headers);
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
                { PartNumber = partNumber, ETag = cpPartResult.ETag, Size = (long)expectedReadSize };
        }

        var etags = new Dictionary<int, string>();
        for (partNumber = 1; partNumber <= partCount; partNumber++) etags[partNumber] = totalParts[partNumber - 1].ETag;
        var completeMultipartUploadArgs = new CompleteMultipartUploadArgs(args)
            .WithUploadId(uploadId)
            .WithETags(etags);
        // Complete multi part upload
        await CompleteMultipartUploadAsync(completeMultipartUploadArgs, cancellationToken).ConfigureAwait(false);
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken);
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken);
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
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
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
    private async Task CompleteMultipartUploadAsync(CompleteMultipartUploadArgs args,
        CancellationToken cancellationToken)
    {
        args.Validate();
        var requestMessageBuilder = await CreateRequest(args).ConfigureAwait(false);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Internal method to complete multi part upload of object to server.
    /// </summary>
    /// <param name="bucketName">Bucket Name</param>
    /// <param name="objectName">Object to be uploaded</param>
    /// <param name="uploadId">Upload Id</param>
    /// <param name="etags">Etags</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task CompleteMultipartUploadAsync(string bucketName, string objectName, string uploadId,
        Dictionary<int, string> etags, CancellationToken cancellationToken)
    {
        var requestMessageBuilder = await CreateRequest(HttpMethod.Post, bucketName,
                objectName)
            .ConfigureAwait(false);
        requestMessageBuilder.AddQueryParameter("uploadId", $"{uploadId}");

        var parts = new List<XElement>();

        for (var i = 1; i <= etags.Count; i++)
            parts.Add(new XElement("Part",
                new XElement("PartNumber", i),
                new XElement("ETag", etags[i])));

        var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);
        var bodyString = completeMultipartUploadXml.ToString();
        var body = Encoding.UTF8.GetBytes(bodyString);

        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Type", "application/xml");

        requestMessageBuilder.AddXmlBody(bodyString);
        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns an async observable of parts corresponding to a uploadId for a specific bucket and objectName
    /// </summary>
    /// <param name="bucketName">Bucket Name</param>
    /// <param name="objectName">Object Name</param>
    /// <param name="uploadId"></param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private IObservable<Part> ListParts(string bucketName, string objectName, string uploadId,
        CancellationToken cancellationToken)
    {
        return Observable.Create<Part>(
            async obs =>
            {
                var nextPartNumberMarker = 0;
                var isRunning = true;
                while (isRunning)
                {
                    var uploads = await GetListPartsAsync(bucketName, objectName, uploadId, nextPartNumberMarker,
                        cancellationToken).ConfigureAwait(false);
                    foreach (var part in uploads.Item2) obs.OnNext(part);

                    nextPartNumberMarker = uploads.Item1.NextPartNumberMarker;
                    isRunning = uploads.Item1.IsTruncated;
                }
            });
    }

    /// <summary>
    ///     Gets the list of parts corresponding to a uploadId for given bucket and object
    /// </summary>
    /// <param name="bucketName">Bucket Name</param>
    /// <param name="objectName">Object Name</param>
    /// <param name="uploadId"></param>
    /// <param name="partNumberMarker"></param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task<Tuple<ListPartsResult, List<Part>>> GetListPartsAsync(string bucketName, string objectName,
        string uploadId, int partNumberMarker, CancellationToken cancellationToken)
    {
        var requestMessageBuilder = await CreateRequest(HttpMethod.Get, bucketName,
                objectName)
            .ConfigureAwait(false);
        requestMessageBuilder.AddQueryParameter("uploadId", $"{uploadId}");
        if (partNumberMarker > 0) requestMessageBuilder.AddQueryParameter("part-number-marker", $"{partNumberMarker}");
        requestMessageBuilder.AddQueryParameter("max-parts", "1000");

        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);

        var contentBytes = Encoding.UTF8.GetBytes(response.Content);
        ListPartsResult listPartsResult = null;
        using (var stream = new MemoryStream(contentBytes))
        {
            listPartsResult = (ListPartsResult)new XmlSerializer(typeof(ListPartsResult)).Deserialize(stream);
        }

        var root = XDocument.Parse(response.Content);

        var uploads = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Part")
            select new Part
            {
                PartNumber = int.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}PartNumber").Value,
                    CultureInfo.CurrentCulture),
                ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value.Replace("\"", string.Empty),
                Size = long.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value,
                    CultureInfo.CurrentCulture)
            };

        return Tuple.Create(listPartsResult, uploads.ToList());
    }

    /// <summary>
    ///     Start a new multi-part upload request
    /// </summary>
    /// <param name="bucketName">Bucket Name</param>
    /// <param name="objectName">Object Name</param>
    /// <param name="metaData"></param>
    /// <param name="sseHeaders"> Server-side encryption options</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task<string> NewMultipartUploadAsync(string bucketName, string objectName,
        Dictionary<string, string> metaData, Dictionary<string, string> sseHeaders,
        CancellationToken cancellationToken = default)
    {
        foreach (var kv in sseHeaders) metaData.Add(kv.Key, kv.Value);

        var requestMessageBuilder = await CreateRequest(HttpMethod.Post, bucketName,
            objectName, metaData).ConfigureAwait(false);
        requestMessageBuilder.AddQueryParameter("uploads", "");

        using var response = await ExecuteTaskAsync(NoErrorHandlers,
            requestMessageBuilder, cancellationToken).ConfigureAwait(false);

        var contentBytes = Encoding.UTF8.GetBytes(response.Content);
        InitiateMultipartUploadResult newUpload = null;
        using (var contentStream = new MemoryStream(response.ContentBytes))
        {
            newUpload = (InitiateMultipartUploadResult)new XmlSerializer(typeof(InitiateMultipartUploadResult))
                .Deserialize(contentStream);
        }

        return newUpload.UploadId;
    }

    /// <summary>
    ///     Upload object part to bucket for particular uploadId
    /// </summary>
    /// <param name="bucketName">Bucket Name</param>
    /// <param name="objectName">Object Name</param>
    /// <param name="uploadId"></param>
    /// <param name="partNumber"></param>
    /// <param name="data"></param>
    /// <param name="metaData"></param>
    /// <param name="sseHeaders">Server-side encryption headers if any </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    [Obsolete("Use PutObjectAsync method with PutObjectArgs object. Refer PutObject example code.")]
    private async Task<string> PutObjectAsync(string bucketName, string objectName,
        string uploadId, int partNumber, byte[] data, Dictionary<string, string> metaData,
        Dictionary<string, string> sseHeaders, CancellationToken cancellationToken)
    {
        // For multi-part upload requests, metadata needs to be passed in the NewMultiPartUpload request
        var contentType = metaData["Content-Type"];
        if (uploadId != null) metaData = new Dictionary<string, string>();

        foreach (var kv in sseHeaders) metaData.Add(kv.Key, kv.Value);

        var requestMessageBuilder = await CreateRequest(HttpMethod.Put, bucketName,
                objectName,
                contentType: contentType,
                headerMap: metaData,
                body: data)
            .ConfigureAwait(false);
        if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
        {
            requestMessageBuilder.AddQueryParameter("uploadId", $"{uploadId}");
            requestMessageBuilder.AddQueryParameter("partNumber", $"{partNumber}");
        }

        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);

        string etag = null;
        foreach (var parameter in response.Headers)
            if (parameter.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase))
                etag = parameter.Value;
        return etag;
    }

    /// <summary>
    ///     Remove object with matching uploadId from bucket
    /// </summary>
    /// <param name="bucketName">Bucket Name</param>
    /// <param name="objectName"></param>
    /// <param name="uploadId"></param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    [Obsolete("Use RemoveUploadAsync method with RemoveUploadArgs object.")]
    private Task RemoveUploadAsync(string bucketName, string objectName, string uploadId,
        CancellationToken cancellationToken)
    {
        var args = new RemoveUploadArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithUploadId(uploadId);
        return RemoveUploadAsync(args, cancellationToken);
    }

    /// <summary>
    ///     Advances in the stream upto currentPartSize or End of Stream
    /// </summary>
    /// <param name="data"></param>
    /// <param name="currentPartSize"></param>
    /// <returns>bytes read in a byte array</returns>
    internal async Task<byte[]> ReadFullAsync(Stream data, int currentPartSize)
    {
        var result = new byte[currentPartSize];
        var totalRead = 0;
        while (totalRead < currentPartSize)
        {
            var curData = new byte[currentPartSize - totalRead];
            var curRead = await data.ReadAsync(curData, 0, currentPartSize - totalRead).ConfigureAwait(false);
            if (curRead == 0) break;
            for (var i = 0; i < curRead; i++) result[totalRead + i] = curData[i];
            totalRead += curRead;
        }

        if (totalRead == 0) return null;

        if (totalRead == currentPartSize) return result;

        var truncatedResult = new byte[totalRead];
        for (var i = 0; i < totalRead; i++) truncatedResult[i] = result[i];
        return truncatedResult;
    }

    /// <summary>
    ///     Create the copy request, execute it and
    /// </summary>
    /// <param name="bucketName">Bucket name where the object to be copied exists.</param>
    /// <param name="objectName">Object name source to be copied.</param>
    /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
    /// <param name="destObjectName">
    ///     Object name to be created, if not provided uses source object name as destination object
    ///     name.
    /// </param>
    /// <param name="copyConditions">
    ///     optionally can take a key value CopyConditions as well for conditionally attempting
    ///     copyObject.
    /// </param>
    /// <param name="customHeaders">optional custom header to specify byte range</param>
    /// <param name="queryMap">optional query parameters like upload id, part number etc for copy operations</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <param name="type">Type of XML serialization to be applied on the server response</param>
    /// <returns></returns>
    private async Task<object> CopyObjectRequestAsync(string bucketName, string objectName, string destBucketName,
        string destObjectName, CopyConditions copyConditions, Dictionary<string, string> customHeaders,
        Dictionary<string, string> queryMap, CancellationToken cancellationToken, Type type)
    {
        // Escape source object path.
        var sourceObjectPath = bucketName + "/" + utils.UrlEncode(objectName);

        // Destination object name is optional, if empty default to source object name.
        if (destObjectName == null) destObjectName = objectName;

        var requestMessageBuilder = await CreateRequest(HttpMethod.Put, destBucketName,
                destObjectName,
                customHeaders)
            .ConfigureAwait(false);
        if (queryMap != null)
            foreach (var query in queryMap)
                requestMessageBuilder.AddQueryParameter(query.Key, query.Value);
        // Set the object source
        requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source", sourceObjectPath);

        // If no conditions available, skip addition else add the conditions to the header
        if (copyConditions != null)
            foreach (var item in copyConditions.GetConditions())
                requestMessageBuilder.AddOrUpdateHeaderParameter(item.Key, item.Value);

        using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
            .ConfigureAwait(false);

        // Just read the result and parse content.
        var contentBytes = Encoding.UTF8.GetBytes(response.Content);

        object copyResult = null;
        using (var contentStream = new MemoryStream(response.ContentBytes))
        {
            if (type == typeof(CopyObjectResult))
                copyResult =
                    (CopyObjectResult)new XmlSerializer(typeof(CopyObjectResult)).Deserialize(contentStream);

            if (type == typeof(CopyPartResult))
                copyResult = (CopyPartResult)new XmlSerializer(typeof(CopyPartResult)).Deserialize(contentStream);
        }

        return copyResult;
    }

    /// <summary>
    ///     Make a multi part copy upload for objects larger than 5GB or if CopyCondition specifies a byte range.
    /// </summary>
    /// <param name="bucketName">source bucket name</param>
    /// <param name="objectName">source object name</param>
    /// <param name="destBucketName">destination bucket name</param>
    /// <param name="destObjectName">destination object name</param>
    /// <param name="copyConditions">copyconditions </param>
    /// <param name="copySize">size of copy upload</param>
    /// <param name="metadata">optional metadata on the destination side</param>
    /// <param name="sseSrc">optional Server-side encryption options</param>
    /// <param name="sseDest">optional Server-side encryption options</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns></returns>
    private async Task MultipartCopyUploadAsync(string bucketName, string objectName, string destBucketName,
        string destObjectName, CopyConditions copyConditions, long copySize,
        Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null,
        ServerSideEncryption sseDest = null, CancellationToken cancellationToken = default)
    {
        // For all sizes greater than 5GB or if Copy byte range specified in conditions and byte range larger
        // than minimum part size (5 MB) do multipart.
        dynamic multiPartInfo = utils.CalculateMultiPartSize(copySize, true);
        double partSize = multiPartInfo.partSize;
        double partCount = multiPartInfo.partCount;
        double lastPartSize = multiPartInfo.lastPartSize;
        var totalParts = new Part[(int)partCount];

        var sseHeaders = new Dictionary<string, string>();
        if (sseDest != null) sseDest.Marshal(sseHeaders);

        // No need to resume upload since this is a Server-side copy. Just initiate a new upload.
        var uploadId = await NewMultipartUploadAsync(destBucketName,
                destObjectName, metadata, sseHeaders, cancellationToken)
            .ConfigureAwait(false);

        // Upload each part
        var expectedReadSize = partSize;
        int partNumber;
        for (partNumber = 1; partNumber <= partCount; partNumber++)
        {
            var partCondition = copyConditions.Clone();
            partCondition.byteRangeStart = (long)partSize * (partNumber - 1) + partCondition.byteRangeStart;
            if (partNumber < partCount)
                partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)partSize - 1;
            else
                partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)lastPartSize - 1;

            var queryMap = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                queryMap.Add("uploadId", uploadId);
                queryMap.Add("partNumber", partNumber.ToString());
            }

            var customHeader = new Dictionary<string, string>
            {
                {
                    "x-amz-copy-source-range",
                    "bytes=" + partCondition.byteRangeStart + "-" + partCondition.byteRangeEnd
                }
            };

            if (sseSrc != null && sseSrc is SSECopy) sseSrc.Marshal(customHeader);
            if (sseDest != null) sseDest.Marshal(customHeader);

            var cpPartResult = (CopyPartResult)await CopyObjectRequestAsync(bucketName, objectName,
                destBucketName, destObjectName, copyConditions, customHeader, queryMap, cancellationToken,
                typeof(CopyPartResult)).ConfigureAwait(false);

            totalParts[partNumber - 1] = new Part
            {
                PartNumber = partNumber,
                ETag = cpPartResult.ETag,
                Size = (long)expectedReadSize
            };
        }

        var etags = new Dictionary<int, string>();
        for (partNumber = 1; partNumber <= partCount; partNumber++) etags[partNumber] = totalParts[partNumber - 1].ETag;
        // Complete multi part upload
        await CompleteMultipartUploadAsync(destBucketName, destObjectName,
            uploadId, etags, cancellationToken).ConfigureAwait(false);
    }
}