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

namespace Minio
{
    public abstract class BucketArgs<T> : Args 
                where T : BucketArgs<T>
    {
        internal string BucketName { get; set; }
        internal Dictionary<string, string> HeaderMap { get; set; }

        public BucketArgs()
        {
            this.HeaderMap = new Dictionary<string, string>();
        }

        public T WithBucket(string bucket)
        {
            this.BucketName = bucket;
            return (T)this;
        }

        public T WithHeaders(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count > 0)
            {
                return (T)this;
            }
            this.HeaderMap = this.HeaderMap ?? new Dictionary<string, string>();
            foreach (string key in headers.Keys)
            {
                this.HeaderMap.Add(key, headers[key]);
            }
            return (T)this;
        }

        public virtual void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
        }
    }
}