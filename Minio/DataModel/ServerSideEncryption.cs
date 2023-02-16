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

using System.Text;
using Minio.Helper;

namespace Minio.DataModel;

// Type of Server-side encryption
public enum EncryptionType
{
    SSE_C,
    SSE_S3,
    SSE_KMS
}

/// <summary>
///     ServerSideEncryption interface
/// </summary>
public interface ServerSideEncryption
{
    // GetType() needs to return the type of Server-side encryption
    EncryptionType GetType();

    // Marshals the Server-side encryption headers into dictionary
    void Marshal(Dictionary<string, string> headers);
}

/// <summary>
///     Server-side encryption with customer provided keys (SSE-C)
/// </summary>
public class SSEC : ServerSideEncryption
{
    // secret AES-256 Key
    protected byte[] key;

    public SSEC(byte[] key)
    {
        if (key == null || key.Length != 32)
            throw new ArgumentException("Secret key needs to be a 256 bit AES Key", nameof(key));
        this.key = key;
    }

    public new EncryptionType GetType()
    {
        return EncryptionType.SSE_C;
    }

    public virtual void Marshal(Dictionary<string, string> headers)
    {
        var md5SumStr = Utils.getMD5SumStr(key);
        headers.Add("X-Amz-Server-Side-Encryption-Customer-Algorithm", "AES256");
        headers.Add("X-Amz-Server-Side-Encryption-Customer-Key", Convert.ToBase64String(key));
        headers.Add("X-Amz-Server-Side-Encryption-Customer-Key-Md5", md5SumStr);
    }
}

/// <summary>
///     Server-side encryption option for source side SSE-C copy operation
/// </summary>
public class SSECopy : SSEC
{
    public SSECopy(byte[] key) : base(key)
    {
    }

    public override void Marshal(Dictionary<string, string> headers)
    {
        var md5SumStr = Utils.getMD5SumStr(key);
        headers.Add("X-Amz-Copy-Source-Server-Side-Encryption-Customer-Algorithm", "AES256");
        headers.Add("X-Amz-Copy-Source-Server-Side-Encryption-Customer-Key", Convert.ToBase64String(key));
        headers.Add("X-Amz-Copy-Source-Server-Side-Encryption-Customer-Key-Md5", md5SumStr);
    }

    public SSEC CloneToSSEC()
    {
        return new SSEC(key);
    }
}

/// <summary>
///     Server-side encryption with S3 managed encryption keys (SSE-S3)
/// </summary>
public class SSES3 : ServerSideEncryption
{
    public new EncryptionType GetType()
    {
        return EncryptionType.SSE_S3;
    }

    public virtual void Marshal(Dictionary<string, string> headers)
    {
        headers.Add(Constants.SSEGenericHeader, "AES256");
    }
}

/// <summary>
///     Server-side encryption with AWS KMS managed keys
/// </summary>
public class SSEKMS : ServerSideEncryption
{
    protected Dictionary<string, string> context;

    // Specifies the customer master key(CMK).Cannot be null
    protected string key;

    public SSEKMS(string key, Dictionary<string, string> context = null)
    {
        if (key == string.Empty) throw new ArgumentException("KMS Key cannot be empty", nameof(key));
        this.key = key;
        this.context = context;
    }

    public new EncryptionType GetType()
    {
        return EncryptionType.SSE_KMS;
    }

    public void Marshal(Dictionary<string, string> headers)
    {
        headers.Add(Constants.SSEKMSKeyId, key);
        headers.Add(Constants.SSEGenericHeader, "aws:kms");
        if (context != null) headers.Add(Constants.SSEKMSContext, marshalContext());
    }

    /// <summary>
    ///     Serialize context into JSON string.
    /// </summary>
    /// <returns>Serialized JSON context</returns>
    private string marshalContext()
    {
        var sb = new StringBuilder();

        sb.Append("{");
        var i = 0;
        var len = context.Count;
        foreach (var pair in context)
        {
            sb.Append("\"").Append(pair.Key).Append("\"");
            sb.Append(":");
            sb.Append("\"").Append(pair.Value).Append("\"");
            i += 1;
            if (i != len) sb.Append(":");
        }

        sb.Append("}");
        var contextBytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Convert.ToBase64String(contextBytes);
    }
}