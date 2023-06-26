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

using Minio.DataModel.ObjectLock;
using Minio.DataModel.Tags;
using Minio.Helper;

namespace Minio.DataModel.Args
{
    internal class MultipartCopyUploadArgs : ObjectWriteArgs<MultipartCopyUploadArgs>
    {
        internal MultipartCopyUploadArgs(CopyObjectArgs args)
        {
            if (args is null || args.SourceObject is null)
            {
                var message = args is null
                    ? "The constructor of " + nameof(CopyObjectRequestArgs) +
                      "initialized with arguments of CopyObjectArgs null."
                    : "The constructor of " + nameof(CopyObjectRequestArgs) +
                      "initialized with arguments of CopyObjectArgs type but with " + nameof(args.SourceObject) +
                      " not initialized.";
                throw new InvalidOperationException(message);
            }

            RequestMethod = HttpMethod.Put;

            SourceObject = new CopySourceObjectArgs
            {
                BucketName = args.SourceObject.BucketName,
                ObjectName = args.SourceObject.ObjectName,
                VersionId = args.SourceObject.VersionId,
                CopyOperationConditions = args.SourceObject.CopyOperationConditions.Clone(),
                MatchETag = args.SourceObject.MatchETag,
                ModifiedSince = args.SourceObject.ModifiedSince,
                NotMatchETag = args.SourceObject.NotMatchETag,
                UnModifiedSince = args.SourceObject.UnModifiedSince
            };

            // Destination part.
            BucketName = args.BucketName;
            ObjectName = args.ObjectName ?? args.SourceObject.ObjectName;
            SSE = args.SSE;
            SSE?.Marshal(Headers);
            VersionId = args.VersionId;
            SourceObjectInfo = args.SourceObjectInfo;
            // Header part
            if (!args.ReplaceMetadataDirective)
            {
                Headers = new Dictionary<string, string>(args.SourceObjectInfo.MetaData, StringComparer.Ordinal);
            }
            else if (args.ReplaceMetadataDirective)
            {
                Headers ??= new Dictionary<string, string>(StringComparer.Ordinal);
            }

            if (Headers is not null)
            {
                var newKVList = new List<Tuple<string, string>>();
                foreach (var item in Headers)
                {
                    var key = item.Key;
                    if (!OperationsUtil.IsSupportedHeader(item.Key) &&
                        !item.Key.StartsWith("x-amz-meta", StringComparison.OrdinalIgnoreCase) &&
                        !OperationsUtil.IsSSEHeader(key))
                    {
                        newKVList.Add(new Tuple<string, string>("x-amz-meta-" + key.ToLowerInvariant(), item.Value));
                    }
                }

                foreach (var item in newKVList)
                {
                    Headers[item.Item1] = item.Item2;
                }
            }

            ReplaceTagsDirective = args.ReplaceTagsDirective;
            if (args.ReplaceTagsDirective && args.ObjectTags?.TaggingSet.Tag.Count > 0) // Tags of Source object
            {
                ObjectTags = Tagging.GetObjectTags(args.ObjectTags.Tags);
            }
        }

        internal MultipartCopyUploadArgs()
        {
            RequestMethod = HttpMethod.Put;
        }

        internal CopySourceObjectArgs SourceObject { get; set; }
        internal ObjectStat SourceObjectInfo { get; set; }
        internal long CopySize { get; set; }
        internal bool ReplaceMetadataDirective { get; set; }
        internal bool ReplaceTagsDirective { get; set; }
        internal string StorageClass { get; set; }
        internal ObjectRetentionMode ObjectLockRetentionMode { get; set; }
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
            if (ObjectTags?.TaggingSet?.Tag.Count > 0)
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging-directive",
                    ReplaceTagsDirective ? "REPLACE" : "COPY");
            }

            if (ReplaceMetadataDirective)
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-metadata-directive", "REPLACE");
            }

            if (!string.IsNullOrEmpty(StorageClass))
            {
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-storage-class", StorageClass);
            }

            if (ObjectLockSet)
            {
                if (!RetentionUntilDate.Equals(default))
                {
                    requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                        Utils.To8601String(RetentionUntilDate));
                }

                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode",
                    ObjectLockRetentionMode == ObjectRetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE");
            }

            return requestMessageBuilder;
        }

        internal MultipartCopyUploadArgs WithReplaceMetadataDirective(bool replace)
        {
            ReplaceMetadataDirective = replace;
            return this;
        }

        internal MultipartCopyUploadArgs WithObjectLockMode(ObjectRetentionMode mode)
        {
            ObjectLockSet = true;
            ObjectLockRetentionMode = mode;
            return this;
        }

        internal MultipartCopyUploadArgs WithObjectLockRetentionDate(DateTime untilDate)
        {
            ObjectLockSet = true;
            RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
                untilDate.Hour, untilDate.Minute, untilDate.Second);
            return this;
        }
    }
}