﻿/*
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

namespace Newtera.DataModel.Args;

public class RemoveObjectArgs : ObjectArgs<RemoveObjectArgs>
{
    public RemoveObjectArgs()
    {
        RequestMethod = HttpMethod.Delete;
        BypassGovernanceMode = null;
    }

    internal string VersionId { get; private set; }
    internal bool? BypassGovernanceMode { get; private set; }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        if (!string.IsNullOrEmpty(VersionId))
        {
            requestMessageBuilder.AddQueryParameter("versionId", $"{VersionId}");
            if (BypassGovernanceMode == true)
                requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-bypass-governance-retention",
                    BypassGovernanceMode.Value.ToString());
        }

        return requestMessageBuilder;
    }

    public RemoveObjectArgs WithVersionId(string ver)
    {
        VersionId = ver;
        return this;
    }

    public RemoveObjectArgs WithBypassGovernanceMode(bool? mode)
    {
        BypassGovernanceMode = mode;
        return this;
    }
}
