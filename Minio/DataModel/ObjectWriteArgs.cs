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

using Minio.DataModel.Tags;
using Minio.DataModel.ObjectLock;

namespace Minio
{
    public abstract class ObjectWriteArgs<T> : ObjectConditionalQueryArgs<T>
                            where T : ObjectWriteArgs<T>
    {
        internal Tagging ObjectTags { get; set; }
        internal ObjectRetentionConfiguration Retention { get; set; }
        internal bool? LegalHoldEnabled { get; set; }
        internal string ContentType { get; set; }

        public T WithTagging(Tagging tagging)
        {
            this.ObjectTags = tagging;
            return (T)this;
        }

        public T WithContentType(string type)
        {
            this.ContentType = string.IsNullOrWhiteSpace(type) ? "application/octet-stream" : type;
            if (!this.Headers.ContainsKey("Content-Type"))
            {
                this.Headers["Content-Type"] = type;
            }
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
    }
}