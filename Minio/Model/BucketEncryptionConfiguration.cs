using System.Xml.Linq;

namespace Minio.Model;

/// <summary>Specifies the server-side encryption algorithm to use for a bucket.</summary>
public enum SseAlgorithm
{
    /// <summary>AES-256 server-side encryption managed by S3.</summary>
    Aes256,
    /// <summary>AWS Key Management Service (KMS) managed encryption.</summary>
    AwsKms,
}

internal static class SseAlgorithmExtensions
{
    public static string Serialize(SseAlgorithm algorithm) => algorithm switch
    {
        SseAlgorithm.Aes256 => "AES256",
        SseAlgorithm.AwsKms => "aws:kms",
        _ => throw new ArgumentException($"Unknown SSE algorithm: {algorithm}", nameof(algorithm))
    };

    public static SseAlgorithm Deserialize(string value) => value switch
    {
        "AES256" => SseAlgorithm.Aes256,
        "aws:kms" => SseAlgorithm.AwsKms,
        _ => throw new ArgumentException($"Unknown SSE algorithm: {value}", nameof(value))
    };
}

/// <summary>Represents the server-side encryption configuration for a bucket.</summary>
public class BucketEncryptionConfiguration
{
    /// <summary>The SSE algorithm to apply by default to new objects in the bucket.</summary>
    public SseAlgorithm SseAlgorithm { get; set; }

    /// <summary>
    /// The AWS KMS key ID to use for SSE-KMS encryption.
    /// Applicable only when <see cref="SseAlgorithm"/> is <see cref="SseAlgorithm.AwsKms"/>.
    /// </summary>
    public string? KmsMasterKeyId { get; set; }

    /// <summary>
    /// When <see langword="true"/>, enables an S3 Bucket Key to reduce KMS request costs.
    /// Applicable only when <see cref="SseAlgorithm"/> is <see cref="SseAlgorithm.AwsKms"/>.
    /// </summary>
    public bool? BucketKeyEnabled { get; set; }

    /// <summary>Serializes this configuration to its S3 XML representation.</summary>
    public XElement Serialize()
    {
        var xApply = new XElement("ApplyServerSideEncryptionByDefault",
            new XElement("SSEAlgorithm", SseAlgorithmExtensions.Serialize(SseAlgorithm)));
        if (!string.IsNullOrEmpty(KmsMasterKeyId))
            xApply.Add(new XElement("KMSMasterKeyID", KmsMasterKeyId));
        var xRule = new XElement("Rule", xApply);
        if (BucketKeyEnabled.HasValue)
            xRule.Add(new XElement("BucketKeyEnabled", BucketKeyEnabled.Value ? "true" : "false"));
        return new XElement("ServerSideEncryptionConfiguration", xRule);
    }

    /// <summary>Deserializes a <see cref="BucketEncryptionConfiguration"/> from an XML element.</summary>
    public static BucketEncryptionConfiguration Deserialize(XElement xElement)
    {
        var xRule = xElement.Element("Rule");
        var xApply = xRule?.Element("ApplyServerSideEncryptionByDefault");
        var algorithmText = xApply?.Element("SSEAlgorithm")?.Value ?? "AES256";
        var kmsMasterKeyId = xApply?.Element("KMSMasterKeyID")?.Value;
        var bucketKeyEnabledText = xRule?.Element("BucketKeyEnabled")?.Value;
        bool? bucketKeyEnabled = bucketKeyEnabledText != null ? bool.Parse(bucketKeyEnabledText) : null;
        return new BucketEncryptionConfiguration
        {
            SseAlgorithm = SseAlgorithmExtensions.Deserialize(algorithmText),
            KmsMasterKeyId = string.IsNullOrEmpty(kmsMasterKeyId) ? null : kmsMasterKeyId,
            BucketKeyEnabled = bucketKeyEnabled,
        };
    }
}
