using System.Xml.Linq;

namespace Minio.Model;

/// <summary>Status of a replication rule or replication feature.</summary>
public enum ReplicationStatus { Enabled, Disabled }

/// <summary>A tag filter used in replication rules.</summary>
public class ReplicationTag
{
    public ReplicationTag() { }

    public ReplicationTag(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>Combined AND filter for replication rules.</summary>
public class ReplicationFilterAnd
{
    public string? Prefix { get; set; }
    public IReadOnlyList<ReplicationTag>? Tags { get; set; }
}

/// <summary>Filter that determines which objects a replication rule applies to.</summary>
public class ReplicationFilter
{
    public string? Prefix { get; set; }
    public ReplicationTag? Tag { get; set; }
    public ReplicationFilterAnd? And { get; set; }
}

/// <summary>Replication time value in minutes.</summary>
public class ReplicationTimeValue
{
    public int Minutes { get; set; }
}

/// <summary>S3 Replication Time Control (RTC) configuration.</summary>
public class ReplicationTime
{
    public ReplicationStatus Status { get; set; }
    public ReplicationTimeValue? Time { get; set; }
}

/// <summary>Replication metrics configuration.</summary>
public class ReplicationMetrics
{
    public ReplicationStatus Status { get; set; }
    public ReplicationTimeValue? EventThreshold { get; set; }
}

/// <summary>Encryption configuration for replicated objects.</summary>
public class ReplicationEncryptionConfiguration
{
    public string? ReplicaKmsKeyId { get; set; }
}

/// <summary>Destination configuration for a replication rule.</summary>
public class ReplicationDestination
{
    /// <summary>ARN of the destination bucket.</summary>
    public required string Bucket { get; set; }

    /// <summary>Storage class for replicated objects.</summary>
    public string? StorageClass { get; set; }

    /// <summary>Account ID of the destination bucket owner, required for cross-account replication.</summary>
    public string? Account { get; set; }

    /// <summary>Access control translation owner value, required for cross-account replication.</summary>
    public string? AccessControlTranslationOwner { get; set; }

    /// <summary>Encryption configuration for replicated objects.</summary>
    public ReplicationEncryptionConfiguration? EncryptionConfiguration { get; set; }

    /// <summary>S3 Replication Time Control (RTC) configuration.</summary>
    public ReplicationTime? ReplicationTime { get; set; }

    /// <summary>Replication metrics configuration.</summary>
    public ReplicationMetrics? Metrics { get; set; }
}

/// <summary>Criteria for selecting source objects based on encryption.</summary>
public class SourceSelectionCriteria
{
    /// <summary>Whether to replicate SSE-KMS-encrypted objects.</summary>
    public ReplicationStatus? SseKmsEncryptedObjects { get; set; }
}

/// <summary>A single replication rule.</summary>
public class ReplicationRule
{
    public string? Id { get; set; }
    public int? Priority { get; set; }
    public ReplicationStatus Status { get; set; } = ReplicationStatus.Enabled;
    public ReplicationFilter? Filter { get; set; }
    public required ReplicationDestination Destination { get; set; }
    public ReplicationStatus? DeleteMarkerReplication { get; set; }
    public ReplicationStatus? ExistingObjectReplication { get; set; }
    public SourceSelectionCriteria? SourceSelectionCriteria { get; set; }
}

/// <summary>The complete replication configuration for a bucket.</summary>
public class ReplicationConfiguration
{
    private static readonly XNamespace Ns = Constants.S3Ns;

    /// <summary>IAM role ARN that S3 assumes when replicating objects.</summary>
    public string? Role { get; set; }

    /// <summary>Replication rules.</summary>
    public IReadOnlyList<ReplicationRule> Rules { get; set; } = Array.Empty<ReplicationRule>();

    /// <summary>Serializes this configuration to its S3 XML representation.</summary>
    public XElement Serialize()
    {
        var xConfig = new XElement(Ns + "ReplicationConfiguration");
        if (!string.IsNullOrEmpty(Role))
            xConfig.Add(new XElement(Ns + "Role", Role));
        foreach (var rule in Rules)
            xConfig.Add(SerializeRule(rule));
        return xConfig;
    }

    /// <summary>Deserializes a <see cref="ReplicationConfiguration"/> from an XML element.</summary>
    public static ReplicationConfiguration Deserialize(XElement xElement)
    {
        return new ReplicationConfiguration
        {
            Role = xElement.Element(Ns + "Role")?.Value,
            Rules = xElement.Elements(Ns + "Rule").Select(DeserializeRule).ToList()
        };
    }

    private static string SerializeStatus(ReplicationStatus status) =>
        status == ReplicationStatus.Enabled ? "Enabled" : "Disabled";

    private static ReplicationStatus DeserializeStatus(string? value) =>
        value == "Enabled" ? ReplicationStatus.Enabled : ReplicationStatus.Disabled;

    private static XElement SerializeRule(ReplicationRule rule)
    {
        var xRule = new XElement(Ns + "Rule");
        if (!string.IsNullOrEmpty(rule.Id))
            xRule.Add(new XElement(Ns + "ID", rule.Id));
        if (rule.Priority.HasValue)
            xRule.Add(new XElement(Ns + "Priority", rule.Priority.Value));
        xRule.Add(new XElement(Ns + "Status", SerializeStatus(rule.Status)));
        if (rule.Filter != null)
            xRule.Add(SerializeFilter(rule.Filter));
        xRule.Add(SerializeDestination(rule.Destination));
        if (rule.DeleteMarkerReplication.HasValue)
            xRule.Add(new XElement(Ns + "DeleteMarkerReplication",
                new XElement(Ns + "Status", SerializeStatus(rule.DeleteMarkerReplication.Value))));
        if (rule.ExistingObjectReplication.HasValue)
            xRule.Add(new XElement(Ns + "ExistingObjectReplication",
                new XElement(Ns + "Status", SerializeStatus(rule.ExistingObjectReplication.Value))));
        if (rule.SourceSelectionCriteria != null)
            xRule.Add(SerializeSourceSelectionCriteria(rule.SourceSelectionCriteria));
        return xRule;
    }

    private static XElement SerializeFilter(ReplicationFilter filter)
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
                foreach (var t in filter.And.Tags)
                    xAnd.Add(SerializeTag(t));
            xFilter.Add(xAnd);
        }
        else if (filter.Prefix != null)
            xFilter.Add(new XElement(Ns + "Prefix", filter.Prefix));
        return xFilter;
    }

    private static XElement SerializeTag(ReplicationTag tag) =>
        new XElement(Ns + "Tag",
            new XElement(Ns + "Key", tag.Key),
            new XElement(Ns + "Value", tag.Value));

    private static XElement SerializeDestination(ReplicationDestination dest)
    {
        var xDest = new XElement(Ns + "Destination",
            new XElement(Ns + "Bucket", dest.Bucket));
        if (!string.IsNullOrEmpty(dest.StorageClass))
            xDest.Add(new XElement(Ns + "StorageClass", dest.StorageClass));
        if (!string.IsNullOrEmpty(dest.Account))
            xDest.Add(new XElement(Ns + "Account", dest.Account));
        if (!string.IsNullOrEmpty(dest.AccessControlTranslationOwner))
            xDest.Add(new XElement(Ns + "AccessControlTranslation",
                new XElement(Ns + "Owner", dest.AccessControlTranslationOwner)));
        if (dest.EncryptionConfiguration?.ReplicaKmsKeyId != null)
            xDest.Add(new XElement(Ns + "EncryptionConfiguration",
                new XElement(Ns + "ReplicaKmsKeyID", dest.EncryptionConfiguration.ReplicaKmsKeyId)));
        if (dest.ReplicationTime != null)
        {
            var xRtc = new XElement(Ns + "ReplicationTime",
                new XElement(Ns + "Status", SerializeStatus(dest.ReplicationTime.Status)));
            if (dest.ReplicationTime.Time != null)
                xRtc.Add(new XElement(Ns + "Time", new XElement(Ns + "Minutes", dest.ReplicationTime.Time.Minutes)));
            xDest.Add(xRtc);
        }
        if (dest.Metrics != null)
        {
            var xMetrics = new XElement(Ns + "Metrics",
                new XElement(Ns + "Status", SerializeStatus(dest.Metrics.Status)));
            if (dest.Metrics.EventThreshold != null)
                xMetrics.Add(new XElement(Ns + "EventThreshold",
                    new XElement(Ns + "Minutes", dest.Metrics.EventThreshold.Minutes)));
            xDest.Add(xMetrics);
        }
        return xDest;
    }

    private static XElement SerializeSourceSelectionCriteria(SourceSelectionCriteria ssc)
    {
        var xSsc = new XElement(Ns + "SourceSelectionCriteria");
        if (ssc.SseKmsEncryptedObjects.HasValue)
            xSsc.Add(new XElement(Ns + "SseKmsEncryptedObjects",
                new XElement(Ns + "Status", SerializeStatus(ssc.SseKmsEncryptedObjects.Value))));
        return xSsc;
    }

    private static ReplicationRule DeserializeRule(XElement xRule)
    {
        var id = xRule.Element(Ns + "ID")?.Value;
        var priorityText = xRule.Element(Ns + "Priority")?.Value;
        var status = DeserializeStatus(xRule.Element(Ns + "Status")?.Value);

        var xFilter = xRule.Element(Ns + "Filter");
        ReplicationFilter? filter = null;
        if (xFilter != null)
        {
            var xTag = xFilter.Element(Ns + "Tag");
            var xAnd = xFilter.Element(Ns + "And");
            if (xTag != null)
                filter = new ReplicationFilter { Tag = DeserializeTag(xTag) };
            else if (xAnd != null)
                filter = new ReplicationFilter
                {
                    And = new ReplicationFilterAnd
                    {
                        Prefix = xAnd.Element(Ns + "Prefix")?.Value,
                        Tags = xAnd.Elements(Ns + "Tag").Select(DeserializeTag).ToList()
                    }
                };
            else
                filter = new ReplicationFilter { Prefix = xFilter.Element(Ns + "Prefix")?.Value };
        }

        var xDest = xRule.Element(Ns + "Destination")!;
        var dest = new ReplicationDestination
        {
            Bucket = xDest.Element(Ns + "Bucket")?.Value ?? string.Empty,
            StorageClass = xDest.Element(Ns + "StorageClass")?.Value,
            Account = xDest.Element(Ns + "Account")?.Value,
            AccessControlTranslationOwner = xDest.Element(Ns + "AccessControlTranslation")?.Element(Ns + "Owner")?.Value,
            EncryptionConfiguration = xDest.Element(Ns + "EncryptionConfiguration") is { } xEnc
                ? new ReplicationEncryptionConfiguration { ReplicaKmsKeyId = xEnc.Element(Ns + "ReplicaKmsKeyID")?.Value }
                : null,
            ReplicationTime = xDest.Element(Ns + "ReplicationTime") is { } xRtc
                ? new ReplicationTime
                {
                    Status = DeserializeStatus(xRtc.Element(Ns + "Status")?.Value),
                    Time = xRtc.Element(Ns + "Time") is { } xTime
                        ? new ReplicationTimeValue { Minutes = int.Parse(xTime.Element(Ns + "Minutes")?.Value ?? "0") }
                        : null,
                }
                : null,
            Metrics = xDest.Element(Ns + "Metrics") is { } xMetrics
                ? new ReplicationMetrics
                {
                    Status = DeserializeStatus(xMetrics.Element(Ns + "Status")?.Value),
                    EventThreshold = xMetrics.Element(Ns + "EventThreshold") is { } xEt
                        ? new ReplicationTimeValue { Minutes = int.Parse(xEt.Element(Ns + "Minutes")?.Value ?? "0") }
                        : null,
                }
                : null,
        };

        var xSsc = xRule.Element(Ns + "SourceSelectionCriteria");
        SourceSelectionCriteria? ssc = null;
        if (xSsc != null)
        {
            ssc = new SourceSelectionCriteria
            {
                SseKmsEncryptedObjects = xSsc.Element(Ns + "SseKmsEncryptedObjects") is { } xSseKms
                    ? DeserializeStatus(xSseKms.Element(Ns + "Status")?.Value)
                    : null
            };
        }

        return new ReplicationRule
        {
            Id = id,
            Priority = priorityText != null ? int.Parse(priorityText) : null,
            Status = status,
            Filter = filter,
            Destination = dest,
            DeleteMarkerReplication = xRule.Element(Ns + "DeleteMarkerReplication") is { } xDmr
                ? DeserializeStatus(xDmr.Element(Ns + "Status")?.Value)
                : null,
            ExistingObjectReplication = xRule.Element(Ns + "ExistingObjectReplication") is { } xEor
                ? DeserializeStatus(xEor.Element(Ns + "Status")?.Value)
                : null,
            SourceSelectionCriteria = ssc,
        };
    }

    private static ReplicationTag DeserializeTag(XElement xTag) =>
        new ReplicationTag(xTag.Element(Ns + "Key")?.Value ?? string.Empty, xTag.Element(Ns + "Value")?.Value ?? string.Empty);
}
