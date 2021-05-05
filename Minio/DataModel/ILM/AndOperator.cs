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

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

/*
 * AndOperator is used with Lifecycle RuleFilter to bind the rules together.
 * Please refer:
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutBucketLifecycleConfiguration.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketLifecycleConfiguration.html
 */


namespace Minio.DataModel.ILM
{
    [Serializable]
    [XmlRoot(ElementName = "And")]
    public class AndOperator
    {
        [XmlElement("Prefix")]
        internal string Prefix { get; set; }
        [XmlElement(ElementName = "Tag", IsNullable = false)]
        public List<Tag> Tags { get; set; }

        public AndOperator()
        {
        }

        public AndOperator(string prefix, List<Tag> tag)
        {
            Prefix = prefix;
            if (tag != null && tag.Count > 0)
            {
                Tags = new List<Tag>(tag);
            }
        }

        public AndOperator(string prefix, Dictionary<string, string> tags)
        {
            Prefix = prefix;
            if (tags == null || tags.Count == 0)
                return;
            foreach (var item in tags)
            {
                this.Tags.Add(new Tag(item.Key, item.Value));
            }
        }

    }
}