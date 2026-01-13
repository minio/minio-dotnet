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

using Minio.DataModel.Tags;

namespace Minio.DataModel.Args;

public class SetObjectTagsArgs : ObjectVersionArgs<SetObjectTagsArgs>
{
    public SetObjectTagsArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal Tagging ObjectTags { get; private set; }

    public SetObjectTagsArgs WithTagging(Tagging tags)
    {
        if (tags is null)
            throw new ArgumentNullException(nameof(tags));

        ObjectTags = Tagging.GetObjectTags(tags.Tags);
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("tagging", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        var body = ObjectTags.MarshalXML();
        requestMessageBuilder.AddXmlBody(body);

        return requestMessageBuilder;
    }

    internal override void Validate()
    {
        base.Validate();
        if (ObjectTags is null || ObjectTags.Tags.Count == 0)
            throw new InvalidOperationException("Unable to set empty tags.");
    }
}
