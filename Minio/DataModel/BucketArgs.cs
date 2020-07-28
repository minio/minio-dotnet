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
using System.Collections;
using Minio.Exceptions;

namespace Minio
{
    public class BucketArgs: Args
    {
        internal string BucketName { get; set; }
        internal string Location { get; set; }
        internal string Region { get; set; }
        internal bool Secure { get; set; }
        internal string LifecycleConfig { get; set; }
        // <String, String>
        internal Hashtable TagKeyValue { get; set; }
        internal bool Versioned { get; set; }
        internal bool VersioningEnabled { get; set; }
        internal bool VersioningSuspended { get; set; }

        public new void Validate()
        {
            utils.ValidateBucketName(this.BucketName);
        }
        public BucketArgs WithBucket(string bucket)
        {
            this.BucketName = bucket;
            return this;
        }

        public BucketArgs WithRegion(string reg)
        {
            this.Region = reg;
            return this;
        }

        public BucketArgs WithLocation(string loc)
        {
            this.Location = loc;
            return this;
        }

        public BucketArgs WithLifecycleConfig(string lc)
        {
            this.LifecycleConfig = lc;
            return this;
        }

        public BucketArgs WithTags(Hashtable t)
        {
            this.TagKeyValue = CloneHashTable(t);
            return this;
        }

        public BucketArgs WithVersioningEnabled()
        {
            this.VersioningEnabled = true;
            this.VersioningSuspended = false;
            if ( !this.Versioned )
            {
                this.Versioned = true;
            }
            return this;
        }

        public BucketArgs WithVersioningSuspended()
        {
            this.VersioningEnabled = false;
            this.VersioningSuspended = true;
            if ( !this.Versioned )
            {
                this.Versioned = true;
            }
            return this;
        }
        public BucketArgs WithSSL(bool secure=true)
        {
            this.Secure = secure;
            return this;
        }

        public System.Uri GetRequestURL(string baseURL)
        {
            // Use Path Style set to false - Just the default
            return RequestUtil.MakeTargetURL(baseURL, this.Secure, this.BucketName, this.Region, false);
        }


    }
}