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

internal class NewMultipartUploadCopyArgs : NewMultipartUploadArgs<NewMultipartUploadCopyArgs>
{
    internal bool ReplaceMetadataDirective { get; set; }
    internal bool ReplaceTagsDirective { get; set; }
    internal string StorageClass { get; set; }
    internal ObjectStat SourceObjectInfo { get; set; }

    internal override void Validate()
    {
        base.Validate();
        if (SourceObjectInfo is null)
            throw new InvalidOperationException(nameof(SourceObjectInfo) +
                                                " need to be initialized for a NewMultipartUpload operation to work.");

        Populate();
    }

    private void Populate()
    {
        //Concat as Headers may have byte range info .etc.
        if (!ReplaceMetadataDirective && SourceObjectInfo.MetaData?.Count > 0)
            Headers = SourceObjectInfo.MetaData.Concat(Headers).GroupBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.First().Value, StringComparer.Ordinal);
        else if (ReplaceMetadataDirective) Headers ??= new Dictionary<string, string>(StringComparer.Ordinal);
        if (Headers is not null)
        {
            var newKVList = new List<Tuple<string, string>>();
            foreach (var item in Headers)
            {
                var key = item.Key;
                if (!OperationsUtil.IsSupportedHeader(item.Key) &&
                    !item.Key.StartsWith("x-amz-meta", StringComparison.OrdinalIgnoreCase))
                    newKVList.Add(new Tuple<string, string>("x-amz-meta-" + key.ToLowerInvariant(), item.Value));
            }

            foreach (var item in newKVList) Headers[item.Item1] = item.Item2;
        }
    }

    public new NewMultipartUploadCopyArgs WithHeaders(IDictionary<string, string> headers)
    {
        _ = base.WithHeaders(headers);
        return this;
    }

    internal NewMultipartUploadCopyArgs WithStorageClass(string storageClass)
    {
        StorageClass = storageClass;
        return this;
    }

    internal NewMultipartUploadCopyArgs WithReplaceMetadataDirective(bool replace)
    {
        ReplaceMetadataDirective = replace;
        return this;
    }

    internal NewMultipartUploadCopyArgs WithReplaceTagsDirective(bool replace)
    {
        ReplaceTagsDirective = replace;
        return this;
    }

    public NewMultipartUploadCopyArgs WithSourceObjectInfo(ObjectStat stat)
    {
        SourceObjectInfo = stat;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploads", "");

        if (ReplaceMetadataDirective)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-metadata-directive", "REPLACE");
        if (!string.IsNullOrWhiteSpace(StorageClass))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-storage-class", StorageClass);

        return requestMessageBuilder;
    }
}
