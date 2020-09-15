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
using Minio.Exceptions;
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

    public class GetPolicyArgs : BucketArgs<GetPolicyArgs>
    {
        public GetPolicyArgs()
        {
            this.RequestMethod = Method.GET;
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("policy","");
            return request;
        }
    }

    public class SetPolicyArgs : BucketArgs<SetPolicyArgs>
    {
        internal string PolicyJsonString { get; private set; }
        public SetPolicyArgs()
        {
            this.RequestMethod = Method.PUT;
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            if (string.IsNullOrEmpty(this.PolicyJsonString))
            {
                new MinioException("SetPolicyArgs needs the policy to be set to the right JSON contents.");
            }
            request.AddQueryParameter("policy","");
            request.AddJsonBody(this.PolicyJsonString);
            return request;
        }
        public SetPolicyArgs WithPolicy(string policy)
        {
            this.PolicyJsonString = policy;
            return this;
        }
    }

    public class RemovePolicyArgs : BucketArgs<RemovePolicyArgs>
    {
        public RemovePolicyArgs()
        {
            this.RequestMethod = Method.DELETE;
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request.AddQueryParameter("policy","");
            return request;
        }
    }
}