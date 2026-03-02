using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Minio.Model;

/// <summary>
/// Implements customer-provided server-side encryption (SSE-C) for S3 object operations.
/// With SSE-C, the client supplies the AES-256 encryption key with every request; the key
/// is never stored by the server.
/// </summary>
public class SSE : IServerSideEncryption
{
    // SseGenericHeader is the AWS SSE header used for SSE-S3 and SSE-KMS.
    private const string SseGenericHeader = "X-Amz-Server-Side-Encryption";

    // SseKmsKeyID is the AWS SSE-KMS key id.
    private const string SseKmsKeyID = SseGenericHeader + "-Aws-Kms-Key-Id";
    // SseEncryptionContext is the AWS SSE-KMS Encryption Context data.
    private const string SseEncryptionContext = SseGenericHeader + "-Context";

    // SseCustomerAlgorithm is the AWS SSE-C algorithm HTTP header key.
    private const string SseCustomerAlgorithm = SseGenericHeader + "-Customer-Algorithm";
    // SseCustomerKey is the AWS SSE-C encryption key HTTP header key.
    private const string SseCustomerKey = SseGenericHeader + "-Customer-Key";
    // SseCustomerKeyMD5 is the AWS SSE-C encryption key MD5 HTTP header key.
    private const string SseCustomerKeyMD5 = SseGenericHeader + "-Customer-Key-MD5";

    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new <see cref="SSE"/> instance with the specified 256-bit (32-byte) AES encryption key.
    /// </summary>
    /// <param name="key">A 32-byte AES-256 encryption key supplied by the caller. The key must be exactly 32 bytes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is not exactly 32 bytes in length.</exception>
    public SSE(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != 32) throw new ArgumentException("key should have 32 bytes (256 bit)", nameof(key));

        _key = key;
    }

    /// <summary>
    /// Gets the encryption type identifier. Always returns <c>SSE-C</c> for customer-provided encryption.
    /// </summary>
    public string Type => "SSE-C";

    /// <summary>
    /// Writes the SSE-C HTTP headers to the provided <see cref="HttpHeaders"/> collection.
    /// Adds the customer algorithm (<c>AES256</c>), the Base64-encoded key, and the Base64-encoded
    /// MD5 hash of the key, as required by the S3 SSE-C protocol.
    /// </summary>
    /// <param name="headers">The HTTP headers collection to which the SSE-C headers are added.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> is <c>null</c>.</exception>
    public void WriteHeaders(HttpHeaders headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        headers.Add(SseCustomerAlgorithm, "AES256");
        headers.Add(SseCustomerKey, Convert.ToBase64String(_key, Base64FormattingOptions.None));
#pragma warning disable CA5351  // MD5 is required here
        headers.Add(SseCustomerKeyMD5, Convert.ToBase64String(MD5.HashData(_key), Base64FormattingOptions.None));
#pragma warning restore CA5351
    }
}
