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

using System.Text;
using Minio.DataModel.Tags;
using Minio.Helper;

namespace Minio.DataModel.Args;

public class SetBucketTagsArgs : BucketArgs<SetBucketTagsArgs>
{
    public SetBucketTagsArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    internal Tagging BucketTags { get; private set; }

    public SetBucketTagsArgs WithTagging(Tagging tags)
    {
        if (tags is null)
            throw new ArgumentNullException(nameof(tags));

        BucketTags = Tagging.GetBucketTags(tags.Tags);
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("tagging", "");
        var body = BucketTags.MarshalXML();

        requestMessageBuilder.AddXmlBody(body);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            Utils.GetMD5SumStr(Encoding.UTF8.GetBytes(body)));

        //
        return requestMessageBuilder;
    }

    public override void Validate()
    {
        base.Validate();
        if (BucketTags is null || BucketTags.Tags.Count == 0)
            throw new InvalidOperationException("Unable to set empty tags.");
    }
}
