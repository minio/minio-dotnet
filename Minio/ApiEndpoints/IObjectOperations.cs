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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Minio.DataModel;
using Minio.DataModel.Tags;
using Minio.DataModel.ObjectLock;
using Minio.Exceptions;

namespace Minio
{
    public interface IObjectOperations
    {
        /// <summary>
        /// Get the configuration object for Legal Hold Status 
        /// </summary>
        /// <param name="args">GetObjectLegalHoldArgs Arguments Object which has object identifier information - bucket name, object name, version ID</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation </param>
        /// <returns> True if Legal Hold is ON, false otherwise  </returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MissingObjectLockConfiguration">When object lock configuration on bucket is not set</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        Task<bool> GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Set the configuration for Legal Hold Status
        /// </summary>
        /// <param name="args">SetObjectLegalHoldArgs Arguments Object which has object identifier information - bucket name, object name, version ID and the status (ON/OFF) of legal-hold</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation </param>
        /// <returns> Task </returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MissingObjectLockConfiguration">When object lock configuration on bucket is not set</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        Task SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Set the Retention using the configuration object
        /// </summary>
        /// <param name="args">SetObjectRetentionArgs Arguments Object which has object identifier information - bucket name, object name, version ID</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns> Task </returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MissingObjectLockConfiguration">When object lock configuration on bucket is not set</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        Task SetObjectRetentionAsync(SetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the Retention configuration for the object
        /// </summary>
        /// <param name="args">GetObjectRetentionArgs Arguments Object which has object identifier information - bucket name, object name, version ID</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns> ObjectRetentionConfiguration object which contains the Retention configuration </returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MissingObjectLockConfiguration">When object lock configuration on bucket is not set</exception>
        Task<ObjectRetentionConfiguration> GetObjectRetentionAsync(GetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Clears the Retention configuration for the object
        /// </summary>
        /// <param name="args">ClearObjectRetentionArgs Arguments Object which has object identifier information - bucket name, object name, version ID</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns> Task </returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MissingObjectLockConfiguration">When object lock configuration on bucket is not set</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        Task ClearObjectRetentionAsync(ClearObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Removes an object with given name in specific bucket
        /// </summary>
        /// <param name="args">RemoveObjectArgs Arguments Object encapsulates information like - bucket name, object name, whether delete all versions</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Removes list of objects from bucket
        /// </summary>
        /// <param name="args">RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects, optional list of versions (for each object) to be deleted</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Observable that returns delete error while deleting objects if any</returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Copy a source object into a new destination object.
        /// </summary>
        /// <param name="args">CopyObjectArgs Arguments Object which encapsulates bucket name, object name, destination bucket, destination object names, Copy conditions object, metadata, SSE source, destination objects</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
        Task CopyObjectAsync(CopyObjectArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name, server-side encryption object, action stream, length, offset</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="DirectoryNotFoundException">If the directory to copy to is not found</exception>
        Task<ObjectStat> GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates object in a bucket fom input stream or filename.
        /// </summary>
        /// <param name="args">PutObjectArgs Arguments object encapsulating bucket name, object name, file name, object data stream, object size, content type.</param>
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
        Task PutObjectAsync(PutObjectArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Select an object's content. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="args">SelectObjectContentArgs Arguments Object which encapsulates bucket name, object name, Select Object Options</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        Task<SelectResponseStream> SelectObjectContentAsync(SelectObjectContentArgs args,CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix recursively
        /// </summary>
        /// <param name="args">ListIncompleteUploadsArgs Arguments Object which encapsulates bucket name, prefix, recursive</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        IObservable<Upload> ListIncompleteUploads(ListIncompleteUploadsArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Remove incomplete uploads from a given bucket and objectName
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
        Task RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Presigned get url - returns a presigned url to access an object's data without credentials.URL can have a maximum expiry of
        /// up to 7 days or a minimum of 1 second.Additionally, you can override a set of response headers using reqParams.
        /// </summary>
        /// <param name="args">PresignedGetObjectArgs Arguments object encapsulating bucket and object names, expiry time, response headers, request date</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        Task<string> PresignedGetObjectAsync(PresignedGetObjectArgs args);

        /// <summary>
        /// Presigned post policy
        /// </summary>
        /// <param name="args">PresignedPostPolicyArgs Arguments object encapsulating Policy, Expiry, Region, </param>
        /// <returns>Tuple of URI and Policy Form data</returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        Task<Tuple<string, Dictionary<string, string>>> PresignedPostPolicyAsync(PresignedPostPolicyArgs args);

        /// <summary>
        /// Presigned Put url -returns a presigned url to upload an object without credentials.URL can have a maximum expiry of
        /// upto 7 days or a minimum of 1 second.
        /// </summary>
        /// <param name="args">PresignedPutObjectArgs Arguments Object which encapsulates bucket, object names, expiry</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args);

        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        /// <param name="sse">Optional Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use GetObjectAsync method with GetObjectArgs object. Refer GetObject, GetObjectVersion & GetObjectQuery example code.")]
        Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="offset">offset of the object from where stream will start </param>
        /// <param name="length">length of object to read in from the stream</param>
        /// <param name="cb">A stream will be passed to the callback</param>
        /// <param name="sse">Optional Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use GetObjectAsync method with GetObjectArgs object. Refer GetObject, GetObjectVersion & GetObjectQuery example code.")]
        Task GetObjectAsync(string bucketName, string objectName, long offset, long length, Action<Stream> cb, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Select an object's content. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="opts">Select Object options</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use SelectObjectContentAsync method with SelectObjectContentsArgs object. Refer SelectObjectContent example code.")]
        Task<SelectResponseStream> SelectObjectContentAsync(string bucketName, string objectName, SelectObjectOptions opts, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates an object from file input stream
        /// </summary>
        /// <param name="bucketName">Bucket to create object in</param>
        /// <param name="objectName">Key of the new object</param>
        /// <param name="data">Stream of file to upload</param>
        /// <param name="size">Size of stream</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        /// <param name="metaData">Optional Object metadata to be stored. Defaults to null.</param>
        /// <param name="sse">Optional Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use PutObjectAsync method with PutObjectArgs object. Refer PutObject example code.")]
        Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Removes an object with given name in specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to remove object from</param>
        /// <param name="objectName">Key of object to remove</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use RemoveObjectAsync method with RemoveObjectArgs object. Refer RemoveObject example code.")]
        Task RemoveObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Removes objects in the list from specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to remove objects from</param>
        /// <param name="objectsList">List of object keys to remove</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use RemoveObjectsAsync method with RemoveObjectsArgs object. Refer RemoveObjects example code.")]
        Task<IObservable<DeleteError>> RemoveObjectAsync(string bucketName, IEnumerable<string> objectsList, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Tests the object's existence and returns metadata about existing objects.
        /// </summary>
        /// <param name="bucketName">Bucket to test object in</param>
        /// <param name="objectName">Name of the object to stat</param>
        /// <param name="sse">Optional Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Facts about the object</returns>
        [Obsolete("Use StatObjectAsync method with StatObjectArgs object. Refer StatObject & StatObjectQuery example code.")]
        Task<ObjectStat> StatObjectAsync(string bucketName, string objectName, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix recursively
        /// </summary>
        /// <param name="bucketName">Bucket to list all incomplete uploads from</param>
        /// <param name="prefix">prefix to list all incomplete uploads</param>
        /// <param name="recursive">Set to true to recursively list all incomplete uploads</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        [Obsolete("Use ListIncompleteUploads method with ListIncompleteUploadsArgs object. Refer ListIncompleteUploads example code.")]
        IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix = "", bool recursive = false, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Remove incomplete uploads from a given bucket and objectName
        /// </summary>
        /// <param name="bucketName">Bucket to remove incomplete uploads from</param>
        /// <param name="objectName">Key to remove incomplete uploads from</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use RemoveIncompleteUploadAsync method with RemoveIncompleteUploadArgs object. Refer RemoveIncompleteUpload example code.")]
        Task RemoveIncompleteUploadAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Copy a source object into a new destination object.
        /// </summary>
        /// <param name="bucketName"> Bucket name where the object to be copied exists.</param>
        /// <param name="objectName">Object name source to be copied.</param>
        /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
        /// <param name="destObjectName">Object name to be created, if not provided uses source object name as destination object name.</param>
        /// <param name="copyConditions">optionally can take a key value CopyConditions as well for conditionally attempting copyObject.</param>
        /// <param name="metadata">Optional Object metadata to be stored. Defaults to null.</param>
        /// <param name="sseSrc">Optional Server-side encryption option for source. Defaults to null.</param>
        /// <param name="sseDest">Optional Server-side encryption option for destination. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use CopyObjectAsync method with CopyObjectArgs object. Refer CopyObject example code.")]
        Task CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates an object from file
        /// </summary>
        /// <param name="bucketName">Bucket to create object in</param>
        /// <param name="objectName">Key of the new object</param>
        /// <param name="filePath">Path of file to upload</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        /// <param name="metaData">Optional Object metadata to be stored. Defaults to null.</param>
        /// <param name="sse">Optional Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use PutObjectAsync method with PutObjectArgs object. Refer PutObject example code.")]
        Task PutObjectAsync(string bucketName, string objectName, string filePath, string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="filePath">string with file path</param>
        /// <param name="sse">Optional Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use GetObjectAsync method with GetObjectArgs object. Refer GetObject, GetObjectVersion & GetObjectQuery example code.")]
        Task GetObjectAsync(string bucketName, string objectName, string filePath, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Presigned get url - returns a presigned url to access an object's data without credentials.URL can have a maximum expiry of
        /// upto 7 days or a minimum of 1 second.Additionally, you can override a set of response headers using reqParams.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds.</param>
        /// <param name="reqParams">Optional override response headers</param>
        /// <param name="reqDate">Optional request date and time in UTC</param>
        [Obsolete("Use PresignedGetObjectAsync method with PresignedGetObjectArgs object.")]
        Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string, string> reqParams = null, DateTime? reqDate = null);

        /// <summary>
        /// Presigned Put url - returns a presigned url to upload an object without credentials.URL can have a maximum expiry of
        /// upto 7 days or a minimum of 1 second.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        [Obsolete("Use PresignedPutObjectAsync method with PresignedPutObjectArgs object.")]
        Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt);

        /// <summary>
        /// Presigned post policy
        /// </summary>
        /// <param name="policy"></param>
        /// <returns></returns>
        Task<Tuple<string, Dictionary<string, string>>> PresignedPostPolicyAsync(PostPolicy policy);

        /// <summary>
        /// Gets Tagging values set for this object
        /// </summary>
        /// <param name="args"> GetObjectTagsArgs Arguments Object with information like Bucket, Object name, (optional)version Id</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Tagging Object with key-value tag pairs</returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        Task<Tagging> GetObjectTagsAsync(GetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Sets the Tagging values for this object
        /// </summary>
        /// <param name="args">SetObjectTagsArgs Arguments Object with information like Bucket name,Object name, (optional)version Id, tag key-value pairs</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        Task SetObjectTagsAsync(SetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Removes Tagging values stored for the object
        /// </summary>
        /// <param name="args">RemoveObjectTagsArgs Arguments Object with information like Bucket name</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        Task RemoveObjectTagsAsync(RemoveObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken));
    }
}