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

using System.Xml.Serialization;
using Minio.DataModel.Tags;

/*
 * RuleFilter class used within LifecycleRule which encapsulates filter information.
 * Please refer:
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutBucketLifecycleConfiguration.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketLifecycleConfiguration.html
 */

namespace Minio.DataModel.ILM
{
    [Serializable]
    [XmlRoot(ElementName = "Filter")]
    public class RuleFilter
    {
        public RuleFilter()
        {
        }

        public RuleFilter(AndOperator theAndOperator, string prefix, Tagging tag)
        {
            TheAndOperator = theAndOperator;
            if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrEmpty(prefix))
            {
                Prefix = null;
            }
            else
            {
                Prefix = prefix;
            }

            if (tag?.TaggingSet.Tag.Count == 0)
            {
                tag = null;
            }
            else
            {
                Tag = tag;
            }
        }

        [XmlElement(ElementName = "And", IsNullable = true)]
        public AndOperator TheAndOperator { get; set; }

        [XmlElement(ElementName = "Prefix", IsNullable = true)]
        public string Prefix { get; set; }

        [XmlElement(ElementName = "Tag", IsNullable = true)]
        public Tagging Tag { get; set; }
    }
}