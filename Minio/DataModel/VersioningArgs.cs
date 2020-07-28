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

using System.IO;
using System.Net;
using System.Xml.Serialization;
using Minio.DataModel;
using Minio.Exceptions;
using RestSharp;

namespace Minio
{
    public class GetVersioningInfoArgs : BucketArgs
    {
        public GetVersioningInfoArgs()
        {
        }
        public new GetVersioningInfoArgs WithBucket(string bucket)
        {
            return (GetVersioningInfoArgs)(base.WithBucket(bucket));
        }
        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
        }

        public new RestRequest GetRequest(string baseUrl, RestSharp.Authenticators.IAuthenticator authenticator)
        {
            RestRequest request = RequestUtil.CreateRequest(baseUrl, Method.GET, authenticator, this.BucketName, this.Secure, this.Region);
            request.AddQueryParameter("versioning","");
            return request;
        }

        public VersioningConfiguration ProcessResponse(RestSharp.IRestResponse response)
        {
            VersioningConfiguration config = null;
            if (HttpStatusCode.OK.Equals(response.StatusCode))
            {
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response.Content)))
                {
                    config = (VersioningConfiguration)new XmlSerializer(typeof(VersioningConfiguration)).Deserialize(stream);
                }
                return config;
            }
            return null;
        }
        public new GetVersioningInfoArgs WithSSL(bool secure=true)
        {
            return (GetVersioningInfoArgs)(base.WithSSL(secure));
        }
        public new GetVersioningInfoArgs WithRegion(string reg)
        {
            return (GetVersioningInfoArgs)(base.WithRegion(reg));
        }


        public new System.Uri GetRequestURL(string baseURL)
        {
            // Request is GET.
            bool usePathStyle = false;
            if (this.BucketName != null && this.BucketName.Contains(".") && this.Secure)
            {
                // The '.' in bucket name causes an SSL Validation error.
                usePathStyle = true;
            }

            return RequestUtil.MakeTargetURL(baseURL, this.Secure, this.BucketName, this.Region, usePathStyle);
        }

    }

    public class SetVersioningArgs : BucketArgs
    {
        public new SetVersioningArgs WithBucket(string bucket)
        {
            return (SetVersioningArgs)(base.WithBucket(bucket));
        }

        public SetVersioningArgs()
        {
        }


        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
            if ( !this.Versioned && (this.VersioningEnabled || this.VersioningSuspended) )
            {
                throw new UnexpectedMinioException("For VersioningEnabled or VersioningSuspended to be enabled, enable Versioned.");
            }
            if ( this.Versioned && !this.VersioningEnabled && !this.VersioningSuspended )
            {
                throw new UnexpectedMinioException("If Versioned is enabled, either VersioningEnabled or VersioningSuspended has to be enabled.");
            }
        }

        public new SetVersioningArgs WithVersioningEnabled()
        {
            this.Versioned = true;
            this.VersioningEnabled = true;
            this.VersioningSuspended = false;
            return this;
        }

        public new SetVersioningArgs WithVersioningSuspended()
        {
            this.Versioned = true;
            this.VersioningEnabled = false;
            this.VersioningSuspended = true;
            return this;
        }

        public new RestRequest GetRequest(string baseUrl, RestSharp.Authenticators.IAuthenticator authenticator)
        {
            RestRequest request = new RestRequest("/" + this.BucketName, Method.PUT)
            {
                XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer()
                {
                    Namespace = "http://s3.amazonaws.com/doc/2006-03-01/",
                    ContentType = "application/xml"
                },
                RequestFormat = DataFormat.Xml
            };
            bool versioning = false;
            if ( this.VersioningEnabled )
            {
                versioning = !this.VersioningSuspended;
            }
            VersioningConfiguration config = new VersioningConfiguration(versioning);
            string body = utils.MarshalXML(config, request.XmlSerializer.Namespace);
            request.AddQueryParameter("versioning","");
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            return request;
        }

        public new System.Uri GetRequestURL(string baseURL)
        {
            // Use Path Style set to true - Method.PUT, No object name, no Resource Path
            return RequestUtil.MakeTargetURL(baseURL, this.Secure, this.BucketName, this.Region, true);
        }
        public new SetVersioningArgs WithSSL(bool secure=true)
        {
            return (SetVersioningArgs)(base.WithSSL(secure));
        }
        public new SetVersioningArgs WithRegion(string reg)
        {
            return (SetVersioningArgs)(base.WithRegion(reg));
        }
    }
}