using System.Globalization;
using System.Xml.Linq;
using ArgumentNullException = System.ArgumentNullException;

namespace Minio.Model;

/// <summary>
/// Represents an abstract default retention rule for an S3 bucket's object lock configuration.
/// A retention rule specifies a <see cref="RetentionMode"/> and a duration (either in days or years)
/// for which newly uploaded objects are automatically protected.
/// </summary>
public abstract class RetentionRule
{
    /// <summary>
    /// Initializes a new <see cref="RetentionRule"/> with the specified retention mode.
    /// </summary>
    /// <param name="mode">The retention mode (<see cref="RetentionMode.Governance"/> or <see cref="RetentionMode.Compliance"/>).</param>
    protected RetentionRule(RetentionMode mode)
    {
        Mode = mode;
    }

    /// <summary>
    /// Gets the retention mode applied to objects covered by this rule.
    /// </summary>
    public RetentionMode Mode { get; }

    /// <summary>
    /// Serializes this retention rule to its S3 <c>DefaultRetention</c> XML element.
    /// </summary>
    /// <returns>An <see cref="XElement"/> representing the <c>DefaultRetention</c> XML element.</returns>
    public abstract XElement Serialize();

    /// <summary>
    /// Deserializes a <see cref="RetentionRule"/> from an S3 <c>DefaultRetention</c> XML element,
    /// returning either a <see cref="RetentionRuleDays"/> or a <see cref="RetentionRuleYears"/> instance
    /// depending on which duration element is present.
    /// </summary>
    /// <param name="xElement">The <c>DefaultRetention</c> XML element to deserialize.</param>
    /// <returns>A <see cref="RetentionRuleDays"/> or <see cref="RetentionRuleYears"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when neither a <c>Days</c> nor a <c>Years</c> element is found.</exception>
    public static RetentionRule Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        if (xElement.Element(Constants.S3Ns + "Days") != null)
            return RetentionRuleDays.Deserialize(xElement);
        if (xElement.Element(Constants.S3Ns + "Years") != null)
            return RetentionRuleYears.Deserialize(xElement);
        throw new InvalidOperationException("No 'Days' or 'Years' element found.");
    }
}

/// <summary>
/// Represents a default retention rule that specifies the retention duration in days.
/// </summary>
public class RetentionRuleDays : RetentionRule
{
    /// <summary>
    /// Initializes a new <see cref="RetentionRuleDays"/> with the specified retention mode and duration in days.
    /// </summary>
    /// <param name="mode">The retention mode (<see cref="RetentionMode.Governance"/> or <see cref="RetentionMode.Compliance"/>).</param>
    /// <param name="days">The number of days that objects are retained.</param>
    public RetentionRuleDays(RetentionMode mode, int days) : base(mode)
    {
        Days = days;
    }

    /// <summary>
    /// Gets the number of days that objects are retained under this rule.
    /// </summary>
    public int Days { get; }

    /// <summary>
    /// Serializes this days-based retention rule to its S3 <c>DefaultRetention</c> XML element.
    /// </summary>
    /// <returns>An <see cref="XElement"/> representing the <c>DefaultRetention</c> XML element with a <c>Days</c> child.</returns>
    public override XElement Serialize()
        => new XElement(Constants.S3Ns + "DefaultRetention",
            new XElement(Constants.S3Ns + "Mode", RetentionModeExtensions.Serialize(Mode)),
            new XElement(Constants.S3Ns + "Days", Days));

    /// <summary>
    /// Deserializes a <see cref="RetentionRuleDays"/> from an S3 <c>DefaultRetention</c> XML element.
    /// </summary>
    /// <param name="xElement">The <c>DefaultRetention</c> XML element to deserialize.</param>
    /// <returns>A populated <see cref="RetentionRuleDays"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    public new static RetentionRuleDays Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        var mode = RetentionModeExtensions.Deserialize(xElement.Element(Constants.S3Ns + "Mode")?.Value ?? string.Empty);
        var days = int.Parse(xElement.Element(Constants.S3Ns + "Days")?.Value ?? string.Empty, CultureInfo.InvariantCulture);
        return new RetentionRuleDays(mode, days);
    }
}

/// <summary>
/// Represents a default retention rule that specifies the retention duration in years.
/// </summary>
public class RetentionRuleYears : RetentionRule
{
    /// <summary>
    /// Initializes a new <see cref="RetentionRuleYears"/> with the specified retention mode and duration in years.
    /// </summary>
    /// <param name="mode">The retention mode (<see cref="RetentionMode.Governance"/> or <see cref="RetentionMode.Compliance"/>).</param>
    /// <param name="years">The number of years that objects are retained.</param>
    public RetentionRuleYears(RetentionMode mode, int years) : base(mode)
    {
        Years = years;
    }

    /// <summary>
    /// Gets the number of years that objects are retained under this rule.
    /// </summary>
    public int Years { get; }

    /// <summary>
    /// Serializes this years-based retention rule to its S3 <c>DefaultRetention</c> XML element.
    /// </summary>
    /// <returns>An <see cref="XElement"/> representing the <c>DefaultRetention</c> XML element with a <c>Years</c> child.</returns>
    public override XElement Serialize()
        => new XElement(Constants.S3Ns + "DefaultRetention",
            new XElement(Constants.S3Ns + "Mode", RetentionModeExtensions.Serialize(Mode)),
            new XElement(Constants.S3Ns + "Years", Years));

    /// <summary>
    /// Deserializes a <see cref="RetentionRuleYears"/> from an S3 <c>DefaultRetention</c> XML element.
    /// </summary>
    /// <param name="xElement">The <c>DefaultRetention</c> XML element to deserialize.</param>
    /// <returns>A populated <see cref="RetentionRuleYears"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    public new static RetentionRuleYears Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        var mode = RetentionModeExtensions.Deserialize(xElement.Element(Constants.S3Ns + "Mode")?.Value ?? string.Empty);
        var years = int.Parse(xElement.Element(Constants.S3Ns + "Years")?.Value ?? string.Empty, CultureInfo.InvariantCulture);
        return new RetentionRuleYears(mode, years);
    }
}
