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
using Minio.Exceptions;
using Minio.Helper;
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
using System.Text;

using Minio.DataModel.Tags;
using Minio.DataModel.ObjectLock;

namespace Minio
{
    public partial class MinioClient : IObjectOperations
    {

        /// <summary>
        /// Tests the object's existence and returns metadata about existing objects.
        /// </summary>
        /// <param name="args">StatObjectArgs Arguments Object encapsulates information like - bucket name, object name, server-side encryption object</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Facts about the object</returns>
        public async Task<ObjectStat> StatObjectAsync(StatObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
               .ConfigureAwait(false);
            StatObjectResponse statResponse = new StatObjectResponse(response.StatusCode, response.Content, response.Headers, args);

            return statResponse.ObjectInfo;
        }

        private readonly List<string> supportedHeaders = new List<string>
            {"cache-control", "content-encoding", "content-type", "x-amz-acl", "content-disposition"};

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
        public Task<ObjectStat> GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            utils.Print(args);
            return getObjectHelper(args, cancellationToken);
        }


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
        public async Task<SelectResponseStream> SelectObjectContentAsync(SelectObjectContentArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);
            SelectObjectContentResponse selectObjectContentResponse = new SelectObjectContentResponse(response.StatusCode, response.Content, response.ContentBytes);
            return selectObjectContentResponse.ResponseStream;
        }


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
        public IObservable<Upload> ListIncompleteUploads(ListIncompleteUploadsArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            return Observable.Create<Upload>(
              async obs =>
              {
                  string nextKeyMarker = null;
                  string nextUploadIdMarker = null;
                  bool isRunning = true;

                  while (isRunning)
                  {
                      GetMultipartUploadsListArgs getArgs = new GetMultipartUploadsListArgs()
                                                                            .WithBucket(args.BucketName)
                                                                            .WithDelimiter(args.Delimiter)
                                                                            .WithPrefix(args.Prefix)
                                                                            .WithKeyMarker(nextKeyMarker)
                                                                            .WithUploadIdMarker(nextUploadIdMarker);
                      Tuple<ListMultipartUploadsResult, List<Upload>> uploads = null;
                      try
                      {
                          uploads = await this.GetMultipartUploadsListAsync(getArgs, cancellationToken).ConfigureAwait(false);
                      }
                      catch (Exception)
                      {
                          throw;
                      }
                      if (uploads == null)
                      {
                          isRunning = false;
                          continue;
                      }
                      foreach (Upload upload in uploads.Item2)
                      {
                          obs.OnNext(upload);
                      }
                      nextKeyMarker = uploads.Item1.NextKeyMarker;
                      nextUploadIdMarker = uploads.Item1.NextUploadIdMarker;
                      isRunning = uploads.Item1.IsTruncated;
                  }
              });
        }

        // public async Task GetObjectAsync(string bucketName, string objectName, Action<Stream> cb,
        //     ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        // {
        //     // Stat to see if the object exists
        //     // NOTE: This avoids writing the error body to the action stream passed (Do not remove).
        //     await StatObjectAsync(bucketName, objectName, sse: sse, cancellationToken: cancellationToken)
        //         .ConfigureAwait(false);

        /// <summary>
        /// Get list of multi-part uploads matching particular uploadIdMarker
        /// </summary>
        /// <param name="args">GetMultipartUploadsListArgs Arguments Object which encapsulates bucket name, prefix, recursive</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<Tuple<ListMultipartUploadsResult, List<Upload>>> GetMultipartUploadsListAsync(GetMultipartUploadsListArgs args,
                                                                                     CancellationToken cancellationToken)
        {
            args.Validate();
            ResponseResult response = null;
            try
            {
                HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
                response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
            GetMultipartUploadsListResponse getUploadResponse = new GetMultipartUploadsListResponse(response.StatusCode, response.Content.ToString());
            return getUploadResponse.UploadResult;
        }


        /// <summary>
        /// Remove object with matching uploadId from bucket
        /// </summary>
        /// <param name="args">RemoveUploadArgs Arguments Object which encapsulates bucket, object names, upload Id</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task RemoveUploadAsync(RemoveUploadArgs args, CancellationToken cancellationToken)
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);
            // cb.Invoke(new MemoryStream(response.ContentBytes));
        }


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
        public async Task RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args, CancellationToken cancellationToken = default(CancellationToken))
        // public async Task GetObjectAsync(string bucketName, string objectName, long offset, long length,
        //     Action<Stream> cb, ServerSideEncryption sse = null,
        //     CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            ListIncompleteUploadsArgs listUploadArgs = new ListIncompleteUploadsArgs()
                                                                    .WithBucket(args.BucketName)
                                                                    .WithPrefix(args.ObjectName);

            Upload[] uploads = null;
            try
            {
                uploads = await this.ListIncompleteUploads(listUploadArgs, cancellationToken)?.ToArray();
            }
            catch (Exception ex)
            {
                //Bucket Not found. So, incomplete uploads are removed.
                if (ex.GetType() != typeof(BucketNotFoundException))
                {
                    throw ex;
                }
            }
            if (uploads == null)
            {
                return;
            }
            foreach (var upload in uploads)
            {
                if (upload.Key.ToLower().Equals(args.ObjectName.ToLower()))
                {
                    RemoveUploadArgs rmArgs = new RemoveUploadArgs()
                                                        .WithBucket(args.BucketName)
                                                        .WithObject(args.ObjectName)
                                                        .WithUploadId(upload.UploadId);
                    await this.RemoveUploadAsync(rmArgs, cancellationToken).ConfigureAwait(false);
                }
            }
        }


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
        public string PresignedGetObjectAsync(PresignedGetObjectArgs args)
        {
            args.Validate();
            Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, this.Region);
            HttpRequestMessageBuilder requestMessageBuilder = new HttpRequestMessageBuilder(
                args.RequestMethod, requestUrl, "/" + args.BucketName);
            var authenticator = new V4Authenticator(this.Secure, this.AccessKey, this.SecretKey, region: this.Region,
                sessionToken: this.SessionToken);

            return authenticator.PresignURL(requestMessageBuilder, args.Expiry, this.Region, this.SessionToken, args.RequestDate);
        }


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
        public async Task<Tuple<string, Dictionary<string, string>>> PresignedPostPolicyAsync(PresignedPostPolicyArgs args)
        {
            string region = await this.GetRegion(args.BucketName);
            args.Validate();
            var authenticator = new V4Authenticator(this.Secure, this.AccessKey, this.SecretKey, region: this.Region,
                sessionToken: this.SessionToken);

            //     return authenticator.PresignURL(request, expiresInt, Region, this.SessionToken, reqDate);
            args = args.WithSessionToken(this.SessionToken)
                        .WithCredential(authenticator.GetCredentialString(DateTime.UtcNow, region))
                        .WithSignature(authenticator.PresignPostSignature(region, DateTime.UtcNow, args.Policy.Base64()))
                        .WithRegion(region);
            // this.SetTargetURL(RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, args.BucketName, args.Region, usePathStyle: false));
            PresignedPostPolicyResponse policyResponse = new PresignedPostPolicyResponse(args, this.uri.ToString());
            return policyResponse.URIPolicyTuple;
        }


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
        public async Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args)
        {
            args.Validate();
            var requestMessageBuilder = await this.CreateRequest(HttpMethod.Put, args.BucketName,
                    objectName: args.ObjectName,
                    contentType: Convert.ToString(args.GetType()), // contentType
                    headerMap: args.Headers, // metaData
                    body: utils.ObjectToByteArray(args.RequestBody));
            var authenticator = new V4Authenticator(this.Secure, this.AccessKey, this.SecretKey, region: this.Region,
                sessionToken: this.SessionToken);
            return authenticator.PresignURL(requestMessageBuilder, args.Expiry, this.Region, this.SessionToken);
        }

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
        /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        public async Task<bool> GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
            var legalHoldConfig = new GetLegalHoldResponse(response.StatusCode, response.Content);
            return (legalHoldConfig.CurrentLegalHoldConfiguration == null) ? false : legalHoldConfig.CurrentLegalHoldConfiguration.Status.ToLower().Equals("on");
        }


        /// <summary>
        /// Set the Legal Hold Status using the related configuration
        /// </summary>
        /// <param name="args">SetObjectLegalHoldArgs Arguments Object which has object identifier information - bucket name, object name, version ID</param>
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
        public async Task SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);
        }


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
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        public async Task<Tagging> GetObjectTagsAsync(GetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);
            GetObjectTagsResponse getObjectTagsResponse = new GetObjectTagsResponse(response.StatusCode, response.Content);
            return getObjectTagsResponse.ObjectTags;
        }


        /// <summary>
        /// Removes an object with given name in specific bucket
        /// </summary>
        /// <param name="args">RemoveObjectArgs Arguments Object encapsulates information like - bucket name, object name, optional list of versions to be deleted</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Task</returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        public async Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var restResponse = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken);
        }


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
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        public async Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            List<DeleteError> errs = new List<DeleteError>();
            if (args.ObjectNamesVersions.Count > 0)
            {
                errs = await removeObjectVersionsHelper(args, errs, cancellationToken);
            }
            else
            {
                errs = await removeObjectsHelper(args, errs, cancellationToken);
            }

            return Observable.Create<DeleteError>(   // From Current change
                async (obs) =>
                {
                    await Task.Yield();
                    foreach (DeleteError error in errs)
                    {
                        obs.OnNext(error);
                    }
                }
            );

            // Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, this.Region);
            //         args.BucketName,
            //         objectName: args.ObjectNames,
            //         headerMap: args.Headers)
            //     .ConfigureAwait(false);

            // var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
            //     .ConfigureAwait(false);

            // cb.Invoke(new MemoryStream(response.ContentBytes));
            // public async Task<IObservable<DeleteError>> RemoveObjectAsync(string bucketName,  // Rest Sharp
            //     IEnumerable<string> objectNames, CancellationToken cancellationToken = default(CancellationToken))
            // {
            //     if (objectNames == null)
            //     {
            //         return null;
            //     }

            //     utils.ValidateBucketName(bucketName);
            //     List<DeleteObject> objectList;
            //     return Observable.Create<DeleteError>(
            //         async obs =>
            //         {
            //             bool process = true;
            //             int count = objectNames.Count();
            //             int i = 0;

            //             while (process)
            //             {
            //                 objectList = new List<DeleteObject>();
            //                 while (i < count)
            //                 {
            //                     string objectName = objectNames.ElementAt(i);
            //                     utils.ValidateObjectName(objectName);
            //                     objectList.Add(new DeleteObject(objectName));
            //                     i++;
            //                     if (i % 1000 == 0)
            //                         break;
            //                 }

            //                 if (objectList.Count > 0)
            //                 {
            //                     var errorsList = await removeObjectsAsync(bucketName, objectList, cancellationToken)
            //                         .ConfigureAwait(false);
            //                     foreach (DeleteError error in errorsList)
            //                     {
            //                         obs.OnNext(error);
            //                     }
            //                 }

            //                 if (i >= objectNames.Count())
            //                 {
            //                     process = !process;
            //                 }
            //             }
            //         });
        }


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
        public async Task SetObjectTagsAsync(SetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);
        }


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
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        public async Task RemoveObjectTagsAsync(RemoveObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))
        // public async Task GetObjectAsync(string bucketName, string objectName, string fileName, // From RestSharp
        //     ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);
        }

        // ObjectStat objectStat =   // From RestSharp
        //     await StatObjectAsync(bucketName, objectName, sse: sse, cancellationToken: cancellationToken)
        //         .ConfigureAwait(false);
        // long length = objectStat.Size;
        // string etag = objectStat.ETag;

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
        /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        public async Task SetObjectRetentionAsync(SetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Get the Retention configuration for the object
        /// </summary>
        /// <param name="args">GetObjectRetentionArgs Arguments Object which has object identifier information - bucket name, object name, version ID</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns> Task </returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
        public async Task<ObjectRetentionConfiguration> GetObjectRetentionAsync(GetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
            var retentionResponse = new GetRetentionResponse(response.StatusCode, response.Content);
            return retentionResponse.CurrentRetentionConfiguration;
        }


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
        /// <exception cref="MissingObjectLockConfigurationException">When object lock configuration on bucket is not set</exception>
        /// <exception cref="NotImplementedException">When a functionality or extension is not implemented</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        public async Task ClearObjectRetentionAsync(ClearObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Upload object part to bucket for particular uploadId
        /// </summary>
        /// <param name="args">PutObjectArgs encapsulates bucket name, object name, upload id, part number, object data(body), Headers, SSE Headers</param>
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
        private async Task<string> PutObjectSinglePartAsync(PutObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            //Skipping validate as we need the case where stream sends 0 bytes
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken);
            PutObjectResponse putObjectResponse = new PutObjectResponse(response.StatusCode, response.Content, response.Headers);
            return putObjectResponse.Etag;
        }


        /// <summary>
        /// Upload object in multiple parts. Private Helper function
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
        private async Task<Dictionary<int, string>> PutObjectPartAsync(PutObjectPartArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            dynamic multiPartInfo = utils.CalculateMultiPartSize(args.ObjectSize);
            double partSize = multiPartInfo.partSize;
            double partCount = multiPartInfo.partCount;
            double lastPartSize = multiPartInfo.lastPartSize;
            Part[] totalParts = new Part[(int)partCount];

            double expectedReadSize = partSize;
            int partNumber;
            int numPartsUploaded = 0;
            Dictionary<int, string> etags = new Dictionary<int, string>();
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                byte[] dataToCopy = await ReadFullAsync(args.ObjectStreamData, (int)partSize).ConfigureAwait(false);
                if (dataToCopy == null && numPartsUploaded > 0)
                {
                    break;
                }
                if (partNumber == partCount)
                {
                    expectedReadSize = lastPartSize;
                }
                numPartsUploaded += 1;
                PutObjectArgs putObjectArgs = new PutObjectArgs(args)
                                                        .WithRequestBody(dataToCopy)
                                                        .WithUploadId(args.UploadId)
                                                        .WithPartNumber(partNumber);
                string etag = await this.PutObjectSinglePartAsync(putObjectArgs, cancellationToken).ConfigureAwait(false);
                totalParts[partNumber - 1] = new Part { PartNumber = partNumber, ETag = etag, Size = (long)expectedReadSize };
                etags[partNumber] = totalParts[partNumber - 1].ETag;
            }

            // This shouldn't happen where stream size is known.
            if (partCount != numPartsUploaded && args.ObjectSize != -1)
            {
                RemoveUploadArgs removeUploadArgs = new RemoveUploadArgs()
                                                                .WithBucket(args.BucketName)
                                                                .WithObject(args.ObjectName)
                                                                .WithUploadId(args.UploadId);
                await this.RemoveUploadAsync(removeUploadArgs, cancellationToken).ConfigureAwait(false);
                return null;
            }
            return etags;
        }

        // public async Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, // RestSharp changes
        //     string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null,
        //     CancellationToken cancellationToken = default(CancellationToken))
        // {
        //     utils.ValidateBucketName(bucketName);
        //     utils.ValidateObjectName(objectName);

        //     var sseHeaders = new Dictionary<string, string>();
        //     var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //     if (metaData != null)
        //     {
        //         foreach (KeyValuePair<string, string> p in metaData)
        //         {
        //             var key = p.Key;
        //             if (!supportedHeaders.Contains(p.Key, StringComparer.OrdinalIgnoreCase) &&
        //                 !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
        //             {
        //                 key = "x-amz-meta-" + key.ToLowerInvariant();
        //             }

        //             meta[key] = p.Value;

        //         }
        //     }

        //     if (sse != null)
        //     {
        //         sse.Marshal(sseHeaders);
        //     }

        //     if (string.IsNullOrWhiteSpace(contentType))
        //     {
        //         contentType = "application/octet-stream";
        //     }

        //     if (!meta.ContainsKey("Content-Type"))
        //     {
        //         meta["Content-Type"] = contentType;
        //     }

        //     if (data == null)
        //     {
        //         throw new ArgumentNullException(nameof(data), "Invalid input stream, cannot be null");
        //     }

        //     // for sizes less than 5Mb , put a single object
        //     if (size < Constants.MinimumPartSize && size >= 0)
        //     {
        //         var bytes = await ReadFullAsync(data, (int) size).ConfigureAwait(false);
        //         if (bytes != null && bytes.Length != (int) size)
        //         {
        //             throw new UnexpectedShortReadException(
        //                 $"Data read {bytes.Length} is shorter than the size {size} of input buffer.");
        //         }

        //         await this.PutObjectAsync(bucketName, objectName, null, 0, bytes, meta, sseHeaders, cancellationToken)
        //             .ConfigureAwait(false);
        //         return;
        //     }
        //     // For all sizes greater than 5MiB do multipart.

        //     dynamic multiPartInfo = utils.CalculateMultiPartSize(size);
        //     double partSize = multiPartInfo.partSize;
        //     double partCount = multiPartInfo.partCount;
        //     double lastPartSize = multiPartInfo.lastPartSize;
        //     Part[] totalParts = new Part[(int) partCount];

        //     string uploadId = await this
        //         .NewMultipartUploadAsync(bucketName, objectName, meta, sseHeaders, cancellationToken)
        //         .ConfigureAwait(false);

        //     // Remove SSE-S3 and KMS headers during PutObjectPart operations.
        //     if (sse != null &&
        //         (sse.GetType().Equals(EncryptionType.SSE_S3) ||
        //          sse.GetType().Equals(EncryptionType.SSE_KMS)))
        //     {
        //         sseHeaders.Remove(Constants.SSEGenericHeader);
        //         sseHeaders.Remove(Constants.SSEKMSContext);
        //         sseHeaders.Remove(Constants.SSEKMSKeyId);
        //     }

        //     double expectedReadSize = partSize;
        //     int partNumber;
        //     int numPartsUploaded = 0;
        //     for (partNumber = 1; partNumber <= partCount; partNumber++)
        //     {
        //         byte[] dataToCopy = await ReadFullAsync(data, (int) partSize).ConfigureAwait(false);
        //         if (dataToCopy == null && numPartsUploaded > 0)
        //         {
        //             break;
        //         }

        //         if (partNumber == partCount)
        //         {
        //             expectedReadSize = lastPartSize;
        //         }

        //         numPartsUploaded += 1;
        //         string etag = await this.PutObjectAsync(bucketName, objectName, uploadId, partNumber, dataToCopy, meta,
        //             sseHeaders, cancellationToken).ConfigureAwait(false);
        //         totalParts[partNumber - 1] = new Part
        //             {PartNumber = partNumber, ETag = etag, Size = (long) expectedReadSize};
        //     }

        //     // This shouldn't happen where stream size is known.
        //     if (partCount != numPartsUploaded && size != -1)
        //     {
        //         await this.RemoveUploadAsync(bucketName, objectName, uploadId, cancellationToken).ConfigureAwait(false);
        //         return;
        //     }

        //     Dictionary<int, string> etags = new Dictionary<int, string>();
        //     for (partNumber = 1; partNumber <= numPartsUploaded; partNumber++)
        //     {
        //         etags[partNumber] = totalParts[partNumber - 1].ETag;
        //     }

        //     await this.CompleteMultipartUploadAsync(bucketName, objectName, uploadId, etags, cancellationToken)
        //         .ConfigureAwait(false);


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
        public async Task PutObjectAsync(PutObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (args.Headers != null)
            {
                foreach (KeyValuePair<string, string> p in args.Headers)
                {
                    var key = p.Key;
                    if (!OperationsUtil.IsSupportedHeader(p.Key) && !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase) &&
                        !OperationsUtil.IsSSEHeader(p.Key))
                    {
                        key = "x-amz-meta-" + key.ToLowerInvariant();
                    }
                    meta[key] = p.Value;
                }
            }
            if (args.SSE != null)
            {
                args.SSE.Marshal(meta);
            }
            args.WithHeaders(meta);

            // Upload object in single part if size falls under restricted part size.
            if (args.ObjectSize < Constants.MinimumPartSize && args.ObjectSize >= 0 && args.ObjectStreamData != null)
            {
                var bytes = await ReadFullAsync(args.ObjectStreamData, (int)args.ObjectSize).ConfigureAwait(false);
                int bytesRead = (bytes == null) ? 0 : bytes.Length;
                if (bytesRead != (int)args.ObjectSize)
                {
                    throw new UnexpectedShortReadException($"Data read {bytes.Length} is shorter than the size {args.ObjectSize} of input buffer.");
                }
                args = args.WithRequestBody(bytes)
                           .WithStreamData(null)
                           .WithObjectSize(bytesRead);
                await this.PutObjectSinglePartAsync(args, cancellationToken).ConfigureAwait(false);
                return;
            }
            // For all sizes greater than 5MiB do multipart.
            NewMultipartUploadPutArgs multipartUploadArgs = new NewMultipartUploadPutArgs()
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
            string uploadId = await this.NewMultipartUploadAsync(multipartUploadArgs, cancellationToken).ConfigureAwait(false);
            // Remove SSE-S3 and KMS headers during PutObjectPart operations.
            PutObjectPartArgs putObjectPartArgs = new PutObjectPartArgs()
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
                FileInfo fileInfo = new FileInfo(args.FileName);
                long size = fileInfo.Length;
                using (FileStream fileStream = new FileStream(args.FileName, FileMode.Open, FileAccess.Read))
                {
                    putObjectPartArgs = putObjectPartArgs
                                                .WithStreamData(fileStream)
                                                .WithObjectSize(fileStream.Length)
                                                .WithRequestBody(null);
                    etags = await this.PutObjectPartAsync(putObjectPartArgs, cancellationToken).ConfigureAwait(false);
                }
            }
            // Upload stream contents
            else
            {
                etags = await this.PutObjectPartAsync(putObjectPartArgs, cancellationToken).ConfigureAwait(false);
            }
            CompleteMultipartUploadArgs completeMultipartUploadArgs = new CompleteMultipartUploadArgs()
                                                                                        .WithBucket(args.BucketName)
                                                                                        .WithObject(args.ObjectName)
                                                                                        .WithUploadId(uploadId)
                                                                                        .WithETags(etags);
            await this.CompleteMultipartUploadAsync(completeMultipartUploadArgs, cancellationToken).ConfigureAwait(false);
        }


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
        public async Task CopyObjectAsync(CopyObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            ServerSideEncryption sseGet = null;
            if (args.SourceObject.SSE is SSECopy sSECopy)
            {
                sseGet = sSECopy.CloneToSSEC();
            }
            StatObjectArgs statArgs = new StatObjectArgs()
                                            .WithBucket(args.SourceObject.BucketName)
                                            .WithObject(args.SourceObject.ObjectName)
                                            .WithVersionId(args.SourceObject.VersionId)
                                            .WithServerSideEncryption(sseGet);
            ObjectStat stat = await this.StatObjectAsync(statArgs, cancellationToken: cancellationToken).ConfigureAwait(false);
            args.WithCopyObjectSourceStats(stat);
            if (stat.TaggingCount > 0 && !args.ReplaceTagsDirective)
            {
                GetObjectTagsArgs getTagArgs = new GetObjectTagsArgs()
                                                            .WithBucket(args.SourceObject.BucketName)
                                                            .WithObject(args.SourceObject.ObjectName)
                                                            .WithVersionId(args.SourceObject.VersionId)
                                                            .WithServerSideEncryption(sseGet);
                var tag = await GetObjectTagsAsync(getTagArgs, cancellationToken).ConfigureAwait(false);
                args.WithTagging(tag);
            }
            args.Validate();
            long srcByteRangeSize = (args.SourceObject.CopyOperationConditions != null) ? args.SourceObject.CopyOperationConditions.GetByteRange() : 0L;
            long copySize = (srcByteRangeSize == 0) ? args.SourceObjectInfo.Size : srcByteRangeSize;
            if ((srcByteRangeSize > args.SourceObjectInfo.Size) || ((srcByteRangeSize > 0) && (args.SourceObject.CopyOperationConditions.byteRangeEnd >= args.SourceObjectInfo.Size)))
            {
                throw new ArgumentException("Specified byte range (" + args.SourceObject.CopyOperationConditions.byteRangeStart.ToString() + "-" + args.SourceObject.CopyOperationConditions.byteRangeEnd.ToString() + ") does not fit within source object (size=" + args.SourceObjectInfo.Size.ToString() + ")");
            }

            if ((copySize > Constants.MaxSingleCopyObjectSize) || (srcByteRangeSize > 0 && (srcByteRangeSize != args.SourceObjectInfo.Size)))
            {
                MultipartCopyUploadArgs multiArgs = new MultipartCopyUploadArgs(args)
                                                                .WithCopySize(copySize);
                await MultipartCopyUploadAsync(multiArgs, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                CopySourceObjectArgs sourceObject = new CopySourceObjectArgs()
                                                                .WithBucket(args.SourceObject.BucketName)
                                                                .WithObject(args.SourceObject.ObjectName)
                                                                .WithVersionId(args.SourceObject.VersionId)
                                                                .WithCopyConditions(args.SourceObject.CopyOperationConditions);

                CopyObjectRequestArgs cpReqArgs = new CopyObjectRequestArgs()
                                                                .WithBucket(args.BucketName)
                                                                .WithObject(args.ObjectName)
                                                                .WithVersionId(args.VersionId)
                                                                .WithHeaders(args.Headers)
                                                                .WithCopyObjectSource(sourceObject)
                                                                .WithRequestBody(args.RequestBody)
                                                                .WithSourceObjectInfo(args.SourceObjectInfo)
                                                                .WithCopyOperationObjectType(typeof(CopyObjectResult))
                                                                .WithReplaceMetadataDirective(args.ReplaceMetadataDirective)
                                                                .WithReplaceTagsDirective(args.ReplaceTagsDirective)
                                                                .WithTagging(args.ObjectTags);
                cpReqArgs.Validate();
                Dictionary<string, string> newMeta = null;
                if (args.ReplaceMetadataDirective)
                {
                    newMeta = new Dictionary<string, string>(args.Headers);
                }
                else
                {
                    newMeta = new Dictionary<string, string>(args.SourceObjectInfo.MetaData);
                }
                if (args.SourceObject.SSE != null && args.SourceObject.SSE is SSECopy)
                {
                    args.SourceObject.SSE.Marshal(newMeta);
                    // throw new ArgumentException("'" + fileName + "': object size " + length +
                    //                             " is smaller than file size "
                    //                             + fileSize, nameof(fileSize));
                }
                if (args.SSE != null)
                {
                    args.SSE.Marshal(newMeta);
                }
                cpReqArgs.WithHeaders(newMeta);
                await this.CopyObjectRequestAsync(cpReqArgs, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Make a multi part copy upload for objects larger than 5GB or if CopyCondition specifies a byte range.
        /// </summary>
        /// <param name="args">MultipartCopyUploadArgs Arguments object encapsulating destination and source bucket, object names, copy conditions, size, metadata, SSE</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
        private async Task MultipartCopyUploadAsync(MultipartCopyUploadArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            dynamic multiPartInfo = utils.CalculateMultiPartSize(args.CopySize, copy: true);
            double partSize = multiPartInfo.partSize;
            double partCount = multiPartInfo.partCount;
            double lastPartSize = multiPartInfo.lastPartSize;
            Part[] totalParts = new Part[(int)partCount];

            NewMultipartUploadCopyArgs nmuArgs = new NewMultipartUploadCopyArgs()
                                                            .WithBucket(args.BucketName)
                                                            .WithObject(args.ObjectName ?? args.SourceObject.ObjectName)
                                                            .WithHeaders(args.Headers)
                                                            .WithCopyObjectSource(args.SourceObject)
                                                            .WithSourceObjectInfo(args.SourceObjectInfo)
                                                            .WithReplaceMetadataDirective(args.ReplaceMetadataDirective)
                                                            .WithReplaceTagsDirective(args.ReplaceTagsDirective);
            nmuArgs.Validate();
            // No need to resume upload since this is a Server-side copy. Just initiate a new upload.
            string uploadId = await this.NewMultipartUploadAsync(nmuArgs, cancellationToken).ConfigureAwait(false);
            double expectedReadSize = partSize;
            int partNumber;
            for (partNumber = 1; partNumber <= partCount; partNumber++)

            // await GetObjectAsync(bucketName, objectName, (stream) => // From RestSharp
            {
                CopyConditions partCondition = args.SourceObject.CopyOperationConditions.Clone();
                partCondition.byteRangeStart = (long)partSize * (partNumber - 1) + partCondition.byteRangeStart;
                if (partNumber < partCount)
                {
                    partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)partSize - 1;
                }
                else
                {
                    partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)lastPartSize - 1;
                }
                Dictionary<string, string> queryMap = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
                {
                    queryMap.Add("uploadId", uploadId);
                    queryMap.Add("partNumber", partNumber.ToString());
                }
                if (args.SourceObject.SSE != null && args.SourceObject.SSE is SSECopy)
                {
                    args.SourceObject.SSE.Marshal(args.Headers);
                }
                if (args.SSE != null)
                {
                    args.SSE.Marshal(args.Headers);
                }
                CopyObjectRequestArgs cpPartArgs = new CopyObjectRequestArgs()
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
                CopyPartResult cpPartResult = (CopyPartResult)await this.CopyObjectRequestAsync(cpPartArgs, cancellationToken).ConfigureAwait(false);

                totalParts[partNumber - 1] = new Part { PartNumber = partNumber, ETag = cpPartResult.ETag, Size = (long)expectedReadSize };
            }
            Dictionary<int, string> etags = new Dictionary<int, string>();
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                etags[partNumber] = totalParts[partNumber - 1].ETag;
            }
            CompleteMultipartUploadArgs completeMultipartUploadArgs = new CompleteMultipartUploadArgs(args)
                                                                                        .WithUploadId(uploadId)
                                                                                        .WithETags(etags);
            // Complete multi part upload
            await this.CompleteMultipartUploadAsync(completeMultipartUploadArgs, cancellationToken).ConfigureAwait(false);

        }


        /// <summary>
        /// Start a new multi-part upload request
        /// </summary>
        /// <param name="args">NewMultipartUploadPutArgs arguments object encapsulating bucket name, object name, Headers, SSE Headers</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
        private async Task<string> NewMultipartUploadAsync(NewMultipartUploadPutArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken);
            NewMultipartUploadResponse uploadResponse = new NewMultipartUploadResponse(response.StatusCode, response.Content);
            return uploadResponse.UploadId;
        }

        /// <summary>
        /// Start a new multi-part copy upload request
        /// </summary>
        /// <param name="args">NewMultipartUploadCopyArgs arguments object encapsulating bucket name, object name, Headers, SSE Headers</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="AccessDeniedException">For encrypted copy operation, Access is denied if the key is wrong</exception>
        private async Task<string> NewMultipartUploadAsync(NewMultipartUploadCopyArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken);
            NewMultipartUploadResponse uploadResponse = new NewMultipartUploadResponse(response.StatusCode, response.Content);
            return uploadResponse.UploadId;
        }


        /// <summary>
        /// Create the copy request, execute it and return the copy result.
        /// </summary>
        /// <param name="args"> CopyObjectRequestArgs Arguments Object encapsulating </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        private async Task<object> CopyObjectRequestAsync(CopyObjectRequestArgs args, CancellationToken cancellationToken)
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);
            CopyObjectResponse copyObjectResponse = new CopyObjectResponse(response.StatusCode, response.Content, args.CopyOperationObjectType);
            return copyObjectResponse.CopyPartRequestResult;
        }


        /// <summary>
        /// Internal method to complete multi part upload of object to server.
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
        private async Task CompleteMultipartUploadAsync(CompleteMultipartUploadArgs args, CancellationToken cancellationToken)
        {
            args.Validate();
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);
        }


        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="cb">A stream will be passed to the callback</param>
        /// <param name="sse">Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use GetObjectAsync method with GetObjectArgs object. Refer GetObject, GetObjectVersion & GetObjectQuery example code.")]
        public Task GetObjectAsync(string bucketName, string objectName, Action<Stream> cb, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            GetObjectArgs args = new GetObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithCallbackStream(cb)
                                            .WithServerSideEncryption(sse);
            return this.GetObjectAsync(args, cancellationToken);
        }


        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="offset"> Offset of the object from where stream will start</param>
        /// <param name="length">length of the object that will be read in the stream </param>
        /// <param name="cb">A stream will be passed to the callback</param>
        /// <param name="sse">Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use GetObjectAsync method with GetObjectArgs object. Refer GetObject, GetObjectVersion & GetObjectQuery example code.")]
        public Task GetObjectAsync(string bucketName, string objectName, long offset, long length, Action<Stream> cb, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            GetObjectArgs args = new GetObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithCallbackStream(cb)
                                            .WithOffsetAndLength(offset, length)
                                            .WithServerSideEncryption(sse);
            return this.GetObjectAsync(args);
        }

        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="fileName">string with file path</param>
        /// <param name="sse">Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use GetObjectAsync method with GetObjectArgs object. Refer GetObject, GetObjectVersion & GetObjectQuery example code.")]
        public Task GetObjectAsync(string bucketName, string objectName, string fileName, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            GetObjectArgs args = new GetObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithFile(fileName)
                                            .WithServerSideEncryption(sse);
            return this.GetObjectAsync(args, cancellationToken);
            //         throw new IOException(tempFileName + ": unexpected data written.  expected = " +  // RestSharp changes(7 lines)
            //                               (length - tempFileSize)
            //                               + ", written = " + writtenSize);
            //     }

            //     utils.MoveWithReplace(tempFileName, fileName);
            // }, sse, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Select an object's content. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="opts">Select Object options</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use SelectObjectContentAsync method with SelectObjectContentsArgs object. Refer SelectObjectContent example code.")]
        public Task<SelectResponseStream> SelectObjectContentAsync(string bucketName, string objectName, SelectObjectOptions opts, CancellationToken cancellationToken = default(CancellationToken))
        {
            SelectObjectContentArgs args = new SelectObjectContentArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithExpressionType(opts.ExpressionType)
                                                        .WithInputSerialization(opts.InputSerialization)
                                                        .WithOutputSerialization(opts.OutputSerialization)
                                                        .WithQueryExpression(opts.Expression)
                                                        .WithServerSideEncryption(opts.SSE)
                                                        .WithRequestProgress(opts.RequestProgress);
            return this.SelectObjectContentAsync(args, cancellationToken);
            // public async Task<SelectResponseStream> SelectObjectContentAsync(string bucketName, string objectName, // RestSharp changes
            //     SelectObjectOptions opts, CancellationToken cancellationToken = default(CancellationToken))
            // {
            //     utils.ValidateBucketName(bucketName);
            //     utils.ValidateObjectName(objectName);
            //     if (opts == null)
            //     {
            //         throw new ArgumentException("Options cannot be null", nameof(opts));
            //     }

            //     Dictionary<string, string> sseHeaders = null;
            //     if (opts.SSE != null)
            //     {
            //         sseHeaders = new Dictionary<string, string>();
            //         opts.SSE.Marshal(sseHeaders);
            //     }

            // Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, this.Region);
            //             objectName: objectName,
            //             headerMap: sseHeaders)
            //         .ConfigureAwait(false);
            //     request.Properties.Add("select", "");
            //     request.Properties.Add("select-type", "2");
            //     request.AddXmlBody(opts.MarshalXML());

            //     var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
            //         .ConfigureAwait(false);
            //     return new SelectResponseStream(new MemoryStream(response.ContentBytes));
        }

        /// <summary>
        /// Creates an object from file
        /// </summary>
        /// <param name="bucketName">Bucket to create object in</param>
        /// <param name="objectName">Key of the new object</param>
        /// <param name="fileName">Path of file to upload</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        /// <param name="metaData">Object metadata to be stored. Defaults to null.</param>
        /// <param name="sse">Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use PutObjectAsync method with PutObjectArgs object. Refer to PutObject example code.")]
        public Task PutObjectAsync(string bucketName, string objectName, string fileName, string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            PutObjectArgs args = new PutObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithFileName(fileName)
                                            .WithContentType(contentType)
                                            .WithHeaders(metaData)
                                            .WithServerSideEncryption(sse);
            return this.PutObjectAsync(args, cancellationToken);
            // public async Task PutObjectAsync(string bucketName, string objectName, string fileName, // RestSharp changes
            //     string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null,
            //     CancellationToken cancellationToken = default(CancellationToken))
            // {
            //     utils.ValidateFile(fileName, contentType);
            //     FileInfo fileInfo = new FileInfo(fileName);
            //     long size = fileInfo.Length;
            //     using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            //     {
            //         await PutObjectAsync(bucketName, objectName, file, size, contentType, metaData, sse, cancellationToken)
            //             .ConfigureAwait(false);
            //     }
        }

        /// <summary>
        /// Creates an object from inputstream
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
        public Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            PutObjectArgs args = new PutObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithStreamData(data)
                                            .WithObjectSize(size)
                                            .WithContentType(contentType)
                                            .WithHeaders(metaData)
                                            .WithServerSideEncryption(sse);
            return this.PutObjectAsync(args, cancellationToken);
            // public async Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, // RestSharp changes
            //     string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null,
            //     CancellationToken cancellationToken = default(CancellationToken))
            // {
            //     utils.ValidateBucketName(bucketName);
            //     utils.ValidateObjectName(objectName);

            //     var sseHeaders = new Dictionary<string, string>();
            //     var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            //     if (metaData != null)
            //     {
            //         foreach (KeyValuePair<string, string> p in metaData)
            //         {
            //             var key = p.Key;
            //             if (!supportedHeaders.Contains(p.Key, StringComparer.OrdinalIgnoreCase) &&
            //                 !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
            //             {
            //                 key = "x-amz-meta-" + key.ToLowerInvariant();
            //             }

            //             meta[key] = p.Value;

            //         }
            //     }

            //     if (sse != null)
            //     {
            //         sse.Marshal(sseHeaders);
            //     }

            //     if (string.IsNullOrWhiteSpace(contentType))
            //     {
            //         contentType = "application/octet-stream";
            //     }

            //     if (!meta.ContainsKey("Content-Type"))
            //     {
            //         meta["Content-Type"] = contentType;
            //     }

            //     if (data == null)
            //     {
            //         throw new ArgumentNullException(nameof(data), "Invalid input stream, cannot be null");
            //     }

            //     // for sizes less than 5Mb , put a single object
            //     if (size < Constants.MinimumPartSize && size >= 0)
            //     {
            //         var bytes = await ReadFullAsync(data, (int) size).ConfigureAwait(false);
            //         if (bytes != null && bytes.Length != (int) size)
            //         {
            //             throw new UnexpectedShortReadException(
            //                 $"Data read {bytes.Length} is shorter than the size {size} of input buffer.");
            //         }

            //         await this.PutObjectAsync(bucketName, objectName, null, 0, bytes, meta, sseHeaders, cancellationToken)
            //             .ConfigureAwait(false);
            //         return;
            //     }
            //     // For all sizes greater than 5MiB do multipart.

            //     dynamic multiPartInfo = utils.CalculateMultiPartSize(size);
            //     double partSize = multiPartInfo.partSize;
            //     double partCount = multiPartInfo.partCount;
            //     double lastPartSize = multiPartInfo.lastPartSize;
            //     Part[] totalParts = new Part[(int) partCount];

            //     string uploadId = await this
            //         .NewMultipartUploadAsync(bucketName, objectName, meta, sseHeaders, cancellationToken)
            //         .ConfigureAwait(false);

            //     // Remove SSE-S3 and KMS headers during PutObjectPart operations.
            //     if (sse != null &&
            //         (sse.GetType().Equals(EncryptionType.SSE_S3) ||
            //          sse.GetType().Equals(EncryptionType.SSE_KMS)))
            //     {
            //         sseHeaders.Remove(Constants.SSEGenericHeader);
            //         sseHeaders.Remove(Constants.SSEKMSContext);
            //         sseHeaders.Remove(Constants.SSEKMSKeyId);
            //     }

            //     double expectedReadSize = partSize;
            //     int partNumber;
            //     int numPartsUploaded = 0;
            //     for (partNumber = 1; partNumber <= partCount; partNumber++)
            //     {
            //         byte[] dataToCopy = await ReadFullAsync(data, (int) partSize).ConfigureAwait(false);
            //         if (dataToCopy == null && numPartsUploaded > 0)
            //         {
            //             break;
            //         }

            //         if (partNumber == partCount)
            //         {
            //             expectedReadSize = lastPartSize;
            //         }

            //         numPartsUploaded += 1;
            //         string etag = await this.PutObjectAsync(bucketName, objectName, uploadId, partNumber, dataToCopy, meta,
            //             sseHeaders, cancellationToken).ConfigureAwait(false);
            //         totalParts[partNumber - 1] = new Part
            //             {PartNumber = partNumber, ETag = etag, Size = (long) expectedReadSize};
            //     }

            //     // This shouldn't happen where stream size is known.
            //     if (partCount != numPartsUploaded && size != -1)
            //     {
            //         await this.RemoveUploadAsync(bucketName, objectName, uploadId, cancellationToken).ConfigureAwait(false);
            //         return;
            //     }

            //     Dictionary<int, string> etags = new Dictionary<int, string>();
            //     for (partNumber = 1; partNumber <= numPartsUploaded; partNumber++)
            //     {
            //         etags[partNumber] = totalParts[partNumber - 1].ETag;
            //     }

            //     await this.CompleteMultipartUploadAsync(bucketName, objectName, uploadId, etags, cancellationToken)
            //         .ConfigureAwait(false);
        }

        /// <summary>
        /// Internal method to complete multi part upload of object to server.
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
            var requestMessageBuilder = await this.CreateRequest(HttpMethod.Post, bucketName,
                    objectName: objectName)
                .ConfigureAwait(false);
            requestMessageBuilder.AddQueryParameter("uploadId", $"{uploadId}");

            List<XElement> parts = new List<XElement>();

            for (int i = 1; i <= etags.Count; i++)
            {
                parts.Add(new XElement("Part",
                    new XElement("PartNumber", i),
                    new XElement("ETag", etags[i])));
            }

            var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);
            var bodyString = completeMultipartUploadXml.ToString();

            // request.Headers.Add("application/xml", body);   // Current change

            requestMessageBuilder.AddXmlBody(bodyString);
            await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an async observable of parts corresponding to a uploadId for a specific bucket and objectName
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
                    int nextPartNumberMarker = 0;
                    bool isRunning = true;
                    while (isRunning)
                    {
                        var uploads = await this
                            .GetListPartsAsync(bucketName, objectName, uploadId, nextPartNumberMarker,
                                cancellationToken).ConfigureAwait(false);
                        foreach (Part part in uploads.Item2)
                        {
                            obs.OnNext(part);
                        }

                        nextPartNumberMarker = uploads.Item1.NextPartNumberMarker;
                        isRunning = uploads.Item1.IsTruncated;
                    }
                });
        }

        /// <summary>
        /// Gets the list of parts corresponding to a uploadId for given bucket and object
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
            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(HttpMethod.Get, bucketName,
                    objectName: objectName)
                .ConfigureAwait(false);
            requestMessageBuilder.AddQueryParameter("uploadId", $"{uploadId}");
            if (partNumberMarker > 0)
            {
                requestMessageBuilder.AddQueryParameter("part-number-marker", $"{partNumberMarker}");
            }

            requestMessageBuilder.AddQueryParameter("max-parts", "1000");

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);

            var contentBytes = response.ContentBytes;
            ListPartsResult listPartsResult = null;
            using (var stream = new MemoryStream(contentBytes))
            {
                listPartsResult = (ListPartsResult)new XmlSerializer(typeof(ListPartsResult)).Deserialize(stream);
            }

            XDocument root = XDocument.Parse(response.Content);

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
        /// Start a new multi-part upload request
        /// </summary>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectName">Object Name</param>
        /// <param name="metaData"></param>
        /// <param name="sseHeaders"> Server-side encryption options</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<string> NewMultipartUploadAsync(string bucketName, string objectName,
            Dictionary<string, string> metaData, Dictionary<string, string> sseHeaders,
            CancellationToken cancellationToken = default(CancellationToken))
        {

            foreach (KeyValuePair<string, string> kv in sseHeaders)
            {
                metaData.Add(kv.Key, kv.Value);
            }

            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(HttpMethod.Post, bucketName,
                headerMap: metaData).ConfigureAwait(false);
            requestMessageBuilder.AddQueryParameter("uploads", "");

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);

            using (var contentStream = new MemoryStream(response.ContentBytes))
            {
                InitiateMultipartUploadResult newUpload = null;
                newUpload = (InitiateMultipartUploadResult)new XmlSerializer(typeof(InitiateMultipartUploadResult))
                    .Deserialize(contentStream);
                return newUpload.UploadId;
            }
        }

        /// <summary>
        /// Upload object part to bucket for particular uploadId
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
            string contentType = metaData["Content-Type"];
            if (uploadId != null)
            {
                metaData = new Dictionary<string, string>();
            }

            foreach (KeyValuePair<string, string> kv in sseHeaders)
            {
                metaData.Add(kv.Key, kv.Value);
            }

            HttpRequestMessageBuilder requestMessageBuilder = await this.CreateRequest(HttpMethod.Put, bucketName,
                    objectName: objectName,
                    contentType: contentType,
                    headerMap: metaData,
                    body: data)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                requestMessageBuilder.AddQueryParameter("uploadId", $"{uploadId}");
                requestMessageBuilder.AddQueryParameter("partNumber", $"{partNumber}");
            }

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);

            // string etag = response.Headers   // Current change
            //                         .Where(param => (param.Name.ToLower().Equals("etag")))
            //                         .Select(param => param.Value.ToString())
            //                         .FirstOrDefault()
            //                         .ToString();

            string etag = null;
            foreach (var parameter in response.Headers)
            {
                if (parameter.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase))
                {
                    etag = parameter.Value.ToString();
                }
            }

            return etag;
        }

        // /// <summary>
        // /// Get list of multi-part uploads matching particular uploadIdMarker     // RestSharp changes
        // /// </summary>
        // /// <param name="bucketName">Bucket Name</param>
        // /// <param name="prefix">prefix</param>
        // /// <param name="keyMarker"></param>
        // /// <param name="uploadIdMarker"></param>
        // /// <param name="delimiter"></param>
        // /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        // /// <returns></returns>
        // private async Task<Tuple<ListMultipartUploadsResult, List<Upload>>> GetMultipartUploadsListAsync(
        //     string bucketName,
        //     string prefix,
        //     string keyMarker,
        //     string uploadIdMarker,
        //     string delimiter,
        //     CancellationToken cancellationToken)
        // {
        //     // null values are treated as empty strings.
        //     if (delimiter == null)
        //     {
        //         delimiter = string.Empty;
        //     }

        //     if (prefix == null)
        //     {
        //         prefix = string.Empty;
        //     }

        //     if (keyMarker == null)
        //     {
        //         keyMarker = string.Empty;
        //     }

        //     if (uploadIdMarker == null)
        //     {
        //         uploadIdMarker = string.Empty;
        //     }

        //     request.Properties.Add("uploads", "");
        //     request.Properties.Add("prefix", prefix);
        //     request.Properties.Add("delimiter", delimiter);
        //     request.Properties.Add("key-marker", keyMarker);
        //     request.Properties.Add("upload-id-marker", uploadIdMarker);
        //     request.Properties.Add("max-uploads", "1000");
        //     var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
        //         .ConfigureAwait(false);

        //     ListMultipartUploadsResult listBucketResult = null;
        //     using (var stream = new MemoryStream(response.ContentBytes))
        //     {
        //         listBucketResult =
        //             (ListMultipartUploadsResult) new XmlSerializer(typeof(ListMultipartUploadsResult)).Deserialize(
        //                 stream);
        //     }

        //     XDocument root = XDocument.Parse(response.Content);

        //     var uploads = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Upload")
        //         select new Upload
        //         {
        //             Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
        //             UploadId = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}UploadId").Value,
        //             Initiated = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Initiated").Value
        //         };

        //     return Tuple.Create(listBucketResult, uploads.ToList());
        // }

        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix recursively
        /// </summary>
        /// <param name="bucketName">Bucket to list all incomplete uploads from</param>
        /// <param name="prefix">Filter all incomplete uploads starting with this prefix</param>
        /// <param name="recursive">Set to true to recursively list all incomplete uploads</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        [Obsolete("Use ListIncompleteUploads method with ListIncompleteUploadsArgs object. Refer ListIncompleteUploads example code.")]
        public IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix = null, bool recursive = true, CancellationToken cancellationToken = default(CancellationToken))
        // public IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix = null, bool recursive = true,    // RestSharp change
        //     CancellationToken cancellationToken = default(CancellationToken))
        {
            ListIncompleteUploadsArgs args = new ListIncompleteUploadsArgs()
                                                            .WithBucket(bucketName)
                                                            .WithPrefix(prefix)
                                                            .WithDelimiter("/");
            if (recursive)
            {
                args = args.WithDelimiter(null);
                return this.ListIncompleteUploads(args, cancellationToken);
            }
            return this.ListIncompleteUploads(args, cancellationToken);

            //     return this.listIncompleteUploads(bucketName, prefix, "/", cancellationToken);    // RestSharp change
            // }

            // /// <summary>
            // /// Lists all or delimited incomplete uploads in a given bucket with a given objectName
            // /// </summary>
            // /// <param name="bucketName">Bucket to list incomplete uploads from</param>
            // /// <param name="prefix">Key of object to list incomplete uploads from</param>
            // /// <param name="delimiter">delimiter of object to list incomplete uploads</param>
            // /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
            // /// <returns>Observable that notifies when next next upload becomes available</returns>
            // private IObservable<Upload> listIncompleteUploads(string bucketName, string prefix, string delimiter,
            //     CancellationToken cancellationToken)
            // {
            //     return Observable.Create<Upload>(
            //         async obs =>
            //         {
            //             string nextKeyMarker = null;
            //             string nextUploadIdMarker = null;
            //             bool isRunning = true;

            //             while (isRunning)
            //             {
            //                 var uploads = await this.GetMultipartUploadsListAsync(bucketName, prefix, nextKeyMarker,
            //                     nextUploadIdMarker, delimiter, cancellationToken).ConfigureAwait(false);
            //                 foreach (Upload upload in uploads.Item2)
            //                 {
            //                     obs.OnNext(upload);
            //                 }

            //                 nextKeyMarker = uploads.Item1.NextKeyMarker;
            //                 nextUploadIdMarker = uploads.Item1.NextUploadIdMarker;
            //                 isRunning = uploads.Item1.IsTruncated;
            //             }
            //         });
        }

        /// <summary>
        /// Remove incomplete uploads from a given bucket and objectName
        /// </summary>
        /// <param name="bucketName">Bucket to remove incomplete uploads from</param>
        /// <param name="objectName">Key to remove incomplete uploads from</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use RemoveIncompleteUploadAsync method with RemoveIncompleteUploadArgs object. Refer RemoveIncompleteUpload example code.")]
        public Task RemoveIncompleteUploadAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))
        {
            RemoveIncompleteUploadArgs args = new RemoveIncompleteUploadArgs()
                                                                .WithBucket(bucketName)
                                                                .WithObject(objectName);
            return this.RemoveIncompleteUploadAsync(args);
            // public async Task RemoveIncompleteUploadAsync(string bucketName, string objectName,   //RestSharp
            //     CancellationToken cancellationToken = default(CancellationToken))
            // {
            //     var uploads = await this.ListIncompleteUploads(bucketName, objectName, cancellationToken: cancellationToken)
            //         .ToArray();
            //     foreach (Upload upload in uploads)
            //     {
            //         if (objectName == upload.Key)
            //         {
            //             await this.RemoveUploadAsync(bucketName, objectName, upload.UploadId, cancellationToken)
            //                 .ConfigureAwait(false);
            //         }
            //     }
        }

        /// <summary>
        /// Remove object with matching uploadId from bucket
        /// </summary>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectName"></param>
        /// <param name="uploadId"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use RemoveUploadAsync method with RemoveUploadArgs object.")]
        private Task RemoveUploadAsync(string bucketName, string objectName, string uploadId, CancellationToken cancellationToken)
        {
            RemoveUploadArgs args = new RemoveUploadArgs()
                                                .WithBucket(bucketName)
                                                .WithObject(objectName)
                                                .WithUploadId(uploadId);
            return this.RemoveUploadAsync(args, cancellationToken);
        }

        /// <summary>
        /// Removes an object with given name in specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to remove object from</param>
        /// <param name="objectName">Key of object to remove</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use RemoveObjectAsync method with RemoveObjectArgs object. Refer RemoveObject example code.")]
        public Task RemoveObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var args = new RemoveObjectArgs()
                                    .WithBucket(bucketName)
                                    .WithObject(objectName);
            return this.RemoveObjectAsync(args, cancellationToken);
            // public async Task RemoveObjectAsync(string bucketName, string objectName,    // RestSharp change
            //     CancellationToken cancellationToken = default(CancellationToken))
            // {
            // Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, this.Region);
            //         .ConfigureAwait(false);

            //     var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
            //         .ConfigureAwait(false);
        }

        // /// <summary>
        // /// private helper method to remove list of objects from bucket
        // /// </summary>
        // /// <param name="bucketName">Bucket Name</param>
        // /// <param name="objectsList"></param>
        // /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        // /// <returns></returns>
        // [Obsolete("Use RemoveObjectsAsync method with RemoveObjectsArgs object. Refer RemoveObjects example code.")]
        // private async Task<List<DeleteError>> removeObjectsAsync(string bucketName, List<DeleteObject> objectsList, CancellationToken cancellationToken)
        // {
        //     request.Properties.Add("delete", "");
        //     List<XElement> objects = new List<XElement>();

        //     foreach (var obj in objectsList)
        //     {
        //         objects.Add(new XElement("Object",
        //             new XElement("Key", obj.Key)));
        //     }

        //     var deleteObjectsRequest = new XElement("Delete", objects,
        //         new XElement("Quiet", true));

        //     request.AddXmlBody(deleteObjectsRequest.ToString());
        //     var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
        //             .ConfigureAwait(false);
        //     // var contentBytes = Encoding.UTF8.GetBytes(response.Content);
        //     // using (var stream = new MemoryStream(contentBytes))
        //     // using (var contentStream = new MemoryStream(response.ContentBytes))
        //     {
        //         var deleteResult =
        //             (DeleteObjectsResult) new XmlSerializer(typeof(DeleteObjectsResult)).Deserialize(response.ContentStream);

        //         if (deleteResult == null)
        //         {
        //             return new List<DeleteError>();
        //         }

        //         return deleteResult.ErrorList();
        //     }
        // }

        /// <summary>
        /// Removes multiple objects from a specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to remove objects from</param>
        /// <param name="objectNames">List of object keys to remove.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use RemoveObjectsAsync method with RemoveObjectsArgs object. Refer RemoveObjects example code.")]
        public Task<IObservable<DeleteError>> RemoveObjectAsync(string bucketName, IEnumerable<string> objectNames, CancellationToken cancellationToken = default(CancellationToken))
        {
            RemoveObjectsArgs args = new RemoveObjectsArgs()
                                                .WithBucket(bucketName)
                                                .WithObjects(new List<string>(objectNames));
            return this.RemoveObjectsAsync(args);
        }

        /// <summary>
        /// Tests the object's existence and returns metadata about existing objects.
        /// </summary>
        /// <param name="bucketName">Bucket to test object in</param>
        /// <param name="objectName">Name of the object to stat</param>
        /// <param name="sse"> Server-side encryption option.Defaults to null</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Facts about the object</returns>
        [Obsolete("Use StatObjectAsync method with StatObjectArgs object. Refer StatObject & StatObjectQuery example code.")]
        public Task<ObjectStat> StatObjectAsync(string bucketName, string objectName,
            ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            StatObjectArgs args = new StatObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithServerSideEncryption(sse);
            return this.StatObjectAsync(args, cancellationToken: cancellationToken);
            // public async Task<ObjectStat> StatObjectAsync(string bucketName, string objectName,   //Rest Sharp
            //     ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
            // {
            //     var headerMap = new Dictionary<string, string>();

            //     if (sse != null && sse.GetType().Equals(EncryptionType.SSE_C))
            //     {
            //         sse.Marshal(headerMap);
            //     }

            //     var request = await this
            //         .CreateRequest(HttpMethod.Head, bucketName, objectName: objectName, headerMap: headerMap)
            //         .ConfigureAwait(false);

            //     var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
            //         .ConfigureAwait(false);

            //     // Extract stats from response
            //     long size = 0;
            //     DateTime lastModified = new DateTime();
            //     string etag = string.Empty;
            //     string contentType = null;
            //     var metaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            //     foreach (var parameter in response.Headers)
            //     {
            //         if (parameter.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            //         {
            //             size = long.Parse(parameter.Value.ToString());
            //         }
            //         else if (parameter.Key.Equals("Last-Modified", StringComparison.OrdinalIgnoreCase))
            //         {
            //             lastModified = DateTime.Parse(parameter.Value.ToString(), CultureInfo.InvariantCulture);
            //         }
            //         else if (parameter.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase))
            //         {
            //             etag = parameter.Value.ToString().Replace("\"", string.Empty);
            //         }
            //         else if (parameter.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            //         {
            //             contentType = parameter.Value.ToString();
            //             metaData["Content-Type"] = contentType;
            //         }
            //         else if (supportedHeaders.Contains(parameter.Key, StringComparer.OrdinalIgnoreCase))
            //         {
            //             metaData[parameter.Key] = parameter.Value.ToString();
            //         }
            //         else if (parameter.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
            //         {
            //             metaData[parameter.Key.Substring("x-amz-meta-".Length)] = parameter.Value.ToString();
            //         }
            //     }

            //     return new ObjectStat(objectName, size, lastModified, etag, contentType, metaData);
        }

        /// <summary>
        /// Advances in the stream upto currentPartSize or End of Stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="currentPartSize"></param>
        /// <returns>bytes read in a byte array</returns>
        internal async Task<byte[]> ReadFullAsync(Stream data, int currentPartSize)
        {
            byte[] result = new byte[currentPartSize];
            int totalRead = 0;
            while (totalRead < currentPartSize)
            {
                byte[] curData = new byte[currentPartSize - totalRead];
                int curRead = await data.ReadAsync(curData, 0, currentPartSize - totalRead).ConfigureAwait(false);
                if (curRead == 0)
                {
                    break;
                }

                for (int i = 0; i < curRead; i++)
                {
                    result[totalRead + i] = curData[i];
                }

                totalRead += curRead;
            }

            if (totalRead == 0)
            {
                return null;
            }

            if (totalRead == currentPartSize)
            {
                return result;
            }

            byte[] truncatedResult = new byte[totalRead];
            for (int i = 0; i < totalRead; i++)
            {
                truncatedResult[i] = result[i];
            }

            return truncatedResult;
        }

        /// <summary>
        /// Copy a source object into a new destination object.
        /// </summary>
        /// <param name="bucketName">Bucket name where the object to be copied exists.</param>
        /// <param name="objectName">Object name source to be copied.</param>
        /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
        /// <param name="destObjectName">Object name to be created, if not provided uses source object name as destination object name.</param>
        /// <param name="copyConditions">Optionally can take a key value CopyConditions as well for conditionally attempting copyObject.</param>
        /// <param name="metadata">Optional Object metadata to be stored. Defaults to null.</param>
        /// <param name="sseSrc">Optional source encryption options.Defaults to null. </param>
        /// <param name="sseDest">Optional destination encryption options.Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        [Obsolete("Use CopyObjectAsync method with CopyObjectArgs object. Refer CopyObject example code.")]
        public Task CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            CopySourceObjectArgs cpSrcArgs = new CopySourceObjectArgs()
                                                            .WithBucket(bucketName)
                                                            .WithObject(objectName)
                                                            .WithCopyConditions(copyConditions)
                                                            .WithServerSideEncryption(sseSrc);
            CopyObjectArgs args = new CopyObjectArgs()
                                            .WithBucket(destBucketName)
                                            .WithObject(destObjectName)
                                            .WithCopyObjectSource(cpSrcArgs)
                                            .WithHeaders(metadata)
                                            .WithServerSideEncryption(sseDest);
            return this.CopyObjectAsync(args, cancellationToken);
            // public async Task CopyObjectAsync(string bucketName, string objectName, string destBucketName,   //RestSharp
            //     string destObjectName = null, CopyConditions copyConditions = null,
            //     Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null,
            //     ServerSideEncryption sseDest = null, CancellationToken cancellationToken = default(CancellationToken))
            // {
            //     if (bucketName == null)
            //     {
            //         throw new ArgumentException("Source bucket name cannot be empty", nameof(bucketName));
            //     }

            //     if (objectName == null)
            //     {
            //         throw new ArgumentException("Source object name cannot be empty", nameof(objectName));
            //     }

            //     if (destBucketName == null)
            //     {
            //         throw new ArgumentException("Destination bucket name cannot be empty", nameof(destBucketName));
            //     }

            //     // Escape source object path.
            //     string sourceObjectPath = $"{bucketName}/{utils.UrlEncode(objectName)}";

            //     // Destination object name is optional, if empty default to source object name.
            //     if (destObjectName == null)
            //     {
            //         destObjectName = objectName;
            //     }

            //     ServerSideEncryption sseGet = sseSrc;
            //     if (sseSrc is SSECopy sseCpy)
            //     {
            //         sseGet = sseCpy.CloneToSSEC();
            //     }

            //     // Get Stats on the source object
            //     ObjectStat srcStats = await this
            //         .StatObjectAsync(bucketName, objectName, sse: sseGet, cancellationToken: cancellationToken)
            //         .ConfigureAwait(false);
            //     // Copy metadata from the source object if no metadata replace directive
            //     var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            //     Dictionary<string, string> m = metadata;
            //     if (copyConditions != null && !copyConditions.HasReplaceMetadataDirective())
            //     {
            //         m = srcStats.MetaData;
            //     }

            //     if (m != null)
            //     {
            //         foreach (var item in m)
            //         {
            //             var key = item.Key;
            //             if (!supportedHeaders.Contains(key, StringComparer.OrdinalIgnoreCase) &&
            //                 !key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
            //             {
            //                 key = "x-amz-meta-" + key.ToLowerInvariant();
            //             }

            //             meta[key] = item.Value;
            //         }
            //     }

            //     long srcByteRangeSize = 0L;

            //     if (copyConditions != null)
            //     {
            //         srcByteRangeSize = copyConditions.GetByteRange();
            //     }

            //     long copySize = (srcByteRangeSize == 0) ? srcStats.Size : srcByteRangeSize;

            //     if ((srcByteRangeSize > srcStats.Size) ||
            //         ((srcByteRangeSize > 0) && (copyConditions.byteRangeEnd >= srcStats.Size)))
            //     {
            //         throw new ArgumentException("Specified byte range (" + copyConditions.byteRangeStart.ToString() + "-" +
            //                                     copyConditions.byteRangeEnd.ToString() +
            //                                     ") does not fit within source object (size=" + srcStats.Size.ToString() +
            //                                     ")");
            //     }

            //     if ((copySize > Constants.MaxSingleCopyObjectSize) ||
            //         (srcByteRangeSize > 0 && (srcByteRangeSize != srcStats.Size)))
            //     {
            //         await MultipartCopyUploadAsync(bucketName, objectName, destBucketName, destObjectName, copyConditions,
            //             copySize, meta, sseSrc, sseDest, cancellationToken).ConfigureAwait(false);
            //     }
            //     else
            //     {
            //         if (sseSrc != null && sseSrc is SSECopy)
            //         {
            //             sseSrc.Marshal(meta);
            //         }

            //         if (sseDest != null)
            //         {
            //             sseDest.Marshal(meta);
            //         }

            //         await this.CopyObjectRequestAsync(bucketName, objectName, destBucketName, destObjectName,
            //             copyConditions, meta, null, cancellationToken, typeof(CopyObjectResult)).ConfigureAwait(false);
            //     }
        }

        /// <summary>
        /// Create the copy request, execute it and
        /// </summary>
        /// <param name="bucketName">Bucket name where the object to be copied exists.</param>
        /// <param name="objectName">Object name source to be copied.</param>
        /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
        /// <param name="destObjectName">Object name to be created, if not provided uses source object name as destination object name.</param>
        /// <param name="copyConditions">optionally can take a key value CopyConditions as well for conditionally attempting copyObject.</param>
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
            string sourceObjectPath = bucketName + "/" + utils.UrlEncode(objectName);

            // Destination object name is optional, if empty default to source object name.
            if (destObjectName == null)
            {
                destObjectName = objectName;
            }

            var requestMessageBuilder = await this.CreateRequest(HttpMethod.Put, destBucketName,
                    objectName: destObjectName,
                    headerMap: customHeaders)
                .ConfigureAwait(false);
            if (queryMap != null)
            {
                foreach (var query in queryMap)
                {
                    requestMessageBuilder.AddQueryParameter(query.Key, query.Value);
                }
            }

            // Set the object source
            requestMessageBuilder.AddHeaderParameter("x-amz-copy-source", sourceObjectPath);

            // If no conditions available, skip addition else add the conditions to the header
            if (copyConditions != null)
            {
                foreach (var item in copyConditions.GetConditions())
                {
                    requestMessageBuilder.AddHeaderParameter(item.Key, item.Value);
                }
            }

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, requestMessageBuilder, cancellationToken)
                .ConfigureAwait(false);

            // Just read the result and parse content.
            using (var contentStream = new MemoryStream(response.ContentBytes))
            {
                object copyResult = null;
                if (type == typeof(CopyObjectResult))
                {
                    copyResult =
                        (CopyObjectResult)new XmlSerializer(typeof(CopyObjectResult)).Deserialize(contentStream);
                }

                if (type == typeof(CopyPartResult))
                {
                    copyResult = (CopyPartResult)new XmlSerializer(typeof(CopyPartResult)).Deserialize(contentStream);
                }

                return copyResult;
            }
        }

        /// <summary>
        /// Make a multi part copy upload for objects larger than 5GB or if CopyCondition specifies a byte range.
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
            ServerSideEncryption sseDest = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // For all sizes greater than 5GB or if Copy byte range specified in conditions and byte range larger
            // than minimum part size (5 MB) do multipart.

            dynamic multiPartInfo = utils.CalculateMultiPartSize(copySize, copy: true);
            double partSize = multiPartInfo.partSize;
            double partCount = multiPartInfo.partCount;
            double lastPartSize = multiPartInfo.lastPartSize;
            Part[] totalParts = new Part[(int)partCount];

            var sseHeaders = new Dictionary<string, string>();
            if (sseDest != null)
            {
                sseDest.Marshal(sseHeaders);
            }

            // No need to resume upload since this is a Server-side copy. Just initiate a new upload.
            string uploadId = await this
                .NewMultipartUploadAsync(destBucketName, destObjectName, metadata, sseHeaders, cancellationToken)
                .ConfigureAwait(false);

            // Upload each part
            double expectedReadSize = partSize;
            int partNumber;
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                CopyConditions partCondition = copyConditions.Clone();
                partCondition.byteRangeStart = (long)partSize * (partNumber - 1) + partCondition.byteRangeStart;
                if (partNumber < partCount)
                {
                    partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)partSize - 1;
                }
                else
                {
                    partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)lastPartSize - 1;
                }

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
                        "bytes=" + partCondition.byteRangeStart.ToString() + "-" + partCondition.byteRangeEnd.ToString()
                    }
                };

                if (sseSrc != null && sseSrc is SSECopy)
                {
                    sseSrc.Marshal(customHeader);
                }

                if (sseDest != null)
                {
                    sseDest.Marshal(customHeader);
                }

                CopyPartResult cpPartResult = (CopyPartResult)await this.CopyObjectRequestAsync(bucketName, objectName,
                    destBucketName, destObjectName, copyConditions, customHeader, queryMap, cancellationToken,
                    typeof(CopyPartResult)).ConfigureAwait(false);

                totalParts[partNumber - 1] = new Part
                { PartNumber = partNumber, ETag = cpPartResult.ETag, Size = (long)expectedReadSize };
            }

            Dictionary<int, string> etags = new Dictionary<int, string>();
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                etags[partNumber] = totalParts[partNumber - 1].ETag;
            }

            // Complete multi part upload
            await this.CompleteMultipartUploadAsync(destBucketName, destObjectName, uploadId, etags, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Presigned get url - returns a presigned url to access an object's data without credentials.URL can have a maximum expiry of
        /// upto 7 days or a minimum of 1 second.Additionally, you can override a set of response headers using reqParams.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        /// <param name="reqParams">optional override response headers</param>
        /// <param name="reqDate">optional request date and time in UTC</param>
        /// <returns></returns>
        [Obsolete("Use PresignedGetObjectAsync method with PresignedGetObjectArgs object.")]
        public string PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string, string> reqParams = null, DateTime? reqDate = null)
        {
            PresignedGetObjectArgs args = new PresignedGetObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithHeaders(reqParams)
                                                        .WithExpiry(expiresInt)
                                                        .WithRequestDate(reqDate);

            return this.PresignedGetObjectAsync(args);
            // public async Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt,
            //     Dictionary<string, string> reqParams = null, DateTime? reqDate = null)
            // {
            //     if (!utils.IsValidExpiry(expiresInt))
            //     {
            //         throw new InvalidExpiryRangeException("expiry range should be between 1 and " +
            //                                               Constants.DefaultExpiryTime.ToString());
            //     }

            // Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, this.Region);
            //             objectName: objectName,
            //             headerMap: reqParams)
            //         .ConfigureAwait(false);

            //     var authenticator = new V4Authenticator(this.Secure, this.AccessKey, this.SecretKey, region: this.Region,
            //         sessionToken: this.SessionToken);

            //     return authenticator.PresignURL(request, expiresInt, Region, this.SessionToken, reqDate);
        }

        /// <summary>
        /// Presigned Put url -returns a presigned url to upload an object without credentials.URL can have a maximum expiry of
        /// upto 7 days or a minimum of 1 second.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        /// <returns></returns>
        [Obsolete("Use PresignedPutObjectAsync method with PresignedPutObjectArgs object.")]
        public Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)
        {
            PresignedPutObjectArgs args = new PresignedPutObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithExpiry(expiresInt);
            return this.PresignedPutObjectAsync(args);
            //     // if (!utils.IsValidExpiry(expiresInt))  // Rest Sharp
            //     // {
            //     //     throw new InvalidExpiryRangeException("expiry range should be between 1 and " +
            //     //                                           Constants.DefaultExpiryTime.ToString());
            //     // }
            //     // var authenticator = new V4Authenticator(this.Secure, this.AccessKey, this.SecretKey, region: this.Region,
            //     //     sessionToken: this.SessionToken);
            //     // return authenticator.PresignURL(request, expiresInt, Region, this.SessionToken);
        }

        /// <summary>
        /// Presigned post policy
        /// </summary>
        /// <param name="policy"></param>
        /// <returns></returns>
        public Task<Tuple<string, Dictionary<string, string>>> PresignedPostPolicyAsync(PostPolicy policy)
        {
            PresignedPostPolicyArgs args = new PresignedPostPolicyArgs()
                                                        .WithBucket(policy.Bucket)
                                                        .WithObject(policy.Key)
                                                        .WithPolicy(policy);
            return this.PresignedPostPolicyAsync(args);
            // string region = null;

            // if (!policy.IsBucketSet())
            // {
            //     throw new ArgumentException("bucket should be set", nameof(policy));
            // }

            // if (!policy.IsKeySet())
            // {
            //     throw new ArgumentException("key should be set", nameof(policy));
            // }

            // if (!policy.IsExpirationSet())
            // {
            //     throw new ArgumentException("expiration should be set", nameof(policy));
            // }

            // // Initialize a new client.
            // if (!BucketRegionCache.Instance.Exists(policy.Bucket))
            // {
            //     region = await BucketRegionCache.Instance.Update(this, policy.Bucket).ConfigureAwait(false);
            // }

            // if (region == null)
            // {
            //     region = BucketRegionCache.Instance.Region(policy.Bucket);
            // }

            // // Set Target URL
            // Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, bucketName: policy.Bucket,
            //     region: region, usePathStyle: false);
            // DateTime signingDate = DateTime.UtcNow;

            // var authenticator = new V4Authenticator(this.Secure, this.AccessKey, this.SecretKey, region: this.Region,
            //     sessionToken: this.SessionToken);

            // policy.SetAlgorithm("AWS4-HMAC-SHA256");
            // policy.SetCredential(authenticator.GetCredentialString(signingDate, region));
            // policy.SetDate(signingDate);
            // policy.SetSessionToken(this.SessionToken);
            // string policyBase64 = policy.Base64();
            // string signature = authenticator.PresignPostSignature(region, signingDate, policyBase64);

            // policy.SetPolicy(policyBase64);
            // policy.SetSignature(signature);

            // return Tuple.Create(this.uri.AbsoluteUri, policy.GetFormData());
        }
    }
}