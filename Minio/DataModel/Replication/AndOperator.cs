/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2021 MinIO, Inc.
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

using System.Collections.ObjectModel;
using System.Xml.Serialization;
using Minio.DataModel.Tags;

/*
 * AndOperator class used within RuleFilter of ReplicationRule which is used to specify rule components and is equivalent of a Logical And for two or more predicates.
 * Please refer:
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketReplication.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutBucketReplication.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_DeleteBucketReplication.html
 */

namespace Minio.DataModel.Replication;

[Serializable]
[XmlRoot(ElementName = "And")]
public class AndOperator
{
    public AndOperator()
    {
    }

    public AndOperator(string prefix, Tagging tagObj)
    {
        if (tagObj is null) throw new ArgumentNullException(nameof(tagObj));

        Prefix = prefix;
        Tags = new Collection<Tag>(tagObj.TaggingSet.Tag);
    }

    public AndOperator(string prefix, ICollection<Tag> tag)
    {
        Prefix = prefix;
        Tags = tag;
    }

    public AndOperator(string prefix, IReadOnlyDictionary<string, string> tags)
    {
        if (tags is null) throw new ArgumentNullException(nameof(tags));

        Prefix = prefix;
        foreach (var item in tags) Tags.Add(new Tag(item.Key, item.Value));
    }

    [XmlElement("Prefix")] internal string Prefix { get; set; }

    [XmlElement("Tag")] public ICollection<Tag> Tags { get; set; }
}
