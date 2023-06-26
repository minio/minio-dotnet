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
///     Server-side encryption option for source side SSE-C copy operation
/// </summary>
public class SSECopy : SSEC
{
    public SSECopy(byte[] key) : base(key)
    {
    }

    public override void Marshal(IDictionary<string, string> headers)
    {
        if (headers is null) throw new ArgumentNullException(nameof(headers));

        var md5SumStr = Utils.GetMD5SumStr(Key);
        headers.Add("X-Amz-Copy-Source-Server-Side-Encryption-Customer-Algorithm", "AES256");
        headers.Add("X-Amz-Copy-Source-Server-Side-Encryption-Customer-Key", Convert.ToBase64String(Key));
        headers.Add("X-Amz-Copy-Source-Server-Side-Encryption-Customer-Key-Md5", md5SumStr);
    }

    public SSEC CloneToSSEC()
    {
        return new SSEC(Key);
    }
}
