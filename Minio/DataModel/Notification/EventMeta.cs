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

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Minio.DataModel.Notification
{
    [DataContract]
    public class EventMeta
    {
        [DataMember]
        [JsonPropertyName("bucket")]
        public BucketMeta Bucket { get; set; }

        [DataMember]
        [JsonPropertyName("configurationId")]
        public string ConfigurationId { get; set; }

        [DataMember(Name = "object")]
        [JsonPropertyName("object")]
        public ObjectMeta ObjectMeta { get; set; } // C# won't allow the keyword 'object' as a name

        [DataMember]
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; }
    }
}