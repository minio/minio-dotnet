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
///     Server-side encryption with customer provided keys (SSE-C)
/// </summary>
public class SSEC : IServerSideEncryption
{
    // secret AES-256 Key
    internal byte[] Key;

    public SSEC(byte[] key)
    {
        if (key is null || key.Length != 32)
            throw new ArgumentException("Secret key needs to be a 256 bit AES Key", nameof(key));
        Key = key;
    }

    public EncryptionType GetEncryptionType()
    {
        return EncryptionType.SSE_C;
    }

    public virtual void Marshal(IDictionary<string, string> headers)
    {
        if (headers is null) throw new ArgumentNullException(nameof(headers));

        var md5SumStr = Utils.GetMD5SumStr(Key);
        headers.Add("X-Amz-Server-Side-Encryption-Customer-Algorithm", "AES256");
        headers.Add("X-Amz-Server-Side-Encryption-Customer-Key", Convert.ToBase64String(Key));
        headers.Add("X-Amz-Server-Side-Encryption-Customer-Key-Md5", md5SumStr);
    }
}
