﻿/*
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
using System.Net;
using RestSharp;

using System.Threading.Tasks;
using System.Linq;

using System.Reactive.Linq;
using Minio.DataModel;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.Exceptions;
using System.Globalization;
using Minio.Helper;

namespace Minio
{
    public partial class ClientApiOperations : IObjectOperations
    {
   
        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        public async Task GetObjectAsync(string bucketName, string objectName, Action<Stream> cb)
        {

            var request = await client.CreateRequest(Method.GET, 
                                                     bucketName,
                                                     objectName: objectName,
                                                     region: BucketRegionCache.Instance.Region(bucketName)
                                                     );
            request.ResponseWriter = cb;
            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {              
                this.client.ParseError(response);
            }
         
            return;
        }
        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="fileName">string with file path</param>
        /// <returns></returns>
        public async Task GetObjectAsync(string bucketName, string objectName, string fileName)
        {
           
            bool fileExists = File.Exists(fileName);
            utils.ValidateFile(fileName);

            ObjectStat objectStat = await StatObjectAsync(bucketName, objectName);
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
            await GetObjectAsync(bucketName, objectName, (stream) => {
                var fileStream = File.Create(tempFileName);
                stream.CopyTo(fileStream);
                fileStream.Close();
                FileInfo writtenInfo = new FileInfo(tempFileName);
                long writtenSize = writtenInfo.Length;
                if (writtenSize != length - tempFileSize)
                {
                    new IOException(tempFileName + ": unexpected data written.  expected = " + (length - tempFileSize)
                                           + ", written = " + writtenSize);
                }
                utils.MoveWithReplace(tempFileName, fileName);
            });
           
        }
        /// <summary>
        /// Creates an object from file
        /// </summary>
        /// <param name="bucketName">Bucket to create object in</param>
        /// <param name="objectName">Key of the new object</param>
        /// <param name="fileName">Path of file to upload</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        public async Task PutObjectAsync(string bucketName, string objectName, string fileName, string contentType=null)
        {
            utils.ValidateFile(fileName, contentType);
            FileInfo fileInfo = new FileInfo(fileName);
            long size = fileInfo.Length;
            using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                await PutObjectAsync(bucketName, objectName, file, size, contentType);
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
        public async Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType)
        {
            utils.validateBucketName(bucketName);
            utils.validateObjectName(objectName);
            if (data == null)
            {
                throw new ArgumentNullException("Invalid input stream,cannot be null");
            }
           
            //for sizes less than 5Mb , put a single object
            if (size < Constants.MinimumPartSize && size >= 0)
            {
                var bytes = ReadFull(data, (int)size);
                if (bytes.Length != (int)size)
                {
                    throw new UnexpectedShortReadException("Data read " + bytes.Length + " is shorter than the size " + size + " of input buffer.");
                }
                await this.PutObjectAsync(bucketName, objectName, null, 0, bytes, contentType);
                return;
            }
            // For all sizes greater than 5MiB do multipart.
      
            dynamic multiPartInfo = CalculateMultiPartSize(size);
            double partSize = multiPartInfo.partSize;
            double partCount = multiPartInfo.partCount;
            double lastPartSize = multiPartInfo.lastPartSize;
            Part[] totalParts = new Part[(int)partCount];
            Part part = null;
            Part[] existingParts = null;

            string uploadId = await this.getLatestIncompleteUploadIdAsync(bucketName, objectName);

            if (uploadId == null)
            {
                uploadId = await this.NewMultipartUploadAsync(bucketName, objectName, contentType);
            }
            else
            {
                existingParts = await this.ListParts(bucketName, objectName,uploadId).ToArray();
            }

            double expectedReadSize = partSize;
            int partNumber;
            bool skipUpload = false;
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                byte[] dataToCopy = ReadFull(data, (int)partSize);

                if (partNumber == partCount)
                {
                    expectedReadSize = lastPartSize;
                }
                if (existingParts != null && partNumber <= existingParts.Length)
                {
                    part = existingParts[partNumber - 1];
                    if (part != null && partNumber == part.PartNumber && expectedReadSize == part.partSize())
                    {
                        System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                        byte[] hash = md5.ComputeHash(dataToCopy);
                        string etag = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                        if (etag.Equals(part.ETag))
                        {
                            totalParts[partNumber - 1] = new Part() { PartNumber = part.PartNumber, ETag = part.ETag, size = part.partSize() };
                            skipUpload = true;
                          
                        }

                    }
                } else
                {
                    skipUpload = false;
                }
        
                if (!skipUpload)
                {
                    string etag = await this.PutObjectAsync(bucketName, objectName, uploadId, partNumber, dataToCopy, contentType);
                    totalParts[partNumber - 1] = new Part() { PartNumber = partNumber, ETag = etag, size = (long)expectedReadSize };
                }

            }
            Dictionary<int, string> etags = new Dictionary<int, string>();
            for (partNumber = 1; partNumber <= partCount; partNumber++)
            {
                etags[partNumber] = totalParts[partNumber-1].ETag;
            }
            await this.CompleteMultipartUploadAsync(bucketName, objectName, uploadId, etags);

        }      
        /// <summary>
        /// internal method to complete multi part upload of object to server
        /// </summary>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectName">Object to be uploaded</param>
        /// <param name="uploadId">Upload Id</param>
        /// <param name="etags">Etags</param>
        /// <returns></returns>
        private async Task CompleteMultipartUploadAsync(string bucketName, string objectName, string uploadId, Dictionary<int, string> etags)
        {

            string resourcePath = "?uploadId=" + uploadId;
            var request = await client.CreateRequest(Method.POST, bucketName,
                                                     objectName: objectName,
                                                     resourcePath:resourcePath,
                                                     region: BucketRegionCache.Instance.Region(bucketName)
                                           );
 
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

            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers,request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                return;
            }
            this.client.ParseError(response);
        }

        /// <summary>
        /// Calculate part size and number of parts required
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private Object CalculateMultiPartSize(long size)
        {
            // make sure to have enough buffer for last part, use 9999 instead of 10000
            if (size == -1)
            {
                size = Constants.MaximumStreamObjectSize;
            }
            if (size > Constants.MaxMultipartPutObjectSize)
            {
                throw new EntityTooLargeException("Your proposed upload size " + size + " exceeds the maximum allowed object size " + Constants.MaxMultipartPutObjectSize);
            }
            double partSize = (double) Math.Ceiling((decimal)size / Constants.MaxParts);
            partSize = (double)Math.Ceiling((decimal)partSize / Constants.MinimumPartSize) * Constants.MinimumPartSize;
            double partCount = (double)Math.Ceiling(size / partSize);
            double lastPartSize = size - (partCount - 1) * partSize;
            return new
            {
                partSize = partSize,
                partCount =partCount ,
                lastPartSize = lastPartSize
            };
        }
        //Returns an async observable of parts corresponding to a uploadId for a specific bucket and objectName   
        private IObservable<Part> ListParts(string bucketName, string objectName, string uploadId)
        {

            return Observable.Create<Part>(
              async obs =>
              {
                  int nextPartNumberMarker = 0;
                  bool isRunning = true;
                  while (isRunning)
                  {
                      var uploads = await this.GetListPartsAsync(bucketName, objectName, uploadId, nextPartNumberMarker);
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
        /// <returns></returns>
        private async Task<Tuple<ListPartsResult, List<Part>>> GetListPartsAsync(string bucketName, string objectName, string uploadId, int partNumberMarker)
        {
            var resourcePath = "?uploadId=" + uploadId;
            if (partNumberMarker > 0)
            {
                resourcePath += "&part-number-marker=" + partNumberMarker;
            }
            resourcePath += "&max-parts=1000";
            var request = await client.CreateRequest(Method.GET, bucketName,
                                                     objectName: objectName,
                                                     resourcePath: resourcePath,
                                                     region: BucketRegionCache.Instance.Region(bucketName)
                                           );
     
            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers,request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this.client.ParseError(response);
            }
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ListPartsResult listPartsResult = (ListPartsResult)(new XmlSerializer(typeof(ListPartsResult)).Deserialize(stream));

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
        
        //starts a multi-part upload request
        private async Task<string> NewMultipartUploadAsync(string bucketName, string objectName, string contentType)
        {
            var resource = "?uploads";
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }

            var request = await client.CreateRequest(Method.POST, bucketName, objectName: objectName,
                            contentType: contentType, resourcePath: resource,
                            region: BucketRegionCache.Instance.Region(bucketName)
                                           );
            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers,request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this.client.ParseError(response);
            }
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            InitiateMultipartUploadResult newUpload = (InitiateMultipartUploadResult)(new XmlSerializer(typeof(InitiateMultipartUploadResult)).Deserialize(stream));
            return newUpload.UploadId;
        }

        //Actual doer
        private async Task<string> PutObjectAsync(string bucketName, string objectName, string uploadId, int partNumber, byte[] data, string contentType)
        {
            var resource = "";
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                resource += "?uploadId=" + uploadId + "&partNumber=" + partNumber;
            }
            var request = await client.CreateRequest(Method.PUT, bucketName, 
                                                     objectName: objectName,
                                                     contentType: contentType,
                                                     body: data, 
                                                     resourcePath: resource,
                                                     region: BucketRegionCache.Instance.Region(bucketName)
                                           );
   
            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers,request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this.client.ParseError(response);
            }
            string etag = null;
            foreach (Parameter parameter in response.Headers)
            {
                if (parameter.Name == "ETag")
                {
                    etag = parameter.Value.ToString();
                }
            }
            return etag;
            
           
        }
        
        private async Task<Tuple<ListMultipartUploadsResult, List<Upload>>> GetMultipartUploadsListAsync(string bucketName,
                                                                                     string prefix,
                                                                                     string keyMarker,
                                                                                     string uploadIdMarker,
                                                                                     string delimiter)
        {
            var queries = new List<string>();
            queries.Add("uploads");
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

            string query = string.Join("&", queries);

            var request = await client.CreateRequest(Method.GET, bucketName,
                                                     region: BucketRegionCache.Instance.Region(bucketName),
                                                     resourcePath:"?" + query);                      
                     
            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers,request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                this.client.ParseError(response);
            }
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ListMultipartUploadsResult listBucketResult = (ListMultipartUploadsResult)(new XmlSerializer(typeof(ListMultipartUploadsResult)).Deserialize(stream));

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
        /// <param name="bucketName">Bucket to list all incomplepte uploads from</param>
        /// <param name="prefix">prefix to list all incomplete uploads</param>
        /// <param name="recursive">option to list incomplete uploads recursively</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        public  IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix="", bool recursive=true)
        {
            if (recursive)
            {
                return this.listIncompleteUploads(bucketName, prefix, null);
            }
            return this.listIncompleteUploads(bucketName, prefix, "/");
        }

    
        /// <summary>
        /// Lists all or delimited incomplete uploads in a given bucket with a given objectName
        /// </summary>
        /// <param name="bucketName">Bucket to list incomplete uploads from</param>
        /// <param name="objectName">Key of object to list incomplete uploads from</param>
        /// <param name="delimiter">delimiter of object to list incomplete uploads</param>
        /// <returns></returns>
        private IObservable<Upload> listIncompleteUploads(string bucketName, string prefix, string delimiter)
        {
            return Observable.Create<Upload>(
              async obs =>
              {
                  string nextKeyMarker = null;
                  string nextUploadIdMarker = null;
                  bool isRunning = true;

                  while (isRunning)
                  {
                      var uploads = await this.GetMultipartUploadsListAsync(bucketName, prefix, nextKeyMarker, nextUploadIdMarker, delimiter);
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
        // find uploadId of most recent unsuccessful attempt to put object
        private async Task<string> getLatestIncompleteUploadIdAsync(string bucketName, string objectName)
        {
            Upload latestUpload = null;
            var uploads  = await this.ListIncompleteUploads(bucketName, objectName).ToArray();
            foreach (Upload upload in uploads)
            {
                if (objectName == upload.Key && (latestUpload == null || latestUpload.Initiated.CompareTo(upload.Initiated) < 0))
                {
                    latestUpload = upload;

                }
            }
            if (latestUpload != null)
            {
                return latestUpload.UploadId;
            }
            else
            {
                return null;
            }

        }
        /// <summary>
        /// Remove incomplete uploads from a given bucket and objectName
        /// </summary>
        /// <param name="bucketName">Bucket to remove incomplete uploads from</param>
        /// <param name="objectName">Key to remove incomplete uploads from</param>
        public async Task RemoveIncompleteUploadAsync(string bucketName, string objectName)
        {
            var uploads = await this.ListIncompleteUploads(bucketName, objectName).ToArray();
            foreach (Upload upload in uploads)
            {
                if (objectName == upload.Key)
                {
                   await this.RemoveUploadAsync(bucketName, objectName, upload.UploadId);
                }
            }
        }
        private async Task RemoveUploadAsync(string bucketName, string objectName, string uploadId)
        {
           // var resourcePath = "/" + utils.UrlEncode(objectName) + "?uploadId=" + uploadId;
            var resourcePath = "?uploadId=" + uploadId;

            var request = await client.CreateRequest(Method.DELETE, bucketName, 
                                                     objectName: objectName,
                                                     region: BucketRegionCache.Instance.Region(bucketName),
                                                     resourcePath: resourcePath
                                           );
    
            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers,request);

            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                this.client.ParseError(response);
            }
        }
        /// <summary>
        /// Removes an object with given name in specific bucket
        /// </summary>
        /// <param name="bucketName">Bucket to list incomplete uploads from</param>
        /// <param name="objectName">Key of object to list incomplete uploads from</param>
        /// <returns></returns>
        public async Task RemoveObjectAsync(string bucketName, string objectName)
        {

            var request = await client.CreateRequest(Method.DELETE, bucketName,
                                                     objectName: objectName,
                                                     region: BucketRegionCache.Instance.Region(bucketName)
                                           );
            
            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers, request);

            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                this.client.ParseError(response);
            }
        }
        /// <summary>
        /// Tests the object's existence and returns metadata about existing objects.
        /// </summary>
        /// <param name="bucketName">Bucket to test object in</param>
        /// <param name="objectName">Name of the object to stat</param>
        /// <returns>Facts about the object</returns>
        public async Task<ObjectStat> StatObjectAsync(string bucketName, string objectName)
        {
            var request = await client.CreateRequest(Method.HEAD, bucketName,
                                                     objectName: objectName,
                                                     region: BucketRegionCache.Instance.Region(bucketName)
                                           );
            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                this.client.ParseError(response);
            }
         
            long size = 0;
            DateTime lastModified = new DateTime();
            string etag = "";
            string contentType = null;
            foreach (Parameter parameter in response.Headers)
            {
                switch (parameter.Name)
                {
                    case "Content-Length":
                        size = long.Parse(parameter.Value.ToString());
                        break;
                    case "Last-Modified":
                        lastModified = DateTime.Parse(parameter.Value.ToString());
                        break;
                    case "ETag":
                        etag = parameter.Value.ToString().Replace("\"", "");
                        break;
                    case "Content-Type":
                        contentType = parameter.Value.ToString();
                        break;
                    default:
                        break;
                }
            }
            return new ObjectStat(objectName, size, lastModified, etag, contentType);
           
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
        /// <returns></returns>
        public async Task<CopyObjectResult> CopyObjectAsync(string bucketName, string objectName, string destBucketName,string destObjectName=null,CopyConditions copyConditions=null)
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

            var path = destBucketName  + "/" + utils.UrlEncode(destObjectName);
            var request = await client.CreateRequest(Method.PUT, bucketName, 
                                                     objectName: objectName,
                                                     region: BucketRegionCache.Instance.Region(bucketName), resourcePath:path
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

            var response = await this.client.ExecuteTaskAsync(this.client.NoErrorHandlers, request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this.client.ParseError(response);
            }


            // For now ignore the copyObjectResult, just read and parse it.
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);

            CopyObjectResult copyObjectResult = (CopyObjectResult)(new XmlSerializer(typeof(CopyObjectResult)).Deserialize(stream));       
            return copyObjectResult;
        }


        /// <summary>
        /// Presigned Get url.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        public string PresignedGetObject(string bucketName, string objectName, int expiresInt)
        {
            RestRequest request = new RestRequest(bucketName + "/" + utils.UrlEncode(objectName), Method.GET);
            return this.client.authenticator.PresignURL(this.client.restClient, request, expiresInt);
        }

        /// <summary>
        /// Presigned Put url.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        public string PresignedPutObject(string bucketName, string objectName, int expiresInt)
        {
            RestRequest request = new RestRequest(bucketName + "/" + utils.UrlEncode(objectName), Method.PUT);
            return this.client.authenticator.PresignURL(this.client.restClient, request, expiresInt);
        }

        /// <summary>
        ///  Presigned post policy
        /// </summary>
        public Dictionary<string, string> PresignedPostPolicy(PostPolicy policy)
        {
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

            string region = Regions.GetRegion(this.client.restClient.BaseUrl.Host);
            DateTime signingDate = DateTime.UtcNow;

            policy.SetAlgorithm("AWS4-HMAC-SHA256");
            policy.SetCredential(this.client.authenticator.GetCredentialString(signingDate, region));
            policy.SetDate(signingDate);

            string policyBase64 = policy.Base64();
            string signature = this.client.authenticator.PresignPostSignature(region, signingDate, policyBase64);

            policy.SetPolicy(policyBase64);
            policy.SetSignature(signature);

            return policy.GetFormData();
        }
    }
}

