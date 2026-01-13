/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017-2021 MinIO, Inc.
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

using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Serialization;

namespace Minio.DataModel.Notification;

/// <summary>
///     Helper class to parse NotificationConfiguration from AWS S3 response XML.
/// </summary>
[Serializable]
[XmlRoot(ElementName = "NotificationConfiguration", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
public class BucketNotification
{
    public BucketNotification()
    {
        LambdaConfigs = [];
        TopicConfigs = [];
        QueueConfigs = [];
    }

    [XmlElement("CloudFunctionConfiguration")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists",
        Justification = "Needs to be concrete type for XML deserialization")]
    public List<LambdaConfig> LambdaConfigs { get; set; }

    [XmlElement("TopicConfiguration")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists",
        Justification = "Needs to be concrete type for XML deserialization")]
    public List<TopicConfig> TopicConfigs { get; set; }

    [XmlElement("QueueConfiguration")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists",
        Justification = "Needs to be concrete type for XML deserialization")]
    public List<QueueConfig> QueueConfigs { get; set; }

    public string Name { get; set; }

    /// <summary>
    ///     AddTopic adds a given topic config to the general bucket notification config
    /// </summary>
    /// <param name="topicConfig"></param>
    public void AddTopic(TopicConfig topicConfig)
    {
        var isTopicFound = TopicConfigs.Exists(t => t.Topic.Equals(topicConfig));
        if (!isTopicFound) TopicConfigs.Add(topicConfig);
    }

    /// <summary>
    ///     AddQueue adds a given queue config to the general bucket notification config
    /// </summary>
    /// <param name="queueConfig"></param>
    public void AddQueue(QueueConfig queueConfig)
    {
        var isQueueFound = QueueConfigs.Exists(t => t.Equals(queueConfig));
        if (!isQueueFound) QueueConfigs.Add(queueConfig);
    }

    /// <summary>
    ///     AddLambda adds a given lambda config to the general bucket notification config
    /// </summary>
    /// <param name="lambdaConfig"></param>
    public void AddLambda(LambdaConfig lambdaConfig)
    {
        var isLambdaFound = LambdaConfigs.Exists(t => t.Lambda.Equals(lambdaConfig));
        if (!isLambdaFound) LambdaConfigs.Add(lambdaConfig);
    }

    /// <summary>
    ///     RemoveTopicByArn removes all topic configurations that match the exact specified ARN
    /// </summary>
    /// <param name="topicArn"></param>
    public void RemoveTopicByArn(Arn topicArn)
    {
        var numRemoved = TopicConfigs.RemoveAll(t => t.Topic.Equals(topicArn));
    }

    /// <summary>
    ///     RemoveQueueByArn removes all queue configurations that match the exact specified ARN
    /// </summary>
    /// <param name="queueArn"></param>
    public void RemoveQueueByArn(Arn queueArn)
    {
        var numRemoved = QueueConfigs.RemoveAll(t => t.Queue.Equals(queueArn));
    }

    /// <summary>
    ///     RemoveLambdaByArn removes all lambda configurations that match the exact specified ARN
    /// </summary>
    /// <param name="lambdaArn"></param>
    public void RemoveLambdaByArn(Arn lambdaArn)
    {
        var numRemoved = LambdaConfigs.RemoveAll(t => t.Lambda.Equals(lambdaArn));
    }

    /// <summary>
    ///     Helper methods to guide XMLSerializer
    /// </summary>
    /// <returns></returns>
    public bool ShouldSerializeLambdaConfigs()
    {
        return LambdaConfigs.Count > 0;
    }

    public bool ShouldSerializeTopicConfigs()
    {
        return TopicConfigs.Count > 0;
    }

    public bool ShouldSerializeQueueConfigs()
    {
        return QueueConfigs.Count > 0;
    }

    public bool ShouldSerializeName()
    {
        return Name is not null;
    }

    /// <summary>
    ///     Serializes the notification configuration as an XML string
    /// </summary>
    /// <returns></returns>
    public string ToXML()
    {
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
        using var ms = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(ms, settings);
        var names = new XmlSerializerNamespaces();
        names.Add(string.Empty, "http://s3.amazonaws.com/doc/2006-03-01/");

        var cs = new XmlSerializer(typeof(BucketNotification));
        cs.Serialize(xmlWriter, this, names);

        ms.Flush();
        _ = ms.Seek(0, SeekOrigin.Begin);
        using var streamReader = new StreamReader(ms);
        return streamReader.ReadToEnd();
    }
}
