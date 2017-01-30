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
    class ObjectOperations : IObjectOperations
    {
        internal static readonly ApiResponseErrorHandlingDelegate NoSuchBucketHandler = (response) =>
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BucketNotFoundException();
            }
        };

        private const string RegistryAuthHeaderKey = "X-Registry-Auth";

        private readonly MinioRestClient _client;
        

        internal ObjectOperations(MinioRestClient client)
        {
            this._client = client;
        }
        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        public async Task GetObjectAsync(string bucketName, string objectName, Action<Stream> cb)
        {

            RestRequest request = new RestRequest(bucketName + "/" + utils.UrlEncode(objectName), Method.GET);
            request.ResponseWriter = cb;
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
              
                this._client.ParseError(response);
            }
         
            return;
        }
        /// <summary>
        /// Creates an object
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

        private async Task CompleteMultipartUploadAsync(string bucketName, string objectName, string uploadId, Dictionary<int, string> etags)
        {
            var path = bucketName + "/" + utils.UrlEncode(objectName) + "?uploadId=" + uploadId;
            var request = new RestRequest(path, Method.POST);

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

            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                return;
            }
            this._client.ParseError(response);
        }
        // Calculate part size and number of parts required
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

        private async Task<Tuple<ListPartsResult, List<Part>>> GetListPartsAsync(string bucketName, string objectName, string uploadId, int partNumberMarker)
        {
            var path = bucketName + "/" + utils.UrlEncode(objectName) + "?uploadId=" + uploadId;
            if (partNumberMarker > 0)
            {
                path += "&part-number-marker=" + partNumberMarker;
            }
            path += "&max-parts=1000";
            var request = new RestRequest(path, Method.GET);
            Console.Out.WriteLine(request.Resource);

            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this._client.ParseError(response);
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
            var path = bucketName + "/" + utils.UrlEncode(objectName) + "?uploads";
            var request = new RestRequest(path, Method.POST);
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }
            request.AddHeader("Content-Type", contentType);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this._client.ParseError(response);
            }
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            InitiateMultipartUploadResult newUpload = (InitiateMultipartUploadResult)(new XmlSerializer(typeof(InitiateMultipartUploadResult)).Deserialize(stream));
            return newUpload.UploadId;
        }
        //Actual doer
        private async Task<string> PutObjectAsync(string bucketName, string objectName, string uploadId, int partNumber, byte[] data, string contentType)
        {
            var path = bucketName + "/" + utils.UrlEncode(objectName);
            if (!string.IsNullOrEmpty(uploadId) && partNumber > 0)
            {
                path += "?uploadId=" + uploadId + "&partNumber=" + partNumber;
            }
            var request = new RestRequest(path, Method.PUT);
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }

            request.AddHeader("Content-Type", contentType);
            request.AddParameter(contentType, data, RestSharp.ParameterType.RequestBody);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this._client.ParseError(response);
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
            string path = bucketName;
            path += "?" + query;

            var request = new RestRequest(path, Method.GET);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                this._client.ParseError(response);
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
        /// <param name="prefix">prefix to list all incomplepte uploads</param>
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
            var path = bucketName + "/" + utils.UrlEncode(objectName) + "?uploadId=" + uploadId;
            var request = new RestRequest(path, Method.DELETE);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);

            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                this._client.ParseError(response);
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
            var request = new RestRequest(bucketName + "/" + utils.UrlEncode(objectName), Method.DELETE);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);


            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                this._client.ParseError(response);
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
            var request = new RestRequest(bucketName + "/" + utils.UrlEncode(objectName), Method.HEAD);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                this._client.ParseError(response);
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
        /**
         * Copy a source object into a new destination object.
         *
         * @param bucketName
         *          Bucket name where the object to be copied exists.
         * @param objectName
         *          Object name source to be copied.
         * @param destBucketName
         *          Bucket name where the object will be copied to.
         * @param destObjectName
         *          Object name to be created, if not provided uses source object name
         *          as destination object name.
         * @param optionally can take a key value CopyConditions as well for conditionally attempting
         * copyObject.
         */
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
            var request = new RestRequest(path, Method.PUT);
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

            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                this._client.ParseError(response);
            }


            // For now ignore the copyObjectResult, just read and parse it.
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);

            CopyObjectResult copyObjectResult = (CopyObjectResult)(new XmlSerializer(typeof(CopyObjectResult)).Deserialize(stream));       
            return copyObjectResult;
        }
   
   
    }
}
