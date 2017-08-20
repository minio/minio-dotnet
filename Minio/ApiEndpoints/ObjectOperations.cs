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

namespace Minio
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Minio.DataModel;
    using Minio.Exceptions;
    using Minio.Helper;
    using RestSharp.Portable;

    public partial class DefaultMinioClient
    {
        public async Task<byte[]> GetObjectAsync(string bucketName, string objectName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = await this.CreateRequest(Method.GET,
                bucketName,
                objectName);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);

            return response.RawBytes;
        }

        public async Task<byte[]> GetObjectAsync(string bucketName, string objectName, long offset, long length,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (offset < 0)
            {
                throw new ArgumentException("Offset should be zero or greater");
            }
            if (length < 0)
            {
                throw new ArgumentException("Length should be greater than zero");
            }
            var headerMap = new Dictionary<string, string>();
            if (length > 0)
            {
                headerMap.Add("Range", "bytes=" + offset + "-" + (offset + length - 1));
            }

            var request = await this.CreateRequest(Method.GET,
                bucketName,
                objectName,
                headerMap);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);

            return response.RawBytes;
        }

        /// <summary>
        /// Creates an object from inputstream
        /// </summary>
        /// <param name="bucketName">Bucket to create object in</param>
        /// <param name="objectName">Key of the new object</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        /// <param name="data">Stream of bytes to send</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <param name="metaData">Object metadata to be stored. Defaults to null.</param>
        public Task PutObjectAsync(string bucketName, string objectName, Stream data, string contentType = null,
            Dictionary<string, string> metaData = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.PutObjectAsync(bucketName, objectName, data, data.Length, contentType, metaData,
                cancellationToken);
        }

        /// <summary>
        /// Creates an object from inputstream
        /// </summary>
        /// <param name="bucketName">Bucket to create object in</param>
        /// <param name="objectName">Key of the new object</param>
        /// <param name="size">Total size of bytes to be written, must match with data's length</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        /// <param name="data">Stream of bytes to send</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <param name="metaData">Object metadata to be stored. Defaults to null.</param>
        public async Task PutObjectAsync(string bucketName, string objectName, Stream data, long size,
            string contentType = null, Dictionary<string, string> metaData = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Utils.ValidateBucketName(bucketName);
            Utils.ValidateObjectName(objectName);
            metaData = metaData == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(metaData, StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }
            if (!metaData.ContainsKey("Content-Type"))
            {
                metaData["Content-Type"] = contentType;
            }
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // for sizes less than 5Mb , put a single object
            if (size < Constants.MinimumPartSize && size >= 0)
            {
                var bytes = ReadFull(data, (int) size);
                if (bytes.Length != (int) size)
                {
                    throw new UnexpectedShortReadException("Data read " + bytes.Length + " is shorter than the size " +
                                                           size + " of input buffer.");
                }

                await this.PutObjectAsync(bucketName, objectName, null, 0, bytes, metaData, cancellationToken);
                return;
            }
            // For all sizes greater than 5MiB do multipart.

            dynamic multiPartInfo = Utils.CalculateMultiPartSize(size);
            double partSize = multiPartInfo.partSize;
            double partCount = multiPartInfo.partCount;
            double lastPartSize = multiPartInfo.lastPartSize;
            var totalParts = new Part[(int) partCount];
            IList<Part> existingParts = null;
            var uploadId = await this.getLatestIncompleteUploadIdAsync(bucketName, objectName, cancellationToken);

            if (uploadId == null)
            {
                uploadId = await this.NewMultipartUploadAsync(bucketName, objectName, metaData, cancellationToken);
            }
            else
            {
                existingParts = await this.ListParts(bucketName, objectName, uploadId, cancellationToken);
            }

            var expectedReadSize = partSize;
            int partNumber;
            var skipUpload = false;
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                var dataToCopy = ReadFull(data, (int) partSize);
                if (partNumber == partCount)
                {
                    expectedReadSize = lastPartSize;
                }
				if (existingParts != null && partNumber <= existingParts.Count)
                {
                    var part = existingParts[partNumber - 1];
                    if (part != null && partNumber == part.PartNumber && expectedReadSize == part.PartSize())
                    {
                        var hash = this.CryptoProvider.Md5.ComputeHash(dataToCopy);
                        var etag = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                        if (etag.Equals(part.ETag))
                        {
                            totalParts[partNumber - 1] = new Part
                            {
                                PartNumber = part.PartNumber,
                                ETag = part.ETag,
                                Size = part.PartSize()
                            };
                            skipUpload = true;
                        }
                    }
                }
                else
                {
                    skipUpload = false;
                }

                if (skipUpload)
                {
                    continue;
                }

                {
                    var etag = await this.PutObjectAsync(bucketName, objectName, uploadId, partNumber, dataToCopy,
                        metaData, cancellationToken);
                    totalParts[partNumber - 1] =
                        new Part {PartNumber = partNumber, ETag = etag, Size = (long) expectedReadSize};
                }
            }
            var etags = new Dictionary<int, string>();
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                etags[partNumber] = totalParts[partNumber - 1].ETag;
            }
            await this.CompleteMultipartUploadAsync(bucketName, objectName, uploadId, etags, cancellationToken);
        }

        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix recursively
        /// </summary>
        /// <param name="bucketName">Bucket to list all incomplepte uploads from</param>
        /// <param name="prefix">prefix to list all incomplete uploads</param>
        /// <param name="recursive">option to list incomplete uploads recursively</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        public Task<IList<Upload>> ListIncompleteUploads(string bucketName, string prefix = "", bool recursive = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.ListIncompleteUploadsImpl(bucketName, prefix, recursive ? null : "/", cancellationToken);
        }

        /// <summary>
        /// Remove incomplete uploads from a given bucket and objectName
        /// </summary>
        /// <param name="bucketName">Bucket to remove incomplete uploads from</param>
        /// <param name="objectName">Key to remove incomplete uploads from</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task RemoveIncompleteUploadAsync(string bucketName, string objectName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var uploads =
                await this.ListIncompleteUploads(bucketName, objectName, cancellationToken: cancellationToken);
            foreach (var upload in uploads)
            {
                if (objectName == upload.Key)
                {
                    await this.RemoveUploadAsync(bucketName, objectName, upload.UploadId, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Removes an object with given name in specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to list incomplete uploads from</param>
        /// <param name="objectName">Key of object to list incomplete uploads from</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task RemoveObjectAsync(string bucketName, string objectName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = await this.CreateRequest(Method.DELETE, bucketName,
                objectName);

            await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);
        }

        /// <summary>
        /// Tests the object's existence and returns metadata about existing objects.
        /// </summary>
        /// <param name="bucketName">Bucket to test object in</param>
        /// <param name="objectName">Name of the object to stat</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Facts about the object</returns>
        public async Task<ObjectStat> StatObjectAsync(string bucketName, string objectName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = await this.CreateRequest(Method.HEAD, bucketName,
                objectName);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);


            // Extract stats from response
            long size = 0;
            var lastModified = new DateTime();
            var etag = "";
            string contentType = null;
            var metaData = new Dictionary<string, string>();

            //Supported headers for object.
            var supportedHeaders = new List<string> {"cache-control", "content-encoding", "content-type"};

            foreach (var parameter in response.Headers)
            {
                if (parameter.Key.Equals("Content-Length"))
                {
                    size = long.Parse(parameter.Value.ToString());
                }
                else if (parameter.Key.Equals("Last-Modified"))
                {
                    lastModified = DateTime.Parse(parameter.Value.ToString());
                }
                else if (parameter.Key.Equals("ETag"))
                {
                    etag = parameter.Value.ToString().Replace("\"", "");
                }
                else if (parameter.Key.Equals("Content-Type"))
                {
                    contentType = parameter.Value.ToString();
                    metaData["Content-Type"] = contentType;
                }
                else if (supportedHeaders.Contains(parameter.Key.ToLower()) ||
                         parameter.Key.ToLower().StartsWith("x-amz-meta-"))
                {
                    metaData[parameter.Key] = parameter.Value.ToString();
                }
            }
            return new ObjectStat(objectName, size, lastModified, etag, contentType, metaData);
        }

        /// <summary>
        /// Copy a source object into a new destination object.
        /// </summary>
        /// <param name="bucketName"> Bucket name where the object to be copied exists.</param>
        /// <param name="objectName">Object name source to be copied.</param>
        /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
        /// <param name="destObjectName">
        /// Object name to be created, if not provided uses source object name as destination object
        /// name.
        /// </param>
        /// <param name="copyConditions">
        /// optionally can take a key value CopyConditions as well for conditionally attempting
        /// copyObject.
        /// </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task CopyObjectAsync(string bucketName, string objectName, string destBucketName,
            string destObjectName = null, CopyConditions copyConditions = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (bucketName == null)
            {
                throw new ArgumentException("Source bucket name cannot be empty");
            }
            if (objectName == null)
            {
                throw new ArgumentException("Source object name cannot be empty");
            }
            if (destBucketName == null)
            {
                throw new ArgumentException("Destination bucket name cannot be empty");
            }
            // Escape source object path.
            var sourceObjectPath = bucketName + "/" + Utils.UrlEncode(objectName);

            // Destination object name is optional, if empty default to source object name.
            if (destObjectName == null)
            {
                destObjectName = objectName;
            }

            // Get Stats on the source object 
            var srcStats = await this.StatObjectAsync(bucketName, objectName, cancellationToken);

            var srcByteRangeSize = 0L;

            if (copyConditions != null)
            {
                srcByteRangeSize = copyConditions.GetByteRange();
            }
            var copySize = srcByteRangeSize == 0 ? srcStats.Size : srcByteRangeSize;

            if (srcByteRangeSize > srcStats.Size ||
                srcByteRangeSize > 0 && copyConditions?.ByteRangeEnd >= srcStats.Size)
            {
                throw new ArgumentException("Specified byte range (" + copyConditions?.ByteRangeStart + "-" +
                                            copyConditions?.ByteRangeEnd +
                                            ") does not fit within source object (size=" +
                                            srcStats.Size + ")");
            }

            if (copySize > Constants.MaxSingleCopyObjectSize ||
                srcByteRangeSize > 0 && srcByteRangeSize != srcStats.Size)
            {
                await this.MultipartCopyUploadAsync(bucketName, objectName, destBucketName, destObjectName,
                    copyConditions, copySize, cancellationToken);
            }
            else
            {
                await this.CopyObjectRequestAsync(bucketName, objectName, destBucketName, destObjectName,
                    copyConditions, null, null, cancellationToken, typeof(CopyObjectResult));
            }
        }


        /// <summary>
        /// Presigned Get url.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        /// <returns></returns>
        public async Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt)
        {
            var request = await this.CreateRequest(Method.GET, bucketName,
                objectName);

            return this.authenticator.PresignURL(this.restClient, request, expiresInt);
        }

        /// <summary>
        /// Presigned Put url.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        /// <returns></returns>
        public async Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)
        {
            var request = await this.CreateRequest(Method.PUT, bucketName,
                objectName);
            return this.authenticator.PresignURL(this.restClient, request, expiresInt);
        }

        /// <summary>
        /// Presigned post policy
        /// </summary>
        public async Task<Tuple<string, Dictionary<string, string>>> PresignedPostPolicyAsync(PostPolicy policy)
        {
            string region;

            if (!policy.IsBucketSet())
            {
                throw new ArgumentException("bucket should be set");
            }

            if (!policy.IsKeySet())
            {
                throw new ArgumentException("key should be set");
            }

            if (!policy.IsExpirationSet())
            {
                throw new ArgumentException("expiration should be set");
            }

            // Initialize a new client.
            if (!BucketRegionCache.Instance.Exists(policy.Bucket))
            {
                region = await BucketRegionCache.Instance.Update(this, policy.Bucket);
            }
            else
            {
                region = BucketRegionCache.Instance.Region(policy.Bucket);
            }
            // Set Target URL
            var requestUrl = RequestUtil.MakeTargetUrl(this.BaseUrl, this.Secure, policy.Bucket, region, false);
            this.SetTargetUrl(requestUrl);
            var signingDate = DateTime.UtcNow;

            policy.SetAlgorithm("AWS4-HMAC-SHA256");
            policy.SetCredential(this.authenticator.GetCredentialString(signingDate, region));
            policy.SetDate(signingDate);

            var policyBase64 = policy.Base64();
            var signature = this.authenticator.PresignPostSignature(region, signingDate, policyBase64);

            policy.SetPolicy(policyBase64);
            policy.SetSignature(signature);

            return new Tuple<string, Dictionary<string, string>>(this.restClient.BaseUrl.AbsoluteUri,
                policy.GetFormData());
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
            IReadOnlyDictionary<int, string> etags, CancellationToken cancellationToken)
        {
            var resourcePath = "?uploadId=" + uploadId;
            var request = await this.CreateRequest(Method.POST, bucketName,
                objectName,
                resourcePath: resourcePath
            );

            var parts = new List<XElement>();

            for (var i = 1; i <= etags.Count; i++)
            {
                parts.Add(new XElement("Part",
                    new XElement("PartNumber", i),
                    new XElement("ETag", etags[i])));
            }

            var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);

            var bodyString = completeMultipartUploadXml.ToString();

            var body = Encoding.UTF8.GetBytes(bodyString);

            request.AddParameter("application/xml", body, ParameterType.RequestBody);

            await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);
        }


        /// <summary>
        /// Returns an async observable of parts corresponding to a uploadId for a specific bucket and objectName
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="uploadId"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
		private async Task<IList<Part>> ListParts(string bucketName, string objectName, string uploadId,
            CancellationToken cancellationToken)
        {
			var parts = new List<Part>();
			var nextPartNumberMarker = 0;
			var isRunning = true;
			while (isRunning)
			{
				var uploads = await this.GetListPartsAsync(bucketName, objectName, uploadId, nextPartNumberMarker, cancellationToken);
				nextPartNumberMarker = uploads.Item1.NextPartNumberMarker;
				if(uploads.Item2.Count > 0)
				{
					parts.AddRange(uploads.Item2);
				}
				isRunning = uploads.Item1.IsTruncated;
			}

			return parts;
		}

        /// <summary>
        /// Gets the list of parts corresponding to a uploadId for given bucket and object
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="uploadId"></param>
        /// <param name="partNumberMarker"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<Tuple<ListPartsResult, List<Part>>> GetListPartsAsync(string bucketName, string objectName,
            string uploadId, int partNumberMarker, CancellationToken cancellationToken)
        {
            var resourcePath = "?uploadId=" + uploadId;
            if (partNumberMarker > 0)
            {
                resourcePath += "&part-number-marker=" + partNumberMarker;
            }
            resourcePath += "&max-parts=1000";
            var request = await this.CreateRequest(Method.GET, bucketName,
                objectName,
                resourcePath: resourcePath
            );

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);

            var contentBytes = Encoding.UTF8.GetBytes(response.Content);
            ListPartsResult listPartsResult;
            using (var stream = new MemoryStream(contentBytes))
            {
                listPartsResult = (ListPartsResult) new XmlSerializer(typeof(ListPartsResult)).Deserialize(stream);
            }

            var root = XDocument.Parse(response.Content);

            var uploads = from c in root.Root?.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Part")
                select new Part
                {
                    PartNumber = int.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}PartNumber")?.Value,
                        CultureInfo.CurrentCulture),
                    ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag")?.Value.Replace("\"", ""),
                    Size = long.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size")?.Value,
                        CultureInfo.CurrentCulture)
                };

            return new Tuple<ListPartsResult, List<Part>>(listPartsResult, uploads.ToList());
        }


        /// <summary>
        /// Start a new multi-part upload request
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="metaData"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<string> NewMultipartUploadAsync(string bucketName, string objectName,
            Dictionary<string, string> metaData, CancellationToken cancellationToken)
        {
            const string resource = "?uploads";
            var request = await this.CreateRequest(Method.POST, bucketName, objectName,
                metaData, resourcePath: resource);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);

            var contentBytes = Encoding.UTF8.GetBytes(response.Content);
            InitiateMultipartUploadResult newUpload;
            using (var stream = new MemoryStream(contentBytes))
            {
                newUpload = (InitiateMultipartUploadResult) new XmlSerializer(typeof(InitiateMultipartUploadResult))
                    .Deserialize(stream);
            }
            return newUpload.UploadId;
        }


        /// <summary>
        /// Upload object part to bucket for particular uploadId
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="uploadId"></param>
        /// <param name="partNumber"></param>
        /// <param name="data"></param>
        /// <param name="metaData"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<string> PutObjectAsync(string bucketName, string objectName, string uploadId, int partNumber,
            byte[] data, Dictionary<string, string> metaData, CancellationToken cancellationToken)
        {
            var resource = "";
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                resource += "?uploadId=" + uploadId + "&partNumber=" + partNumber;
            }
            // For multi-part upload requests, metadata needs to be passed in the NewMultiPartUpload request
            var contentType = metaData["Content-Type"];
            if (uploadId != null)
            {
                metaData = null;
            }
            var request = await this.CreateRequest(Method.PUT, bucketName,
                objectName,
                contentType: contentType,
                headerMap: metaData,
                body: data,
                resourcePath: resource
            );

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);

            string etag = null;
            foreach (var parameter in response.Headers)
            {
                if (parameter.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase))
                {
					etag = parameter.Value?.FirstOrDefault();
				}
            }

            return etag;
        }

        /// <summary>
        /// Get list of multi-part uploads matching particular uploadIdMarker
        /// </summary>
        /// <param name="bucketName">bucketName</param>
        /// <param name="prefix">prefix</param>
        /// <param name="keyMarker"></param>
        /// <param name="uploadIdMarker"></param>
        /// <param name="delimiter"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<Tuple<ListMultipartUploadsResult, List<Upload>>> GetMultipartUploadsListAsync(
            string bucketName,
            string prefix,
            string keyMarker,
            string uploadIdMarker,
            string delimiter,
            CancellationToken cancellationToken)
        {
            var queries = new List<string> {"uploads"};
            if (prefix != null)
            {
                queries.Add("prefix=" + Uri.EscapeDataString(prefix));
            }
            if (keyMarker != null)
            {
                queries.Add("key-marker=" + Uri.EscapeDataString(keyMarker));
            }
            if (uploadIdMarker != null)
            {
                queries.Add("upload-id-marker=" + uploadIdMarker);
            }
            if (delimiter != null)
            {
                queries.Add("delimiter=" + delimiter);
            }

            queries.Add("max-uploads=1000");

            var query = string.Join("&", queries);

            var request = await this.CreateRequest(Method.GET, bucketName,
                resourcePath: "?" + query);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);

            var contentBytes = Encoding.UTF8.GetBytes(response.Content);
            ListMultipartUploadsResult listBucketResult;
            using (var stream = new MemoryStream(contentBytes))
            {
                listBucketResult =
                    (ListMultipartUploadsResult) new XmlSerializer(typeof(ListMultipartUploadsResult)).Deserialize(
                        stream);
            }

            var root = XDocument.Parse(response.Content);

            var uploads = from c in root.Root?.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Upload")
                select new Upload
                {
                    Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key")?.Value,
                    UploadId = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}UploadId")?.Value,
                    Initiated = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Initiated")?.Value
                };

            return new Tuple<ListMultipartUploadsResult, List<Upload>>(listBucketResult, uploads.ToList());
        }


        /// <summary>
        /// Lists all or delimited incomplete uploads in a given bucket with a given objectName
        /// </summary>
        /// <param name="bucketName">Bucket to list incomplete uploads from</param>
        /// <param name="prefix">prefix</param>
        /// <param name="delimiter">delimiter of object to list incomplete uploads</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        private async Task<IList<Upload>> ListIncompleteUploadsImpl(string bucketName, string prefix, string delimiter,
            CancellationToken cancellationToken)
        {
            var objects = new List<Upload>();
            string nextKeyMarker = null;
            string nextUploadIdMarker = null;
            var isRunning = true;
            while (isRunning)
            {
                var uploads = await this.GetMultipartUploadsListAsync(bucketName, prefix, nextKeyMarker,
                    nextUploadIdMarker, delimiter, cancellationToken);
                nextKeyMarker = uploads.Item1.NextKeyMarker;
                nextUploadIdMarker = uploads.Item1.NextUploadIdMarker;

                if (uploads.Item2.Count > 0)
                {
                    objects.AddRange(uploads.Item2);
                }

                isRunning = uploads.Item1.IsTruncated;
            }

            return objects;
        }

        /// <summary>
        /// Find uploadId of most recent unsuccessful attempt to upload object to bucket.
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<string> getLatestIncompleteUploadIdAsync(string bucketName, string objectName,
            CancellationToken cancellationToken)
        {
            Upload latestUpload = null;
            var uploads = await this.ListIncompleteUploads(bucketName, objectName, cancellationToken: cancellationToken);
            foreach (var upload in uploads)
            {
                if (objectName == upload.Key &&
                    (latestUpload == null ||
                     string.Compare(latestUpload.Initiated, upload.Initiated, StringComparison.Ordinal) < 0))
                {
                    latestUpload = upload;
                }
            }

            return latestUpload?.UploadId;
        }

        /// <summary>
        /// Remove object with matching uploadId from bucket
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="uploadId"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task RemoveUploadAsync(string bucketName, string objectName, string uploadId,
            CancellationToken cancellationToken)
        {
            // var resourcePath = "/" + utils.UrlEncode(objectName) + "?uploadId=" + uploadId;
            var resourcePath = "?uploadId=" + uploadId;

            var request = await this.CreateRequest(Method.DELETE, bucketName,
                objectName,
                resourcePath: resourcePath
            );

            await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);
        }

        /// <summary>
        /// Advances in the stream upto currentPartSize or End of Stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="currentPartSize"></param>
        /// <returns>bytes read in a byte array</returns>
        private static byte[] ReadFull(Stream data, int currentPartSize)
        {
            var result = new byte[currentPartSize];
            var totalRead = 0;
            while (totalRead < currentPartSize)
            {
                var curData = new byte[currentPartSize - totalRead];
                var curRead = data.Read(curData, 0, currentPartSize - totalRead);
                if (curRead == 0)
                {
                    break;
                }
                for (var i = 0; i < curRead; i++)
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

            var truncatedResult = new byte[totalRead];
            for (var i = 0; i < totalRead; i++)
            {
                truncatedResult[i] = result[i];
            }
            return truncatedResult;
        }

        /// <summary>
        /// Create the copy request,execute it and
        /// </summary>
        /// <param name="bucketName"> Bucket name where the object to be copied exists.</param>
        /// <param name="objectName">Object name source to be copied.</param>
        /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
        /// <param name="destObjectName">
        /// Object name to be created, if not provided uses source object name as destination object
        /// name.
        /// </param>
        /// <param name="copyConditions">
        /// optionally can take a key value CopyConditions as well for conditionally attempting
        /// copyObject.
        /// </param>
        /// <param name="customHeaders">optional custom header to specify byte range</param>
        /// <param name="resource"> optional string to specify upload id and part number </param>
        /// <param name="type"> type of XML serialization to be applied on the server response</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<object> CopyObjectRequestAsync(string bucketName, string objectName, string destBucketName,
            string destObjectName, CopyConditions copyConditions, Dictionary<string, string> customHeaders,
            string resource, CancellationToken cancellationToken, Type type)
        {
            // Escape source object path.
            var sourceObjectPath = bucketName + "/" + Utils.UrlEncode(objectName);

            // Destination object name is optional, if empty default to source object name.
            if (destObjectName == null)
            {
                destObjectName = objectName;
            }
            var path = destBucketName + "/" + Utils.UrlEncode(destObjectName);

            var request = await this.CreateRequest(Method.PUT, destBucketName,
                destObjectName,
                resourcePath: resource,
                headerMap: customHeaders
            );
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

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken);

            // Just read the result and parse content.
            var contentBytes = Encoding.UTF8.GetBytes(response.Content);

            object copyResult = null;
            using (var stream = new MemoryStream(contentBytes))
            {
                if (type == typeof(CopyObjectResult))
                {
                    copyResult = (CopyObjectResult) new XmlSerializer(typeof(CopyObjectResult)).Deserialize(stream);
                }
                if (type == typeof(CopyPartResult))
                {
                    copyResult = (CopyPartResult) new XmlSerializer(typeof(CopyPartResult)).Deserialize(stream);
                }
            }

            return copyResult;
        }

        /// <summary>
        /// Make a multi part copy upload for objects larger than 5GB or if CopyCondition specifies a byte range.
        /// </summary>
        /// <param name="bucketName"> source bucket name</param>
        /// <param name="objectName"> source object name</param>
        /// <param name="destBucketName"> destination bucket name</param>
        /// <param name="destObjectName"> destiantion object name</param>
        /// <param name="copyConditions"> copyconditions </param>
        /// <param name="copySize"> size of copy upload</param>
        /// <param name="cancellationToken"> optional cancellation token</param>
        /// <returns></returns>
        private async Task MultipartCopyUploadAsync(string bucketName, string objectName, string destBucketName,
            string destObjectName, CopyConditions copyConditions, long copySize, CancellationToken cancellationToken)
        {
            // For all sizes greater than 5GB or if Copy byte range specified in conditions and byte range larger 
            // than minimum part size (5 MB) do multipart.

            dynamic multiPartInfo = Utils.CalculateMultiPartSize(copySize);
            double partSize = multiPartInfo.partSize;
            double partCount = multiPartInfo.partCount;
            double lastPartSize = multiPartInfo.lastPartSize;
            var totalParts = new Part[(int) partCount];

            // No need to resume upload since this is a server side copy. Just initiate a new upload.
            var uploadId = await this.NewMultipartUploadAsync(destBucketName, destObjectName, null, cancellationToken);

            // Upload each part
            var expectedReadSize = partSize;
            int partNumber;
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                var partCondition = copyConditions.Clone();
                partCondition.ByteRangeStart = (long) partSize * (partNumber - 1) + partCondition.ByteRangeStart;
                if (partNumber < partCount)
                {
                    partCondition.ByteRangeEnd = partCondition.ByteRangeStart + (long) partSize - 1;
                }
                else
                {
                    partCondition.ByteRangeEnd = partCondition.ByteRangeStart + (long) lastPartSize - 1;
                }
                var resource = "";
                if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
                {
                    resource += "?uploadId=" + uploadId + "&partNumber=" + partNumber;
                }
                var customHeader = new Dictionary<string, string>
                {
                    {
                        "x-amz-copy-source-range",
                        "bytes=" + partCondition.ByteRangeStart + "-" + partCondition.ByteRangeEnd
                    }
                };

                var cpPartResult = (CopyPartResult) await this.CopyObjectRequestAsync(bucketName, objectName,
                    destBucketName, destObjectName, copyConditions, customHeader, resource, cancellationToken,
                    typeof(CopyPartResult));

                totalParts[partNumber - 1] = new Part
                {
                    PartNumber = partNumber,
                    ETag = cpPartResult.ETag,
                    Size = (long) expectedReadSize
                };
            }

            var etags = new Dictionary<int, string>();
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                etags[partNumber] = totalParts[partNumber - 1].ETag;
            }
            // Complete multi part upload
            await this.CompleteMultipartUploadAsync(destBucketName, destObjectName, uploadId, etags, cancellationToken);
        }
    }
}