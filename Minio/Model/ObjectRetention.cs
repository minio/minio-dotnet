using System.Globalization;
using System.Xml.Linq;

namespace Minio.Model;

/// <summary>
/// Represents the object-level retention configuration applied to a specific S3 object version.
/// </summary>
public class ObjectRetention
{
    /// <summary>Gets or sets the retention mode (Governance or Compliance).</summary>
    public RetentionMode Mode { get; set; }

    /// <summary>Gets or sets the date until which the object is retained.</summary>
    public DateTimeOffset RetainUntilDate { get; set; }

    /// <summary>Serializes this configuration to its S3 XML representation.</summary>
    public XElement Serialize()
    {
        return new XElement(Constants.S3Ns + "Retention",
            new XElement(Constants.S3Ns + "Mode", RetentionModeExtensions.Serialize(Mode)),
            new XElement(Constants.S3Ns + "RetainUntilDate",
                RetainUntilDate.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)));
    }

    /// <summary>Deserializes an <see cref="ObjectRetention"/> from an XML element.</summary>
    public static ObjectRetention Deserialize(XElement xElement)
    {
        var ns = Constants.S3Ns;
        return new ObjectRetention
        {
            Mode = RetentionModeExtensions.Deserialize(xElement.Element(ns + "Mode")?.Value ?? string.Empty),
            RetainUntilDate = DateTimeOffset.Parse(
                xElement.Element(ns + "RetainUntilDate")?.Value ?? string.Empty,
                CultureInfo.InvariantCulture),
        };
    }
}
