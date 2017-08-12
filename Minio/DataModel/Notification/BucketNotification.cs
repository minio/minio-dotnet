/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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

namespace Minio.DataModel.Notification
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    [XmlRoot(ElementName = "NotificationConfiguration", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/", IsNullable = true)]
	public class AwsBucketNotification : BucketNotification
	{
		
	}

    // Helper class to parse NotificationConfiguration from AWS S3 response XML.
	[XmlRoot(ElementName = "NotificationConfiguration", IsNullable = true)]
    public class BucketNotification
    {
        public BucketNotification()
        {
            this.LambdaConfigs = new List<LambdaConfig>();
            this.TopicConfigs = new List<TopicConfig>();
            this.QueueConfigs = new List<QueueConfig>();
        }

        [XmlElement("CloudFunctionConfiguration")]
        public List<LambdaConfig> LambdaConfigs { get; set; }

        [XmlElement("TopicConfiguration")]
        public List<TopicConfig> TopicConfigs { get; set; }

        [XmlElement("QueueConfiguration")]
        public List<QueueConfig> QueueConfigs { get; set; }

        public string Name { get; set; }

        // AddTopic adds a given topic config to the general bucket notification config
        public void AddTopic(TopicConfig topicConfig)
        {
            var isTopicFound = this.TopicConfigs.Exists(t => t.Topic.Equals(topicConfig.Topic));
            if (!isTopicFound)
            {
                this.TopicConfigs.Add(topicConfig);
            }
        }

        // AddQueue adds a given queue config to the general bucket notification config
        public void AddQueue(QueueConfig queueConfig)
        {
            var isQueueFound = this.QueueConfigs.Exists(t => t.Equals(queueConfig));
            if (!isQueueFound)
            {
                this.QueueConfigs.Add(queueConfig);
            }
        }

        // AddLambda adds a given lambda config to the general bucket notification config
        public void AddLambda(LambdaConfig lambdaConfig)
        {
            var isLambdaFound = this.LambdaConfigs.Exists(t => t.Lambda.Equals(lambdaConfig.Lambda));
            if (!isLambdaFound)
            {
                this.LambdaConfigs.Add(lambdaConfig);
            }
        }

        // RemoveTopicByArn removes all topic configurations that match the exact specified ARN
        public void RemoveTopicByArn(Arn topicArn)
        {
            this.TopicConfigs.RemoveAll(t => t.Topic.Equals(topicArn.ArnString));
        }

        // RemoveQueueByArn removes all queue configurations that match the exact specified ARN
        public void RemoveQueueByArn(Arn queueArn)
        {
            this.QueueConfigs.RemoveAll(t => t.Queue.Equals(queueArn.ArnString));
        }

        // RemoveLambdaByArn removes all lambda configurations that match the exact specified ARN
        public void RemoveLambdaByArn(Arn lambdaArn)
        {
            this.LambdaConfigs.RemoveAll(t => t.Lambda.Equals(lambdaArn.ArnString));
        }

        // Helper methods to guide XMLSerializer
        public bool ShouldSerializeLambdaConfigs()
        {
            return this.LambdaConfigs.Count > 0;
        }

        public bool ShouldSerializeTopicConfigs()
        {
            return this.TopicConfigs.Count > 0;
        }

        public bool ShouldSerializeQueueConfigs()
        {
            return this.QueueConfigs.Count > 0;
        }

        public bool ShouldSerializeName()
        {
            return this.Name != null;
        }

        // Serializes the notification configuration as an XML string
        public string ToXml()
        {
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            using (var ms = new MemoryStream())
            {
                var writer = XmlWriter.Create(ms, settings);

                var names = new XmlSerializerNamespaces();
                names.Add("", "http://s3.amazonaws.com/doc/2006-03-01/");

                var cs = new XmlSerializer(typeof(BucketNotification));

                cs.Serialize(writer, this, names);

                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                var sr = new StreamReader(ms);
                var xml = sr.ReadToEnd();
                return xml;
            }
        }
    }
}