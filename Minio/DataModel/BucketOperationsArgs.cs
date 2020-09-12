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

using Minio.DataModel;
using RestSharp;

namespace Minio
{
    public class BucketExistsArgs: BucketArgs<BucketExistsArgs>
    {
        public BucketExistsArgs()
        {
            this.RequestMethod = Method.HEAD;
        }
    }

    public class RemoveBucketArgs : BucketArgs<RemoveBucketArgs>
    {
        public RemoveBucketArgs()
        {
            this.RequestMethod = Method.DELETE;
        }
    }

    public class MakeBucketArgs : BucketArgs<MakeBucketArgs>
    {
        internal string Location { get; set; }
        internal bool ObjectLock { get; set; }
        public MakeBucketArgs()
        {
            this.RequestMethod = Method.PUT;
        }

        public MakeBucketArgs WithLocation(string loc)
        {
            this.Location = loc;
            return this;
        }

        public MakeBucketArgs WithObjectLock()
        {
            this.ObjectLock = true;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            request.RequestFormat = DataFormat.Xml;
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (this.Location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(this.Location);
                string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
                request.AddParameter(new Parameter("text/xml", body, ParameterType.RequestBody));
            }
            if (this.ObjectLock)
            {
                request.AddOrUpdateParameter("X-Amz-Bucket-Object-Lock-Enabled", "true", ParameterType.HttpHeader);
            }
            return request;
        }
    }
    public class ListObjectsArgs : BucketArgs<ListObjectsArgs>
    {
        internal string Prefix;
        internal bool Recursive;
        internal bool Versions;
        public ListObjectsArgs WithPrefix(string prefix)
        {
            this.Prefix = prefix;
            return this;
        }
        public ListObjectsArgs WithRecursive(bool rec)
        {
            this.Recursive = rec;
            return this;
        }
        public ListObjectsArgs WithVersions(bool ver)
        {
            this.Versions = ver;
            return this;
        }
    }

    public class GetObjectListArgs : BucketArgs<GetObjectListArgs>
    {
        internal string Delimiter { get; private set; }
        internal string Prefix { get; private set; }
        internal string Marker { get; private set; }
        internal bool Versions { get; private set; }


        public GetObjectListArgs()
        {
            this.RequestMethod = Method.GET;
            // Avoiding null values. Default is empty strings.
            this.Delimiter = string.Empty;
            this.Prefix = string.Empty;
            this.Marker = string.Empty;
        }
        public GetObjectListArgs WithDelimiter(string delim)
        {
            this.Delimiter = delim ?? string.Empty;
            return this;
        }
        public GetObjectListArgs WithPrefix(string prefix)
        {
            this.Prefix = prefix ?? string.Empty;
            return this;
        }
        public GetObjectListArgs WithMarker(string marker)
        {
            this.Marker = marker ?? string.Empty;
            return this;
        }

        public GetObjectListArgs WithVersions(bool versions)
        {
            this.Versions = versions;
            return this;
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("delimiter",this.Delimiter);
            request.AddQueryParameter("prefix",this.Prefix);
            request.AddQueryParameter("max-keys", "1000");
            request.AddQueryParameter("marker",this.Marker);
            request.AddQueryParameter("encoding-type","url");
            if (this.Versions)
            {
                request.AddQueryParameter("versions", "");
            }
            return request;
        }
    }
}