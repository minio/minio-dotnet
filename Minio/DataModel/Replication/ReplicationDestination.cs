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

/*
 * ReplicationDestination class used within ReplicationRule to denote information about the destination of the operation.
 * Please refer:
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketReplication.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutBucketReplication.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_DeleteBucketReplication.html
 */

namespace Minio.DataModel.Replication;

[Serializable]
[XmlRoot(ElementName = "Destination")]
public class ReplicationDestination
{
    public ReplicationDestination(AccessControlTranslation accessControlTranslation, string account,
        string bucketARN, EncryptionConfiguration encryptionConfiguration,
        Metrics metrics, ReplicationTime replicationTime, string storageClass)
    {
        AccessControlTranslation = accessControlTranslation;
        Account = account;
        BucketARN = bucketARN;
        EncryptionConfiguration = encryptionConfiguration;
        Metrics = metrics;
        ReplicationTime = replicationTime;
        StorageClass = storageClass;
    }

    public ReplicationDestination()
    {
    }

    [XmlElement(ElementName = "AccessControlTranslation", IsNullable = true)]
    public AccessControlTranslation AccessControlTranslation { get; set; }

    [XmlElement(ElementName = "Account", IsNullable = true)]
    public string Account { get; set; }

    [XmlElement("Bucket")] public string BucketARN { get; set; }

    [XmlElement(ElementName = "EncryptionConfiguration", IsNullable = true)]
    public EncryptionConfiguration EncryptionConfiguration { get; set; }

    [XmlElement(ElementName = "Metrics", IsNullable = true)]
    public Metrics Metrics { get; set; }

    [XmlElement(ElementName = "ReplicationTime", IsNullable = true)]
    public ReplicationTime ReplicationTime { get; set; }

    [XmlElement(ElementName = "StorageClass", IsNullable = true)]
    public string StorageClass { get; set; }
}
