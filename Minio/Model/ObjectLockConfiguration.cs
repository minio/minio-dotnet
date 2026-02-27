using System.Xml.Linq;

namespace Minio.Model;

/// <summary>
/// Represents the object lock configuration for an S3 bucket, which enables
/// write-once-read-many (WORM) protection. Optionally includes a default retention rule
/// applied to all objects uploaded to the bucket.
/// </summary>
public class ObjectLockConfiguration
{
    /// <summary>
    /// Gets or sets the default retention rule applied to objects uploaded to the bucket.
    /// When <c>null</c>, no default retention is enforced, but per-object retention can still be set.
    /// </summary>
    public RetentionRule? DefaultRetentionRule { get; set; }

    /// <summary>
    /// Serializes this object lock configuration to its S3 XML representation.
    /// </summary>
    /// <returns>An <see cref="XElement"/> representing the <c>ObjectLockConfiguration</c> XML element.</returns>
    public XElement Serialize()
    {
        var xConfig = new XElement(Constants.S3Ns + "ObjectLockConfiguration",
            new XElement(Constants.S3Ns + "ObjectLockEnabled", "Enabled"));
        if (DefaultRetentionRule != null)
            xConfig.Add(new XElement(Constants.S3Ns + "Rule", DefaultRetentionRule.Serialize()));
        return xConfig;
    }

    /// <summary>
    /// Deserializes an <see cref="ObjectLockConfiguration"/> from an S3 <c>ObjectLockConfiguration</c> XML element.
    /// </summary>
    /// <param name="xElement">The <c>ObjectLockConfiguration</c> XML element to deserialize.</param>
    /// <returns>A populated <see cref="ObjectLockConfiguration"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    public static ObjectLockConfiguration Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        var xDefaultRetention = xElement.Element(Constants.S3Ns + "Rule")?.Element(Constants.S3Ns + "DefaultRetention");
        var defaultRetentionRule = xDefaultRetention != null ? RetentionRule.Deserialize(xDefaultRetention) : null;
        return new ObjectLockConfiguration
        {
            DefaultRetentionRule = defaultRetentionRule,
        };
    }
}
