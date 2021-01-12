/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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

namespace Minio
{
    [Serializable]
    [XmlRoot(ElementName = "Rule", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class ServerSideEncryptionConfigurationRule
    {
        internal const string SSE_AES256 = "AES256";
        internal const string SSE_AWSKMS = "aws:kms";
        [XmlElement("ApplyServerSideEncryptionByDefault")]
        public ServerSideEncryptionConfigurationApply Apply { get; set; }

        public ServerSideEncryptionConfigurationRule()
        {
            this.Apply = new ServerSideEncryptionConfigurationApply();
        }

        public ServerSideEncryptionConfigurationRule(string algorithm = SSE_AES256, string keyId = null)
        {
            this.Apply = new ServerSideEncryptionConfigurationApply(algorithm, keyId);
        }

        public class ServerSideEncryptionConfigurationApply
        {
            [XmlElement("KMSMasterKeyID")]
            public string KMSMasterKeyId { get; set; }
            [XmlElement("SSEAlgorithm")]
            public string SSEAlgorithm { get; set; }

            public ServerSideEncryptionConfigurationApply()
            {
                this.SSEAlgorithm = SSE_AES256;
                this.KMSMasterKeyId = null;
            }
            public ServerSideEncryptionConfigurationApply(string algorithm = SSE_AES256, string keyId = null)
            {
                if (string.IsNullOrEmpty(algorithm))
                {
                    throw new ArgumentNullException("The SSE Algorithm " + nameof(SSEAlgorithm) + " cannot be null or empty");
                }
                this.SSEAlgorithm = algorithm;
                this.KMSMasterKeyId = keyId;
            }

        }
    }
}