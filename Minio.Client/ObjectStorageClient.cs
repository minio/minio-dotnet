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
            this.client.UserAgent =  "minio-cs/0.0.1 (Windows 8.1; x86_64)";
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

            if(!response.StatusCode.Equals(HttpStatusCode.NoContent)) {
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
            request.AddHeader("Range", "bytes=" + offset + "-" + (stat.Size-1));
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
                foreach (Parameter parameter in response.Headers) {
                    if(parameter.Name == "Content-Length") {
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
                this.DoPutObject(bucket, key, null, null, contentType, bytes);
            }
        }

        private string DoPutObject(string bucket, string key, string uploadId, string partNumber, string contentType, byte[] data)
        {
            var request = new RestRequest(bucket + "/" + key, Method.PUT);
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
            bool isRunning = true;

            string marker = null;

            while (isRunning)
            {
                Tuple<ListBucketResult, List<Item>> result = GetObjectList(bucket, marker, true);
                foreach (Item item in result.Item2)
                {
                    yield return item;
                }
                marker = result.Item1.NextMarker;
                isRunning = result.Item1.IsTruncated;
            }
        }

        private Tuple<ListBucketResult, List<Item>> GetObjectList(string bucket, string prefix, bool recursive)
        {
            var queries = new List<string>();
            if (!recursive)
            {
                queries.Add("delim=/");
            }
            if (prefix != null)
            {
                queries.Add("prefix=" + WebUtility.UrlEncode(prefix));
            }
            string path = bucket;
            var request = new RestRequest(path, Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                var stream = new MemoryStream(contentBytes);
                ListBucketResult listBucketResult = (ListBucketResult)(new XmlSerializer(typeof(ListBucketResult)).Deserialize(stream));

                XDocument root = XDocument.Parse(response.Content);

                var query = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Contents")
                             select new Item()
                             {
                                 Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                                 LastModified = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}LastModified").Value,
                                 ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value,
                                 Size = UInt64.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value)
                             });

                return new Tuple<ListBucketResult, List<Item>>(listBucketResult, query.ToList());
            }
            throw ParseError(response);
        }
    }
}
