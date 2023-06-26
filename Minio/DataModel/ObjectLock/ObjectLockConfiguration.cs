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

using System.Xml.Serialization;

namespace Minio.DataModel.ObjectLock
{
    [Serializable]
    [XmlRoot(ElementName = "ObjectLockConfiguration", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class ObjectLockConfiguration
    {
        public const string LockEnabled = "Enabled";

        public ObjectLockConfiguration()
        {
            ObjectLockEnabled = LockEnabled;
        }

        public ObjectLockConfiguration(ObjectRetentionMode mode, int numOfDays)
        {
            ObjectLockEnabled = LockEnabled;
            Rule = new ObjectLockRule(mode, numOfDays);
        }

        [XmlElement("ObjectLockEnabled")] public string ObjectLockEnabled { get; set; }

        [XmlElement("Rule")] public ObjectLockRule Rule { get; set; }
    }
}