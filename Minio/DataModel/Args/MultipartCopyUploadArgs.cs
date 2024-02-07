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

internal class MultipartCopyUploadArgs : ObjectWriteArgs<MultipartCopyUploadArgs>
{
    internal MultipartCopyUploadArgs(CopyObjectArgs args)
    {
        if (args is null)
        {
            var message = args is null
                ? "The constructor of " + nameof(CopyObjectRequestArgs) +
                  "initialized with arguments of CopyObjectArgs null."
                : "The constructor of " + nameof(CopyObjectRequestArgs) +
                  " not initialized.";
            throw new InvalidOperationException(message);
        }

        RequestMethod = HttpMethod.Put;

        // Destination part.
        BucketName = args.BucketName;
        ObjectName = args.ObjectName;
        VersionId = args.VersionId;
        SourceObjectInfo = args.SourceObjectInfo;
        // Header part
        if (!args.ReplaceMetadataDirective)
            Headers = new Dictionary<string, string>(args.SourceObjectInfo.MetaData, StringComparer.Ordinal);
        else if (args.ReplaceMetadataDirective) Headers ??= new Dictionary<string, string>(StringComparer.Ordinal);
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

        ReplaceTagsDirective = args.ReplaceTagsDirective;
    }

    internal MultipartCopyUploadArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal ObjectStat SourceObjectInfo { get; set; }
    internal long CopySize { get; set; }
    internal bool ReplaceMetadataDirective { get; set; }
    internal bool ReplaceTagsDirective { get; set; }
    internal string StorageClass { get; set; }
    internal DateTime RetentionUntilDate { get; set; }
    internal bool ObjectLockSet { get; set; }

    internal MultipartCopyUploadArgs WithCopySize(long copySize)
    {
        CopySize = copySize;
        return this;
    }

    internal MultipartCopyUploadArgs WithStorageClass(string storageClass)
    {
        StorageClass = storageClass;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (ReplaceMetadataDirective)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-metadata-directive", "REPLACE");
        if (!string.IsNullOrEmpty(StorageClass))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-storage-class", StorageClass);

        return requestMessageBuilder;
    }

    internal MultipartCopyUploadArgs WithReplaceMetadataDirective(bool replace)
    {
        ReplaceMetadataDirective = replace;
        return this;
    }
}
