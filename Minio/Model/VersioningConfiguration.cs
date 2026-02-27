using System.Xml.Linq;

namespace Minio.Model;

/// <summary>
/// Represents the versioning configuration for an S3 bucket, controlling whether
/// versioning is enabled, suspended, or off, and whether MFA delete is required.
/// </summary>
public class VersioningConfiguration
{
    private static readonly XNamespace Ns = Constants.S3Ns;

    /// <summary>
    /// Gets the current versioning status of the bucket.
    /// </summary>
    public VersioningStatus Status { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether MFA (multi-factor authentication) delete
    /// is enabled on the bucket. When <c>true</c>, a valid MFA token is required to change
    /// the versioning state or permanently delete a versioned object.
    /// </summary>
    public bool MfaDelete { get; set; }

    /// <summary>
    /// Serializes this versioning configuration to its S3 XML representation.
    /// </summary>
    /// <returns>An <see cref="XElement"/> representing the <c>VersioningConfiguration</c> XML element.</returns>
    public XElement Serialize()
    {
        return new XElement(Ns + "VersioningConfiguration",
            new XElement(Ns + "Status", VersioningStatusExtensions.Serialize(Status)),
            new XElement(Ns + "MfaDelete", MfaDelete ? "Enabled" : "Disabled"));
    }

    /// <summary>
    /// Deserializes a <see cref="VersioningConfiguration"/> from an S3 <c>VersioningConfiguration</c> XML element.
    /// </summary>
    /// <param name="xElement">The <c>VersioningConfiguration</c> XML element to deserialize.</param>
    /// <returns>A populated <see cref="VersioningConfiguration"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    public static VersioningConfiguration Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        var status = VersioningStatusExtensions.Deserialize(xElement.Element(Constants.S3Ns + "Status")?.Value ?? string.Empty);
        var mfaDelete = xElement.Element(Constants.S3Ns + "Status")?.Value is "Enabled";
        return new VersioningConfiguration
        {
            Status = status,
            MfaDelete = mfaDelete,
        };
    }
}
