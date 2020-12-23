/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017, 2018, 2019, 2020 MinIO, Inc.
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
            RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
            IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            StatObjectResponse statResponse = new StatObjectResponse(response.StatusCode, response.Content, response.Headers, args);
            return statResponse.ObjectInfo;
        }


        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name, server-side encryption object, action stream, length, offset</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        public async Task GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            // First we call StatObject to verify the existence of the object.
            // NOTE: This avoids writing the error body to the action stream passed (Do not remove).
            StatObjectArgs statArgs = new StatObjectArgs()
                                            .WithBucket(args.BucketName)
                                            .WithObject(args.ObjectName)
                                            .WithVersionId(args.VersionId)
                                            .WithMatchETag(args.MatchETag)
                                            .WithNotMatchETag(args.NotMatchETag)
                                            .WithModifiedSince(args.ModifiedSince)
                                            .WithUnModifiedSince(args.UnModifiedSince)
                                            .WithServerSideEncryption(args.SSE);
            ObjectStat objStat = await this.StatObjectAsync(statArgs, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (args.FileName != null)
            {
                await this.getObjectFileAsync(args, objStat, cancellationToken);
            }
            else
            {
                await this.getObjectStreamAsync(args, objStat, args.CallBack, cancellationToken);
            }
        }


        /// <summary>
        /// Select an object's content. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="args">SelectObjectContentArgs Arguments Object which encapsulates bucket name, object name, Select Object Options</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        public async Task<SelectResponseStream> SelectObjectContentAsync(SelectObjectContentArgs args,CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
            IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            SelectObjectContentResponse selectObjectContentResponse = new SelectObjectContentResponse(response.StatusCode, response.Content, response.RawBytes);
            return selectObjectContentResponse.ResponseStream;
        }


        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix recursively
        /// </summary>
        /// <param name="args">ListIncompleteUploadsArgs Arguments Object which encapsulates bucket name, prefix, recursive</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
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
            IRestResponse response = null;
            try
            {
                RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
                response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
            GetMultipartUploadsListResponse getUploadResponse = new GetMultipartUploadsListResponse(response.StatusCode, response.Content);
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
            RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
            await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Remove incomplete uploads from a given bucket and objectName
        /// </summary>
        /// <param name="args">RemoveIncompleteUploadArgs Arguments Object which encapsulates bucket, object names</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args, CancellationToken cancellationToken = default(CancellationToken))
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
                if(upload.Key.ToLower().Equals(args.ObjectName.ToLower()))
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
        /// upto 7 days or a minimum of 1 second.Additionally, you can override a set of response headers using reqParams.
        /// </summary>
        /// <param name="args">PresignedGetObjectArgs Arguments object encapsulating bucket and object names, expiry time, response headers, request date</param>
        /// <returns></returns>
        public async Task<string> PresignedGetObjectAsync(PresignedGetObjectArgs args)
        {
            args.Validate();
            RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
            return this.authenticator.PresignURL(this.restClient, request, args.Expiry, this.Region, this.SessionToken, args.RequestDate);
        }


        /// <summary>
        /// Presigned post policy
        /// </summary>
        /// <param name="args">PresignedPostPolicyArgs Arguments object encapsulating Policy, Expiry, Region, </param>
        /// <returns>Tuple of URI and Policy Form data</returns>
        public async Task<Tuple<string, Dictionary<string, string>>> PresignedPostPolicyAsync(PresignedPostPolicyArgs args)
        {
            string region = await this.GetRegion(args.BucketName);
            args.Validate();
            args =  args.WithSessionToken(this.SessionToken)
                        .WithCredential(this.authenticator.GetCredentialString(DateTime.UtcNow, region))
                        .WithSignature(this.authenticator.PresignPostSignature(region, DateTime.UtcNow, args.Policy.Base64()))
                        .WithRegion(region);
            this.SetTargetURL(RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, args.BucketName, args.Region, usePathStyle: false));
            PresignedPostPolicyResponse policyResponse = new PresignedPostPolicyResponse(args, this.restClient.BaseUrl.AbsoluteUri);
            return policyResponse.URIPolicyTuple;
        }


        /// <summary>
        /// Presigned Put url -returns a presigned url to upload an object without credentials.URL can have a maximum expiry of
        /// upto 7 days or a minimum of 1 second.
        /// </summary>
        /// <param name="args">PresignedPutObjectArgs Arguments Object which encapsulates bucket, object names, expiry</param>
        /// <returns></returns>
        public async Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args)
        {
            args.Validate();
            RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
            return this.authenticator.PresignURL(this.restClient, request, args.Expiry, Region, this.SessionToken);
        }

        /// <summary>
        /// Get the configuration object for Legal Hold Status 
        /// </summary>
        /// <param name="args">GetObjectLegalHoldArgs Arguments Object which has object identifier information - bucket name, object name, version ID</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation </param>
        /// <returns> True if Legal Hold is ON, false otherwise  </returns>
        /// <exception cref="InvalidBucketNameException">When bucketName is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When objectName is invalid</exception>
        public async Task<bool> GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            var request = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            var legalHoldConfig = new GetLegalHoldResponse(response.StatusCode, response.Content);
            return (legalHoldConfig.CurrentLegalHoldConfiguration == null)?false: legalHoldConfig.CurrentLegalHoldConfiguration.Status.ToLower().Equals("on");
        }


        /// <summary>
        /// Set the Legal Hold Status using the related configuration
        /// </summary>
        /// <param name="args">SetObjectLegalHoldArgs Arguments Object which has object identifier information - bucket name, object name, version ID</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns> Task </returns>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        public async Task SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            var request = await this.CreateRequest(args).ConfigureAwait(false);
            await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets Tagging values set for this object
        /// </summary>
        /// <param name="args"> GetObjectTagsArgs Arguments Object with information like Bucket, Object name, (optional)version Id</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Tagging Object with key-value tag pairs</returns>
        public async Task<Tagging> GetObjectTagsAsync(GetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
            IRestResponse response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            GetObjectTagsResponse getObjectTagsResponse = new GetObjectTagsResponse(response.StatusCode, response.Content);
            return getObjectTagsResponse.ObjectTags;
        }


        /// <summary>
        /// Sets the Tagging values for this object
        /// </summary>
        /// <param name="args">SetObjectTagsArgs Arguments Object with information like Bucket name,Object name, (optional)version Id, tag key-value pairs</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task SetObjectTagsAsync(SetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
            await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Removes Tagging values stored for the object
        /// </summary>
        /// <param name="args">RemoveObjectTagsArgs Arguments Object with information like Bucket name</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task RemoveObjectTagsAsync(RemoveObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            RestRequest request = await this.CreateRequest(args).ConfigureAwait(false);
            await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }


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
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        public async Task SetObjectRetentionAsync(SetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            var request = await this.CreateRequest(args).ConfigureAwait(false);
            await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
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
        /// <exception cref="MissingObjectLockConfiguration">When object lock configuration on bucket is not set</exception>
        public async Task<ObjectRetentionConfiguration> GetObjectRetentionAsync(GetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            var request = await this.CreateRequest(args).ConfigureAwait(false);
            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
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
        /// <exception cref="MissingObjectLockConfiguration">When object lock configuration on bucket is not set</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        public async Task ClearObjectRetentionAsync(ClearObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            args.Validate();
            var request = await this.CreateRequest(args).ConfigureAwait(false);
            await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="cb">A stream will be passed to the callback</param>
        /// <param name="sse">Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        public async Task GetObjectAsync(string bucketName, string objectName, Action<Stream> cb, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Stat to see if the object exists
            // NOTE: This avoids writing the error body to the action stream passed (Do not remove).
            StatObjectArgs statArgs = new StatObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithServerSideEncryption(sse);
            await this.StatObjectAsync(statArgs, cancellationToken: cancellationToken).ConfigureAwait(false);

            var headers = new Dictionary<string, string>();
            if (sse != null && sse.GetType().Equals(EncryptionType.SSE_C))
            {
                sse.Marshal(headers);
            }
            var request = await this.CreateRequest(Method.GET,
                                                bucketName,
                                                objectName: objectName,
                                                headerMap: headers)
                                    .ConfigureAwait(false);
            request.ResponseWriter = cb;

            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
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
        public async Task GetObjectAsync(string bucketName, string objectName, long offset, long length, Action<Stream> cb, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (offset < 0)
            {
                throw new ArgumentException("Offset should be zero or greater", nameof(offset));
            }

            if (length < 0)
            {
                throw new ArgumentException("Length should be greater than zero", nameof(length));
            }

            // Stat to see if the object exists
            // NOTE: This avoids writing the error body to the action stream passed (Do not remove).
            StatObjectArgs statArgs = new StatObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithServerSideEncryption(sse);
            await this.StatObjectAsync(statArgs, cancellationToken: cancellationToken).ConfigureAwait(false);

            var headerMap = new Dictionary<string, string>();
            if (length > 0)
            {
                headerMap.Add("Range", "bytes=" + offset.ToString() + "-" + (offset + length - 1).ToString());
            }

            if (sse != null && sse.GetType().Equals(EncryptionType.SSE_C))
            {
                sse.Marshal(headerMap);
            }
            var request = await this.CreateRequest(Method.GET,
                                                     bucketName,
                                                     objectName: objectName,
                                                     headerMap: headerMap)
                                .ConfigureAwait(false);

            request.ResponseWriter = cb;
            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
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
        public async Task GetObjectAsync(string bucketName, string objectName, string fileName, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool fileExists = File.Exists(fileName);
            utils.ValidateFile(fileName);

            StatObjectArgs statArgs = new StatObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithServerSideEncryption(sse);
            ObjectStat objectStat = await this.StatObjectAsync(statArgs, cancellationToken: cancellationToken).ConfigureAwait(false);

            long length = objectStat.Size;
            string etag = objectStat.ETag;

            string tempFileName = $"{fileName}.{etag}.part.minio";

            bool tempFileExists = File.Exists(tempFileName);

            utils.ValidateFile(tempFileName);

            FileInfo tempFileInfo = new FileInfo(tempFileName);
            long tempFileSize = 0;
            if (tempFileExists)
            {
                tempFileSize = tempFileInfo.Length;
                if (tempFileSize > length)
                {
                    File.Delete(tempFileName);
                    tempFileExists = false;
                    tempFileSize = 0;
                }
            }

            if (fileExists)
            {
                FileInfo fileInfo = new FileInfo(fileName);
                long fileSize = fileInfo.Length;
                if (fileSize == length)
                {
                    // already downloaded. nothing to do
                    return;
                }
                else if (fileSize > length)
                {
                    throw new ArgumentException("'" + fileName + "': object size " + length + " is smaller than file size "
                                                       + fileSize, nameof(fileSize));
                }
                else if (!tempFileExists)
                {
                    // before resuming the download, copy filename to tempfilename
                    File.Copy(fileName, tempFileName);
                    tempFileSize = fileSize;
                    tempFileExists = true;
                }
            }
            await GetObjectAsync(bucketName, objectName, (stream) =>
            {
                var fileStream = File.Create(tempFileName);
                stream.CopyTo(fileStream);
                fileStream.Dispose();
                FileInfo writtenInfo = new FileInfo(tempFileName);
                long writtenSize = writtenInfo.Length;
                if (writtenSize != length - tempFileSize)
                {
                    throw new IOException(tempFileName + ": unexpected data written.  expected = " + (length - tempFileSize)
                                           + ", written = " + writtenSize);
                }
                utils.MoveWithReplace(tempFileName, fileName);
            }, sse, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Select an object's content. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="opts">Select Object options</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        [Obsolete("Use SelectObjectContentAsync method with SelectObjectContentsArgs object. Refer SelectObjectContent example code.")]
        public async Task<SelectResponseStream> SelectObjectContentAsync(string bucketName, string objectName, SelectObjectOptions opts,CancellationToken cancellationToken = default(CancellationToken))
        {
            utils.ValidateBucketName(bucketName);
            utils.ValidateObjectName(objectName);
            if (opts == null)
            {
                throw new ArgumentException("Options cannot be null", nameof(opts));
            }
            Dictionary<string,string> sseHeaders = null;
            if (opts.SSE != null)
            {
                sseHeaders = new Dictionary<string,string>();
                opts.SSE.Marshal(sseHeaders);
            }
            var selectReqBytes = System.Text.Encoding.UTF8.GetBytes(opts.MarshalXML());

            var request = await this.CreateRequest(Method.POST, bucketName,
                                                    objectName: objectName,
                                                    headerMap: sseHeaders)
                                    .ConfigureAwait(false);
            request.AddQueryParameter("select","");
            request.AddQueryParameter("select-type","2");
            request.AddParameter("application/xml", selectReqBytes, ParameterType.RequestBody);

            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            return new SelectResponseStream(new MemoryStream(response.RawBytes));
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
        public async Task PutObjectAsync(string bucketName, string objectName, string fileName, string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            utils.ValidateFile(fileName, contentType);
            FileInfo fileInfo = new FileInfo(fileName);
            long size = fileInfo.Length;
            using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                await PutObjectAsync(bucketName, objectName, file, size, contentType, metaData, sse, cancellationToken).ConfigureAwait(false);
            }
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
        public async Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            utils.ValidateBucketName(bucketName);
            utils.ValidateObjectName(objectName);

            var sseHeaders = new Dictionary<string, string>();
            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (metaData != null) {
                foreach (KeyValuePair<string, string> p in metaData)
                {
                    var key = p.Key;
                    if (!OperationsUtil.IsSupportedHeader(p.Key) && !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "x-amz-meta-" + key.ToLowerInvariant();
                    }
                    meta[key] = p.Value;

                }
            }

            if (sse != null)
            {
                sse.Marshal(sseHeaders);
            }
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }
            if (!meta.ContainsKey("Content-Type"))
            {
                meta["Content-Type"] = contentType;
            }
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Invalid input stream, cannot be null");
            }

            // for sizes less than 5Mb , put a single object
            if (size < Constants.MinimumPartSize && size >= 0)
            {
                var bytes = await ReadFullAsync(data, (int)size).ConfigureAwait(false);
                if (bytes != null && bytes.Length != (int)size)
                {
                    throw new UnexpectedShortReadException($"Data read {bytes.Length} is shorter than the size {size} of input buffer.");
                }
                await this.PutObjectAsync(bucketName, objectName, null, 0, bytes, meta, sseHeaders, cancellationToken).ConfigureAwait(false);
                return;
            }
            // For all sizes greater than 5MiB do multipart.

            dynamic multiPartInfo = utils.CalculateMultiPartSize(size);
            double partSize = multiPartInfo.partSize;
            double partCount = multiPartInfo.partCount;
            double lastPartSize = multiPartInfo.lastPartSize;
            Part[] totalParts = new Part[(int)partCount];

            string uploadId = await this.NewMultipartUploadAsync(bucketName, objectName, meta, sseHeaders, cancellationToken).ConfigureAwait(false);

            // Remove SSE-S3 and KMS headers during PutObjectPart operations.
            if (sse != null &&
               (sse.GetType().Equals(EncryptionType.SSE_S3) ||
                sse.GetType().Equals(EncryptionType.SSE_KMS)))
            {
                sseHeaders.Remove(Constants.SSEGenericHeader);
                sseHeaders.Remove(Constants.SSEKMSContext);
                sseHeaders.Remove(Constants.SSEKMSKeyId);
            }

            double expectedReadSize = partSize;
            int partNumber;
            int numPartsUploaded = 0;
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                byte[] dataToCopy = await ReadFullAsync(data, (int)partSize).ConfigureAwait(false);
                if (dataToCopy == null && numPartsUploaded > 0)
                {
                    break;
                }

                if (partNumber == partCount)
                {
                    expectedReadSize = lastPartSize;
                }
                numPartsUploaded += 1;
                string etag = await this.PutObjectAsync(bucketName, objectName, uploadId, partNumber, dataToCopy, meta, sseHeaders, cancellationToken).ConfigureAwait(false);
                totalParts[partNumber - 1] = new Part { PartNumber = partNumber, ETag = etag, Size = (long)expectedReadSize };
            }

            // This shouldn't happen where stream size is known.
            if (partCount != numPartsUploaded && size != -1)
            {
                await this.RemoveUploadAsync(bucketName, objectName, uploadId, cancellationToken).ConfigureAwait(false);
                return;
            }

            Dictionary<int, string> etags = new Dictionary<int, string>();
            for (partNumber = 1; partNumber <= numPartsUploaded; partNumber++)
            {
                etags[partNumber] = totalParts[partNumber - 1].ETag;
            }
            await this.CompleteMultipartUploadAsync(bucketName, objectName, uploadId, etags, cancellationToken).ConfigureAwait(false);
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
        private async Task CompleteMultipartUploadAsync(string bucketName, string objectName, string uploadId, Dictionary<int, string> etags, CancellationToken cancellationToken)
        {
            var request = await this.CreateRequest(Method.POST, bucketName,
                                                     objectName: objectName)
                                    .ConfigureAwait(false);
            request.AddQueryParameter("uploadId",$"{uploadId}");

            List<XElement> parts = new List<XElement>();

            for (int i = 1; i <= etags.Count; i++)
            {
                parts.Add(new XElement("Part",
                                       new XElement("PartNumber", i),
                                       new XElement("ETag", etags[i])));
            }

            var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);
            var bodyString = completeMultipartUploadXml.ToString();
            var body = System.Text.Encoding.UTF8.GetBytes(bodyString);

            request.AddParameter("application/xml", body, ParameterType.RequestBody);

            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an async observable of parts corresponding to a uploadId for a specific bucket and objectName
        /// </summary>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectName">Object Name</param>
        /// <param name="uploadId"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private IObservable<Part> ListParts(string bucketName, string objectName, string uploadId, CancellationToken cancellationToken)
        {
            return Observable.Create<Part>(
              async obs =>
              {
                  int nextPartNumberMarker = 0;
                  bool isRunning = true;
                  while (isRunning)
                  {
                      var uploads = await this.GetListPartsAsync(bucketName, objectName, uploadId, nextPartNumberMarker, cancellationToken).ConfigureAwait(false);
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
        private async Task<Tuple<ListPartsResult, List<Part>>> GetListPartsAsync(string bucketName, string objectName, string uploadId, int partNumberMarker, CancellationToken cancellationToken)
        {
            var request = await this.CreateRequest(Method.GET, bucketName,
                                                     objectName: objectName)
                                .ConfigureAwait(false);
            request.AddQueryParameter("uploadId",$"{uploadId}");
            if (partNumberMarker > 0)
            {
                request.AddQueryParameter("part-number-marker",$"{partNumberMarker}");
            }
            request.AddQueryParameter("max-parts","1000");

            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            ListPartsResult listPartsResult = null;
            using (var stream = new MemoryStream(contentBytes))
            {
                listPartsResult = (ListPartsResult)new XmlSerializer(typeof(ListPartsResult)).Deserialize(stream);
            }

            XDocument root = XDocument.Parse(response.Content);

            var uploads = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Part")
                          select new Part
                          {
                              PartNumber = int.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}PartNumber").Value, CultureInfo.CurrentCulture),
                              ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value.Replace("\"", string.Empty),
                              Size = long.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value, CultureInfo.CurrentCulture)
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
        private async Task<string> NewMultipartUploadAsync(string bucketName, string objectName, Dictionary<string, string> metaData, Dictionary<string, string> sseHeaders, CancellationToken cancellationToken = default(CancellationToken))
        {

            foreach (KeyValuePair<string, string> kv in sseHeaders)
            {
                metaData.Add(kv.Key, kv.Value);
            }
            var request = await this.CreateRequest(Method.POST, bucketName, objectName: objectName,
                            headerMap: metaData).ConfigureAwait(false);
            request.AddQueryParameter("uploads","");

            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            InitiateMultipartUploadResult newUpload = null;
            using (var stream = new MemoryStream(contentBytes))
            {
                newUpload = (InitiateMultipartUploadResult)new XmlSerializer(typeof(InitiateMultipartUploadResult)).Deserialize(stream);
            }
            return newUpload.UploadId;
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
        private async Task<string> PutObjectAsync(string bucketName, string objectName, string uploadId, int partNumber, byte[] data, Dictionary<string, string> metaData, Dictionary<string, string> sseHeaders, CancellationToken cancellationToken)
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
            var request = await this.CreateRequest(Method.PUT, bucketName,
                                                     objectName: objectName,
                                                     contentType: contentType,
                                                     headerMap: metaData,
                                                     body: data)
                                    .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                request.AddQueryParameter("uploadId",$"{uploadId}");
                request.AddQueryParameter("partNumber",$"{partNumber}");
            }

            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            string etag = null;
            foreach (Parameter parameter in response.Headers)
            {
                if (parameter.Name.Equals("ETag", StringComparison.OrdinalIgnoreCase))
                {
                    etag = parameter.Value.ToString();
                }
            }
            return etag;
        }

        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix recursively
        /// </summary>
        /// <param name="bucketName">Bucket to list all incomplete uploads from</param>
        /// <param name="prefix">Filter all incomplete uploads starting with this prefix</param>
        /// <param name="recursive">Set to true to recursively list all incomplete uploads</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        public IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix = null, bool recursive = true, CancellationToken cancellationToken = default(CancellationToken))
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
        }

        /// <summary>
        /// Remove incomplete uploads from a given bucket and objectName
        /// </summary>
        /// <param name="bucketName">Bucket to remove incomplete uploads from</param>
        /// <param name="objectName">Key to remove incomplete uploads from</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task RemoveIncompleteUploadAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))
        {
            ListIncompleteUploadsArgs listArgs = new ListIncompleteUploadsArgs()
                                                                .WithBucket(bucketName)
                                                                .WithPrefix(objectName);
            IObservable<Upload> observable = this.ListIncompleteUploads(listArgs, cancellationToken: cancellationToken);

            IDisposable subscription = observable.Subscribe(
                async upload =>
                {
                    try
                    {
                        if (objectName == upload.Key)
                        {
                            await this.RemoveUploadAsync(bucketName, objectName, upload.UploadId, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch(Exception ex)
                    {
                        if (ex.GetType() != typeof(BucketNotFoundException)) // Ignoring bucket not found as upload is deleted as desired
                        {
                            throw ex;
                        }
                    }
                },
                ex => 
                {
                    throw ex;
                }
                );
        }

        /// <summary>
        /// Remove object with matching uploadId from bucket
        /// </summary>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectName"></param>
        /// <param name="uploadId"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task RemoveUploadAsync(string bucketName, string objectName, string uploadId, CancellationToken cancellationToken)
        {
            var request = await this.CreateRequest(Method.DELETE, bucketName,
                                                     objectName: objectName)
                                    .ConfigureAwait(false);
            request.AddQueryParameter("uploadId",$"{uploadId}");
            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes an object with given name in specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to remove object from</param>
        /// <param name="objectName">Key of object to remove</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task RemoveObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = await this.CreateRequest(Method.DELETE, bucketName, objectName: objectName).ConfigureAwait(false);

            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// private helper method to remove list of objects from bucket
        /// </summary>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectsList"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<List<DeleteError>> removeObjectsAsync(string bucketName, List<DeleteObject> objectsList, CancellationToken cancellationToken)
        {
            var request = await this.CreateRequest(Method.POST, bucketName).ConfigureAwait(false);
            request.AddQueryParameter("delete","");
            List<XElement> objects = new List<XElement>();

            foreach (var obj in objectsList)
            {
                objects.Add(new XElement("Object",
                                       new XElement("Key", obj.Key)));
            }

            var deleteObjectsRequest = new XElement("Delete", objects,
                                        new XElement("Quiet", true));

            request.AddXmlBody(deleteObjectsRequest);
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            request.RequestFormat = DataFormat.Xml;

            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            DeleteObjectsResult deleteResult = null;
            using (var stream = new MemoryStream(contentBytes))
            {
                deleteResult = (DeleteObjectsResult)new XmlSerializer(typeof(DeleteObjectsResult)).Deserialize(stream);
            }

            if (deleteResult == null)
            {
                return new List<DeleteError>();
            }
            return deleteResult.ErrorList();
        }

        /// <summary>
        /// Removes multiple objects from a specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to remove objects from</param>
        /// <param name="objectNames">List of object keys to remove.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task<IObservable<DeleteError>> RemoveObjectAsync(string bucketName, IEnumerable<string> objectNames, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (objectNames == null)
            {
                return null;
            }

            utils.ValidateBucketName(bucketName);
            List<DeleteObject> objectList;
            return Observable.Create<DeleteError>(
              async obs =>
              {
                  bool process = true;
                  int count = objectNames.Count();
                  int i = 0;

                  while (process)
                  {
                      objectList = new List<DeleteObject>();
                      while (i < count)
                      {
                          string objectName = objectNames.ElementAt(i);
                          utils.ValidateObjectName(objectName);
                          objectList.Add(new DeleteObject(objectName));
                          i++;
                          if (i % 1000 == 0)
                              break;
                      }
                      if (objectList.Count > 0)
                      {
                          var errorsList = await removeObjectsAsync(bucketName, objectList, cancellationToken).ConfigureAwait(false);
                          foreach (DeleteError error in errorsList)
                          {
                              obs.OnNext(error);
                          }
                      }
                      if (i >= objectNames.Count())
                      {
                          process = !process;
                      }
                  }
              });
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
        public async Task<ObjectStat> StatObjectAsync(string bucketName, string objectName, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            StatObjectArgs args = new StatObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithServerSideEncryption(sse);
            return await this.StatObjectAsync(args, cancellationToken: cancellationToken).ConfigureAwait(false);
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
        public async Task CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (bucketName == null)
            {
                throw new ArgumentException("Source bucket name cannot be empty", nameof(bucketName));
            }
            if (objectName == null)
            {
                throw new ArgumentException("Source object name cannot be empty", nameof(objectName));
            }
            if (destBucketName == null)
            {
                throw new ArgumentException("Destination bucket name cannot be empty", nameof(destBucketName));
            }
            // Escape source object path.
            string sourceObjectPath = $"{bucketName}/{utils.UrlEncode(objectName)}";

            // Destination object name is optional, if empty default to source object name.
            if (destObjectName == null)
            {
                destObjectName = objectName;
            }

            ServerSideEncryption sseGet = sseSrc;
            if (sseSrc is SSECopy sseCpy)
            {
                sseGet = sseCpy.CloneToSSEC();
            }
            // Get Stats on the source object
            StatObjectArgs statArgs = new StatObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName)
                                            .WithServerSideEncryption(sseGet);
            ObjectStat srcStats = await this.StatObjectAsync(statArgs, cancellationToken: cancellationToken).ConfigureAwait(false);
            // Copy metadata from the source object if no metadata replace directive
            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> m = metadata;
            if (copyConditions != null && !copyConditions.HasReplaceMetadataDirective())
            {
                m = srcStats.MetaData;
            }

            if (m != null)
            {
                foreach (var item in m)
                {
                    var key = item.Key;
                    if (!OperationsUtil.IsSupportedHeader(key) && !key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "x-amz-meta-" + key.ToLowerInvariant();
                    }
                    meta[key] = item.Value;
                }
            }

            long srcByteRangeSize = 0L;

            if (copyConditions != null)
            {
                srcByteRangeSize = copyConditions.GetByteRange();
            }
            long copySize = (srcByteRangeSize == 0) ? srcStats.Size : srcByteRangeSize;

            if ((srcByteRangeSize > srcStats.Size) || ((srcByteRangeSize > 0) && (copyConditions.byteRangeEnd >= srcStats.Size)))
            {
                throw new ArgumentException("Specified byte range (" + copyConditions.byteRangeStart.ToString() + "-" + copyConditions.byteRangeEnd.ToString() + ") does not fit within source object (size=" + srcStats.Size.ToString() + ")");
            }

            if ((copySize > Constants.MaxSingleCopyObjectSize) || (srcByteRangeSize > 0 && (srcByteRangeSize != srcStats.Size)))
            {
                await MultipartCopyUploadAsync(bucketName, objectName, destBucketName, destObjectName, copyConditions, copySize, meta, sseSrc, sseDest, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (sseSrc != null && sseSrc is SSECopy)
                {
                    sseSrc.Marshal(meta);
                }
                if (sseDest != null)
                {
                    sseDest.Marshal(meta);
                }
                await this.CopyObjectRequestAsync(bucketName, objectName, destBucketName, destObjectName, copyConditions, meta, null, cancellationToken, typeof(CopyObjectResult)).ConfigureAwait(false);
            }
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
        /// <param name="resource">Optional string to specify upload id and part number </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <param name="type">Type of XML serialization to be applied on the server response</param>
        /// <returns></returns>
        private async Task<object> CopyObjectRequestAsync(string bucketName, string objectName, string destBucketName, string destObjectName, CopyConditions copyConditions, Dictionary<string, string> customHeaders, Dictionary<string, string> queryMap, CancellationToken cancellationToken, Type type)
        {
            // Escape source object path.
            string sourceObjectPath = bucketName + "/" + utils.UrlEncode(objectName);

            // Destination object name is optional, if empty default to source object name.
            if (destObjectName == null)
            {
                destObjectName = objectName;
            }

            var request = await this.CreateRequest(Method.PUT, destBucketName,
                                                   objectName: destObjectName,
                                                   headerMap: customHeaders)
                                .ConfigureAwait(false);
            if (queryMap != null)
            {
                foreach (var query in queryMap)
                {
                    request.AddQueryParameter(query.Key,query.Value);
                }
            }
            // Set the object source
            request.AddHeader("x-amz-copy-source", sourceObjectPath);

            // If no conditions available, skip addition else add the conditions to the header
            if (copyConditions != null)
            {
                foreach (var item in copyConditions.GetConditions())
                {
                    request.AddHeader(item.Key, item.Value);
                }
            }

            var response = await this.ExecuteAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            // Just read the result and parse content.
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);

            object copyResult = null;
            using (var stream = new MemoryStream(contentBytes))
            {
                if (type == typeof(CopyObjectResult))
                {
                    copyResult = (CopyObjectResult)new XmlSerializer(typeof(CopyObjectResult)).Deserialize(stream);
                }

                if (type == typeof(CopyPartResult))
                {
                    copyResult = (CopyPartResult)new XmlSerializer(typeof(CopyPartResult)).Deserialize(stream);
                }
            }

            return copyResult;
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
        private async Task MultipartCopyUploadAsync(string bucketName, string objectName, string destBucketName, string destObjectName, CopyConditions copyConditions, long copySize, Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // For all sizes greater than 5GB or if Copy byte range specified in conditions and byte range larger
            // than minimum part size (5 MB) do multipart.

            dynamic multiPartInfo = utils.CalculateMultiPartSize(copySize);
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
            string uploadId = await this.NewMultipartUploadAsync(destBucketName, destObjectName, metadata, sseHeaders, cancellationToken).ConfigureAwait(false);

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

                var queryMap = new Dictionary<string,string>();
                if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
                {
                    queryMap.Add("uploadId",uploadId);
                    queryMap.Add("partNumber",partNumber.ToString());
                }

                var customHeader = new Dictionary<string, string>
                {
                    { "x-amz-copy-source-range", "bytes=" + partCondition.byteRangeStart.ToString() + "-" + partCondition.byteRangeEnd.ToString() }
                };

                if (sseSrc != null && sseSrc is SSECopy)
                {
                    sseSrc.Marshal(customHeader);
                }
                if (sseDest != null)
                {
                    sseDest.Marshal(customHeader);
                }
                CopyPartResult cpPartResult = (CopyPartResult)await this.CopyObjectRequestAsync(bucketName, objectName, destBucketName, destObjectName, copyConditions, customHeader, queryMap, cancellationToken, typeof(CopyPartResult)).ConfigureAwait(false);

                totalParts[partNumber - 1] = new Part { PartNumber = partNumber, ETag = cpPartResult.ETag, Size = (long)expectedReadSize };
            }

            Dictionary<int, string> etags = new Dictionary<int, string>();
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                etags[partNumber] = totalParts[partNumber - 1].ETag;
            }
            // Complete multi part upload
            await this.CompleteMultipartUploadAsync(destBucketName, destObjectName, uploadId, etags, cancellationToken).ConfigureAwait(false);
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
        public async Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt, Dictionary<string, string> reqParams = null, DateTime? reqDate = null)
        {
            PresignedGetObjectArgs args = new PresignedGetObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithHeaders(reqParams)
                                                        .WithExpiry(expiresInt)
                                                        .WithRequestDate(reqDate);

            return await this.PresignedGetObjectAsync(args);
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
        public async Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)
        {
            PresignedPutObjectArgs args = new PresignedPutObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName)
                                                        .WithExpiry(expiresInt);
            return await this.PresignedPutObjectAsync(args);
        }

        /// <summary>
        /// Presigned post policy
        /// </summary>
        /// <param name="policy"></param>
        /// <returns></returns>
        public async Task<Tuple<string, Dictionary<string, string>>> PresignedPostPolicyAsync(PostPolicy policy)
        {
            PresignedPostPolicyArgs args = new PresignedPostPolicyArgs()
                                                        .WithBucket(policy.Bucket)
                                                        .WithObject(policy.Key)
                                                        .WithPolicy(policy);
            return await this.PresignedPostPolicyAsync(args);
        }
    }
}