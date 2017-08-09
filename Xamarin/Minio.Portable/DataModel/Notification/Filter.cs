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

    // Filter - a tag in the notification xml structure which carries
    // suffix/prefix filters
    public class Filter
    {
        public Filter()
        {
            this.S3Key = new S3Key();
        }

        public Filter(S3Key key)
        {
            this.S3Key = key;
        }

        [XmlElement("S3Key")]
        public S3Key S3Key { get; set; }

        // Helper to XMLSerializer which decides whether to serialize S3Key
        public bool ShouldSerializeS3Key()
        {
            return this.S3Key.FilterRules.Count != 0;
        }
    }
}