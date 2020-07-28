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
    public class BucketExistsArgs: BucketArgs
    {
        public BucketExistsArgs()
        {
            Location = "";
            Region = "";
            Secure = false;
            BucketName = "";
        }
        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
        }
        public new RestRequest GetRequest(string baseUrl, RestSharp.Authenticators.IAuthenticator authenticator)
        {
            return RequestUtil.CreateRequest(baseUrl, Method.HEAD, authenticator, this.BucketName, this.Secure, this.Region );
        }
        public new BucketExistsArgs WithBucket(string bucket)
        {
            return (BucketExistsArgs)(base.WithBucket(bucket));
        }

        public new BucketExistsArgs WithLocation(string loc)
        {
            return (BucketExistsArgs)(base.WithLocation(loc));
        }
        public new BucketExistsArgs WithRegion(string reg)
        {
            return (BucketExistsArgs)(base.WithRegion(reg));
        }
        public new BucketExistsArgs WithSSL(bool secure=true)
        {
            return (BucketExistsArgs)(base.WithSSL(secure));
        }
    }

    public class RemoveBucketArgs : BucketArgs
    {
        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
        }
        public new RemoveBucketArgs WithBucket(string bucket)
        {
            return (RemoveBucketArgs)(base.WithBucket(bucket));
        }

        public new RemoveBucketArgs WithLocation(string loc)
        {
            return (RemoveBucketArgs)(base.WithLocation(loc));
        }
        public new RemoveBucketArgs WithRegion(string reg)
        {
            return (RemoveBucketArgs)(base.WithRegion(reg));
        }
        public new RestRequest GetRequest(string baseUrl, RestSharp.Authenticators.IAuthenticator authenticator)
        {
            return RequestUtil.CreateRequest(baseUrl, Method.DELETE, authenticator, this.BucketName, this.Secure, this.Region);
        }
        public new RemoveBucketArgs WithSSL(bool secure=true)
        {
            return (RemoveBucketArgs)(base.WithSSL(secure));
        }
    }

    public class ListObjectsArgs : BucketArgs
    {
        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
        }
        public new ListObjectsArgs WithLocation(string loc)
        {
            return (ListObjectsArgs)(base.WithLocation(loc));
        }
        public new ListObjectsArgs WithRegion(string reg)
        {
            return (ListObjectsArgs)(base.WithRegion(reg));
        }
        public new RestRequest GetRequest(string baseUrl, RestSharp.Authenticators.IAuthenticator authenticator)
        {
            return RequestUtil.CreateRequest(baseUrl, Method.GET, authenticator, this.BucketName, this.Secure,  this.Region);
        }
        public new ListObjectsArgs WithSSL(bool secure=true)
        {
            return (ListObjectsArgs)(base.WithSSL(secure));
        }
    }

    public class MakeBucketArgs : BucketArgs
    {
        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
        }

        public new MakeBucketArgs WithBucket(string bucket)
        {
            return (MakeBucketArgs)(base.WithBucket(bucket));
        }

        public new MakeBucketArgs WithLocation(string loc)
        {
            return (MakeBucketArgs)(base.WithLocation(loc));
        }
        public new MakeBucketArgs WithRegion(string reg)
        {
            return (MakeBucketArgs)(base.WithRegion(reg));
        }

        public new RestRequest GetRequest()
        {
            if (this.Location == "us-east-1")
            {
                if (string.IsNullOrEmpty(this.Region))
                {
                    this.Location = this.Region;
                }
            }
            var request = new RestRequest("/" + this.BucketName, Method.PUT)
            {
                XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer(),
                RequestFormat = DataFormat.Xml
            };
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (this.Location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(this.Location);
                string body = utils.MarshalXML(config, "http://s3.amazonaws.com/doc/2006-03-01/");
                request.AddParameter("text/xml", body, ParameterType.RequestBody);
            }
            return request;
        }
        public new MakeBucketArgs WithSSL(bool secure=true)
        {
            return (MakeBucketArgs)(base.WithSSL(secure));
        }
    }
}