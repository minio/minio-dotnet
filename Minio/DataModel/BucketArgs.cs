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

using System.Collections.Generic;

namespace Minio;

public abstract class BucketArgs<T> : Args
    where T : BucketArgs<T>
{
    protected const string BucketForceDeleteKey = "X-Minio-Force-Delete";

    public BucketArgs()
    {
        Headers = new Dictionary<string, string>();
    }

    public bool IsBucketCreationRequest { get; set; } = false;

    internal string BucketName { get; set; }

    internal Dictionary<string, string> Headers { get; set; }

    public T WithBucket(string bucket)
    {
        BucketName = bucket;
        return (T)this;
    }

    public T WithHeaders(Dictionary<string, string> headers)
    {
        if (headers == null || headers.Count <= 0) return (T)this;
        Headers = Headers ?? new Dictionary<string, string>();
        foreach (var key in headers.Keys)
        {
            if (Headers.ContainsKey(key))
                Headers.Remove(key);
            Headers[key] = headers[key];
        }

        return (T)this;
    }

    internal virtual void Validate()
    {
        utils.ValidateBucketName(BucketName);
    }
}