/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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

namespace Minio.DataModel.Notification
{
    using System.Xml.Serialization;

    // FilterRule - child of S3Key, a tag in the notification xml which
    // carries suffix/prefix filters
    public class FilterRule
    {
        public FilterRule()
        {
            this.Name = null;
            this.Value = null;
        }

        public FilterRule(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public string Value { get; set; }

        public bool ShouldSerializeName()
        {
            return this.Name != null;
        }

        public bool ShouldSerializeValue()
        {
            return this.Value != null;
        }
    }
}