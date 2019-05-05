/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017, 2018, 2019 MinIO, Inc.
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
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="cb">A stream will be passed to the callback</param>
        /// <param name="sse">Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        public async Task GetObjectAsync(string bucketName, string objectName, Action<Stream> cb, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await StatObjectAsync(bucketName, objectName, sse: sse, cancellationToken: cancellationToken).ConfigureAwait(false);

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

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
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
                throw new ArgumentException("Offset should be zero or greater");
            if (length < 0)
                throw new ArgumentException("Length should be greater than zero");

            await StatObjectAsync(bucketName, objectName, cancellationToken: cancellationToken).ConfigureAwait(false);

            Dictionary<string, string> headerMap = new Dictionary<string, string>();
            if (length > 0)
                headerMap.Add("Range", "bytes=" + offset.ToString() + "-" + (offset + length - 1).ToString());

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
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
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

            ObjectStat objectStat = await StatObjectAsync(bucketName, objectName, sse: sse, cancellationToken: cancellationToken).ConfigureAwait(false);
            long length = objectStat.Size;
            string etag = objectStat.ETag;

            string tempFileName = fileName + "." + etag + ".part.minio";

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
                                                       + fileSize);
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
        /// <param name="size">Total size of bytes to be written, must match with data's length</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        /// <param name="data">Stream of bytes to send</param>
        /// <param name="metaData">Object metadata to be stored. Defaults to null.</param>
        /// <param name="sse">Server-side encryption option. Defaults to null.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        public async Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType = null, Dictionary<string, string> metaData = null, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            utils.validateBucketName(bucketName);
            utils.validateObjectName(objectName);
            var sseHeaders = new Dictionary<string, string>();

            if (metaData == null)
            {
                metaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                metaData = new Dictionary<string, string>(metaData, StringComparer.OrdinalIgnoreCase);
            }
            if (sse != null)
            {
                sse.Marshal(sseHeaders);
            }
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
                throw new ArgumentNullException("Invalid input stream,cannot be null");
            }

            // for sizes less than 5Mb , put a single object
            if (size < Constants.MinimumPartSize && size >= 0)
            {
                var bytes = ReadFull(data, (int)size);
                if (bytes != null && bytes.Length != (int)size)
                {
                    throw new UnexpectedShortReadException("Data read " + bytes.Length + " is shorter than the size " + size + " of input buffer.");
                }
                await this.PutObjectAsync(bucketName, objectName, null, 0, bytes, metaData, sseHeaders, cancellationToken).ConfigureAwait(false);
                return;
            }
            // For all sizes greater than 5MiB do multipart.

            dynamic multiPartInfo = utils.CalculateMultiPartSize(size);
            double partSize = multiPartInfo.partSize;
            double partCount = multiPartInfo.partCount;
            double lastPartSize = multiPartInfo.lastPartSize;
            Part[] totalParts = new Part[(int)partCount];

            string uploadId = await this.NewMultipartUploadAsync(bucketName, objectName, metaData, sseHeaders, cancellationToken).ConfigureAwait(false);

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
                byte[] dataToCopy = ReadFull(data, (int)partSize);
                if (dataToCopy == null && numPartsUploaded > 0)
                {
                    break;
                }

                if (partNumber == partCount)
                {
                    expectedReadSize = lastPartSize;
                }
                numPartsUploaded += 1;
                string etag = await this.PutObjectAsync(bucketName, objectName, uploadId, partNumber, dataToCopy, metaData, sseHeaders, cancellationToken).ConfigureAwait(false);
                totalParts[partNumber - 1] = new Part() { PartNumber = partNumber, ETag = etag, size = (long)expectedReadSize };
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
            string resourcePath = "?uploadId=" + uploadId;
            var request = await this.CreateRequest(Method.POST, bucketName,
                                                     objectName: objectName,
                                                     resourcePath: resourcePath)
                                    .ConfigureAwait(false);

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

            request.AddParameter("application/xml", body, RestSharp.ParameterType.RequestBody);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an async observable of parts corresponding to a uploadId for a specific bucket and objectName
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
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
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="uploadId"></param>
        /// <param name="partNumberMarker"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<Tuple<ListPartsResult, List<Part>>> GetListPartsAsync(string bucketName, string objectName, string uploadId, int partNumberMarker, CancellationToken cancellationToken)
        {
            var resourcePath = "?uploadId=" + uploadId;
            if (partNumberMarker > 0)
            {
                resourcePath += "&part-number-marker=" + partNumberMarker;
            }
            resourcePath += "&max-parts=1000";
            var request = await this.CreateRequest(Method.GET, bucketName,
                                                     objectName: objectName,
                                                     resourcePath: resourcePath)
                                .ConfigureAwait(false);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            ListPartsResult listPartsResult = null;
            using (var stream = new MemoryStream(contentBytes))
                listPartsResult = (ListPartsResult)(new XmlSerializer(typeof(ListPartsResult)).Deserialize(stream));

            XDocument root = XDocument.Parse(response.Content);

            var uploads = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Part")
                           select new Part()
                           {
                               PartNumber = int.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}PartNumber").Value, CultureInfo.CurrentCulture),
                               ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value.Replace("\"", ""),
                               size = long.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value, CultureInfo.CurrentCulture)
                           });

            return new Tuple<ListPartsResult, List<Part>>(listPartsResult, uploads.ToList());
        }

        /// <summary>
        /// Start a new multi-part upload request
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="metaData"></param>
        /// <param name="sseHeaders"> Server-side encryption options</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<string> NewMultipartUploadAsync(string bucketName, string objectName, Dictionary<string, string> metaData, Dictionary<string, string> sseHeaders, CancellationToken cancellationToken = default(CancellationToken))
        {
            var resource = "?uploads";

            foreach (KeyValuePair<string, string> kv in sseHeaders)
            {
                metaData.Add(kv.Key, kv.Value);
            }
            var request = await this.CreateRequest(Method.POST, bucketName, objectName: objectName,
                            headerMap: metaData, resourcePath: resource).ConfigureAwait(false);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            InitiateMultipartUploadResult newUpload = null;
            using (var stream = new MemoryStream(contentBytes))
                newUpload = (InitiateMultipartUploadResult)(new XmlSerializer(typeof(InitiateMultipartUploadResult)).Deserialize(stream));
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
        /// <param name="sseHeaders">Server-side encryption headers if any </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<string> PutObjectAsync(string bucketName, string objectName, string uploadId, int partNumber, byte[] data, Dictionary<string, string> metaData, Dictionary<string, string> sseHeaders, CancellationToken cancellationToken)
        {
            var resource = "";
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                resource += "?uploadId=" + uploadId + "&partNumber=" + partNumber;
            }
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
                                                     body: data,
                                                     resourcePath: resource)
                                    .ConfigureAwait(false);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

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
        /// Get list of multi-part uploads matching particular uploadIdMarker
        /// </summary>
        /// <param name="bucketName">bucketName</param>
        /// <param name="prefix">prefix</param>
        /// <param name="keyMarker"></param>
        /// <param name="uploadIdMarker"></param>
        /// <param name="delimiter"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<Tuple<ListMultipartUploadsResult, List<Upload>>> GetMultipartUploadsListAsync(string bucketName,
                                                                                     string prefix,
                                                                                     string keyMarker,
                                                                                     string uploadIdMarker,
                                                                                     string delimiter,
                                                                                     CancellationToken cancellationToken)
        {
            var queries = new List<string>();

            // null values are treated as empty strings.
            if (delimiter == null)
            {
                delimiter = "";
            }
            if (prefix == null)
            {
                prefix = "";
            }
            if (keyMarker == null)
            {
                keyMarker = "";
            }
            if (uploadIdMarker == null)
            {
                uploadIdMarker = "";
            }

            queries.Add("uploads");
            queries.Add("prefix=" + Uri.EscapeDataString(prefix));
            queries.Add("delimiter=" + Uri.EscapeDataString(delimiter));
            queries.Add("key-marker=" + Uri.EscapeDataString(keyMarker));
            queries.Add("upload-id-marker=" + uploadIdMarker);
            queries.Add("max-uploads=1000");

            string query = string.Join("&", queries);

            var request = await this.CreateRequest(Method.GET, bucketName, resourcePath: "?" + query).ConfigureAwait(false);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            ListMultipartUploadsResult listBucketResult = null;
            using (var stream = new MemoryStream(contentBytes))
                listBucketResult = (ListMultipartUploadsResult)(new XmlSerializer(typeof(ListMultipartUploadsResult)).Deserialize(stream));

            XDocument root = XDocument.Parse(response.Content);

            var uploads = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Upload")
                           select new Upload()
                           {
                               Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                               UploadId = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}UploadId").Value,
                               Initiated = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Initiated").Value
                           });

            return new Tuple<ListMultipartUploadsResult, List<Upload>>(listBucketResult, uploads.ToList());
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
            if (recursive)
            {
                return this.listIncompleteUploads(bucketName, prefix, null, cancellationToken);
            }
            return this.listIncompleteUploads(bucketName, prefix, "/", cancellationToken);
        }

        /// <summary>
        /// Lists all or delimited incomplete uploads in a given bucket with a given objectName
        /// </summary>
        /// <param name="bucketName">Bucket to list incomplete uploads from</param>
        /// <param name="prefix">Key of object to list incomplete uploads from</param>
        /// <param name="delimiter">delimiter of object to list incomplete uploads</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns>Observable that notifies when next next upload becomes available</returns>
        private IObservable<Upload> listIncompleteUploads(string bucketName, string prefix, string delimiter, CancellationToken cancellationToken)
        {
            return Observable.Create<Upload>(
              async obs =>
              {
                  string nextKeyMarker = null;
                  string nextUploadIdMarker = null;
                  bool isRunning = true;

                  while (isRunning)
                  {
                      var uploads = await this.GetMultipartUploadsListAsync(bucketName, prefix, nextKeyMarker, nextUploadIdMarker, delimiter, cancellationToken).ConfigureAwait(false);
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
        /// Remove incomplete uploads from a given bucket and objectName
        /// </summary>
        /// <param name="bucketName">Bucket to remove incomplete uploads from</param>
        /// <param name="objectName">Key to remove incomplete uploads from</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task RemoveIncompleteUploadAsync(string bucketName, string objectName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var uploads = await this.ListIncompleteUploads(bucketName, objectName, cancellationToken: cancellationToken).ToArray();
            foreach (Upload upload in uploads)
            {
                if (objectName == upload.Key)
                {
                    await this.RemoveUploadAsync(bucketName, objectName, upload.UploadId, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Remove object with matching uploadId from bucket
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="uploadId"></param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task RemoveUploadAsync(string bucketName, string objectName, string uploadId, CancellationToken cancellationToken)
        {
            // var resourcePath = "/" + utils.UrlEncode(objectName) + "?uploadId=" + uploadId;
            var resourcePath = "?uploadId=" + uploadId;

            var request = await this.CreateRequest(Method.DELETE, bucketName,
                                                     objectName: objectName,
                                                     resourcePath: resourcePath)
                                    .ConfigureAwait(false);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
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

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// private helper method to remove list of objects from bucket
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectsList"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<List<DeleteError>> removeObjectsAsync(string bucketName, List<DeleteObject> objectsList, CancellationToken cancellationToken)
        {
            string resource = "?delete";
            var request = await this.CreateRequest(Method.POST, bucketName, resourcePath: resource).ConfigureAwait(false);
            List<XElement> objects = new List<XElement>();

            foreach (var obj in objectsList)
            {
                objects.Add(new XElement("Object",
                                       new XElement("Key", obj.Key)));
            }

            var deleteObjectsRequest = new XElement("DeleteObject", objects,
                                        new XElement("Quiet", true));

            var bodyString = deleteObjectsRequest.ToString();

            var body = System.Text.Encoding.UTF8.GetBytes(bodyString);
            request.AddParameter("application/xml", body, RestSharp.ParameterType.RequestBody);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            DeleteObjectsResult deleteResult = null;
            using (var stream = new MemoryStream(contentBytes))
                deleteResult = (DeleteObjectsResult)(new XmlSerializer(typeof(DeleteObjectsResult)).Deserialize(stream));

            if (deleteResult == null)
            {
                return new List<DeleteError>();
            }
            return deleteResult.ErrorList();
        }

        /// <summary>
        /// Removes multiple objects from a specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to  remove objects from</param>
        /// <param name="objectNames">List of object keys to remove.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task<IObservable<DeleteError>> RemoveObjectAsync(string bucketName, IEnumerable<string> objectNames, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (objectNames == null)
            {
                return null;
            }
            utils.validateBucketName(bucketName);
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
                          utils.validateObjectName(objectName);
                          objectList.Add(new DeleteObject(objectName));
                          i++;
                          if (i % 1000 == 0)
                              break;
                      }
                      if (objectList.Count() > 0)
                      {
                          var errorsList = await removeObjectsAsync(bucketName, objectList, cancellationToken).ConfigureAwait(false);
                          foreach (DeleteError error in errorsList)
                          {
                              obs.OnNext(error);
                          }
                      }
                      if (i >= objectNames.Count())
                          process = !process;
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
        public async Task<ObjectStat> StatObjectAsync(string bucketName, string objectName, ServerSideEncryption sse = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var headerMap = new Dictionary<string, string>();

            if (sse != null && sse.GetType().Equals(EncryptionType.SSE_C))
            {
                sse.Marshal(headerMap);
            }
            var request = await this.CreateRequest(Method.HEAD, bucketName, objectName: objectName, headerMap: headerMap).ConfigureAwait(false);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            // Extract stats from response
            long size = 0;
            DateTime lastModified = new DateTime();
            string etag = "";
            string contentType = null;
            Dictionary<string, string> metaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            //Supported headers for object.
            List<string> supportedHeaders = new List<string> { "cache-control", "content-encoding", "content-type" };

            foreach (Parameter parameter in response.Headers)
            {
                if (parameter.Name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    size = long.Parse(parameter.Value.ToString());
                }
                else if (parameter.Name.Equals("Last-Modified", StringComparison.OrdinalIgnoreCase))
                {
                    lastModified = DateTime.Parse(parameter.Value.ToString(), CultureInfo.InvariantCulture);
                }
                else if (parameter.Name.Equals("ETag", StringComparison.OrdinalIgnoreCase))
                {
                    etag = parameter.Value.ToString().Replace("\"", "");
                }
                else if (parameter.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = parameter.Value.ToString();
                    metaData["Content-Type"] = contentType;
                }
                else if (supportedHeaders.Contains(parameter.Name.ToLower()) || parameter.Name.ToLower().StartsWith("x-amz-meta-"))
                {
                    metaData[parameter.Name] = parameter.Value.ToString();
                }

            }
            return new ObjectStat(objectName, size, lastModified, etag, contentType, metaData);
        }

        /// <summary>
        /// Advances in the stream upto currentPartSize or End of Stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="currentPartSize"></param>
        /// <returns>bytes read in a byte array</returns>
        internal byte[] ReadFull(Stream data, int currentPartSize)
        {
            byte[] result = new byte[currentPartSize];
            int totalRead = 0;
            while (totalRead < currentPartSize)
            {
                byte[] curData = new byte[currentPartSize - totalRead];
                int curRead = data.Read(curData, 0, currentPartSize - totalRead);
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

            if (totalRead == 0) return null;

            if (totalRead == currentPartSize) return result;

            byte[] truncatedResult = new byte[totalRead];
            for (int i = 0; i < totalRead; i++)
            {
                truncatedResult[i] = result[i];
            }
            return truncatedResult;
        }

        /// <summary>
        ///  Copy a source object into a new destination object.
        /// </summary>
        /// <param name="bucketName"> Bucket name where the object to be copied exists.</param>
        /// <param name="objectName">Object name source to be copied.</param>
        /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
        /// <param name="destObjectName">Object name to be created, if not provided uses source object name as destination object name.</param>
        /// <param name="copyConditions">optionally can take a key value CopyConditions as well for conditionally attempting copyObject.</param>
        /// <param name="metadata">Optional Object metadata to be stored. Defaults to null.</param>
        /// <param name="sseSrc"> Optional source encryption options.Defaults to null. </param>
        /// <param name="sseDest"> Optional destination encryption options.Defaults to null. </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        public async Task CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null, Dictionary<string, string> metadata = null, ServerSideEncryption sseSrc = null, ServerSideEncryption sseDest = null, CancellationToken cancellationToken = default(CancellationToken))
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
            string sourceObjectPath = bucketName + "/" + utils.UrlEncode(objectName);

            // Destination object name is optional, if empty default to source object name.
            if (destObjectName == null)
            {
                destObjectName = objectName;
            }

            ServerSideEncryption sseGet = sseSrc;
            if (sseSrc is SSECopy)
            {
                SSECopy sseCpy = (SSECopy)sseSrc;
                sseGet = sseCpy.CloneToSSEC();
            }
            // Get Stats on the source object
            ObjectStat srcStats = await this.StatObjectAsync(bucketName, objectName, sse: sseGet, cancellationToken: cancellationToken).ConfigureAwait(false);
            // Copy metadata from the source object if no metadata replace directive
            Dictionary<string, string> meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> m = metadata;
            if (copyConditions != null && !copyConditions.HasReplaceMetadataDirective())
            {
                m = srcStats.metaData;
            }

            if (m != null)
            {
                foreach (var item in m)
                {
                    meta[item.Key] = item.Value;
                }
            }

            long srcByteRangeSize = 0L;

            if (copyConditions != null)
            {
                srcByteRangeSize = copyConditions.GetByteRange();
            }
            long copySize = (srcByteRangeSize == 0) ? srcStats.Size : srcByteRangeSize;

            if ((srcByteRangeSize > srcStats.Size) ||
                ((srcByteRangeSize > 0) && (copyConditions.byteRangeEnd >= srcStats.Size)))
                throw new ArgumentException("Specified byte range (" + copyConditions.byteRangeStart.ToString() + "-" + copyConditions.byteRangeEnd.ToString() + ") does not fit within source object (size=" + srcStats.Size.ToString() + ")");

            if ((copySize > Constants.MaxSingleCopyObjectSize) ||
                    (srcByteRangeSize > 0 && (srcByteRangeSize != srcStats.Size)))
                await MultipartCopyUploadAsync(bucketName, objectName, destBucketName, destObjectName, copyConditions, copySize, meta, sseSrc, sseDest, cancellationToken).ConfigureAwait(false);
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
        ///  Create the copy request,execute it and
        /// </summary>
        /// <param name="bucketName"> Bucket name where the object to be copied exists.</param>
        /// <param name="objectName">Object name source to be copied.</param>
        /// <param name="destBucketName">Bucket name where the object will be copied to.</param>
        /// <param name="destObjectName">Object name to be created, if not provided uses source object name as destination object name.</param>
        /// <param name="copyConditions">optionally can take a key value CopyConditions as well for conditionally attempting copyObject.</param>
        /// <param name="customHeaders">optional custom header to specify byte range</param>
        /// <param name="resource"> optional string to specify upload id and part number </param>
        /// <param name="type"> type of XML serialization to be applied on the server response</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<object> CopyObjectRequestAsync(string bucketName, string objectName, string destBucketName, string destObjectName, CopyConditions copyConditions, Dictionary<string, string> customHeaders, string resource, CancellationToken cancellationToken, Type type)
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
                                                   resourcePath: resource,
                                                   headerMap: customHeaders)
                                .ConfigureAwait(false);
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

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request, cancellationToken).ConfigureAwait(false);

            // Just read the result and parse content.
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);

            object copyResult = null;
            using (var stream = new MemoryStream(contentBytes))
            {
                if (type == typeof(CopyObjectResult))
                    copyResult = (CopyObjectResult)(new XmlSerializer(typeof(CopyObjectResult)).Deserialize(stream));
                if (type == typeof(CopyPartResult))
                    copyResult = (CopyPartResult)(new XmlSerializer(typeof(CopyPartResult)).Deserialize(stream));
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
        /// <param name="metadata"> optional metadata on the destination side</param>
        /// <param name="sseSrc"> optional Server-side encryption options </param>
        /// <param name="sseDest"> optional Server-side encryption options </param>
        /// <param name="cancellationToken"> optional cancellation token</param>
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
                    partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)partSize - 1;
                else
                    partCondition.byteRangeEnd = partCondition.byteRangeStart + (long)lastPartSize - 1;
                var resource = "";
                if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
                {
                    resource += "?uploadId=" + uploadId + "&partNumber=" + partNumber;
                }
                Dictionary<string, string> customHeader = new Dictionary<string, string>();
                customHeader.Add("x-amz-copy-source-range", "bytes=" + partCondition.byteRangeStart.ToString() + "-" + partCondition.byteRangeEnd.ToString());
                if (sseSrc != null && sseSrc is SSECopy)
                {
                    sseSrc.Marshal(customHeader);
                }
                if (sseDest != null)
                {
                    sseDest.Marshal(customHeader);
                }
                CopyPartResult cpPartResult = (CopyPartResult)await this.CopyObjectRequestAsync(bucketName, objectName, destBucketName, destObjectName, copyConditions, customHeader, resource, cancellationToken, typeof(CopyPartResult)).ConfigureAwait(false);

                totalParts[partNumber - 1] = new Part() { PartNumber = partNumber, ETag = cpPartResult.ETag, size = (long)expectedReadSize };
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
            if (!utils.IsValidExpiry(expiresInt))
            {
                throw new InvalidExpiryRangeException("expiry range should be between 1 and " + Constants.DefaultExpiryTime.ToString());
            }
            var request = await this.CreateRequest(Method.GET, bucketName,
                                                    objectName: objectName,
                                                    headerMap: reqParams)
                                    .ConfigureAwait(false);

            return this.authenticator.PresignURL(this.restClient, request, expiresInt, Region, this.SessionToken, reqDate);
        }

        /// <summary>
        /// Presigned Put url -returns a presigned url to upload an object without credentials.URL can have a maximum expiry of
        /// upto 7 days or a minimum of 1 second.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        /// <returns></returns>
        public async Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)
        {
            if (!utils.IsValidExpiry(expiresInt))
            {
                throw new InvalidExpiryRangeException("expiry range should be between 1 and " + Constants.DefaultExpiryTime.ToString());
            }
            var request = await this.CreateRequest(Method.PUT, bucketName, objectName: objectName).ConfigureAwait(false);
            return this.authenticator.PresignURL(this.restClient, request, expiresInt, Region, this.SessionToken);
        }

        /// <summary>
        ///  Presigned post policy
        /// </summary>
        public async Task<Tuple<string, Dictionary<string, string>>> PresignedPostPolicyAsync(PostPolicy policy)
        {
            string region = null;

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
                region = await BucketRegionCache.Instance.Update(this, policy.Bucket).ConfigureAwait(false);
            }
            if (region == null)
            {
                region = BucketRegionCache.Instance.Region(policy.Bucket);
            }
            // Set Target URL
            Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure, bucketName: policy.Bucket, region: region, usePathStyle: false);
            SetTargetURL(requestUrl);
            DateTime signingDate = DateTime.UtcNow;

            policy.SetAlgorithm("AWS4-HMAC-SHA256");
            policy.SetCredential(this.authenticator.GetCredentialString(signingDate, region));
            policy.SetDate(signingDate);
            policy.SetSessionToken(this.SessionToken);
            string policyBase64 = policy.Base64();
            string signature = this.authenticator.PresignPostSignature(region, signingDate, policyBase64);

            policy.SetPolicy(policyBase64);
            policy.SetSignature(signature);

            return new Tuple<string, Dictionary<string, string>>(this.restClient.BaseUrl.AbsoluteUri, policy.GetFormData());
        }
    }
}
