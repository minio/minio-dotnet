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
using System.IO;
using RestSharp;

using Minio.DataModel;
using Minio.Exceptions;

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
            this.VersionArgs.VersionId = vid;
            return this;
        }

        public StatObjectArgs WithStatVersionArgs(StatVersionArgs versionArgs)
        {
            this.VersionArgs.WithVersionId(versionArgs.VersionId);
            return this;
        }
    }

    public class GetObjectVersionArgs : ObjectVersionArgs<GetObjectVersionArgs>
    {
        public GetObjectVersionArgs()
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

    public class GetObjectArgs : ObjectQueryArgs<GetObjectArgs>
    {
        internal Action<Stream> CallBack { get; private set; }
        internal long ObjectOffset { get; private set; }
        internal long ObjectLength { get; private set; }
        internal string FileName { get; private set; }
        private bool OffsetLengthSet { get; set; }
        // We need to expose the value of the above VersionArgs
        internal string VersionId { get; set; }

        public GetObjectArgs()
        {
            this.RequestMethod = Method.GET;
            this.OffsetLengthSet = false;
        }

        public override void Validate()
        {
            base.Validate();
            if (this.CallBack == null)
            {
                throw new MinioException("CallBack method not set of GetObject operation.");
            }
            if (OffsetLengthSet)
            {
                if (this.ObjectOffset < 0)
                {
                    throw new ArgumentException("Offset should be zero or greater", nameof(this.ObjectOffset));
                }

                if (this.ObjectLength < 0)
                {
                    throw new ArgumentException("Length should be greater than zero", nameof(this.ObjectLength));
                }
            }
            if (this.FileName != null)
            {
                utils.ValidateFile(this.FileName);
            }
        }
        public override RestRequest BuildRequest(RestRequest request)
        {
            request = base.BuildRequest(request);
            var headers = new Dictionary<string, string>();
            if (this.SSE != null && this.SSE.GetType().Equals(EncryptionType.SSE_C))
            {
                this.SSE.Marshal(headers);
            }
            request.ResponseWriter = this.CallBack;

            return request;
        }   


        public GetObjectArgs WithCallbackStream(Action<Stream> cb)
        {
            this.CallBack = cb;
            return this;
        }

        public GetObjectArgs WithLengthAndOffset(long offset, long length)
        {
            this.OffsetLengthSet = true;
            this.ObjectOffset = offset;
            this.ObjectLength = length;
            if (ObjectLength > 0)
            {
                this.HeaderMap.Add("Range", "bytes=" + offset.ToString() + "-" + (offset + length - 1).ToString());
            }

            if (this.SSE != null && this.SSE.GetType().Equals(EncryptionType.SSE_C))
            {
                this.SSE.Marshal(this.HeaderMap);
            }
            return this;
        }

        public GetObjectArgs WithFile(string file)
        {
            this.FileName = file;
            return this;
        }

        public GetObjectArgs WithVersionId(string vid)
        {
            this.VersionId = vid;
            return this;
        }
    }
}