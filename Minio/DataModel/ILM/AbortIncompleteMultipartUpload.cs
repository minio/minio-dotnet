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
 * AbortIncompleteMultipartUpload is a helper class to denote abort incomplete multipart upload information specifically for LifecycleRule.
 * Please refer:
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutBucketLifecycleConfiguration.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketLifecycleConfiguration.html
 */

namespace Minio.DataModel.ILM
{
    [Serializable]
    [XmlRoot(ElementName = "AbortIncompleteMultipartUpload")]
    public class AbortIncompleteMultipartUpload
    {
        [XmlElement(ElementName = "DaysAfterInitiation", IsNullable = true)]
        internal uint? DaysAfterInitiation { get; set; }

        public AbortIncompleteMultipartUpload()
        {
            this.DaysAfterInitiation = null;
        }

        public AbortIncompleteMultipartUpload(uint daysAfterInitiation)
        {
            this.DaysAfterInitiation = daysAfterInitiation;
        }
    }
}