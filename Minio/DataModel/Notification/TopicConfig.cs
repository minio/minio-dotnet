/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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
using System.Xml.Serialization;

namespace Minio.DataModel
{
    /// <summary>
    /// TopicConfig carries one single topic notification configuration
    /// </summary>
    [Serializable]
    public class TopicConfig : NotificationConfiguration
    {
        [XmlElement]
        public string Topic { get; set; }

        public TopicConfig() : base()
        {
        }

        public TopicConfig(string arn) : base(arn)
        {
            this.Topic = arn;
        }

        public TopicConfig(Arn arn) : base(arn)
        {
            this.Topic = arn.ToString();
        }

        /// <summary>
        /// Implement equality for this object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            TopicConfig other = (TopicConfig)obj;
            // If parameter is null return false.
            if (other == null)
            {
                return false;
            }
            return other.Topic.Equals(this.Topic);
        }

        public override int GetHashCode() => this.Topic.GetHashCode();
    }
}
