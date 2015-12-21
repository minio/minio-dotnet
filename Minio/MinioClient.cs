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
using System.Text.RegularExpressions;
using System.Net;
using System.Linq;
using RestSharp;
using System.IO;
using Minio.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using Minio.Errors;
using System.Globalization;

namespace Minio
{
    public class MinioClient
    {
        // Maximum number of parts.
        private static int maxParts = 10000;
        // Minimum part size.
        private static long minimumPartSize = 5 * 1024L * 1024L;
        // Maximum part size.
        private static long maximumPartSize = 5 * 1024L * 1024L * 1024L;
        // Maximum streaming object size.
        private static long maximumStreamObjectSize = maxParts * minimumPartSize;

        private RestClient client;
        private Minio.V4Authenticator authenticator;

        private static string SystemUserAgent
        {
            get
            {
                string userAgent = "Minio";
                userAgent += " (" + System.Environment.OSVersion.ToString() + "; ";
                string arch = "";
                if (System.Environment.Is64BitOperatingSystem)
                {
                    arch = "x86_64";
                }
                else
                {
                    arch = "x86";
                }
                return userAgent + arch + ") minio-dotnet/0.2.1";;
            }
        }
        private string CustomUserAgent = "";

        private string FullUserAgent
        {
            get
            {
                return SystemUserAgent + " " + CustomUserAgent;
            }
        }

        /// <summary>
        /// Creates and returns an Cloud Storage client
        /// </summary>
        /// <param name="url">Location of the server, supports HTTP and HTTPS</param>
        /// <returns>Client with the uri set as the server location and authentication parameters set.</returns>
        public MinioClient(string endpoint)
            : this(endpoint, 0, null, null, false)
        {
        }
        /// <summary>
        /// Creates and returns an Cloud Storage client.
        /// </summary>
        /// <param name="uri">Location of the server, supports HTTP and HTTPS.</param>
        /// <returns>Client with the uri set as the server location.</returns>
        public MinioClient(Uri uri)
            : this(uri.ToString(), 0, null, null, false)
        {
        }

        /// <summary>
        /// Creates and returns an Cloud Storage client
        /// </summary>
        /// <param name="url">Location of the server, supports HTTP and HTTPS</param>
        /// <param name="accessKey">Access Key for authenticated requests</param>
        /// <param name="secretKey">Secret Key for authenticated requests</param>
        /// <returns>Client with the uri set as the server location and authentication parameters set.</returns>
        public MinioClient(string url, string accessKey, string secretKey)
            : this(url, 0, accessKey, secretKey, false)
        {
        }

        public MinioClient(Uri uri, string accessKey, string secretKey)
            : this(uri.ToString(), 0, accessKey, secretKey, false)
        {
        }

        public MinioClient(string endpoint, int port, string accessKey, string secretKey)
            : this(endpoint, port, accessKey, secretKey, false)
        {
        }

        public MinioClient(string endpoint, string accessKey, string secretKey, bool insecure)
            : this(endpoint, 0, accessKey, secretKey, insecure)
        {
        }

        /// <summary>
        /// Creates and returns an Cloud Storage client
        /// </summary>
        /// <param name="uri">Location of the server, supports HTTP and HTTPS</param>
        /// <param name="accessKey">Access Key for authenticated requests</param>
        /// <param name="secretKey">Secret Key for authenticated requests</param>
        /// <returns>Client with the uri set as the server location and authentication parameters set.</returns>
        public MinioClient(string endpoint, int port, string accessKey, string secretKey, bool insecure)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }

            try
            {
                var uri = new Uri(endpoint);
                if (uri != null)
                {
                    if (uri.AbsolutePath.Length > 0 && !uri.AbsolutePath.Equals("/", StringComparison.CurrentCultureIgnoreCase))
                    {
                         throw new InvalidEndpointException(endpoint, "No path allowed in endpoint.");
                    }
                    if (uri.Query.Length > 0)
                    {
                         throw new InvalidEndpointException(endpoint, "No query parameter allowed in endpoint.");
                    }
                    if (!uri.Scheme.Equals("http") && !uri.Scheme.Equals("https"))
                    {
                         throw new InvalidEndpointException(endpoint, "Invalid scheme detected in endpoint.");
                    }
                    string amzHost = uri.Host;
                    if ((amzHost.EndsWith(".amazonaws.com", StringComparison.CurrentCultureIgnoreCase))
                        && !(amzHost.Equals("s3.amazonaws.com", StringComparison.CurrentCultureIgnoreCase)))
                    {
                         throw new InvalidEndpointException(endpoint, "For Amazon S3, host should be \'s3.amazonaws.com\' in endpoint.");
                    }
                    this.client = new RestClient(uri);
                    this.client.UserAgent = this.FullUserAgent;
                    if (accessKey != null && secretKey != null)
                    {
                         this.authenticator = new Minio.V4Authenticator(accessKey, secretKey);
                         this.client.Authenticator = new Minio.V4Authenticator(accessKey, secretKey);
                    }
                    return;
                }
            }
            catch (UriFormatException)
            {
                if (!this.isValidEndpoint(endpoint))
                {
                    throw new InvalidEndpointException(endpoint, "Invalid endpoint.");
                }

                if (endpoint.EndsWith(".amazonaws.com", StringComparison.CurrentCultureIgnoreCase)
                    && !endpoint.Equals("s3.amazonaws.com", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new InvalidEndpointException(endpoint, "For Amazon S3, endpoint should be \'s3.amazonaws.com\'");
                }

                if (port < 0 || port > 65535)
                {
                    throw new InvalidPortException(port, "port must be in range of 1 to 65535.");
                }

                // TODO use a typed scheme.
                string scheme = "https";
                if (insecure)
                {
                    scheme = "http";
                }

                String path = scheme + "://" + endpoint + "/";
                if (port > 0)
                {
                    path = scheme + "://" + endpoint + ":" + port + "/";
                }
                var uri = new Uri(path);
                this.client = new RestClient(uri);
                this.client.UserAgent = this.FullUserAgent;
                if (accessKey != null && secretKey != null)
                {
                    this.authenticator = new V4Authenticator(accessKey, secretKey);
                    this.client.Authenticator = new V4Authenticator(accessKey, secretKey);
                }
            }
        }

        private bool isValidEndpoint(string endpoint)
        {
            // endpoint may be a hostname
            // refer https://en.wikipedia.org/wiki/Hostname#Restrictions_on_valid_host_names
            // why checks are as shown below.
            if (endpoint.Length < 1 || endpoint.Length > 253)
            {
                return false;
            }

            foreach (var label in endpoint.Split('.'))
            {
                if (label.Length < 1 || label.Length > 63)
                {
                    return false;
                }

                Regex validLabel = new Regex("^[a-zA-Z0-9][a-zA-Z0-9-]*");
                Regex validEndpoint = new Regex(".*[a-zA-Z0-9]$");

                if (!(validLabel.IsMatch(label) && validEndpoint.IsMatch(endpoint)))
                {
                    return false;
                }
            }

            return true;
        }
        public void SetAppInfo(string appName, string appVersion)
        {
            if (string.IsNullOrEmpty(appName))
            {
                    throw new ArgumentException("Appname cannot be null or empty");
            }
            if (string.IsNullOrEmpty(appVersion))
            {
                    throw new ArgumentException("Appversion cannot be null or empty");
            }
            string customAgent = appName + "/" + appVersion;
            this.CustomUserAgent = customAgent;
            this.client.UserAgent = this.FullUserAgent;
        }

        /// <summary>
        /// Returns true if the specified bucketName exists, otherwise returns false.
        /// </summary>
        /// <param name="bucketName">Bucket to test existence of</param>
        /// <returns>true if exists and user has access</returns>
        public bool BucketExists(string bucketName)
        {
                var request = new RestRequest(bucketName, Method.HEAD);
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
        /// Create a private bucket with a give name.
        /// </summary>
        /// <param name="bucketName">Name of the new bucket</param>
        public void MakeBucket(string bucketName)
        {
            this.MakeBucket(bucketName, Acl.Private, "us-east-1");
        }

        /// <summary>
        /// Create a private bucket with a give name at a location.
        /// </summary>
        /// <param name="bucketName">Name of the new bucket</param>
        /// <param name="location">Name of the location</param>
        public void MakeBucket(string bucketName, string location)
        {
            this.MakeBucket(bucketName, Acl.Private, location);
        }

        /// <summary>
        /// Create a private bucket with a give name and canned Acl.
        /// </summary>
        /// <param name="bucketName">Name of the new bucket</param>
        /// <param name="acl">Canned Acl to set</param>
        public void MakeBucket(string bucketName, Acl acl)
        {
            this.MakeBucket(bucketName, acl, "us-east-1");
        }

        /// <summary>
        /// Create a bucket with a given name, canned Acl and location.
        /// </summary>
        /// <param name="bucketName">Name of the new bucket</param>
        /// <param name="acl">Canned Acl to set</param>
        public void MakeBucket(string bucketName, Acl acl, string location)
        {
            var request = new RestRequest("/" + bucketName, Method.PUT);

            request.AddHeader("x-amz-acl", acl.ToString());
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(location);
                request.AddBody(config);
            }

            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            throw ParseError(response);
        }

        /// <summary>
        /// Remove a bucket
        /// </summary>
        /// <param name="bucketName">Name of bucket to remove</param>
        public void RemoveBucket(string bucketName)
        {
            var request = new RestRequest(bucketName, Method.DELETE);
            var response = client.Execute(request);

            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                throw ParseError(response);
            }
        }

        /// <summary>
        /// Remove an object
        /// </summary>
        /// <param name="bucketName">Name of bucket to remove</param>
        /// <param name="objectName">Name of object to remove</param>
        public void RemoveObject(string bucketName, string objectName)
        {
            var request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.DELETE);
            var response = client.Execute(request);

            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                throw ParseError(response);
            }
        }

        /// <summary>
        /// Get bucket Acl
        /// </summary>
        /// <param name="bucketName">Name of bucket to retrieve canned Acl</param>
        /// <returns>Canned Acl</returns>
        public Acl GetBucketAcl(string bucketName)
        {
            var request = new RestRequest(bucketName + "?acl", Method.GET);
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
                    if ("http://acs.amazonaws.com/groups/global/AllUsers".Equals(x.Grantee.Uri)
                        && x.Permission.Equals("READ"))
                    {
                        publicRead = true;
                    }
                    if ("http://acs.amazonaws.com/groups/global/AllUsers".Equals(x.Grantee.Uri)
                        && x.Permission.Equals("WRITE"))
                    {
                        publicWrite = true;
                    }
                    if ("http://acs.amazonaws.com/groups/global/AuthenticatedUsers".Equals(x.Grantee.Uri)
                        && x.Permission.Equals("READ"))
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
        /// Set a bucket's canned Acl
        /// </summary>
        /// <param name="bucketName">Name of bucket to set canned Acl</param>
        /// <param name="acl">Canned Acl to set</param>
        public void SetBucketAcl(string bucketName, Acl acl)
        {
            var request = new RestRequest(bucketName + "?acl", Method.PUT);
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
        public IReadOnlyCollection<Bucket> ListBuckets()
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
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        public void GetObject(string bucketName, string objectName, Action<Stream> callback)
        {
            RestRequest request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.GET);
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
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Name of object to retrieve</param>
        /// <param name="offset">Number of bytes to skip</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        public void GetPartialObject(string bucketName, string objectName, ulong offset, Action<Stream> callback)
        {
            var stat = this.StatObject(bucketName, objectName);
            RestRequest request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.GET);
            request.AddHeader("Range", "bytes=" + offset + "-" + (stat.Size - 1));
            request.ResponseWriter = callback;
            client.Execute(request);
            // response status code is 0, bug in upstream library, cannot rely on it for errors with PartialContent
        }

        /// <summary>
        /// Get a byte range of an object given by the offset and length. The object will be streamed to the callback given by the user
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="offset">Number of bytes to skip</param>
        /// <param name="length">Length of requested byte range</param>
        /// <param name="callback">A stream will be passed to the callback</param>
        public void GetPartialObject(string bucketName, string objectName, ulong offset, ulong length, Action<Stream> callback)
        {
            RestRequest request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.GET);
            request.AddHeader("Range", "bytes=" + offset + "-" + (offset + length - 1));
            request.ResponseWriter = callback;
            client.Execute(request);
            // response status code is 0, bug in upstream library, cannot rely on it for errors with PartialContent
        }

        /// <summary>
        /// Tests the object's existence and returns metadata about existing objects.
        /// </summary>
        /// <param name="bucketName">Bucket to test object in</param>
        /// <param name="objectName">Name of the object to stat</param>
        /// <returns>Facts about the object</returns>
        public ObjectStat StatObject(string bucketName, string objectName)
        {
            var request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.HEAD);
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
                return new ObjectStat(objectName, size, lastModified, etag, contentType);
            }
            throw ParseError(response);
        }

        /// <summary>
        /// Creates an object
        /// </summary>
        /// <param name="bucketName">Bucket to create object in</param>
        /// <param name="objectName">Key of the new object</param>
        /// <param name="size">Total size of bytes to be written, must match with data's length</param>
        /// <param name="contentType">Content type of the new object, null defaults to "application/octet-stream"</param>
        /// <param name="data">Stream of bytes to send</param>
        public void PutObject(string bucketName, string objectName, Stream data, long size, string contentType)
        {
            if (size >= MinioClient.maximumStreamObjectSize)
            {
                throw new ArgumentException("Input size is bigger than stipulated maximum of 50GB.");
            }

            if (size <= MinioClient.minimumPartSize)
            {
                var bytes = ReadFull(data, (int)size);
                if (bytes.Length != (int)size)
                {
                    throw new UnexpectedShortReadException("Data read "+ bytes.Length + " is shorter than the size " + size + " of input buffer.");
                }
                this.PutObject(bucketName, objectName, null, 0, bytes, contentType);
                return;
            }
            var partSize = MinioClient.minimumPartSize;
            var uploads = this.ListIncompleteUploads(bucketName, objectName);
            string uploadId = null;
            Dictionary<int, string> etags = new Dictionary<int, string>();
            if (uploads.Count() > 0)
            {
               foreach (Upload upload in uploads)
               {
                   if (objectName == upload.Key)
                   {
                      uploadId = upload.UploadId;
                      var parts = this.ListParts(bucketName, objectName, uploadId);
                      foreach (Part part in parts)
                      {
                          etags[part.PartNumber] = part.ETag;
                      }
                      break;
                   }
               }
            }
            if (uploadId == null)
            {
                uploadId = this.NewMultipartUpload(bucketName, objectName, contentType);
            }
            int partNumber = 0;
            long totalWritten = 0;
            while (totalWritten < size)
            {
                partNumber++;
                byte[] dataToCopy = ReadFull(data, (int)partSize);
                if (dataToCopy == null)
                {
                    break;
                }
                if (dataToCopy.Length < partSize)
                {
                    var expectedSize = size - totalWritten;
                    if (expectedSize != dataToCopy.Length)
                    {
                        throw new UnexpectedShortReadException("Unexpected short read. Read only " + dataToCopy.Length + " out of " + expectedSize + "bytes");
                    }
                }
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] hash = md5.ComputeHash(dataToCopy);
                string etag = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                if (!etags.ContainsKey(partNumber) || !etags[partNumber].Equals(etag))
                {
                   etag = this.PutObject(bucketName, objectName, uploadId, partNumber, dataToCopy, contentType);
                }
                etags[partNumber] = etag;
                totalWritten += dataToCopy.Length;
            }

            foreach (int curPartNumber in etags.Keys)
            {
               if (curPartNumber > partNumber)
               {
                   etags.Remove(curPartNumber);
               }
            }
            this.CompleteMultipartUpload(bucketName, objectName, uploadId, etags);
        }

        /// <summary>
        /// Presigned Get url.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        public string PresignedGetObject(string bucketName, string objectName, int expiresInt)
        {
            RestRequest request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.GET);
            return this.authenticator.PresignURL(this.client, request, expiresInt);
        }

        /// <summary>
        /// Presigned Put url.
        /// </summary>
        /// <param name="bucketName">Bucket to retrieve object from</param>
        /// <param name="objectName">Key of object to retrieve</param>
        /// <param name="expiresInt">Expiration time in seconds</param>
        public string PresignedPutObject(string bucketName, string objectName, int expiresInt)
        {
            RestRequest request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.PUT);
            return this.authenticator.PresignURL(this.client, request, expiresInt);
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

                string region = Regions.GetRegion(this.client.BaseUrl.Host);
                DateTime signingDate = DateTime.UtcNow;

                policy.SetAlgorithm("AWS4-HMAC-SHA256");
                policy.SetCredential(this.authenticator.GetCredentialString(signingDate, region));
                policy.SetDate(signingDate);

                string policyBase64 = policy.Base64();
                string signature = this.authenticator.PresignPostSignature(region, signingDate, policyBase64);

                policy.SetPolicy(policyBase64);
                policy.SetSignature(signature);

                return policy.GetFormData();
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

        private void CompleteMultipartUpload(string bucketName, string objectName, string uploadId, Dictionary<int, string> etags)
        {
            var path = bucketName + "/" + UrlEncode(objectName) + "?uploadId=" + uploadId;
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

        private long CalculatePartSize(long size)
        {
            // make sure to have enough buffer for last part, use 9999 instead of 10000
            long partSize = (size / 9999);
            if (partSize > MinioClient.minimumPartSize)
            {
                if (partSize > MinioClient.maximumPartSize)
                {
                    return MinioClient.maximumPartSize;
                }
                return partSize;
            }
            return MinioClient.minimumPartSize;
        }

        private IEnumerable<Part> ListParts(string bucketName, string objectName, string uploadId)
        {
            int nextPartNumberMarker = 0;
            bool isRunning = true;
            while (isRunning)
            {
                var uploads = GetListParts(bucketName, objectName, uploadId, nextPartNumberMarker);
                foreach (Part part in uploads.Item2)
                {
                    yield return part;
                }
                nextPartNumberMarker = uploads.Item1.NextPartNumberMarker;
                isRunning = uploads.Item1.IsTruncated;
            }
        }

        private Tuple<ListPartsResult, List<Part>> GetListParts(string bucketName, string objectName, string uploadId, int partNumberMarker)
        {
            var path = bucketName + "/" + UrlEncode(objectName) + "?uploadId=" + uploadId;
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
                                   PartNumber = int.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}PartNumber").Value, CultureInfo.CurrentCulture),
                                   ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value.Replace("\"", "")
                               });

                return new Tuple<ListPartsResult, List<Part>>(listPartsResult, uploads.ToList());
            }
            throw ParseError(response);
        }

        private string NewMultipartUpload(string bucketName, string objectName, string contentType)
        {
            var path = bucketName + "/" + UrlEncode(objectName) + "?uploads";
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

        private string PutObject(string bucketName, string objectName, string uploadId, int partNumber, byte[] data, string contentType)
        {
            var path = bucketName + "/" + UrlEncode(objectName);
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
                return new ConnectionException("Response is nil. Please report this issue https://github.com/minio/minio-dotnet/issues");
            }
            if (HttpStatusCode.Redirect.Equals(response.StatusCode) || HttpStatusCode.TemporaryRedirect.Equals(response.StatusCode) || HttpStatusCode.MovedPermanently.Equals(response.StatusCode))
            {
                return new RedirectionException("Redirection detected. Please report this issue https://github.com/minio/minio-dotnet/issues");
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
                            errorResponse.HostId = parameter.Value.ToString();
                        }
                        if (parameter.Name.Equals("x-amz-request-id", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.RequestId = parameter.Value.ToString();
                        }
                        if (parameter.Name.Equals("x-amz-bucket-region", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.BucketRegion = parameter.Value.ToString();
                        }
                    }

                    errorResponse.Resource = response.Request.Resource;

                    if (HttpStatusCode.NotFound.Equals(response.StatusCode))
                    {
                        int pathLength = response.Request.Resource.Split('/').Count();
                        if (pathLength > 1)
                        {
                            errorResponse.Code = "NoSuchKey";
                            var objectName = response.Request.Resource.Split('/')[1];
                            e = new ObjectNotFoundException(objectName, "Not found.");
                        }
                        else if (pathLength == 1)
                        {
                            errorResponse.Code = "NoSuchBucket";
                            var bucketName = response.Request.Resource.Split('/')[0];
                            e = new BucketNotFoundException(bucketName, "Not found.");
                        }
                        else
                        {
                            e = new InternalClientException("404 without body resulted in path with less than two components");
                        }
                    }
                    else if (HttpStatusCode.Forbidden.Equals(response.StatusCode))
                    {
                        errorResponse.Code = "Forbidden";
                        e = new AccessDeniedException("Access denied on the resource: " + response.Request.Resource);
                    }
                    e.Response = errorResponse;
                    return e;
                }
                throw new InternalClientException("Unsuccessful response from server without XML error: " + response.StatusCode);
            }

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ErrorResponse errResponse = (ErrorResponse)(new XmlSerializer(typeof(ErrorResponse)).Deserialize(stream));

            ClientException clientException = new ClientException(errResponse.Message);
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
        /// <param name="bucketName">Bucket to list objects from</param>
        /// <returns>An iterator lazily populated with objects</returns>
        public IEnumerable<Item> ListObjects(string bucketName)
        {
            return this.ListObjects(bucketName, null, true);
        }

        /// <summary>
        /// List all objects in a bucket with a given prefix
        /// </summary>
        /// <param name="bucketName">Bucket to list objects from</param>
        /// <param name="prefix">Filters all objects not beginning with a given prefix</param>
        /// <returns>An iterator lazily populated with objects</returns>
        public IEnumerable<Item> ListObjects(string bucketName, string prefix)
        {
            return this.ListObjects(bucketName, prefix, true);
        }

        /// <summary>
        /// List all objects non-recursively in a bucket with a given prefix, optionally emulating a directory
        /// </summary>
        /// <param name="bucketName">Bucket to list objects from</param>
        /// <param name="prefix">Filters all objects not beginning with a given prefix</param>
        /// <param name="recursive">Set to false to emulate a directory</param>
        /// <returns>A iterator lazily populated with objects</returns>
        public IEnumerable<Item> ListObjects(string bucketName, string prefix, bool recursive)
        {
            bool isRunning = true;

            string marker = null;

            while (isRunning)
            {
                Tuple<ListBucketResult, List<Item>> result = GetObjectList(bucketName, prefix, recursive, marker);
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

        private Tuple<ListBucketResult, List<Item>> GetObjectList(string bucketName, string prefix, bool recursive, string marker)
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

            string path = bucketName;
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
                                 Size = UInt64.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value, CultureInfo.CurrentCulture),
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

        private Tuple<ListMultipartUploadsResult, List<Upload>> GetMultipartUploadsList(string bucketName,
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
        /// <param name="bucketName">Bucket to list all incomplepte uploads from</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        public IEnumerable<Upload> ListIncompleteUploads(string bucketName)
        {
            return this.ListIncompleteUploads(bucketName, null);
        }

        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix
        /// </summary>
        /// <param name="bucketName">Bucket to list all incomplepte uploads from</param>
        /// <param name="prefix">prefix to list all incomplepte uploads</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        public IEnumerable<Upload> ListIncompleteUploads(string bucketName, string prefix)
        {
            return this.ListIncompleteUploads(bucketName, prefix, true);
        }

        /// <summary>
        /// Lists all incomplete uploads in a given bucket and prefix recursively
        /// </summary>
        /// <param name="bucketName">Bucket to list all incomplepte uploads from</param>
        /// <param name="prefix">prefix to list all incomplepte uploads</param>
        /// <param name="recursive">option to list incomplete uploads recursively</param>
        /// <returns>A lazily populated list of incomplete uploads</returns>
        public IEnumerable<Upload> ListIncompleteUploads(string bucketName, string prefix, bool recursive)
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
        private IEnumerable<Upload> listIncompleteUploads(string bucketName, string prefix, string delimiter)
        {
            string nextKeyMarker = null;
            string nextUploadIdMarker = null;
            bool isRunning = true;
            while (isRunning)
            {
                var uploads = GetMultipartUploadsList(bucketName, prefix, nextKeyMarker, nextUploadIdMarker, delimiter);
                foreach (Upload upload in uploads.Item2)
                {
                    yield return upload;
                }
                nextKeyMarker = uploads.Item1.NextKeyMarker;
                nextUploadIdMarker = uploads.Item1.NextUploadIdMarker;
                isRunning = uploads.Item1.IsTruncated;
            }
        }

        /// <summary>
        /// Remove incomplete uploads from a given bucket and objectName
        /// </summary>
        /// <param name="bucketName">Bucket to remove incomplete uploads from</param>
        /// <param name="objectName">Key to remove incomplete uploads from</param>
        public void RemoveIncompleteUpload(string bucketName, string objectName)
        {
            var uploads = this.ListIncompleteUploads(bucketName, objectName);
            foreach (Upload upload in uploads)
            {
                if (objectName == upload.Key)
                {
                    this.RemoveUpload(bucketName, objectName, upload.UploadId);
                }
            }
        }

        private void RemoveUpload(string bucketName, string objectName, string uploadId)
        {
            var path = bucketName + "/" + UrlEncode(objectName) + "?uploadId=" + uploadId;
            var request = new RestRequest(path, Method.DELETE);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return;
            }
            throw ParseError(response);
        }

        private static string UrlEncode(string input)
        {
            return Uri.EscapeDataString(input).Replace("%2F", "/");
        }
    }
}
