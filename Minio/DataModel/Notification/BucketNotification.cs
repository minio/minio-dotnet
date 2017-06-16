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

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Minio.DataModel
{
    // Helper class to parse NotificationConfiguration from AWS S3 response XML.
    [Serializable]
    [XmlRoot(ElementName = "NotificationConfiguration", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class BucketNotification
    {
        public string Name { get; set; }

        [XmlElement("CloudFunctionConfiguration")]
        public List<LambdaConfig> LambdaConfigs;
        [XmlElement("TopicConfiguration")]
        public List<TopicConfig> TopicConfigs;
        [XmlElement("QueueConfiguration")]
        public List<QueueConfig> QueueConfigs;

        public BucketNotification()
        {
            LambdaConfigs = new List<LambdaConfig>();
            TopicConfigs = new List<TopicConfig>();
            QueueConfigs = new List<QueueConfig>();
        }
        // AddTopic adds a given topic config to the general bucket notification config
        public void AddTopic(TopicConfig topicConfig)
        {
            bool isTopicFound = this.TopicConfigs.Exists(t => t.Topic.Equals(topicConfig));
            if (!isTopicFound)
            {
                this.TopicConfigs.Add(topicConfig);
                return;
            }
        }

        // AddQueue adds a given queue config to the general bucket notification config
        public void AddQueue(QueueConfig queueConfig)
        {

            bool isQueueFound = this.QueueConfigs.Exists(t => t.Equals(queueConfig));
            if (!isQueueFound)
            {
                this.QueueConfigs.Add(queueConfig);
                return;
            }
        }

        // AddLambda adds a given lambda config to the general bucket notification config
        public void AddLambda(LambdaConfig lambdaConfig)
        {
            bool isLambdaFound = this.LambdaConfigs.Exists(t => t.Lambda.Equals(lambdaConfig));
            if (!isLambdaFound)
            {
                this.LambdaConfigs.Add(lambdaConfig);
                return;
            }
        }

        // RemoveTopicByArn removes all topic configurations that match the exact specified ARN
        public void RemoveTopicByArn(Arn topicArn)
        {
            var numRemoved = this.TopicConfigs.RemoveAll(t => t.Topic.Equals(topicArn));
        }

        // RemoveQueueByArn removes all queue configurations that match the exact specified ARN
        public void RemoveQueueByArn(Arn queueArn)
        {
            var numRemoved = this.QueueConfigs.RemoveAll(t => t.Queue.Equals(queueArn));
        }

        // RemoveLambdaByArn removes all lambda configurations that match the exact specified ARN
        public void RemoveLambdaByArn(Arn lambdaArn)
        {
            var numRemoved = this.LambdaConfigs.RemoveAll(t => t.Lambda.Equals(lambdaArn));
        }
        // Helper methods to guide XMLSerializer
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
            return this.Name != null;
        }
        // Serializes the notification configuration as an XML string
        public string ToXML()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            using (MemoryStream ms = new MemoryStream())
            {
                XmlWriter writer = XmlWriter.Create(ms, settings);

                XmlSerializerNamespaces names = new XmlSerializerNamespaces();
                names.Add("", "http://s3.amazonaws.com/doc/2006-03-01/");

                XmlSerializer cs = new XmlSerializer(typeof(BucketNotification));

                cs.Serialize(writer, this, names);

                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                StreamReader sr = new StreamReader(ms);
                var xml = sr.ReadToEnd();
                return xml;
            }
          
        }
    }
}
