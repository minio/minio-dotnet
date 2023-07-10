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

using Minio.Helper;

namespace Minio.DataModel.Args;

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
        if (!string.IsNullOrEmpty(MatchETag) && !string.IsNullOrEmpty(NotMatchETag))
            throw new InvalidOperationException("Cannot set both " + nameof(MatchETag) + " and " +
                                                nameof(NotMatchETag) + " for query.");

        if (ModifiedSince != default &&
            UnModifiedSince != default)
            throw new InvalidOperationException("Cannot set both " + nameof(ModifiedSince) + " and " +
                                                nameof(UnModifiedSince) + " for query.");
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (!string.IsNullOrEmpty(MatchETag)) requestMessageBuilder.AddOrUpdateHeaderParameter("If-Match", MatchETag);
        if (!string.IsNullOrEmpty(NotMatchETag))
            requestMessageBuilder.AddOrUpdateHeaderParameter("If-None-Match", NotMatchETag);
        if (ModifiedSince != default)
            requestMessageBuilder.AddOrUpdateHeaderParameter("If-Modified-Since", Utils.To8601String(ModifiedSince));
        if (UnModifiedSince != default)
            requestMessageBuilder.AddOrUpdateHeaderParameter("If-Unmodified-Since",
                Utils.To8601String(UnModifiedSince));

        return requestMessageBuilder;
    }

    public T WithMatchETag(string etag)
    {
        MatchETag = etag;
        return (T)this;
    }

    public T WithNotMatchETag(string etag)
    {
        NotMatchETag = etag;
        return (T)this;
    }

    public T WithModifiedSince(DateTime d)
    {
        ModifiedSince = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0);
        return (T)this;
    }

    public T WithUnModifiedSince(DateTime d)
    {
        UnModifiedSince = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0);
        return (T)this;
    }
}
