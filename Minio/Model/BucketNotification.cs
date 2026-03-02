using System.Xml.Linq;

namespace Minio.Model;

/// <summary>
/// Represents the full notification configuration for an S3 bucket, including
/// Lambda (CloudFunction), SNS topic, and SQS queue configurations.
/// </summary>
public sealed class BucketNotification
{
    /// <summary>
    /// Gets the list of Lambda (CloudFunction) notification configurations.
    /// </summary>
    public IList<LambdaConfig> LambdaConfigs { get; } = new List<LambdaConfig>();

    /// <summary>
    /// Gets the list of SNS topic notification configurations.
    /// </summary>
    public IList<TopicConfig> TopicConfigs { get; } = new List<TopicConfig>();

    /// <summary>
    /// Gets the list of SQS queue notification configurations.
    /// </summary>
    public IList<QueueConfig> QueueConfigs { get; } = new List<QueueConfig>();

    /// <summary>
    /// Serializes this bucket notification configuration to its S3 XML representation.
    /// </summary>
    /// <returns>An <see cref="XElement"/> representing the <c>NotificationConfiguration</c> XML element.</returns>
    public XElement Serialize()
    {
        return new XElement(Constants.S3Ns + "NotificationConfiguration",
            LambdaConfigs.Select(c => c.Serialize()),
            TopicConfigs.Select(c => c.Serialize()),
            QueueConfigs.Select(c => c.Serialize()));
    }

    /// <summary>
    /// Deserializes a <see cref="BucketNotification"/> from an S3 <c>NotificationConfiguration</c> XML element.
    /// </summary>
    /// <param name="xElement">The <c>NotificationConfiguration</c> XML element to deserialize.</param>
    /// <returns>A populated <see cref="BucketNotification"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the XML element name is not <c>NotificationConfiguration</c>.</exception>
    public static BucketNotification Deserialize(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (xElement.Name != Constants.S3Ns + "NotificationConfiguration")
            throw new InvalidOperationException("Invalid XML element encountered");

        var bucketNotification = new BucketNotification();
        foreach (var xConfig in xElement.Elements().Where(x => x.Name.NamespaceName == Constants.S3Ns))
        {
            switch (xConfig.Name.LocalName)
            {
                case "CloudFunctionConfiguration":
                    bucketNotification.LambdaConfigs.Add(LambdaConfig.Deserialize(xConfig));
                    break;
                case "QueueConfiguration":
                    bucketNotification.QueueConfigs.Add(QueueConfig.Deserialize(xConfig));
                    break;
                case "TopicConfiguration":
                    bucketNotification.TopicConfigs.Add(TopicConfig.Deserialize(xConfig));
                    break;
            }
        }
        return bucketNotification;
    }
}

/// <summary>
/// Base class for S3 notification configuration entries. Provides common properties
/// such as an identifier, the list of events that trigger the notification, and
/// optional key-name filtering rules.
/// </summary>
public abstract class NotificationConfiguration
{
    /// <summary>
    /// Gets or sets the optional unique identifier for this notification configuration.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of S3 event types that trigger this notification.
    /// </summary>
    public IList<EventType> Events { get; } = new List<EventType>();

    /// <summary>
    /// Gets a dictionary of S3 key-name filter rules (e.g., prefix or suffix filters)
    /// that control which objects trigger the notification.
    /// </summary>
    public IDictionary<string, string> Filter { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Serializes common notification configuration fields (Id, Events, and Filter rules)
    /// into the provided XML element.
    /// </summary>
    /// <param name="xElement">The XML element to populate with common configuration fields.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    protected void SerializeInner(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (!string.IsNullOrEmpty(Id))
            xElement.Add(new XElement(Constants.S3Ns + "Id", Id));
        foreach (var evt in Events)
            xElement.Add(new XElement(Constants.S3Ns + "Event", evt.ToString()));

        if (Filter.Count > 0)
            xElement.Add(new XElement(Constants.S3Ns + "Filter",
                new XElement(Constants.S3Ns + "S3Key",
                    Filter.Select(kv => new XElement(Constants.S3Ns + "FilterRule",
                        new XElement(Constants.S3Ns + "Name", kv.Key),
                        new XElement(Constants.S3Ns + "Value", kv.Value))))));
    }

    /// <summary>
    /// Deserializes common notification configuration fields (Id, Events, and Filter rules)
    /// from the provided XML element.
    /// </summary>
    /// <param name="xElement">The XML element containing the common configuration fields.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a filter rule is missing a required <c>Name</c> or <c>Value</c> element.</exception>
    protected void DeserializeInner(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        Id = xElement.Element(Constants.S3Ns + "Id")?.Value ?? string.Empty;
        foreach (var xEvent in xElement.Elements(Constants.S3Ns + "Event"))
            Events.Add(new EventType(xEvent.Value));
        var xFilterRules = xElement
            .Element(Constants.S3Ns + "Filter")?
            .Element(Constants.S3Ns + "S3Key")?
            .Elements(Constants.S3Ns + "FilterRule");
        if (xFilterRules != null)
        {
            foreach (var xFilter in xFilterRules)
            {
                var name = xFilter.Element(Constants.S3Ns + "Name")?.Value ??
                           throw new InvalidOperationException("Missing Name in XML");
                var value = xFilter.Element(Constants.S3Ns + "Value")?.Value ??
                            throw new InvalidOperationException("Missing Value in XML");
                Filter[name] = value;
            }
        }
    }
}

/// <summary>
/// Represents a Lambda (CloudFunction) destination for S3 bucket notifications.
/// </summary>
public sealed class LambdaConfig : NotificationConfiguration
{
    /// <summary>
    /// Gets or sets the ARN of the Lambda function that receives the notification.
    /// </summary>
    public required string Lambda { get; set; }

    /// <summary>
    /// Serializes this Lambda notification configuration to its S3 XML representation.
    /// </summary>
    /// <returns>An <see cref="XElement"/> representing the <c>CloudFunctionConfiguration</c> XML element.</returns>
    public XElement Serialize()
    {
        var xElement = new XElement(Constants.S3Ns + "CloudFunctionConfiguration",
            new XElement(Constants.S3Ns + "CloudFunction", Lambda));
        SerializeInner(xElement);
        return xElement;
    }

    /// <summary>
    /// Deserializes a <see cref="LambdaConfig"/> from an S3 <c>CloudFunctionConfiguration</c> XML element.
    /// </summary>
    /// <param name="xElement">The <c>CloudFunctionConfiguration</c> XML element to deserialize.</param>
    /// <returns>A populated <see cref="LambdaConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the XML element name is not <c>CloudFunctionConfiguration</c> or the <c>CloudFunction</c> element is missing.</exception>
    public static LambdaConfig Deserialize(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (xElement.Name != Constants.S3Ns + "CloudFunctionConfiguration")
            throw new InvalidOperationException("Invalid XML element encountered");

        var lambdaConfig = new LambdaConfig
        {
            Lambda = xElement.Element(Constants.S3Ns + "CloudFunction")?.Value ??
                     throw new InvalidOperationException("Missing CloudFunction in XML")
        };
        lambdaConfig.DeserializeInner(xElement);
        return lambdaConfig;
    }
}

/// <summary>
/// Represents an SNS topic destination for S3 bucket notifications.
/// </summary>
public sealed class TopicConfig : NotificationConfiguration
{
    /// <summary>
    /// Gets or sets the ARN of the SNS topic that receives the notification.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// Serializes this topic notification configuration to its S3 XML representation.
    /// </summary>
    /// <returns>An <see cref="XElement"/> representing the <c>TopicConfiguration</c> XML element.</returns>
    public XElement Serialize()
    {
        var xElement = new XElement(Constants.S3Ns + "TopicConfiguration",
            new XElement(Constants.S3Ns + "Topic", Topic));
        SerializeInner(xElement);
        return xElement;
    }

    /// <summary>
    /// Deserializes a <see cref="TopicConfig"/> from an S3 <c>TopicConfiguration</c> XML element.
    /// </summary>
    /// <param name="xElement">The <c>TopicConfiguration</c> XML element to deserialize.</param>
    /// <returns>A populated <see cref="TopicConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the XML element name is not <c>TopicConfiguration</c> or the <c>Topic</c> element is missing.</exception>
    public static TopicConfig Deserialize(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (xElement.Name != Constants.S3Ns + "TopicConfiguration")
            throw new InvalidOperationException("Invalid XML element encountered");

        var topicConfig = new TopicConfig
        {
            Topic = xElement.Element(Constants.S3Ns + "Topic")?.Value ??
                    throw new InvalidOperationException("Missing Topic in XML")
        };
        topicConfig.DeserializeInner(xElement);
        return topicConfig;
    }
}

/// <summary>
/// Represents an SQS queue destination for S3 bucket notifications.
/// </summary>
public sealed class QueueConfig: NotificationConfiguration
{
    /// <summary>
    /// Gets or sets the ARN of the SQS queue that receives the notification.
    /// </summary>
    public string Queue { get; set; }

    /// <summary>
    /// Serializes this queue notification configuration to its S3 XML representation.
    /// </summary>
    /// <returns>An <see cref="XElement"/> representing the <c>QueueConfiguration</c> XML element.</returns>
    public XElement Serialize()
    {
        var xElement = new XElement(Constants.S3Ns + "QueueConfiguration",
            new XElement(Constants.S3Ns + "Queue", Queue));
        SerializeInner(xElement);
        return xElement;
    }

    /// <summary>
    /// Deserializes a <see cref="QueueConfig"/> from an S3 <c>QueueConfiguration</c> XML element.
    /// </summary>
    /// <param name="xElement">The <c>QueueConfiguration</c> XML element to deserialize.</param>
    /// <returns>A populated <see cref="QueueConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xElement"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the XML element name is not <c>QueueConfiguration</c> or the <c>Queue</c> element is missing.</exception>
    public static QueueConfig Deserialize(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (xElement.Name != Constants.S3Ns + "QueueConfiguration")
            throw new InvalidOperationException("Invalid XML element encountered");

        var queueConfig = new QueueConfig
        {
            Queue = xElement.Element(Constants.S3Ns + "Queue")?.Value ??
                     throw new InvalidOperationException("Missing Queue in XML")
        };
        queueConfig.DeserializeInner(xElement);
        return queueConfig;
    }
}

/// <summary>
/// Represents an S3 event type used to configure bucket notification triggers.
/// This is a strongly-typed wrapper around the S3 event name string (e.g., <c>s3:ObjectCreated:*</c>).
/// </summary>
public readonly struct EventType : IEquatable<EventType>
{
    /// <summary>
    /// Matches all object-created events (<c>s3:ObjectCreated:*</c>).
    /// </summary>
    public static EventType ObjectCreatedAll { get; } = new("s3:ObjectCreated:*");

    /// <summary>
    /// Matches object-created events triggered by a PUT operation (<c>s3:ObjectCreated:Put</c>).
    /// </summary>
    public static EventType ObjectCreatedPut { get; } = new("s3:ObjectCreated:Put");

    /// <summary>
    /// Matches object-created events triggered by a POST operation (<c>s3:ObjectCreated:Post</c>).
    /// </summary>
    public static EventType ObjectCreatedPost { get; } = new("s3:ObjectCreated:Post");

    /// <summary>
    /// Matches object-created events triggered by a COPY operation (<c>s3:ObjectCreated:Copy</c>).
    /// </summary>
    public static EventType ObjectCreatedCopy { get; } = new("s3:ObjectCreated:Copy");

    /// <summary>
    /// Matches object-created events triggered by completing a multipart upload (<c>s3:ObjectCreated:CompleteMultipartUpload</c>).
    /// </summary>
    public static EventType ObjectCreatedCompleteMultipartUpload { get; } = new("s3:ObjectCreated:CompleteMultipartUpload");

    /// <summary>
    /// Matches object-accessed events triggered by a GET operation (<c>s3:ObjectAccessed:Get</c>).
    /// </summary>
    public static EventType ObjectAccessedGet { get; } = new("s3:ObjectAccessed:Get");

    /// <summary>
    /// Matches object-accessed events triggered by a HEAD operation (<c>s3:ObjectAccessed:Head</c>).
    /// </summary>
    public static EventType ObjectAccessedHead { get; } = new("s3:ObjectAccessed:Head");

    /// <summary>
    /// Matches all object-accessed events (<c>s3:ObjectAccessed:*</c>).
    /// </summary>
    public static EventType ObjectAccessedAll { get; } = new("s3:ObjectAccessed:*");

    /// <summary>
    /// Matches all object-removed events (<c>s3:ObjectRemoved:*</c>).
    /// </summary>
    public static EventType ObjectRemovedAll { get; } = new("s3:ObjectRemoved:*");

    /// <summary>
    /// Matches object-removed events triggered by a DELETE operation (<c>s3:ObjectRemoved:Delete</c>).
    /// </summary>
    public static EventType ObjectRemovedDelete { get; } = new("s3:ObjectRemoved:Delete");

    /// <summary>
    /// Matches object-removed events triggered by the creation of a delete marker (<c>s3:ObjectRemoved:DeleteMarkerCreated</c>).
    /// </summary>
    public static EventType ObjectRemovedDeleteMarkerCreated { get; } = new("s3:ObjectRemoved:DeleteMarkerCreated");

    /// <summary>
    /// Matches events indicating that an object stored with Reduced Redundancy Storage has been lost (<c>s3:ReducedRedundancyLostObject</c>).
    /// </summary>
    public static EventType ReducedRedundancyLostObject { get; } = new("s3:ReducedRedundancyLostObject");

    private readonly string _value;

    /// <summary>
    /// Initializes a new <see cref="EventType"/> with the specified S3 event name string.
    /// </summary>
    /// <param name="value">The S3 event name string, for example <c>s3:ObjectCreated:*</c>.</param>
    public EventType(string value)
    {
        _value = value;
    }

    /// <summary>
    /// Returns the S3 event name string for this event type.
    /// </summary>
    /// <returns>The S3 event name string.</returns>
    public override string ToString() => _value;

    /// <summary>
    /// Implicitly converts an <see cref="EventType"/> to its underlying S3 event name string.
    /// </summary>
    /// <param name="et">The event type to convert.</param>
    /// <returns>The S3 event name string.</returns>
    public static implicit operator string(EventType et) => et._value;

    /// <summary>
    /// Determines whether this instance is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an <see cref="EventType"/> with the same value; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is EventType other &&
               other._value.Equals(_value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether this instance is equal to another <see cref="EventType"/>.
    /// </summary>
    /// <param name="other">The other <see cref="EventType"/> to compare with.</param>
    /// <returns><c>true</c> if both instances have the same S3 event name string; otherwise, <c>false</c>.</returns>
    public bool Equals(EventType other) =>
        _value.Equals(other._value, StringComparison.Ordinal);

    /// <summary>
    /// Returns the hash code for this instance, based on the S3 event name string.
    /// </summary>
    /// <returns>An integer hash code.</returns>
    public override int GetHashCode() => _value.GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Determines whether two <see cref="EventType"/> instances are equal.
    /// </summary>
    /// <param name="left">The first event type.</param>
    /// <param name="right">The second event type.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(EventType left, EventType right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="EventType"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first event type.</param>
    /// <param name="right">The second event type.</param>
    /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(EventType left, EventType right) => !(left == right);
}
