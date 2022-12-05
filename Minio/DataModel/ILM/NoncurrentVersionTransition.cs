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

/*
 * NoncurrentVersionTransition is used within LifecycleRule to specify when the noncurrent object transitions and which storage class is to be used.
 * Please refer:
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutBucketLifecycleConfiguration.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketLifecycleConfiguration.html
 */


namespace Minio.DataModel.ILM;

[Serializable]
[XmlRoot(ElementName = "NoncurrentVersionTransition")]
public class NoncurrentVersionTransition : NoncurrentVersionExpiration
{
    public NoncurrentVersionTransition()
    {
        StorageClass = null;
    }

    public NoncurrentVersionTransition(uint nonCurrentDays, string storageClass) : base(nonCurrentDays)
    {
        Transition.CheckStorageClass(storageClass);
        StorageClass = storageClass;
        NoncurrentDays = nonCurrentDays;
    }

    [XmlElement(ElementName = "StorageClass", IsNullable = true)]
    public string StorageClass { get; set; }
}