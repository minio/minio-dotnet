/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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
using System.Threading.Tasks;
using MinioCore2.DataModel;

namespace MinioCore2
{
    public interface IObjectOperations
    {

        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback);

        /// <summary>
        /// Creates an object from file
        /// </summary>
        /// <param name="bucketName">Bucket to create object in</param>
        /// <param name="objectName">Key of the new object</param>
        /// <param name="fileName">Path of file to upload</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType);

        /// <summary>
        /// Removes an object with given name in specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to list incomplete uploads from</param>
        /// <param name="objectName">Key of object to list incomplete uploads from</param>
        /// <returns></returns>
        Task RemoveObjectAsync(string bucketName, string objectName);

        /// <summary>
        /// Tests the object's existence and returns metadata about existing objects.
        /// </summary>
        /// <param name="bucketName">Bucket to test object in</param>
        /// <param name="objectName">Name of the object to stat</param>
        /// <returns>Facts about the object</returns>
        Task<ObjectStat> StatObjectAsync(string bucketName, string objectName);

        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix recursively
        /// </summary>
        /// <param name="bucketName">Bucket to list all incomplepte uploads from</param>
        /// <param name="prefix">prefix to list all incomplete uploads</param>
        /// <param name="recursive">option to list incomplete uploads recursively</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix, bool recursive);

        /// <summary>
        /// Remove incomplete uploads from a given bucket and objectName
        /// </summary>
        /// <param name="bucketName">Bucket to remove incomplete uploads from</param>
        /// <param name="objectName">Key to remove incomplete uploads from</param>
        Task RemoveIncompleteUploadAsync(string bucketName, string objectName);

        /// <summary>
        ///  Copy a source object into a new destination object.
        /// </summary>
        /// <param name="bucketName"> Bucket name where the object to be copied exists.</param>
        /// <param name="objectName">Object name source to be copied.</param>
        /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
        /// <param name="destObjectName">Object name to be created, if not provided uses source object name as destination object name.</param>
        /// <param name="copyConditions">optionally can take a key value CopyConditions as well for conditionally attempting copyObject.</param>
        /// <returns></returns>

        Task<CopyObjectResult> CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null);

        Task PutObjectAsync(string bucketName, string objectName, string filePath, string contentType = null);

        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="fileName">string with file path</param>
        /// <returns></returns>
        Task GetObjectAsync(string bucketName, string objectName, string filePath);

        /// <summary>
        /// Presigned Get url.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        string PresignedGetObject(string bucketName, string objectName, int expiresInt);

        /// <summary>
        /// Presigned Put url.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>

        string PresignedPutObject(string bucketName, string objectName, int expiresInt);

        /// <summary>
        ///  Presigned post policy
        /// </summary>
        Dictionary<string, string> PresignedPostPolicy(PostPolicy policy);

    }
}