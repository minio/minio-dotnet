/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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

using RestSharp;

using Minio.DataModel;
using System.Collections.Generic;
using System.Linq;
using System;
using Minio.Helper;
using System.IO;
using System.Xml.Linq;
using System.Security.Cryptography;

namespace Minio
{
    public class NewMultipartUploadArgs: ObjectArgs<NewMultipartUploadArgs>
    {
        internal ServerSideEncryption SSE { get; private set; }
        public NewMultipartUploadArgs()
        {
            this.RequestMethod = Method.POST;
        }
        public override void Validate()
        {
            base.Validate();
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploads","");
            return request;
        }

        public NewMultipartUploadArgs WithSSEHeaders(Dictionary<string, string> hdr)
        {
            this.WithExtraHeaders(hdr);
            return this;
        }
    }

    public class RemoveUploadArgs: ObjectArgs<RemoveUploadArgs>
    {
        public RemoveUploadArgs()
        {
            this.RequestMethod = Method.DELETE;
        }

        internal string UploadId { get; private set; }

        public RemoveUploadArgs WithUploadId(string id)
        {
            this.UploadId = id;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploadId",$"{this.UploadId}");
            return request;
        }
    }

    public class PutObjectPartArgs : PutObjectArgs
    {
        public PutObjectPartArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public new PutObjectPartArgs WithBucket(string bkt)
        {
            return (PutObjectPartArgs)base.WithBucket(bkt);
        }

        public new PutObjectPartArgs WithObject(string obj)
        {
            return (PutObjectPartArgs)base.WithObject(obj);
        }

        public new PutObjectPartArgs WithObjectSize(long size)
        {
            return (PutObjectPartArgs)base.WithObjectSize(size);
        }

        public new PutObjectPartArgs WithHeaders(Dictionary<string, string> hdr)
        {
            return (PutObjectPartArgs)base.WithHeaders(hdr);
        }
        public new PutObjectPartArgs WithSSEHeaders(Dictionary<string, string> sseHeaders)
        {
            this.SSEHeaders = this.SSEHeaders ?? new Dictionary<string, string>();
            if (sseHeaders != null)
            {
                this.SSEHeaders = this.SSEHeaders.Concat(sseHeaders).ToDictionary(ele => ele.Key, ele => ele.Value);
                if (this.SSE != null &&
                (this.SSE.GetType().Equals(EncryptionType.SSE_S3) ||
                    this.SSE.GetType().Equals(EncryptionType.SSE_KMS)))
                {
                    this.SSEHeaders.Remove(Constants.SSEGenericHeader);
                    this.SSEHeaders.Remove(Constants.SSEKMSContext);
                    this.SSEHeaders.Remove(Constants.SSEKMSKeyId);
                }
            }
            return this;
        }

        public new PutObjectPartArgs WithStreamData(Stream data)
        {
            return (PutObjectPartArgs)base.WithStreamData(data);
        }
        public new PutObjectPartArgs WithContentType(string type)
        {
            return (PutObjectPartArgs)base.WithContentType(type);
        }

        public new PutObjectPartArgs WithUploadId(string id)
        {
            return (PutObjectPartArgs)base.WithUploadId(id);
        }
    }

    public class CompleteMultipartUploadArgs: ObjectArgs<CompleteMultipartUploadArgs>
    {
        public CompleteMultipartUploadArgs()
        {
            this.RequestMethod = Method.POST;
        }

        internal string UploadId { get; private set; }
        internal Dictionary<int, string> ETags { get; private set; }

        public CompleteMultipartUploadArgs WithUploadID(string id)
        {
            this.UploadId = id;
            return this;
        }

        public CompleteMultipartUploadArgs WithETags(Dictionary<int, string> etags)
        {
            this.ETags = this.ETags ?? new Dictionary<int, string>();
            this.ETags = this.ETags.Concat(etags).ToDictionary(ele => ele.Key, ele => ele.Value);
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request.AddQueryParameter("uploadId",$"{this.UploadId}");

            List<XElement> parts = new List<XElement>();

            for (int i = 1; i <= this.ETags.Count; i++)
            {
                parts.Add(new XElement("Part",
                                       new XElement("PartNumber", i),
                                       new XElement("ETag", this.ETags[i])));
            }

            var completeMultipartUploadXml = new XElement("CompleteMultipartUpload", parts);
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            request.XmlSerializer.Namespace = "http://s3.amazonaws.com/doc/2006-03-01/";
            request.XmlSerializer.ContentType = "application/xml";
            string body = utils.MarshalXML(completeMultipartUploadXml, "http://s3.amazonaws.com/doc/2006-03-01/");
            Console.WriteLine("CompleteMultipartUploadArgs config " + body);
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            return request;
        }
    }
    public class PutObjectArgs : ObjectQueryArgs<PutObjectArgs>
    {
        internal string UploadId { get; private set; }
        internal ServerSideEncryption SSE { get; set; }
        internal int PartNumber { get; set; }
        internal byte[] ObjectData { get; set; }
        internal Dictionary<string, string> SSEHeaders { get; set; }
        internal string FileName { get; set; }
        internal long ObjectSize { get; set; }

        internal Stream ObjectStreamData { get; set; }

        public PutObjectArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public PutObjectArgs(PutObjectPartArgs args)
        {
            this.RequestMethod = Method.PUT;
            this.BucketName = args.BucketName;
            this.ContentType = args.ContentType ?? "application/octet-stream";
            this.ExtraHeaders = args.ExtraHeaders;
            this.ExtraQueryParams = args.ExtraQueryParams;
            this.FileName = args.UploadId;
            this.HeaderMap = args.HeaderMap;
            this.ObjectData = args.ObjectData;
            this.ObjectName = args.ObjectName;
            this.ObjectSize = args.ObjectSize;
            this.PartNumber = args.PartNumber;
            this.SSE = args.SSE;
            this.SSEHeaders = args.SSEHeaders;
            this.UploadId = args.UploadId;
        }

        public override void Validate()
        {
            base.Validate();
            if (this.RequestBody == null && this.ObjectStreamData == null)
            {
                throw new ArgumentNullException(nameof(RequestBody), "Invalid input stream, cannot be null");
            }
            if (this.PartNumber < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(PartNumber), this.PartNumber, "Invalid Part number value. Cannot be less than 0");
            }
            if (!string.IsNullOrEmpty(this.FileName))
            {
                utils.ValidateFile(this.FileName);
            }
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            this.HeaderMap = this.HeaderMap ?? new Dictionary<string, string>();
            if (this.SSE != null)
            {
                this.SSE.Marshal(this.SSEHeaders);
            }
            if (string.IsNullOrEmpty(this.ContentType) || string.IsNullOrWhiteSpace(this.ContentType))
            {
                this.ContentType = "application/octet-stream";
            }
            if (!this.HeaderMap.ContainsKey("Content-Type"))
            {
                this.HeaderMap["Content-Type"] = this.ContentType;
            }
            if (!string.IsNullOrEmpty(this.UploadId) && this.PartNumber > 0)
            {
                request.AddQueryParameter("uploadId",$"{this.UploadId}");
                request.AddQueryParameter("partNumber",$"{this.PartNumber}");
            }
            if (this.ObjectData != null)
            {
                var sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash((byte[])this.ObjectData);
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                request.AddOrUpdateParameter("x-amz-content-sha256", hex, ParameterType.HttpHeader);
            }

            return request;
        }
 
         public new PutObjectArgs WithHeaders(Dictionary<string, string> metaData)
        {
            var sseHeaders = new Dictionary<string, string>();
            this.HeaderMap = this.HeaderMap ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (metaData != null) {
                foreach (KeyValuePair<string, string> p in metaData)
                {
                    var key = p.Key;
                    if (!OperationsUtil.IsSupportedHeader(p.Key) && !p.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "x-amz-meta-" + key.ToLowerInvariant();
                    }
                    this.HeaderMap[key] = p.Value;
                }
            }
            if (string.IsNullOrEmpty(this.ContentType) || string.IsNullOrWhiteSpace(this.ContentType))
            {
                this.ContentType = "application/octet-stream";
            }
            if (!this.HeaderMap.ContainsKey("Content-Type"))
            {
                this.HeaderMap["Content-Type"] = this.ContentType;
            }
            return this;
        }

        public PutObjectArgs WithSSEHeaders(Dictionary<string, string> hdr)
        {
            this.HeaderMap = this.HeaderMap?? new Dictionary<string, string>();
            if (hdr != null)
            {
                this.HeaderMap = this.HeaderMap.Concat(hdr).ToDictionary(ele => ele.Key, ele => ele.Value);
                this.SSEHeaders = hdr;
            }
            return this;
        }

        public PutObjectArgs WithUploadId(string id = null)
        {
            this.UploadId = id;
            return this;
        }

        public PutObjectArgs WithPartNumber(int num)
        {
            this.PartNumber = num;
            return this;
        }

        public PutObjectArgs WithObjectBody(byte[] data)
        {
            this.RequestBody = data;
            return this;
        }

        public PutObjectArgs WithServerSideEncryption(ServerSideEncryption sse)
        {
            this.SSE = sse;
            return this;
        }

        public PutObjectArgs WithFileName(string file)
        {
            this.FileName = file;
            FileInfo fileInfo = new FileInfo(file);
            this.ObjectSize = fileInfo.Length;
            this.ObjectStreamData = new FileStream(file, FileMode.Open, FileAccess.Read);
            return this;
        }

        public PutObjectArgs WithObjectSize(long size)
        {
            this.ObjectSize = size;
            return this;
        }

        public PutObjectArgs WithStreamData(Stream data)
        {
            this.ObjectStreamData = data;
            return this;
        }
   }
}