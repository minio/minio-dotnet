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
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using System.IO;
using Minio.Client.xml;
using System.Xml.Serialization;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Collections;

namespace Minio.Client
{
    public class ObjectStorageClient
    {
        private static int PART_SIZE = 5 * 1024 * 1024;

        private RestClient client;
        private string region;

        internal ObjectStorageClient(Uri uri, string accessKey, string secretKey)
        {
            this.client = new RestClient(uri);
            this.client.UserAgent = "minio-cs/0.0.1 (Windows 8.1; x86_64)";
            this.region = "us-west-2";
            if (accessKey != null && secretKey != null)
            {
                this.client.Authenticator = new V4Authenticator(accessKey, secretKey);
            }
        }
        public static ObjectStorageClient GetClient(Uri uri)
        {
            return GetClient(uri, null, null);
        }
        public static ObjectStorageClient GetClient(Uri uri, string accessKey, string secretKey)
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
                return new ObjectStorageClient(new Uri(path), accessKey, secretKey);
            }
            throw new UriFormatException("Expecting AbsolutePath to be empty");
        }

        public static ObjectStorageClient GetClient(string url)
        {
            return GetClient(url, null, null);
        }

        public static ObjectStorageClient GetClient(string url, string accessKey, string secretKey)
        {
            Uri uri = new Uri(url);
            return GetClient(uri, accessKey, secretKey);
        }

        public bool BucketExists(string bucket)
        {
            var request = new RestRequest(bucket, Method.HEAD);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }

        public void MakeBucket(string bucket, Acl acl)
        {
            var request = new RestRequest("/" + bucket, Method.PUT);

            CreateBucketConfiguration config = new CreateBucketConfiguration()
            {
                LocationConstraint = this.region
            };

            request.AddBody(config);

            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            throw ParseError(response);
        }

        public void MakeBucket(string bucket)
        {
            this.MakeBucket(bucket, Acl.Private);
        }

        public void RemoveBucket(string bucket)
        {
            var request = new RestRequest(bucket, Method.DELETE);
            var response = client.Execute(request);

            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                throw ParseError(response);
            }
        }

        public Acl GetBucketAcl(string bucket)
        {
            var request = new RestRequest(bucket + "?acl", Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = StripXmlnsXsi(response.Content);
                Console.Out.WriteLine(content);
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

        public void SetBucketAcl(string bucket, Acl acl)
        {
            var request = new RestRequest(bucket + "?acl", Method.PUT);
            // TODO add acl header
            request.AddHeader("x-amz-acl", acl.ToString());
            var response = client.Execute(request);
        }

        public ListAllMyBucketsResult ListBuckets()
        {
            var request = new RestRequest("/", Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                var stream = new MemoryStream(contentBytes);
                ListAllMyBucketsResult bucketList = (ListAllMyBucketsResult)(new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream));
                return bucketList;
            }
            throw ParseError(response);
        }

        public void GetObject(string bucket, string key, Action<Stream> writer)
        {
            RestRequest request = new RestRequest(bucket + "/" + key, Method.GET);
            request.ResponseWriter = writer;
            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }
            throw ParseError(response);
        }

        public void GetObject(string bucket, string key, ulong offset, Action<Stream> writer)
        {
            var stat = this.StatObject(bucket, key);
            RestRequest request = new RestRequest(bucket + "/" + key, Method.GET);
            request.AddHeader("Range", "bytes=" + offset + "-" + (stat.Size - 1));
            request.ResponseWriter = writer;
            client.Execute(request);
            // response status code is 0, bug in upstream library, cannot rely on it for errors with PartialContent
        }

        public void GetObject(string bucket, string key, ulong offset, ulong length, Action<Stream> writer)
        {
            RestRequest request = new RestRequest(bucket + "/" + key, Method.GET);
            request.AddHeader("Range", "bytes=" + offset + "-" + (offset + length - 1));
            request.ResponseWriter = writer;
            client.Execute(request);
            // response status code is 0, bug in upstream library, cannot rely on it for errors with PartialContent
        }

        public ObjectStat StatObject(string bucket, string key)
        {
            var request = new RestRequest(bucket + "/" + key, Method.HEAD);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                UInt64 size = 0;
                DateTime lastModified = new DateTime();
                string etag = "";
                foreach (Parameter parameter in response.Headers)
                {
                    if (parameter.Name == "Content-Length")
                    {
                        size = UInt64.Parse(parameter.Value.ToString());
                    }
                    if (parameter.Name == "Last-Modified")
                    {
                        // TODO parse datetime
                        lastModified = new DateTime();
                    }
                    if (parameter.Name == "ETag")
                    {
                        etag = parameter.Value.ToString();
                    }
                }

                return new ObjectStat(key, size, lastModified, etag);
            }
            throw ParseError(response);
        }

        public void PutObject(string bucket, string key, UInt64 size, string contentType, Stream data)
        {
            if (size <= (UInt64)(ObjectStorageClient.PART_SIZE))
            {
                var stream = new MemoryStream(new byte[(int)size]);
                data.CopyTo(stream, (int)size);
                var bytes = stream.ToArray();
                this.DoPutObject(bucket, key, null, 0, contentType, bytes);
            }
            else
            {
                var partSize = CalculatePartSize(size);
                var uploads = this.ListAllUnfinishedUploads(bucket, key);
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
                    uploadId = this.NewMultipartUpload(bucket, key);
                }
                int partNumber = 0;
                UInt64 totalWritten = 0;
                while (totalWritten < size)
                {
                    partNumber++;
                    var currentPartSize = (int)Math.Min((UInt64)partSize, (size - totalWritten));
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
                    totalWritten += (UInt64)dataToCopy.Length;
                }

                // test if any more data is on the stream
                if (data.ReadByte() != -1)
                {
                    throw new InputSizeMismatchError()
                    {
                        Bucket = bucket,
                        Key = key,
                        UserSpecifiedSize = size,
                        ActualReadSize = totalWritten + 1
                    };
                }

                if (totalWritten != size)
                {
                    throw new InputSizeMismatchError()
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
            var path = bucket + "/" + key + "?uploadId=" + uploadId;
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

            Console.Out.WriteLine(bodyString);

            request.AddParameter("application/xml", body, RestSharp.ParameterType.RequestBody);

            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                return;
            }
            throw ParseError(response);
        }

        private int CalculatePartSize(UInt64 size)
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
                var uploads = GetListParts(bucket, key, uploadId);
                foreach (Part part in uploads.Item2)
                {
                    yield return part;
                }
                nextPartNumberMarker = uploads.Item1.NextPartNumberMarker;
                isRunning = uploads.Item1.IsTruncated;
            }
        }

        private Tuple<ListPartsResult, List<Part>> GetListParts(string bucket, string key, string uploadId)
        {
            var path = bucket + "/" + key + "?uploadId=" + uploadId;
            var request = new RestRequest(path, Method.GET);
            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                var stream = new MemoryStream(contentBytes);
                ListPartsResult listPartsResult = (ListPartsResult)(new XmlSerializer(typeof(ListPartsResult)).Deserialize(stream));

                XDocument root = XDocument.Parse(response.Content);

                Console.Out.WriteLine(root);

                var uploads = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Part")
                               select new Part()
                               {
                                   PartNumber = int.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}PartNumber").Value),
                                   ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value.Replace("\"", "")
                               });

                return new Tuple<ListPartsResult, List<Part>>(listPartsResult, new List<Part>());
            }
            throw ParseError(response);
        }

        private string NewMultipartUpload(string bucket, string key)
        {
            var path = bucket + "/" + key + "?uploads";
            var request = new RestRequest(path, Method.POST);
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
            var path = bucket + "/" + key;
            var queries = new List<string>();
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

        private RequestException ParseError(IRestResponse response)
        {
            Console.Out.WriteLine("Status: " + response.StatusCode + " " + response.StatusDescription);
            Console.Out.WriteLine("Output:");
            Console.Out.WriteLine(response.Content);
            Console.Out.WriteLine("---");
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ErrorResponse errorResponse = (ErrorResponse)(new XmlSerializer(typeof(ErrorResponse)).Deserialize(stream));
            return new RequestException()
            {
                Response = errorResponse,
                XmlError = response.Content
            };
        }

        private string StripXmlnsXsi(string input)
        {
            string result = input.Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"CanonicalUser\"", "");
            result = result.Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"Group\"", "");
            return result;
        }

        public IEnumerable<Item> ListObjects(string bucket)
        {
            return this.ListObjects(bucket, null, true);
        }

        public IEnumerable<Item> ListObjects(string bucket, string prefix)
        {
            return this.ListObjects(bucket, prefix, true);
        }

        public IEnumerable<Item> ListObjects(string bucket, string prefix, bool recursive)
        {
            bool isRunning = true;

            string marker = null;

            while (isRunning)
            {
                Tuple<ListBucketResult, List<Item>> result = GetObjectList(bucket, prefix, recursive, marker);
                foreach (Item item in result.Item2)
                {
                    yield return item;
                }
                marker = result.Item1.NextMarker;
                isRunning = result.Item1.IsTruncated;
            }
        }

        private Tuple<ListBucketResult, List<Item>> GetObjectList(string bucket, string prefix, bool recursive, string marker)
        {
            var queries = new List<string>();
            if (!recursive)
            {
                queries.Add("delimiter=/");
            }
            if (prefix != null)
            {
                queries.Add("prefix=" + prefix);
            }
            if (marker != null)
            {
                queries.Add("marker=" + marker);
            }
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

                Console.Out.WriteLine(root);

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

        public Tuple<ListMultipartUploadsResult, List<Upload>> GetMultipartUploadsList(string bucket, string prefix, string keyMarker, string uploadIdMarker)
        {
            var queries = new List<string>();
            queries.Add("uploads");
            if (prefix != null)
            {
                queries.Add("prefix=" + prefix);
            }
            if (keyMarker != null)
            {
                queries.Add("key-marker=" + keyMarker);
            }
            if (uploadIdMarker != null)
            {
                queries.Add("upload-id-marker=" + uploadIdMarker);
            }

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

                Console.Out.WriteLine(root);

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

        public IEnumerable<Upload> ListAllUnfinishedUploads(string bucket)
        {
            return this.ListAllUnfinishedUploads(bucket, null);
        }

        public IEnumerable<Upload> ListAllUnfinishedUploads(string bucket, string prefix)
        {
            string nextKeyMarker = null;
            string nextUploadIdMarker = null;
            bool isRunning = true;
            while (isRunning)
            {
                var uploads = GetMultipartUploadsList(bucket, prefix, nextKeyMarker, nextUploadIdMarker);
                foreach (Upload upload in uploads.Item2)
                {
                    if (prefix != null && !prefix.Equals(upload.Key))
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

        public void DropIncompleteUpload(string bucket, string key)
        {
            var uploads = this.ListAllUnfinishedUploads(bucket, key);
            foreach (Upload upload in uploads)
            {
                this.DropUpload(bucket, key, upload.UploadId);
            }
        }

        private void DropUpload(string bucket, string key, string uploadId)
        {
            var path = bucket + "/" + key + "?uploadId=" + uploadId;
            var request = new RestRequest(path, Method.DELETE);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return;
            }
            throw ParseError(response);
        }

        public void DropAllIncompleteUploads(string bucket)
        {
            var uploads = this.ListAllUnfinishedUploads(bucket);
            foreach (Upload upload in uploads)
            {
                this.DropUpload(bucket, upload.Key, upload.UploadId);
            }
        }
    }
}
