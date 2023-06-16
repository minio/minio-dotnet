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
using Minio.DataModel.Result;
using Minio.Helper;

namespace Minio.DataModel.Args;

internal class CopyObjectRequestArgs : ObjectWriteArgs<CopyObjectRequestArgs>
{
    internal CopyObjectRequestArgs()
    {
        RequestMethod = HttpMethod.Put;
        Headers = new Dictionary<string, string>(StringComparer.Ordinal);
        CopyOperationObjectType = typeof(CopyObjectResult);
    }

    internal CopySourceObjectArgs SourceObject { get; set; }
    internal ObjectStat SourceObjectInfo { get; set; }
    internal Type CopyOperationObjectType { get; set; }
    internal bool ReplaceTagsDirective { get; set; }
    internal bool ReplaceMetadataDirective { get; set; }
    internal string StorageClass { get; set; }
    internal Dictionary<string, string> QueryMap { get; set; }
    internal CopyConditions CopyCondition { get; set; }
    internal RetentionMode ObjectLockRetentionMode { get; set; }
    internal DateTime RetentionUntilDate { get; set; }
    internal bool ObjectLockSet { get; set; }

    internal CopyObjectRequestArgs WithQueryMap(IDictionary<string, string> queryMap)
    {
        QueryMap = new Dictionary<string, string>(queryMap, StringComparer.Ordinal);
        return this;
    }

    internal CopyObjectRequestArgs WithPartCondition(CopyConditions partCondition)
    {
        CopyCondition = partCondition.Clone();
        Headers ??= new Dictionary<string, string>(StringComparer.Ordinal);
        Headers["x-amz-copy-source-range"] = "bytes=" + partCondition.byteRangeStart + "-" + partCondition.byteRangeEnd;

        return this;
    }

    internal CopyObjectRequestArgs WithReplaceMetadataDirective(bool replace)
    {
        ReplaceMetadataDirective = replace;
        return this;
    }

    internal CopyObjectRequestArgs WithReplaceTagsDirective(bool replace)
    {
        ReplaceTagsDirective = replace;
        return this;
    }

    public CopyObjectRequestArgs WithCopyObjectSource(CopySourceObjectArgs cs)
    {
        if (cs is null)
            throw new InvalidOperationException("The copy source object needed for copy operation is not initialized.");

        SourceObject ??= new CopySourceObjectArgs();
        SourceObject.RequestMethod = HttpMethod.Put;
        SourceObject.BucketName = cs.BucketName;
        SourceObject.ObjectName = cs.ObjectName;
        SourceObject.VersionId = cs.VersionId;
        SourceObject.SSE = cs.SSE;
        SourceObject.Headers = new Dictionary<string, string>(cs.Headers, StringComparer.Ordinal);
        SourceObject.MatchETag = cs.MatchETag;
        SourceObject.ModifiedSince = cs.ModifiedSince;
        SourceObject.NotMatchETag = cs.NotMatchETag;
        SourceObject.UnModifiedSince = cs.UnModifiedSince;
        SourceObject.CopySourceObjectPath = $"{cs.BucketName}/{Utils.UrlEncode(cs.ObjectName)}";
        SourceObject.CopyOperationConditions = cs.CopyOperationConditions?.Clone();
        return this;
    }

    public CopyObjectRequestArgs WithSourceObjectInfo(ObjectStat stat)
    {
        SourceObjectInfo = stat;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        var sourceObjectPath = SourceObject.BucketName + "/" + Utils.UrlEncode(SourceObject.ObjectName);
        if (!string.IsNullOrEmpty(SourceObject.VersionId)) sourceObjectPath += "?versionId=" + SourceObject.VersionId;
        // Set the object source
        requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source", sourceObjectPath);

        if (QueryMap is not null)
            foreach (var query in QueryMap)
                requestMessageBuilder.AddQueryParameter(query.Key, query.Value);

        if (SourceObject.CopyOperationConditions is not null)
            foreach (var item in SourceObject.CopyOperationConditions.Conditions)
                requestMessageBuilder.AddOrUpdateHeaderParameter(item.Key, item.Value);

        if (!string.IsNullOrEmpty(MatchETag))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-match", MatchETag);
        if (!string.IsNullOrEmpty(NotMatchETag))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-none-match", NotMatchETag);
        if (ModifiedSince != default)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-unmodified-since",
                Utils.To8601String(ModifiedSince));

        if (UnModifiedSince != default)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-copy-source-if-modified-since",
                Utils.To8601String(UnModifiedSince));

        if (ObjectTags?.TaggingSet?.Tag.Count > 0)
        {
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging", ObjectTags.GetTagString());
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging-directive",
                ReplaceTagsDirective ? "REPLACE" : "COPY");
            if (ReplaceMetadataDirective)
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-tagging-directive", "REPLACE");
        }

        if (ReplaceMetadataDirective)
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-metadata-directive", "REPLACE");
        if (!string.IsNullOrEmpty(StorageClass))
            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-storage-class", StorageClass);
        if (ObjectLockSet)
        {
            if (!RetentionUntilDate.Equals(default))
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-retain-until-date",
                    Utils.To8601String(RetentionUntilDate));

            requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-object-lock-mode",
                ObjectLockRetentionMode == RetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE");
        }

        if (!RequestBody.IsEmpty) requestMessageBuilder.SetBody(RequestBody);
        return requestMessageBuilder;
    }

    internal CopyObjectRequestArgs WithCopyOperationObjectType(Type cp)
    {
        CopyOperationObjectType = cp;
        return this;
    }

    public CopyObjectRequestArgs WithObjectLockMode(RetentionMode mode)
    {
        ObjectLockSet = true;
        ObjectLockRetentionMode = mode;
        return this;
    }

    public CopyObjectRequestArgs WithObjectLockRetentionDate(DateTime untilDate)
    {
        ObjectLockSet = true;
        RetentionUntilDate = new DateTime(untilDate.Year, untilDate.Month, untilDate.Day,
            untilDate.Hour, untilDate.Minute, untilDate.Second);
        return this;
    }

    internal override void Validate()
    {
        Utils.ValidateBucketName(BucketName); //Object name can be same as that of source.
        if (SourceObject is null) throw new InvalidOperationException(nameof(SourceObject) + " has not been assigned.");
        Populate();
    }

    internal void Populate()
    {
        ObjectName = string.IsNullOrEmpty(ObjectName) ? SourceObject.ObjectName : ObjectName;
        // Opting for concat as Headers may have byte range info .etc.
        if (!ReplaceMetadataDirective && SourceObjectInfo.MetaData is not null)
            Headers = SourceObjectInfo.MetaData.Concat(Headers).GroupBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.First().Value, StringComparer.Ordinal);
        else if (ReplaceMetadataDirective) Headers ??= new Dictionary<string, string>(StringComparer.Ordinal);
    }
}