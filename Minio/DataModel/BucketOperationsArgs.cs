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
        public BucketExistsArgs(string bucketName)
                :base(bucketName)
        {
        }
    }

    public class RemoveBucketArgs : BucketArgs
    {
        public RemoveBucketArgs(string bucketName)
                    : base (bucketName)
        {
        }
    }

    public class MakeBucketArgs : BucketArgs
    {
        public MakeBucketArgs(string bucketName)
                    : base(bucketName)
        {
        }

        internal string Location { get; set; }
        public MakeBucketArgs WithLocation(string loc)
        {
            this.Location = loc;
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
            return request;
        }
    }
}