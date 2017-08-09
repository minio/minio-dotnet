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

    // LambdaConfig carries one single cloudfunction notification configuration
    public class LambdaConfig : NotificationConfiguration
    {
        public LambdaConfig()
        {
        }

        public LambdaConfig(string arn) : base(arn)
        {
            this.Lambda = arn;
        }

        public LambdaConfig(Arn arn) : base(arn)
        {
            this.Lambda = arn.ToString();
        }

        [XmlElement("CloudFunction")]
        public string Lambda { get; }

        // Implement equality for this object
        public override bool Equals(object obj)
        {
            var other = obj as LambdaConfig;
            // If parameter is null return false.
            return other != null && other.Lambda.Equals(this.Lambda);
        }

        public override int GetHashCode()
        {
            return this.Lambda.GetHashCode();
        }
    }
}