/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2019 MinIO, Inc.
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

using Minio.Helper;

namespace Minio.DataModel.Encryption;

/// <summary>
///     Server-side encryption with S3 managed encryption keys (SSE-S3)
/// </summary>
public class SSES3 : IServerSideEncryption
{
    public EncryptionType GetEncryptionType()
    {
        return EncryptionType.SSE_S3;
    }

    public virtual void Marshal(IDictionary<string, string> headers)
    {
        if (headers is null) throw new ArgumentNullException(nameof(headers));

        headers.Add(Constants.SSEGenericHeader, "AES256");
    }
}
