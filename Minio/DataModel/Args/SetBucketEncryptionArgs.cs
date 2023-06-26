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

using System.Text;
using Minio.DataModel.Encryption;
using Minio.Helper;

namespace Minio.DataModel.Args
{
    public class SetBucketEncryptionArgs : BucketArgs<SetBucketEncryptionArgs>
    {
        public SetBucketEncryptionArgs()
        {
            RequestMethod = HttpMethod.Put;
        }

        internal ServerSideEncryptionConfiguration EncryptionConfig { get; set; }

        public SetBucketEncryptionArgs WithEncryptionConfig(ServerSideEncryptionConfiguration config)
        {
            EncryptionConfig = config;
            return this;
        }

        public SetBucketEncryptionArgs WithAESConfig()
        {
            EncryptionConfig = ServerSideEncryptionConfiguration.GetSSEConfigurationWithS3Rule();
            return this;
        }

        public SetBucketEncryptionArgs WithKMSConfig(string keyId = null)
        {
            EncryptionConfig = ServerSideEncryptionConfiguration.GetSSEConfigurationWithKMSRule(keyId);
            return this;
        }

        internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
        {
            EncryptionConfig ??= ServerSideEncryptionConfiguration.GetSSEConfigurationWithS3Rule();

            requestMessageBuilder.AddQueryParameter("encryption", "");
            var body = Utils.MarshalXML(EncryptionConfig, "http://s3.amazonaws.com/doc/2006-03-01/");
            // Convert string to a byte array
            ReadOnlyMemory<byte> bodyInBytes = Encoding.ASCII.GetBytes(body);
            requestMessageBuilder.BodyParameters.Add("content-type", "text/xml");
            requestMessageBuilder.SetBody(bodyInBytes);

            return requestMessageBuilder;
        }
    }
}