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

using System.Collections.Generic;
using System.Linq;

namespace Minio
{
    public abstract class ObjectArgs<T> : BucketArgs<T>
                            where T : ObjectArgs<T>
    {
        internal string ObjectName { get; set; }
        internal Dictionary<string, string> HeaderMap { get; set; }
        internal string ContentType { get; set; }
        internal object RequestBody { get; set; }
        internal string ResourcePath { get; set; }
        internal string VersionId { get; set; }


        internal static readonly List<string> SupportedHeaders = new List<string> { "cache-control", "content-encoding", "content-type", "x-amz-acl", "content-disposition" };

        public ObjectArgs()
        {
            HeaderMap = null;
            ContentType = "application/octet-stream" ;
            RequestBody = null;
            ResourcePath = null;
        }

        public T WithObject(string obj)
        {
            this.ObjectName = obj;
            return (T)this;
        }

        public T WithHeaders(Dictionary<string, string> headers)
        {
            if (this.HeaderMap == null)
            {
                this.HeaderMap = new Dictionary<string, string>();
            }
            foreach (string key in headers.Keys)
            {
                this.HeaderMap.Add(key, headers[key]);
            }
            return (T)this;
        }

        public T WithContentType(string contentType)
        {
            this.ContentType = contentType;
            return (T)this;
        }

        public T WithBody(object data)
        {
            this.RequestBody = data;
            return (T)this;
        }

        public T WithResourcePath(string path)
        {
            this.ResourcePath = path;
            return (T)this;
        }

        public T WithVersionId(string vid)
        {
            this.VersionId = vid;
            return (T)this;
        }

        public override void Validate()
        {
            base.Validate();
            utils.ValidateObjectName(this.ObjectName);
        }

        // Merge the Headers map & extra headers.
        public Dictionary<string, string> MergedHeaders()
        {
            if (this.ExtraHeaders == null )
            {
                return this.HeaderMap;
            }
            if  (this.HeaderMap == null)
            {
                return this.ExtraHeaders;
            }
            // Merge headers.
            return this.HeaderMap.Concat(this.ExtraHeaders).ToDictionary(ele => ele.Key, ele => ele.Value);
        }
    }
}