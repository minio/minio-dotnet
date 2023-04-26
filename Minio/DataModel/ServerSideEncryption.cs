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
public interface IServerSideEncryption
{
    // GetType() needs to return the type of Server-side encryption
    EncryptionType GetEncryptionType();

    // Marshals the Server-side encryption headers into dictionary
    void Marshal(IDictionary<string, string> headers);
}

/// <summary>
///     Server-side encryption with customer provided keys (SSE-C)
/// </summary>
public class SSEC : IServerSideEncryption
{
    // secret AES-256 Key
    protected byte[] key;

    public SSEC(byte[] key)
    {
        if (key is null || key.Length != 32)
            throw new ArgumentException("Secret key needs to be a 256 bit AES Key", nameof(key));
        this.key = key;
    }

    public EncryptionType GetEncryptionType()
    {
        return EncryptionType.SSE_C;
    }

    public virtual void Marshal(IDictionary<string, string> headers)
    {
        if (headers is null) throw new ArgumentNullException(nameof(headers));

        var md5SumStr = Utils.GetMD5SumStr(key);
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

    public override void Marshal(IDictionary<string, string> headers)
    {
        if (headers is null) throw new ArgumentNullException(nameof(headers));

        var md5SumStr = Utils.GetMD5SumStr(key);
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

/// <summary>
///     Server-side encryption with AWS KMS managed keys
/// </summary>
public class SSEKMS : IServerSideEncryption
{
    protected IDictionary<string, string> context;

    // Specifies the customer master key(CMK).Cannot be null
    protected string key;

    public SSEKMS(string key, IDictionary<string, string> context = null)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("KMS Key cannot be empty", nameof(key));
        this.key = key;
        this.context = context;
    }

    public EncryptionType GetEncryptionType()
    {
        return EncryptionType.SSE_KMS;
    }

    public void Marshal(IDictionary<string, string> headers)
    {
        if (headers is null) throw new ArgumentNullException(nameof(headers));

        headers.Add(Constants.SSEKMSKeyId, key);
        headers.Add(Constants.SSEGenericHeader, "aws:kms");
        if (context is not null) headers.Add(Constants.SSEKMSContext, MarshalContext());
    }

    /// <summary>
    ///     Serialize context into JSON string.
    /// </summary>
    /// <returns>Serialized JSON context</returns>
    private string MarshalContext()
    {
        var sb = new StringBuilder();

        sb.Append('{');
        var i = 0;
        var len = context.Count;
        foreach (var pair in context)
        {
            sb.Append('"').Append(pair.Key).Append('"');
            sb.Append(':');
            sb.Append('"').Append(pair.Value).Append('"');
            i++;
            if (i != len) sb.Append(':');
        }

        sb.Append('}');
        ReadOnlySpan<byte> contextBytes = Encoding.UTF8.GetBytes(sb.ToString());
#if NETSTANDARD
        return Convert.ToBase64String(contextBytes.ToArray());
#else
        return Convert.ToBase64String(contextBytes);
#endif
    }
}