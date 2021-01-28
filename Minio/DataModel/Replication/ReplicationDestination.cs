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
using System.Xml.Serialization;

namespace Minio.DataModel
{
    [Serializable]
    [XmlRoot(ElementName = "Destination")]
    public class ReplicationDestination
    {
        [XmlElement(ElementName = "AccessControlTranslation", IsNullable = true)]
        public AccessControlTranslation AccessControlTranslation { get; set; }
        [XmlElement(ElementName = "Account", IsNullable = true)]
        public String Account { get; set; }
        [XmlElement("Bucket")]
        public String BucketARN { get; set; }
        [XmlElement(ElementName = "EncryptionConfiguration", IsNullable = true)]
        public EncryptionConfiguration EncryptionConfiguration { get; set; }
        [XmlElement(ElementName = "Metrics", IsNullable = true)]
        public Metrics Metrics { get; set; }
        [XmlElement(ElementName = "ReplicationTime", IsNullable = true)]
        public ReplicationTime ReplicationTime { get; set; }
        [XmlElement(ElementName = "StorageClass", IsNullable = true)]
        public String StorageClass { get; set; }

        public ReplicationDestination(AccessControlTranslation accessControlTranslation, String account,
                    String bucketARN, EncryptionConfiguration encryptionConfiguration,
                    Metrics metrics, ReplicationTime replicationTime, String storageClass)
        {
            this.AccessControlTranslation = accessControlTranslation;
            this.Account = account;
            this.BucketARN = bucketARN;
            this.EncryptionConfiguration = encryptionConfiguration;
            this.Metrics = metrics;
            this.ReplicationTime = replicationTime;
            this.StorageClass = storageClass;
        }

        public ReplicationDestination()
        {
        }
    }
}