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

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Minio.DataModel.Notification;

public class ObjectMeta
{
    [JsonPropertyName("contentType")] public string ContentType { get; set; }

    [JsonPropertyName("etag")] public string Etag { get; set; }

    [JsonPropertyName("key")] public string Key { get; set; }

    [JsonPropertyName("sequencer")] public string Sequencer { get; set; }

    [JsonPropertyName("size")] public ulong Size { get; set; }

    [JsonPropertyName("userMetadata")]
    [SuppressMessage("Design", "MA0016:Prefer returning collection abstraction instead of implementation",
        Justification = "Needs to be concrete type for XML deserialization")]
    public Dictionary<string, string> UserMetadata { get; set; }

    [JsonPropertyName("versionId")] public string VersionId { get; set; }
}
