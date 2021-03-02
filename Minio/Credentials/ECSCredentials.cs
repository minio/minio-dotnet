/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2021 MinIO, Inc.
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

using Minio.DataModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Minio.Credentials
{
    public class ECSCredentials
    {
        [JsonProperty("AccessKeyId")]
        public string AccessKeyId { get; set; }
        [JsonProperty("SecretAccessKey")]
        public string SecretAccessKey { get; set; }
        [JsonProperty("Token")]
        public string SessionToken { get; set; }
        [JsonProperty("Expiration")]
        public string ExpirationDate { get; set; }
        [JsonProperty("Code")]
        public string Code { get; set; }
        [JsonProperty("Message")]
        public string Message { get; set; }
        [JsonProperty("Type")]
        public string Type { get; set; }
        [JsonProperty("LastUpdated")]
        public string LastUpdated { get; set; }
        public AccessCredentials GetAccessCredentials()
        {
            return new AccessCredentials(this.AccessKeyId, this.SecretAccessKey, this.SessionToken,
                                            utils.From8601String(this.ExpirationDate));
        }
    }
}