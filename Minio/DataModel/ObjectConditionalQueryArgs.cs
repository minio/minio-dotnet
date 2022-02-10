/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
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
using System.Net.Http;

namespace Minio
{
    public abstract class ObjectConditionalQueryArgs<T> : ObjectVersionArgs<T>
                                    where T : ObjectConditionalQueryArgs<T>
    {
        internal string MatchETag { get; set; }
        internal string NotMatchETag { get; set; }
        internal DateTime ModifiedSince { get; set; }
        internal DateTime UnModifiedSince { get; set; }

        internal override void Validate()
        {
            base.Validate();
            if (!string.IsNullOrEmpty(this.MatchETag) && !string.IsNullOrEmpty(this.NotMatchETag))
            {
                throw new InvalidOperationException("Cannot set both " + nameof(MatchETag) + " and " + nameof(NotMatchETag) + " for query.");
            }
            if (this.ModifiedSince != default(DateTime) &&
                this.UnModifiedSince != default(DateTime))
            {
                throw new InvalidOperationException("Cannot set both " + nameof(ModifiedSince) + " and " + nameof(UnModifiedSince) + " for query.");
            }
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            if (!string.IsNullOrEmpty(this.MatchETag))
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("If-Match", this.MatchETag);
            }
            if (!string.IsNullOrEmpty(this.NotMatchETag))
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("If-None-Match", this.NotMatchETag);
            }
            if (this.ModifiedSince != default(DateTime))
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("If-Modified-Since", utils.To8601String(this.ModifiedSince));
            }
            if (this.UnModifiedSince != default(DateTime))
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("If-Unmodified-Since", utils.To8601String(this.UnModifiedSince));
            }
            return requestMessageBuilder;
        }
        public T WithMatchETag(string etag)
        {
            this.MatchETag = etag;
            return (T)this;
        }
        public T WithNotMatchETag(string etag)
        {
            this.NotMatchETag = etag;
            return (T)this;
        }
        public T WithModifiedSince(DateTime d)
        {
            this.ModifiedSince = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0);
            return (T)this;
        }
        public T WithUnModifiedSince(DateTime d)
        {
            this.UnModifiedSince = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0);
            return (T)this;
        }
    }
}
