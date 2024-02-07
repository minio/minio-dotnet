/*
 * Newtera .NET Library for Newtera TDM, (C) 2020, 2021 Newtera, Inc.
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

using Newtera.Helper;

namespace Newtera.DataModel.Args;

internal class CopyObjectRequestArgs : ObjectWriteArgs<CopyObjectRequestArgs>
{
    internal CopyObjectRequestArgs()
    {
        RequestMethod = HttpMethod.Put;
        Headers = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    internal ObjectStat SourceObjectInfo { get; set; }
    internal Type CopyOperationObjectType { get; set; }
    internal bool ReplaceTagsDirective { get; set; }
    internal bool ReplaceMetadataDirective { get; set; }
    internal string StorageClass { get; set; }
    internal Dictionary<string, string> QueryMap { get; set; }
    internal CopyConditions CopyCondition { get; set; }
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

    public CopyObjectRequestArgs WithSourceObjectInfo(ObjectStat stat)
    {
        SourceObjectInfo = stat;
        return this;
    }

    internal CopyObjectRequestArgs WithCopyOperationObjectType(Type cp)
    {
        CopyOperationObjectType = cp;
        return this;
    }

    internal override void Validate()
    {
        Utils.ValidateBucketName(BucketName); //Object name can be same as that of source.
        Populate();
    }

    internal void Populate()
    {
        // Opting for concat as Headers may have byte range info .etc.
        if (!ReplaceMetadataDirective && SourceObjectInfo.MetaData is not null)
            Headers = SourceObjectInfo.MetaData.Concat(Headers).GroupBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.First().Value, StringComparer.Ordinal);
        else if (ReplaceMetadataDirective) Headers ??= new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
