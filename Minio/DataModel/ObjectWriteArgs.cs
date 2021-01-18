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

using Minio.DataModel;

namespace Minio
{
    public abstract class ObjectWriteArgs<T> : ObjectQueryArgs<T>
                            where T : ObjectWriteArgs<T>
    {
        internal Tagging ObjectTags { get; set; }
        internal ObjectRetentionConfiguration Retention { get; set; }
        internal bool? LegalHoldEnabled { get; set; }
        internal string ContentType { get; set; }
        internal Dictionary<string, string> CustomHeaders { get; set; }

        public T WithTagKeyValuePairs(Dictionary<string, string> kv)
        {
            this.ObjectTags = Tagging.GetObjectTags(kv);
            return (T)this;
        }

        public T WithTagging(Tagging tagging)
        {
            this.ObjectTags = tagging;
            return (T)this;
        }

        public T WithContentType(string type)
        {
            this.ContentType = type;
            return (T)this;
        }

        public T WithRetentionConfiguration(ObjectRetentionConfiguration retentionConfiguration)
        {
            this.Retention = retentionConfiguration;
            return (T)this;
        }

        public T WithLegalHold(bool? legalHold)
        {
            this.LegalHoldEnabled = legalHold;
            return (T)this;
        }

        public T WithCustomHeaders(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count <= 0)
            {
                return (T)this;
            }
            this.CustomHeaders = this.CustomHeaders ?? new Dictionary<string, string>();
            foreach (string key in headers.Keys)
            {
                this.CustomHeaders.Add(key, headers[key]);
            }
            return (T)this;
        }
    }
}