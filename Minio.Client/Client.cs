/*
 * Minimal Object Storage Library, (C) 2015 Minio, Inc.
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
using System.Linq;
using RestSharp;
using System.IO;
using Minio.Client.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using Minio.Client.Errors;

namespace Minio.Client
{
    public class Client
    {
        private static int PART_SIZE = 5 * 1024 * 1024;

        private RestClient client;
        private string region;

        private string SystemUserAgent = "minio-dotnet/0.0.1 (Windows 8.1; x86_64)";
        private string CustomUserAgent = "";

        private string FullUserAgent
        {
            get
            {
                return SystemUserAgent + " " + CustomUserAgent;
            }
        }


        internal Client(Uri uri, string accessKey, string secretKey)
        {
            this.client = new RestClient(uri);
            this.region = Regions.GetRegion(uri.Host);
            this.client.UserAgent = this.FullUserAgent;
            if (accessKey != null && secretKey != null)
            {
                this.client.Authenticator = new V4Authenticator(accessKey, secretKey);
            }
        }
        /// <summary>
        /// Creates and returns an object storage client.
        /// </summary>
        /// <param name="uri">Location of the server, supports HTTP and HTTPS.</param>
        /// <returns>Object Storage Client with the uri set as the server location.</returns>
        public static Client GetClient(Uri uri)
        {
            return GetClient(uri, null, null);
        }
        /// <summary>
        /// Creates and returns an object storage client
        /// </summary>
        /// <param name="uri">Location of the server, supports HTTP and HTTPS</param>
        /// <param name="accessKey">Access Key for authenticated requests</param>
        /// <param name="secretKey">Secret Key for authenticated requests</param>
        /// <returns>Object Storage Client with the uri set as the server location and authentication parameters set.</returns>
        public static Client GetClient(Uri uri, string accessKey, string secretKey)
        {
            if (uri == null)
            {
                throw new NullReferenceException();
            }

            if (!(uri.Scheme == "http" || uri.Scheme == "https"))
            {
                throw new UriFormatException("Expecting http or https");
            }

            if (uri.Query.Length != 0)
            {
                throw new UriFormatException("Expecting no query");
            }

            if (uri.AbsolutePath.Length == 0 || (uri.AbsolutePath.Length == 1 && uri.AbsolutePath[0] == '/'))
            {
                String path = uri.Scheme + "://" + uri.Host + ":" + uri.Port + "/";
                return new Client(new Uri(path), accessKey, secretKey);
            }
            throw new UriFormatException("Expecting AbsolutePath to be empty");
        }

        /// <summary>
        /// Creates and returns an object storage client
        /// </summary>
        /// <param name="uri">Location of the server, supports HTTP and HTTPS</param>
        /// <returns>Object Storage Client with the uri set as the server location and authentication parameters set.</returns>
        public static Client GetClient(string url)
        {
            return GetClient(url, null, null);
        }

        /// <summary>
        /// Creates and returns an object storage client
        /// </summary>
        /// <param name="uri">Location of the server, supports HTTP and HTTPS</param>
        /// <param name="accessKey">Access Key for authenticated requests</param>
        /// <param name="secretKey">Secret Key for authenticated requests</param>
        /// <returns>Object Storage Client with the uri set as the server location and authentication parameters set.</returns>
        public static Client GetClient(string url, string accessKey, string secretKey)
        {
            Uri uri = new Uri(url);
            return GetClient(uri, accessKey, secretKey);
        }

        public void SetUserAgent(string product, string version, IEnumerable<string> attributes)
        {
            if (string.IsNullOrEmpty(product))
            {
                throw new ArgumentException("product cannot be null or empty");
            }
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException("version cannot be null or empty");
            }
            string customAgent = product + "/" + version;
            string[] attributesArray = attributes.ToArray();
            if (attributes.Count() > 0)
            {
                customAgent += "(";
                customAgent += string.Join("; ", attributesArray);
                customAgent += ")";
            }
            this.CustomUserAgent = customAgent;
            this.client.UserAgent = this.FullUserAgent;
            this.client.FollowRedirects = false;
        }

        /// <summary>
        /// Returns true if the specified bucket exists, otherwise returns false.
        /// </summary>
        /// <param name="bucket">Bucket to test existence of</param>
        /// <returns>true if exists and user has access</returns>
        public bool BucketExists(string bucket)
        {
            var request = new RestRequest(bucket, Method.HEAD);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            var ex = ParseError(response);
            if (ex.GetType() == typeof(BucketNotFoundException))
            {
                return false;
            }
            throw ex;
        }

        /// <summary>
        /// Create a bucket with a given name and canned ACL
        /// </summary>
        /// <param name="bucket">Name of the new bucket</param>
        /// <param name="acl">Canned ACL to set</param>
        public void MakeBucket(string bucket, Acl acl)
        {
            var request = new RestRequest("/" + bucket, Method.PUT);

            CreateBucketConfiguration config = new CreateBucketConfiguration()
            {
                LocationConstraint = this.region
            };

            request.AddHeader("x-amz-acl", acl.ToString());

            request.AddBody(config);

            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            throw ParseError(response);
        }

        /// <summary>
        /// Create a private bucket with a give name.
        /// </summary>
        /// <param name="bucket">Name of the new bucket</param>
        public void MakeBucket(string bucket)
        {
            this.MakeBucket(bucket, Acl.Private);
        }

        /// <summary>
        /// Remove a bucket
        /// </summary>
        /// <param name="bucket">Name of bucket to remove</param>
        public void RemoveBucket(string bucket)
        {
            var request = new RestRequest(bucket, Method.DELETE);
            var response = client.Execute(request);

            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                throw ParseError(response);
            }
        }

        /// <summary>
        /// Remove an object
        /// </summary>
        /// <param name="bucket">Name of bucket to remove</param>
        /// <param name="key">Name of object to remove</param>
        public void RemoveObject(string bucket, string key)
        {
            var request = new RestRequest(bucket + "/" + UrlEncode(key), Method.DELETE);
            var response = client.Execute(request);

            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                throw ParseError(response);
            }
        }

        /// <summary>
        /// Get bucket ACL
        /// </summary>
        /// <param name="bucket">NAme of bucket to retrieve canned ACL</param>
        /// <returns>Canned ACL</returns>
        public Acl GetBucketAcl(string bucket)
        {
            var request = new RestRequest(bucket + "?acl", Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = StripXmlnsXsi(response.Content);
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
                var stream = new MemoryStream(contentBytes);
                AccessControlPolicy bucketList = (AccessControlPolicy)(new XmlSerializer(typeof(AccessControlPolicy)).Deserialize(stream));

                bool publicRead = false;
                bool publicWrite = false;
                bool authenticatedRead = false;
                foreach (var x in bucketList.Grants)
                {
                    if ("http://acs.amazonaws.com/groups/global/AllUsers".Equals(x.Grantee.URI) && x.Permission.Equals("READ"))
                    {
                        publicRead = true;
                    }
                    if ("http://acs.amazonaws.com/groups/global/AllUsers".Equals(x.Grantee.URI) && x.Permission.Equals("WRITE"))
                    {
                        publicWrite = true;
                    }
                    if ("http://acs.amazonaws.com/groups/global/AuthenticatedUsers".Equals(x.Grantee.URI) && x.Permission.Equals("READ"))
                    {
                        authenticatedRead = true;
                    }
                }
                if (publicRead && publicWrite && !authenticatedRead)
                {
                    return Acl.PublicReadWrite;
                }
                if (publicRead && !publicWrite && !authenticatedRead)
                {
                    return Acl.PublicRead;
                }
                if (!publicRead && !publicWrite && authenticatedRead)
                {
                    return Acl.AuthenticatedRead;
                }
                return Acl.Private;
            }
            throw ParseError(response);
        }

        /// <summary>
        /// Set a bucket's canned ACL
        /// </summary>
        /// <param name="bucket">Name of bucket to set canned ACL</param>
        /// <param name="acl">Canned ACL to set</param>
        public void SetBucketAcl(string bucket, Acl acl)
        {
            var request = new RestRequest(bucket + "?acl", Method.PUT);
            request.AddHeader("x-amz-acl", acl.ToString());
            var response = client.Execute(request);
            if (!HttpStatusCode.OK.Equals(response.StatusCode))
            {
                throw ParseError(response);
            }
        }

        /// <summary>
        /// Lists all buckets owned by the user
        /// </summary>
        /// <returns>A list of all buckets owned by the user.</returns>
        public List<Bucket> ListBuckets()
        {
            var request = new RestRequest("/", Method.GET);
            var response = client.Execute(request);

            if (HttpStatusCode.OK.Equals(response.StatusCode))
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                var stream = new MemoryStream(contentBytes);
                ListAllMyBucketsResult bucketList = (ListAllMyBucketsResult)(new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream));
                return bucketList.Buckets;
            }

            throw ParseError(response);
        }

        /// <summary>
        /// Get an object. The object will be streamed to the callback given by the user.
        /// </summary>
        /// <param name="bucket">Bucket to retrieve object from</param>
        /// <param name="key">Key of object to retrieve</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        public void GetObject(string bucket, string key, Action<Stream> callback)
        {
            RestRequest request = new RestRequest(bucket + "/" + UrlEncode(key), Method.GET);
            request.ResponseWriter = callback;
            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }
            throw ParseError(response);
        }

        /// <summary>
        /// Get an object starting with the byte specified in offset. The object will be streamed to the callback given by the user
        /// </summary>
        /// <param name="bucket">Bucket to retrieve object from</param>
        /// <param name="key">Key of object to retrieve</param>
        /// <param name="offset">Number of bytes to skip</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        public void GetPartialObject(string bucket, string key, ulong offset, Action<Stream> callback)
        {
            var stat = this.StatObject(bucket, key);
            RestRequest request = new RestRequest(bucket + "/" + UrlEncode(key), Method.GET);
            request.AddHeader("Range", "bytes=" + offset + "-" + (stat.Size - 1));
            request.ResponseWriter = callback;
            client.Execute(request);
            // response status code is 0, bug in upstream library, cannot rely on it for errors with PartialContent
        }

        /// <summary>
        /// Get a byte range of an object given by the offset and length. The object will be streamed to the callback given by the user
        /// </summary>
        /// <param name="bucket">Bucket to retrieve object from</param>
        /// <param name="key">Key of object to retrieve</param>
        /// <param name="offset">Number of bytes to skip</param>
        /// <param name="length">Length of requested byte range</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        public void GetPartialObject(string bucket, string key, ulong offset, ulong length, Action<Stream> callback)
        {
            RestRequest request = new RestRequest(bucket + "/" + UrlEncode(key), Method.GET);
            request.AddHeader("Range", "bytes=" + offset + "-" + (offset + length - 1));
            request.ResponseWriter = callback;
            client.Execute(request);
            // response status code is 0, bug in upstream library, cannot rely on it for errors with PartialContent
        }

        /// <summary>
        /// Tests the object's existence and returns metadata about existing objects.
        /// </summary>
        /// <param name="bucket">Bucket to test object in</param>
        /// <param name="key">Key of object to stat</param>
        /// <returns>Facts about the object</returns>
        public ObjectStat StatObject(string bucket, string key)
        {
            var request = new RestRequest(bucket + "/" + UrlEncode(key), Method.HEAD);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                long size = 0;
                DateTime lastModified = new DateTime();
                string etag = "";
                string contentType = null;
                foreach (Parameter parameter in response.Headers)
                {
                    if (parameter.Name == "Content-Length")
                    {
                        size = long.Parse(parameter.Value.ToString());
                    }
                    if (parameter.Name == "Last-Modified")
                    {
                        DateTime.Parse(parameter.Value.ToString());
                    }
                    if (parameter.Name == "ETag")
                    {
                        etag = parameter.Value.ToString().Replace("\"", "");
                    }
                    if (parameter.Name == "Content-Type")
                    {
                        contentType = parameter.Value.ToString();
                    }
                }
                return new ObjectStat(key, size, lastModified, etag, contentType);
            }
            ClientException ex = ParseError(response);
            if (ex.GetType() == typeof(ObjectNotFoundException))
            {
                if (!this.BucketExists(bucket))
                {
                    var bnfe = new BucketNotFoundException();
                    bnfe.Response = ex.Response;
                    throw bnfe;
                }
            }
            throw ex;
        }

        /// <summary>
        /// Creates an object
        /// </summary>
        /// <param name="bucket">Bucket to create object in</param>
        /// <param name="key">Key of the new object</param>
        /// <param name="size">Total size of bytes to be written, must match with data's length</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        /// <param name="data">Stream of bytes to send</param>
        public void PutObject(string bucket, string key, long size, string contentType, Stream data)
        {
            if (size <= Client.PART_SIZE)
            {
                var bytes = ReadFull(data, (int)size);
                if (data.ReadByte() > 0)
                {
                    throw new DataSizeMismatchException();
                }
                if (bytes.Length != (int)size)
                {
                    throw new DataSizeMismatchException()
                    {
                        Bucket = bucket,
                        Key = key,
                        UserSpecifiedSize = size,
                        ActualReadSize = bytes.Length
                    };
                }
                this.DoPutObject(bucket, key, null, 0, contentType, bytes);
            }
            else
            {
                var partSize = CalculatePartSize(size);
                var uploads = this.ListAllIncompleteUploads(bucket, key);
                string uploadId = null;
                Dictionary<int, string> etags = new Dictionary<int, string>();
                if (uploads.Count() > 0)
                {
                    uploadId = uploads.Last().UploadId;
                    var parts = this.ListParts(bucket, key, uploadId);
                    foreach (Part part in parts)
                    {
                        etags[part.PartNumber] = part.ETag;
                    }
                }
                if (uploadId == null)
                {
                    uploadId = this.NewMultipartUpload(bucket, key, contentType);
                }
                int partNumber = 0;
                long totalWritten = 0;
                while (totalWritten < size)
                {
                    partNumber++;
                    var currentPartSize = (int)Math.Min((long)partSize, (size - totalWritten));
                    byte[] dataToCopy = ReadFull(data, currentPartSize);
                    if (dataToCopy == null)
                    {
                        break;
                    }
                    System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                    byte[] hash = md5.ComputeHash(dataToCopy);
                    string etag = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                    if (!etags.ContainsKey(partNumber) || !etags[partNumber].Equals(etag))
                    {
                        etag = DoPutObject(bucket, key, uploadId, partNumber, contentType, dataToCopy);
                    }
                    etags[partNumber] = etag;
                    totalWritten += dataToCopy.Length;
                }

                // test if any more data is on the stream
                if (data.ReadByte() != -1)
                {
                    throw new DataSizeMismatchException()
                    {
                        Bucket = bucket,
                        Key = key,
                        UserSpecifiedSize = size,
                        ActualReadSize = totalWritten + 1
                    };
                }

                if (totalWritten != size)
                {
                    throw new DataSizeMismatchException()
                    {
                        Bucket = bucket,
                        Key = key,
                        UserSpecifiedSize = size,
                        ActualReadSize = totalWritten
                    };
                }

                foreach (int curPartNumber in etags.Keys)
                {
                    if (curPartNumber > partNumber)
                    {
                        etags.Remove(curPartNumber);
                    }
                }

                this.CompleteMultipartUpload(bucket, key, uploadId, etags);
            }
        }

        private byte[] ReadFull(Stream data, int currentPartSize)
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

        private void CompleteMultipartUpload(string bucket, string key, string uploadId, Dictionary<int, string> etags)
        {
            var path = bucket + "/" + UrlEncode(key) + "?uploadId=" + uploadId;
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

            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                return;
            }
            throw ParseError(response);
        }

        private int CalculatePartSize(long size)
        {
            int minimumPartSize = PART_SIZE; // 5MB
            int partSize = (int)(size / 9999); // using 10000 may cause part size to become too small, and not fit the entire object in
            return Math.Max(minimumPartSize, partSize);
        }

        private IEnumerable<Part> ListParts(string bucket, string key, string uploadId)
        {
            int nextPartNumberMarker = 0;
            bool isRunning = true;
            while (isRunning)
            {
                var uploads = GetListParts(bucket, key, uploadId, nextPartNumberMarker);
                foreach (Part part in uploads.Item2)
                {
                    yield return part;
                }
                nextPartNumberMarker = uploads.Item1.NextPartNumberMarker;
                isRunning = uploads.Item1.IsTruncated;
            }
        }

        private Tuple<ListPartsResult, List<Part>> GetListParts(string bucket, string key, string uploadId, int partNumberMarker)
        {
            var path = bucket + "/" + UrlEncode(key) + "?uploadId=" + uploadId;
            if(partNumberMarker > 0) {
                path += "&part-number-marker=" + partNumberMarker;
            }
            path += "&max-parts=1000";
            var request = new RestRequest(path, Method.GET);
            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                var stream = new MemoryStream(contentBytes);
                ListPartsResult listPartsResult = (ListPartsResult)(new XmlSerializer(typeof(ListPartsResult)).Deserialize(stream));

                XDocument root = XDocument.Parse(response.Content);

                var uploads = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Part")
                               select new Part()
                               {
                                   PartNumber = int.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}PartNumber").Value),
                                   ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value.Replace("\"", "")
                               });

                return new Tuple<ListPartsResult, List<Part>>(listPartsResult, uploads.ToList());
            }
            throw ParseError(response);
        }

        private string NewMultipartUpload(string bucket, string key, string contentType)
        {
            var path = bucket + "/" + UrlEncode(key) + "?uploads";
            var request = new RestRequest(path, Method.POST);
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }
            request.AddHeader("Content-Type", contentType);
            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                var stream = new MemoryStream(contentBytes);
                InitiateMultipartUploadResult newUpload = (InitiateMultipartUploadResult)(new XmlSerializer(typeof(InitiateMultipartUploadResult)).Deserialize(stream));
                return newUpload.UploadId;
            }
            throw ParseError(response);
        }

        private string DoPutObject(string bucket, string key, string uploadId, int partNumber, string contentType, byte[] data)
        {
            var path = bucket + "/" + UrlEncode(key);
            if (uploadId != null)
            {
                path += "?uploadId=" + uploadId + "&partNumber=" + partNumber;
            }
            var request = new RestRequest(path, Method.PUT);
            if (contentType == null)
            {
                contentType = "application/octet-stream";
            }



            request.AddHeader("Content-Type", contentType);
            request.AddParameter(contentType, data, RestSharp.ParameterType.RequestBody);
            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
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
            throw ParseError(response);
        }

        private ClientException ParseError(IRestResponse response)
        {
            if (response == null)
            {
                return new ConnectionException();
            }
            if (HttpStatusCode.Redirect.Equals(response.StatusCode) || HttpStatusCode.TemporaryRedirect.Equals(response.StatusCode) || HttpStatusCode.MovedPermanently.Equals(response.StatusCode))
            {
                return new RedirectionException();
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                if (HttpStatusCode.Forbidden.Equals(response.StatusCode) || HttpStatusCode.NotFound.Equals(response.StatusCode) ||
                    HttpStatusCode.MethodNotAllowed.Equals(response.StatusCode) || HttpStatusCode.NotImplemented.Equals(response.StatusCode))
                {
                    ClientException e = null;
                    ErrorResponse errorResponse = new ErrorResponse();

                    foreach (Parameter parameter in response.Headers)
                    {
                        if (parameter.Name.Equals("x-amz-id-2", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.XAmzID2 = parameter.Value.ToString();
                        }
                        if (parameter.Name.Equals("x-amz-request-id", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.RequestID = parameter.Value.ToString();
                        }
                    }

                    errorResponse.Resource = response.Request.Resource;

                    if (HttpStatusCode.NotFound.Equals(response.StatusCode))
                    {
                        int pathLength = response.Request.Resource.Split('/').Count();
                        if (pathLength > 1)
                        {
                            errorResponse.Code = "NoSuchKey";
                            e = new ObjectNotFoundException();
                        }
                        else if (pathLength == 1)
                        {
                            errorResponse.Code = "NoSuchBucket";
                            e = new BucketNotFoundException();
                        }
                        else
                        {
                            e = new InternalClientException("404 without body resulted in path with less than two components");
                        }
                    }
                    else if (HttpStatusCode.Forbidden.Equals(response.StatusCode))
                    {
                        errorResponse.Code = "Forbidden";
                        e = new AccessDeniedException();
                    }
                    else if (HttpStatusCode.MethodNotAllowed.Equals(response.StatusCode))
                    {
                        errorResponse.Code = "MethodNotAllowed";
                        e = new MethodNotAllowedException();
                    }
                    else
                    {
                        errorResponse.Code = "MethodNotAllowed";
                        e = new MethodNotAllowedException();
                    }
                    e.Response = errorResponse;
                    return e;
                }
                throw new InternalClientException("Unsuccessful response from server without XML error: " + response.StatusCode);
            }

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ErrorResponse errResponse = (ErrorResponse)(new XmlSerializer(typeof(ErrorResponse)).Deserialize(stream));
            string code = errResponse.Code;

            ClientException clientException;

            if ("NoSuchBucket".Equals(code)) clientException = new BucketNotFoundException();
            else if ("NoSuchKey".Equals(code)) clientException = new ObjectNotFoundException();
            else if ("InvalidBucketName".Equals(code)) clientException = new InvalidKeyNameException();
            else if ("InvalidObjectName".Equals(code)) clientException = new InvalidKeyNameException();
            else if ("AccessDenied".Equals(code)) clientException = new AccessDeniedException();
            else if ("InvalidAccessKeyId".Equals(code)) clientException = new AccessDeniedException();
            else if ("BucketAlreadyExists".Equals(code)) clientException = new BucketExistsException();
            else if ("ObjectAlreadyExists".Equals(code)) clientException = new ObjectExistsException();
            else if ("InternalError".Equals(code)) clientException = new InternalServerException();
            else if ("KeyTooLong".Equals(code)) clientException = new InvalidKeyNameException();
            else if ("TooManyBuckets".Equals(code)) clientException = new MaxBucketsReachedException();
            else if ("PermanentRedirect".Equals(code)) clientException = new RedirectionException();
            else if ("MethodNotAllowed".Equals(code)) clientException = new ObjectExistsException();
            else if ("BucketAlreadyOwnedByYou".Equals(code)) clientException = new BucketExistsException();
            else clientException = new InternalClientException(errResponse.ToString());


            clientException.Response = errResponse;
            clientException.XmlError = response.Content;
            return clientException;
        }

        private string StripXmlnsXsi(string input)
        {
            string result = input.Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"CanonicalUser\"", "");
            result = result.Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"Group\"", "");
            return result;
        }

        /// <summary>
        /// List all objects in a bucket
        /// </summary>
        /// <param name="bucket">Bucket to list objects from</param>
        /// <returns>An iterator lazily populated with objects</returns>
        public IEnumerable<Item> ListObjects(string bucket)
        {
            return this.ListObjects(bucket, null, true);
        }

        /// <summary>
        /// List all objects in a bucket with a given prefix
        /// </summary>
        /// <param name="bucket">BUcket to list objects from</param>
        /// <param name="prefix">Filters all objects not beginning with a given prefix</param>
        /// <returns>An iterator lazily populated with objects</returns>
        public IEnumerable<Item> ListObjects(string bucket, string prefix)
        {
            return this.ListObjects(bucket, prefix, true);
        }

        /// <summary>
        /// List all objects non-recursively in a bucket with a given prefix, optionally emulating a directory
        /// </summary>
        /// <param name="bucket">Bucket to list objects from</param>
        /// <param name="prefix">Filters all objects not beginning with a given prefix</param>
        /// <param name="recursive">Set to false to emulate a directory</param>
        /// <returns>A iterator lazily populated with objects</returns>
        public IEnumerable<Item> ListObjects(string bucket, string prefix, bool recursive)
        {
            bool isRunning = true;

            string marker = null;

            while (isRunning)
            {
                Tuple<ListBucketResult, List<Item>> result = GetObjectList(bucket, prefix, recursive, marker);
                Item lastItem = null;
                foreach (Item item in result.Item2)
                {
                    lastItem = item;
                    yield return item;
                }
                if (result.Item1.NextMarker != null)
                {
                    marker = result.Item1.NextMarker;
                }
                else
                {
                    marker = lastItem.Key;
                }
                isRunning = result.Item1.IsTruncated;
            }
        }

        private Tuple<ListBucketResult, List<Item>> GetObjectList(string bucket, string prefix, bool recursive, string marker)
        {
            var queries = new List<string>();
            if (!recursive)
            {
                queries.Add("delimiter=%2F");
            }
            if (prefix != null)
            {
                queries.Add("prefix=" + Uri.EscapeDataString(prefix));
            }
            if (marker != null)
            {
                queries.Add("marker=" + Uri.EscapeDataString(marker));
            }
            queries.Add("max-keys=1000");
            string query = string.Join("&", queries);

            string path = bucket;
            if (query.Length > 0)
            {
                path += "?" + query;
            }
            var request = new RestRequest(path, Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                var stream = new MemoryStream(contentBytes);
                ListBucketResult listBucketResult = (ListBucketResult)(new XmlSerializer(typeof(ListBucketResult)).Deserialize(stream));

                XDocument root = XDocument.Parse(response.Content);

                var items = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Contents")
                             select new Item()
                             {
                                 Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                                 LastModified = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}LastModified").Value,
                                 ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value,
                                 Size = UInt64.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value),
                                 IsDir = false
                             });

                var prefixes = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}CommonPrefixes")
                                select new Item()
                                {
                                    Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Prefix").Value,
                                    IsDir = true
                                });

                items = items.Concat(prefixes);

                return new Tuple<ListBucketResult, List<Item>>(listBucketResult, items.ToList());
            }
            throw ParseError(response);
        }

        private Tuple<ListMultipartUploadsResult, List<Upload>> GetMultipartUploadsList(string bucket, string prefix, string keyMarker, string uploadIdMarker)
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
            queries.Add("max-uploads=1000");

            string query = string.Join("&", queries);
            string path = bucket;
            path += "?" + query;

            var request = new RestRequest(path, Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
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
            throw ParseError(response);
        }

        /// <summary>
        /// Lists all incomplete uploads in a given bucket
        /// </summary>
        /// <param name="bucket">Bucket to list all incomplepte uploads from</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        public IEnumerable<Upload> ListAllIncompleteUploads(string bucket)
        {
            return this.ListAllIncompleteUploads(bucket, null);
        }

        /// <summary>
        /// Lists all incomplete uploads in a given bucket with a given key
        /// </summary>
        /// <param name="bucket">Bucket to list incomplete uploads from</param>
        /// <param name="key">Key of object to list incomplete uploads from</param>
        /// <returns></returns>
        public IEnumerable<Upload> ListAllIncompleteUploads(string bucket, string key)
        {
            string nextKeyMarker = null;
            string nextUploadIdMarker = null;
            bool isRunning = true;
            while (isRunning)
            {
                var uploads = GetMultipartUploadsList(bucket, key, nextKeyMarker, nextUploadIdMarker);
                foreach (Upload upload in uploads.Item2)
                {
                    if (key != null && !key.Equals(upload.Key))
                    {
                        continue;
                    }
                    yield return upload;
                }
                nextKeyMarker = uploads.Item1.NextKeyMarker;
                nextUploadIdMarker = uploads.Item1.NextUploadIdMarker;
                isRunning = uploads.Item1.IsTruncated;
            }
        }

        /// <summary>
        /// Drop incomplete uploads from a given bucket and key
        /// </summary>
        /// <param name="bucket">Bucket to drop incomplete uploads from</param>
        /// <param name="key">Key to drop incomplete uploads from</param>
        public void DropIncompleteUpload(string bucket, string key)
        {
            var uploads = this.ListAllIncompleteUploads(bucket, key);
            foreach (Upload upload in uploads)
            {
                this.DropUpload(bucket, key, upload.UploadId);
            }
        }

        /// <summary>
        /// Drops all incomplete uploads from a given bucket
        /// </summary>
        /// <param name="bucket">Bucket to drop all incomplete uploads from</param>
        public void DropAllIncompleteUploads(string bucket)
        {
            var uploads = this.ListAllIncompleteUploads(bucket);
            foreach (Upload upload in uploads)
            {
                this.DropUpload(bucket, upload.Key, upload.UploadId);
            }
        }

        private void DropUpload(string bucket, string key, string uploadId)
        {
            var path = bucket + "/" + UrlEncode(key) + "?uploadId=" + uploadId;
            var request = new RestRequest(path, Method.DELETE);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return;
            }
            throw ParseError(response);
        }

        private string UrlEncode(string input)
        {
            return Uri.EscapeDataString(input).Replace("%2F", "/");
        }
    }
}
