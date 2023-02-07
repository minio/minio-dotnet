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

using System.Text.Json.Serialization;
using Minio.DataModel;

namespace Minio.Credentials;

public class ECSCredentials
{
    [JsonPropertyName("AccessKeyId")] public string AccessKeyId { get; set; }

    [JsonPropertyName("SecretAccessKey")] public string SecretAccessKey { get; set; }

    [JsonPropertyName("Token")] public string SessionToken { get; set; }

    [JsonPropertyName("Expiration")] public string ExpirationDate { get; set; }

    [JsonPropertyName("Code")] public string Code { get; set; }

    [JsonPropertyName("Message")] public string Message { get; set; }

    [JsonPropertyName("Type")] public string Type { get; set; }

    [JsonPropertyName("LastUpdated")] public string LastUpdated { get; set; }

    public AccessCredentials GetAccessCredentials()
    {
        return new AccessCredentials(AccessKeyId, SecretAccessKey, SessionToken,
            utils.From8601String(ExpirationDate));
    }
}