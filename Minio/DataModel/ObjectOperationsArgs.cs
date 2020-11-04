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
using RestSharp;

namespace Minio
{
    public class StatVersionArgs : ObjectVersionArgs<StatVersionArgs>
    {
        public StatVersionArgs()
        {
            this.RequestMethod = Method.GET;
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            if (!string.IsNullOrEmpty(this.VersionId))
            {
                request.AddQueryParameter("versionId", this.VersionId);
            }
            return request;
        }
    }
    public class StatObjectArgs : ObjectQueryArgs<StatObjectArgs>
    {
        internal StatVersionArgs VersionArgs { get; set; }
        public StatObjectArgs()
        {
            this.RequestMethod = Method.HEAD;
            this.VersionArgs = new StatVersionArgs();
        }

        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            request = this.VersionArgs.BuildRequest(request);
            if (!string.IsNullOrEmpty(this.MatchETag))
            {
                request.AddOrUpdateParameter("If-Match", this.MatchETag, ParameterType.HttpHeader);
            }
            if (!string.IsNullOrEmpty(this.NotMatchETag))
            {
                request.AddOrUpdateParameter("If-None-Match", this.NotMatchETag, ParameterType.HttpHeader);
            }
            if (this.ModifiedSince != null && this.ModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("If-Modified-Since", this.ModifiedSince, ParameterType.HttpHeader);
            }
            if(this.UnModifiedSince != null && this.UnModifiedSince != default(DateTime))
            {
                request.AddOrUpdateParameter("If-Unmodified-Since", this.UnModifiedSince, ParameterType.HttpHeader);
            }
            
            return request;
        }

        public override void Validate()
        {
            base.Validate();
            if (!string.IsNullOrEmpty(this.NotMatchETag) && !string.IsNullOrEmpty(this.MatchETag))
            {
                throw new ArgumentException(nameof(this.NotMatchETag) + " and " + nameof(this.MatchETag) + " being set is not allowed.");
            }
            if ((this.ModifiedSince != null && !this.ModifiedSince.Equals(default(DateTime))) &&
                    (this.ModifiedSince != null && !this.UnModifiedSince.Equals(default(DateTime))))
            {
                throw new ArgumentException(nameof(this.NotMatchETag) + " and " + nameof(this.MatchETag) + " being set is not allowed.");
            }
        }

        public StatObjectArgs WithVersionId(string vid)
        {
            this.VersionArgs.WithVersionId(vid);
            return this;
        }
    }
}