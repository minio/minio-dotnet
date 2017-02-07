/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2015 Minio, Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.DataModel;
using RestSharp;
namespace Minio
{
    public interface IObjectOperations
    {
        Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback);
        Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType);
        Task RemoveObjectAsync(string bucketName, string objectName);
        Task<ObjectStat> StatObjectAsync(string bucketName, string objectName);
        IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix, bool recursive);
        Task RemoveIncompleteUploadAsync(string bucketName, string objectName);
        Task<CopyObjectResult> CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null);
        Task PutObjectAsync(string bucketName, string objectName, string filePath, string contentType=null);
        Task GetObjectAsync(string bucketName, string objectName, string filePath);
 
  
        string PresignedGetObject(string bucketName, string objectName, int expiresInt);
        string PresignedPutObject(string bucketName, string objectName, int expiresInt);
        Dictionary<string, string> PresignedPostPolicy(PostPolicy policy);
       
    }
}
