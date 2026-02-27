using System.Globalization;
using System.Xml.Linq;

namespace Minio.Model;

/// <summary>Status of a lifecycle rule.</summary>
public enum LifecycleRuleStatus { Enabled, Disabled }

/// <summary>A tag filter used in lifecycle rules.</summary>
public class LifecycleTag
{
    public LifecycleTag() { }

    public LifecycleTag(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>Combined prefix and tag filter for lifecycle rules.</summary>
public class LifecycleFilterAnd
{
    public string? Prefix { get; set; }
    public IReadOnlyList<LifecycleTag>? Tags { get; set; }
}

/// <summary>Filter that determines which objects a lifecycle rule applies to.</summary>
public class LifecycleFilter
{
    /// <summary>Applies the rule to objects with this key prefix. An empty string matches all objects.</summary>
    public string? Prefix { get; set; }

    /// <summary>Applies the rule to objects with this tag.</summary>
    public LifecycleTag? Tag { get; set; }

    /// <summary>Applies the rule to objects matching all conditions in this combined filter.</summary>
    public LifecycleFilterAnd? And { get; set; }
}

/// <summary>Expiration configuration for a lifecycle rule.</summary>
public class LifecycleExpiration
{
    /// <summary>Expires the object after this many days from creation.</summary>
    public int? Days { get; set; }

    /// <summary>Expires the object on a specific date.</summary>
    public DateTimeOffset? Date { get; set; }

    /// <summary>When <see langword="true"/>, removes expired delete markers.</summary>
    public bool? ExpiredObjectDeleteMarker { get; set; }
}

/// <summary>Transition that moves objects to a different storage class.</summary>
public class LifecycleTransition
{
    /// <summary>Transitions the object after this many days from creation.</summary>
    public int? Days { get; set; }

    /// <summary>Transitions the object on a specific date.</summary>
    public DateTimeOffset? Date { get; set; }

    /// <summary>The target storage class (e.g., STANDARD_IA, GLACIER).</summary>
    public required string StorageClass { get; set; }
}

/// <summary>Specifies when noncurrent object versions expire.</summary>
public class LifecycleNoncurrentVersionExpiration
{
    /// <summary>Expires noncurrent versions after this many days.</summary>
    public int NoncurrentDays { get; set; }

    /// <summary>Retains at most this many noncurrent versions regardless of days.</summary>
    public int? NewerNoncurrentVersions { get; set; }
}

/// <summary>Specifies transitions for noncurrent object versions.</summary>
public class LifecycleNoncurrentVersionTransition
{
    /// <summary>Transitions noncurrent versions after this many days.</summary>
    public int NoncurrentDays { get; set; }

    /// <summary>The target storage class.</summary>
    public required string StorageClass { get; set; }

    /// <summary>Retains at most this many noncurrent versions regardless of days.</summary>
    public int? NewerNoncurrentVersions { get; set; }
}

/// <summary>Specifies when to abort incomplete multipart uploads.</summary>
public class LifecycleAbortIncompleteMultipartUpload
{
    /// <summary>Aborts uploads that were initiated more than this many days ago.</summary>
    public int DaysAfterInitiation { get; set; }
}

/// <summary>A single lifecycle rule that defines actions on matching objects.</summary>
public class LifecycleRule
{
    /// <summary>Unique identifier for the rule.</summary>
    public string? Id { get; set; }

    /// <summary>Whether this rule is active.</summary>
    public LifecycleRuleStatus Status { get; set; } = LifecycleRuleStatus.Enabled;

    /// <summary>Filter that determines which objects the rule applies to. A <see langword="null"/> filter matches all objects.</summary>
    public LifecycleFilter? Filter { get; set; }

    /// <summary>Expiration configuration for current object versions.</summary>
    public LifecycleExpiration? Expiration { get; set; }

    /// <summary>Storage class transitions for current object versions.</summary>
    public IReadOnlyList<LifecycleTransition>? Transitions { get; set; }

    /// <summary>Expiration configuration for noncurrent object versions.</summary>
    public LifecycleNoncurrentVersionExpiration? NoncurrentVersionExpiration { get; set; }

    /// <summary>Storage class transitions for noncurrent object versions.</summary>
    public IReadOnlyList<LifecycleNoncurrentVersionTransition>? NoncurrentVersionTransitions { get; set; }

    /// <summary>Cleanup rule for incomplete multipart uploads.</summary>
    public LifecycleAbortIncompleteMultipartUpload? AbortIncompleteMultipartUpload { get; set; }
}

/// <summary>The complete lifecycle configuration for a bucket.</summary>
public class LifecycleConfiguration
{
    private static readonly XNamespace Ns = Constants.S3Ns;

    /// <summary>The lifecycle rules to apply.</summary>
    public IReadOnlyList<LifecycleRule> Rules { get; set; } = Array.Empty<LifecycleRule>();

    /// <summary>Serializes this configuration to its S3 XML representation.</summary>
    public XElement Serialize()
    {
        var xConfig = new XElement(Ns + "LifecycleConfiguration");
        foreach (var rule in Rules)
            xConfig.Add(SerializeRule(rule));
        return xConfig;
    }

    /// <summary>Deserializes a <see cref="LifecycleConfiguration"/> from an XML element.</summary>
    public static LifecycleConfiguration Deserialize(XElement xElement)
    {
        return new LifecycleConfiguration
        {
            Rules = xElement.Elements(Ns + "Rule").Select(DeserializeRule).ToList()
        };
    }

    private static XElement SerializeRule(LifecycleRule rule)
    {
        var xRule = new XElement(Ns + "Rule");
        if (!string.IsNullOrEmpty(rule.Id))
            xRule.Add(new XElement(Ns + "ID", rule.Id));
        xRule.Add(new XElement(Ns + "Status", rule.Status == LifecycleRuleStatus.Enabled ? "Enabled" : "Disabled"));
        if (rule.Filter != null)
            xRule.Add(SerializeFilter(rule.Filter));
        if (rule.Expiration != null)
            xRule.Add(SerializeExpiration(rule.Expiration));
        if (rule.Transitions != null)
            foreach (var t in rule.Transitions)
                xRule.Add(SerializeTransition(t));
        if (rule.NoncurrentVersionExpiration != null)
            xRule.Add(SerializeNoncurrentVersionExpiration(rule.NoncurrentVersionExpiration));
        if (rule.NoncurrentVersionTransitions != null)
            foreach (var t in rule.NoncurrentVersionTransitions)
                xRule.Add(SerializeNoncurrentVersionTransition(t));
        if (rule.AbortIncompleteMultipartUpload != null)
            xRule.Add(new XElement(Ns + "AbortIncompleteMultipartUpload",
                new XElement(Ns + "DaysAfterInitiation", rule.AbortIncompleteMultipartUpload.DaysAfterInitiation)));
        return xRule;
    }

    private static XElement SerializeFilter(LifecycleFilter filter)
    {
        var xFilter = new XElement(Ns + "Filter");
        if (filter.Tag != null)
            xFilter.Add(SerializeTag(filter.Tag));
        else if (filter.And != null)
        {
            var xAnd = new XElement(Ns + "And");
            if (filter.And.Prefix != null)
                xAnd.Add(new XElement(Ns + "Prefix", filter.And.Prefix));
            if (filter.And.Tags != null)
                foreach (var tag in filter.And.Tags)
                    xAnd.Add(SerializeTag(tag));
            xFilter.Add(xAnd);
        }
        else if (filter.Prefix != null)
            xFilter.Add(new XElement(Ns + "Prefix", filter.Prefix));
        return xFilter;
    }

    private static XElement SerializeTag(LifecycleTag tag) =>
        new XElement(Ns + "Tag",
            new XElement(Ns + "Key", tag.Key),
            new XElement(Ns + "Value", tag.Value));

    private static XElement SerializeExpiration(LifecycleExpiration exp)
    {
        var xExp = new XElement(Ns + "Expiration");
        if (exp.Days.HasValue)
            xExp.Add(new XElement(Ns + "Days", exp.Days.Value));
        else if (exp.Date.HasValue)
            xExp.Add(new XElement(Ns + "Date", exp.Date.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)));
        else if (exp.ExpiredObjectDeleteMarker.HasValue)
            xExp.Add(new XElement(Ns + "ExpiredObjectDeleteMarker", exp.ExpiredObjectDeleteMarker.Value ? "true" : "false"));
        return xExp;
    }

    private static XElement SerializeTransition(LifecycleTransition t)
    {
        var xTrans = new XElement(Ns + "Transition");
        if (t.Days.HasValue)
            xTrans.Add(new XElement(Ns + "Days", t.Days.Value));
        else if (t.Date.HasValue)
            xTrans.Add(new XElement(Ns + "Date", t.Date.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)));
        xTrans.Add(new XElement(Ns + "StorageClass", t.StorageClass));
        return xTrans;
    }

    private static XElement SerializeNoncurrentVersionExpiration(LifecycleNoncurrentVersionExpiration ncve)
    {
        var xNcve = new XElement(Ns + "NoncurrentVersionExpiration",
            new XElement(Ns + "NoncurrentDays", ncve.NoncurrentDays));
        if (ncve.NewerNoncurrentVersions.HasValue)
            xNcve.Add(new XElement(Ns + "NewerNoncurrentVersions", ncve.NewerNoncurrentVersions.Value));
        return xNcve;
    }

    private static XElement SerializeNoncurrentVersionTransition(LifecycleNoncurrentVersionTransition ncvt)
    {
        var xNcvt = new XElement(Ns + "NoncurrentVersionTransition",
            new XElement(Ns + "NoncurrentDays", ncvt.NoncurrentDays),
            new XElement(Ns + "StorageClass", ncvt.StorageClass));
        if (ncvt.NewerNoncurrentVersions.HasValue)
            xNcvt.Add(new XElement(Ns + "NewerNoncurrentVersions", ncvt.NewerNoncurrentVersions.Value));
        return xNcvt;
    }

    private static LifecycleRule DeserializeRule(XElement xRule)
    {
        var id = xRule.Element(Ns + "ID")?.Value;
        var statusText = xRule.Element(Ns + "Status")?.Value;
        var status = statusText == "Disabled" ? LifecycleRuleStatus.Disabled : LifecycleRuleStatus.Enabled;

        var xFilter = xRule.Element(Ns + "Filter");
        LifecycleFilter? filter = null;
        if (xFilter != null)
        {
            var xTag = xFilter.Element(Ns + "Tag");
            var xAnd = xFilter.Element(Ns + "And");
            if (xTag != null)
                filter = new LifecycleFilter { Tag = DeserializeTag(xTag) };
            else if (xAnd != null)
                filter = new LifecycleFilter
                {
                    And = new LifecycleFilterAnd
                    {
                        Prefix = xAnd.Element(Ns + "Prefix")?.Value,
                        Tags = xAnd.Elements(Ns + "Tag").Select(DeserializeTag).ToList()
                    }
                };
            else
                filter = new LifecycleFilter { Prefix = xFilter.Element(Ns + "Prefix")?.Value };
        }

        var xExpiration = xRule.Element(Ns + "Expiration");
        LifecycleExpiration? expiration = null;
        if (xExpiration != null)
        {
            var daysText = xExpiration.Element(Ns + "Days")?.Value;
            var dateText = xExpiration.Element(Ns + "Date")?.Value;
            var markerText = xExpiration.Element(Ns + "ExpiredObjectDeleteMarker")?.Value;
            expiration = new LifecycleExpiration
            {
                Days = daysText != null ? int.Parse(daysText, CultureInfo.InvariantCulture) : null,
                Date = dateText != null ? DateTimeOffset.Parse(dateText, CultureInfo.InvariantCulture) : null,
                ExpiredObjectDeleteMarker = markerText != null ? bool.Parse(markerText) : null,
            };
        }

        var transitions = xRule.Elements(Ns + "Transition").Select(xt =>
        {
            var daysText = xt.Element(Ns + "Days")?.Value;
            var dateText = xt.Element(Ns + "Date")?.Value;
            return new LifecycleTransition
            {
                Days = daysText != null ? int.Parse(daysText, CultureInfo.InvariantCulture) : null,
                Date = dateText != null ? DateTimeOffset.Parse(dateText, CultureInfo.InvariantCulture) : null,
                StorageClass = xt.Element(Ns + "StorageClass")?.Value ?? string.Empty,
            };
        }).ToList();

        var xNcve = xRule.Element(Ns + "NoncurrentVersionExpiration");
        LifecycleNoncurrentVersionExpiration? ncve = null;
        if (xNcve != null)
        {
            ncve = new LifecycleNoncurrentVersionExpiration
            {
                NoncurrentDays = int.Parse(xNcve.Element(Ns + "NoncurrentDays")?.Value ?? "0", CultureInfo.InvariantCulture),
                NewerNoncurrentVersions = xNcve.Element(Ns + "NewerNoncurrentVersions")?.Value is { } nncv
                    ? int.Parse(nncv, CultureInfo.InvariantCulture) : null,
            };
        }

        var ncvTransitions = xRule.Elements(Ns + "NoncurrentVersionTransition").Select(xNcvt =>
        {
            return new LifecycleNoncurrentVersionTransition
            {
                NoncurrentDays = int.Parse(xNcvt.Element(Ns + "NoncurrentDays")?.Value ?? "0", CultureInfo.InvariantCulture),
                StorageClass = xNcvt.Element(Ns + "StorageClass")?.Value ?? string.Empty,
                NewerNoncurrentVersions = xNcvt.Element(Ns + "NewerNoncurrentVersions")?.Value is { } nncv
                    ? int.Parse(nncv, CultureInfo.InvariantCulture) : null,
            };
        }).ToList();

        var xAbort = xRule.Element(Ns + "AbortIncompleteMultipartUpload");
        LifecycleAbortIncompleteMultipartUpload? abort = null;
        if (xAbort != null)
        {
            abort = new LifecycleAbortIncompleteMultipartUpload
            {
                DaysAfterInitiation = int.Parse(xAbort.Element(Ns + "DaysAfterInitiation")?.Value ?? "0", CultureInfo.InvariantCulture)
            };
        }

        return new LifecycleRule
        {
            Id = id,
            Status = status,
            Filter = filter,
            Expiration = expiration,
            Transitions = transitions.Count > 0 ? transitions : null,
            NoncurrentVersionExpiration = ncve,
            NoncurrentVersionTransitions = ncvTransitions.Count > 0 ? ncvTransitions : null,
            AbortIncompleteMultipartUpload = abort,
        };
    }

    private static LifecycleTag DeserializeTag(XElement xTag) =>
        new LifecycleTag(xTag.Element(Ns + "Key")?.Value ?? string.Empty, xTag.Element(Ns + "Value")?.Value ?? string.Empty);
}
