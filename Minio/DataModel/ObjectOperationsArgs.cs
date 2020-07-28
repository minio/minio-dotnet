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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.DataModel;
using RestSharp;

namespace Minio
{
    public class GetObjectListArgs : ObjectArgs
    {
        internal string Delimiter;
        internal string Prefix;
        internal string Marker;

        public GetObjectListArgs()
        {
            Delimiter = string.Empty;
            Prefix = string.Empty;
            Marker = string.Empty;
        }
        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
        }

        public new GetObjectListArgs WithObjectName(string o)
        {
            return (GetObjectListArgs)(base.WithObjectName(o));
        }
        public new GetObjectListArgs WithVersionID(string v)
        {
            return (GetObjectListArgs)(base.WithVersionID(v));
        }
        public new GetObjectListArgs WithBucket(string bucket)
        {
            return (GetObjectListArgs)(base.WithBucket(bucket));
        }
        public GetObjectListArgs WithPrefix(string p)
        {
            this.Prefix = p;
            return this;
        }
        public GetObjectListArgs WithDelimiter(string d)
        {
            this.Delimiter = d;
            return this;
        }
        public GetObjectListArgs WithMarker(string m)
        {
            this.Marker = m;
            return this;
        }
        public new RestRequest GetRequest(string baseURL, RestSharp.Authenticators.IAuthenticator authenticator)
        {
            RestRequest request = RequestUtil.CreateRequest(baseURL, Method.GET, authenticator, this.Region, this.Secure, this.BucketName);
            request.AddQueryParameter("delimiter",this.Delimiter);
            request.AddQueryParameter("prefix",this.Prefix);
            request.AddQueryParameter("max-keys", "1000");
            request.AddQueryParameter("marker",this.Marker);
            request.AddQueryParameter("encoding-type","url");
            return request;
        }

        public Tuple<ListBucketResult, List<Item>> ProcessResponse(RestSharp.IRestResponse response)
        {
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            ListBucketResult listBucketResult = null;
            using (var stream = new MemoryStream(contentBytes))
            {
                listBucketResult = (ListBucketResult)new XmlSerializer(typeof(ListBucketResult)).Deserialize(stream);
            }

            XDocument root = XDocument.Parse(response.Content);

            var items = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Contents")
                        select new Item
                        {
                            Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                            LastModified = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}LastModified").Value,
                            ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value,
                            Size = ulong.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value, CultureInfo.CurrentCulture),
                            IsDir = false
                        };

            var prefixes = from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}CommonPrefixes")
                           select new Item
                           {
                               Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Prefix").Value,
                               IsDir = true
                           };

            items = items.Concat(prefixes);

            return Tuple.Create(listBucketResult, items.ToList());
        }
    }

}