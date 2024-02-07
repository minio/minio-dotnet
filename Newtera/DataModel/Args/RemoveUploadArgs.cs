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

namespace Newtera.DataModel.Args;

public class RemoveUploadArgs : EncryptionArgs<RemoveUploadArgs>
{
    public RemoveUploadArgs()
    {
        RequestMethod = HttpMethod.Delete;
    }

    internal string UploadId { get; private set; }

    public RemoveUploadArgs WithUploadId(string id)
    {
        UploadId = id;
        return this;
    }

    internal override void Validate()
    {
        base.Validate();
        if (string.IsNullOrEmpty(UploadId))
            throw new InvalidOperationException(nameof(UploadId) +
                                                " cannot be empty. Please assign a valid upload ID to remove.");
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("uploadId", $"{UploadId}");
        return requestMessageBuilder;
    }
}
