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
    [XmlRoot(ElementName = "ServerSideEncryptionConfiguration", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class ServerSideEncryptionConfiguration
    {
        [XmlElement("Rule")]
        public ServerSideEncryptionConfigurationRule Rule { get; set; }

        public ServerSideEncryptionConfiguration()
        {
            this.Rule = null;
        }

        public ServerSideEncryptionConfiguration(ServerSideEncryptionConfigurationRule rule = null)
        {
            this.Rule = rule;
        }

        public static ServerSideEncryptionConfiguration GetSSEConfigurationWithS3Rule()
        {
            return new ServerSideEncryptionConfiguration(new ServerSideEncryptionConfigurationRule(ServerSideEncryptionConfigurationRule.SSE_AES256));
        }

        public static ServerSideEncryptionConfiguration GetSSEConfigurationWithKMSRule(string masterKeyId = null)
        {
            return new ServerSideEncryptionConfiguration(new ServerSideEncryptionConfigurationRule(ServerSideEncryptionConfigurationRule.SSE_AWSKMS, masterKeyId));
        }
    }
}