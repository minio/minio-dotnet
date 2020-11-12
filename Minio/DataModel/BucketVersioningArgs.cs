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
    public class GetVersioningArgs : BucketArgs<GetVersioningArgs>
    {
        public GetVersioningArgs()
        {
            this.RequestMethod = Method.GET;
        }

        public override RestRequest BuildRequest(RestRequest req)
        {
            req.AddQueryParameter("versioning","");
            return req;
        }
    }

    public class SetVersioningArgs : BucketArgs<SetVersioningArgs>
    {
        internal VersioningStatus CurrentVersioningStatus;
        internal enum VersioningStatus : ushort
        {
            Off = 0,
            Enabled = 1,
            Suspended = 2,
        }
        public SetVersioningArgs()
        {
            this.RequestMethod = Method.PUT;
            this.CurrentVersioningStatus = VersioningStatus.Off;
        }
        public override void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
            if (this.CurrentVersioningStatus > VersioningStatus.Suspended )
            {
                throw new UnexpectedMinioException("CurrentVersioningStatus invalid value .");
            }
        }

        public SetVersioningArgs WithVersioningEnabled()
        {
            this.CurrentVersioningStatus = VersioningStatus.Enabled;
            return this;
        }

        public SetVersioningArgs WithVersioningSuspended()
        {
            this.CurrentVersioningStatus = VersioningStatus.Suspended;
            return this;
        }

        public override RestRequest BuildRequest(RestRequest req)
        {
            VersioningConfiguration config = new VersioningConfiguration((this.CurrentVersioningStatus == VersioningStatus.Enabled));
            req.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            req.XmlSerializer.Namespace = "http://s3.amazonaws.com/doc/2006-03-01/";
            req.XmlSerializer.ContentType = "application/xml";
            string body = utils.MarshalXML(config, req.XmlSerializer.Namespace);
            req.AddQueryParameter("versioning","");
            req.AddParameter("text/xml", body, ParameterType.RequestBody);
            return req;
        }
    }
}