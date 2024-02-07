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

public class CopyObjectArgs : ObjectWriteArgs<CopyObjectArgs>
{
    public CopyObjectArgs()
    {
        RequestMethod = HttpMethod.Put;
        ReplaceTagsDirective = false;
        ReplaceMetadataDirective = false;
        ObjectLockSet = false;
    }

    internal ObjectStat SourceObjectInfo { get; set; }
    internal bool ReplaceTagsDirective { get; set; }
    internal bool ReplaceMetadataDirective { get; set; }
    internal string StorageClass { get; set; }
    internal bool ObjectLockSet { get; set; }

    internal override void Validate()
    {
        Utils.ValidateBucketName(BucketName);

        if (!string.IsNullOrEmpty(NotMatchETag) && !string.IsNullOrEmpty(MatchETag))
            throw new InvalidOperationException("Invalid to set both Etag match conditions " + nameof(NotMatchETag) +
                                                " and " + nameof(MatchETag));

        if (!ModifiedSince.Equals(default) &&
            !UnModifiedSince.Equals(default))
            throw new InvalidOperationException("Invalid to set both modified date match conditions " +
                                                nameof(ModifiedSince) + " and " + nameof(UnModifiedSince));

        Populate();
    }

    private void Populate()
    {
        Headers ??= new Dictionary<string, string>(StringComparer.Ordinal);
        if (ReplaceMetadataDirective)
        {
            if (Headers is not null)
#if NETSTANDARD
                foreach (var pair in SourceObjectInfo.MetaData.ToList())
#else
                foreach (var pair in SourceObjectInfo.MetaData)
#endif
                {
                    var comparer = StringComparer.OrdinalIgnoreCase;
                    var newDictionary = new Dictionary<string, string>(Headers, comparer);

                    SourceObjectInfo.MetaData.Remove(pair.Key);
                }

            Headers = Headers
                .Concat(SourceObjectInfo.MetaData)
                .GroupBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item =>
                    item.Last().Value, StringComparer.Ordinal);
        }

        if (Headers is not null)
        {
            var newKVList = new List<Tuple<string, string>>();
#if NETSTANDARD
            foreach (var item in Headers.ToList())
#else
            foreach (var item in Headers)
#endif
            {
                var key = item.Key;
                if (!OperationsUtil.IsSupportedHeader(item.Key) &&
                    !item.Key.StartsWith("x-amz-meta",
                        StringComparison.OrdinalIgnoreCase))
                {
                    newKVList.Add(new Tuple<string, string>("x-amz-meta-" +
                                                            key.ToLowerInvariant(), item.Value));
                    Headers.Remove(item.Key);
                }

                newKVList.Add(new Tuple<string, string>(key, item.Value));
            }

            foreach (var item in newKVList) Headers[item.Item1] = item.Item2;
        }
    }

    public CopyObjectArgs WithReplaceTagsDirective(bool replace)
    {
        ReplaceTagsDirective = replace;
        return this;
    }

    public CopyObjectArgs WithReplaceMetadataDirective(bool replace)
    {
        ReplaceMetadataDirective = replace;
        return this;
    }

    internal CopyObjectArgs WithStorageClass(string storageClass)
    {
        StorageClass = storageClass;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (!string.IsNullOrEmpty(MatchETag))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-match", MatchETag);
        if (!string.IsNullOrEmpty(NotMatchETag))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-none-match", NotMatchETag);
        if (ModifiedSince != default)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-unmodified-since",
                Utils.To8601String(ModifiedSince));

        if (UnModifiedSince != default)
        {
            using var request = requestMessageBuilder.Request;
            request.Headers.Add("x-amz-copy-source-if-modified-since",
                Utils.To8601String(UnModifiedSince));
        }

        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);

        if (ReplaceMetadataDirective)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-metadata-directive", "REPLACE");
        if (!string.IsNullOrEmpty(StorageClass))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-storage-class", StorageClass);
        if (LegalHoldEnabled == true)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-legal-hold", "ON");

        return requestMessageBuilder;
    }
}
