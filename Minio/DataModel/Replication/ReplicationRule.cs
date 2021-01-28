/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2021 MinIO, Inc.
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
using System.Xml;
using System.Xml.Serialization;
using Minio.DataModel.Replication;

namespace Minio.DataModel
{
    [Serializable]
    [XmlRoot(ElementName = "Rule")]
    public class ReplicationRule
    {
        [XmlElement(ElementName = "DeleteMarkerReplication", IsNullable = true)]
        public DeleteMarkerReplication DeleteMarkerReplication { get; set; }
        [XmlElement("Destination")]
        public ReplicationDestination Destination { get; set; }
        [XmlElement(ElementName = "ExistingObjectReplication", IsNullable = true)]
        public ExistingObjectReplication ExistingObjectReplication { get; set; }
        [XmlElement(ElementName = "Filter", IsNullable = true)]
        public RuleFilter Filter { get; set; }
        [XmlElement("Priority")]
        public uint Priority { get; set; }
        [XmlElement("ID")]
        public string ID { get; set; }
        [XmlElement(ElementName = "Prefix", IsNullable = true)]
        public string Prefix { get; set; }
        [XmlElement("DeleteReplication")]
        public DeleteReplication DeleteReplication { get; set; }
        [XmlElement(ElementName = "SourceSelectionCriteria", IsNullable = true)]
        public SourceSelectionCriteria SourceSelectionCriteria { get; set; }
        [XmlElement("Status")]
        public string Status { get; set; }
        public const string StatusEnabled = "Enabled";
        public const string StatusDisabled = "Disabled";

        public ReplicationRule()
        {
        }

        public ReplicationRule(DeleteMarkerReplication deleteMarkerReplication, ReplicationDestination destination,
                ExistingObjectReplication existingObjectReplication, RuleFilter filter, DeleteReplication deleteReplication,
                uint priority, string id, string prefix, SourceSelectionCriteria sourceSelectionCriteria, string status)
        {
            if (string.IsNullOrEmpty(status) || string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentNullException(nameof(Status) + " cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(ID) + " cannot be null or empty.");
            }
            if (deleteReplication == null)
            {
                throw new ArgumentNullException(nameof(DeleteReplication) + " cannot be null or empty.");
            }
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(Destination) + " cannot be null or empty.");
            }
            this.DeleteMarkerReplication = deleteMarkerReplication;
            this.Destination = destination;
            this.ExistingObjectReplication = existingObjectReplication;
            this.Filter = filter;
            this.Priority = priority;
            this.DeleteReplication = deleteReplication;
            this.ID = id;
            this.Prefix = prefix;
            this.SourceSelectionCriteria = sourceSelectionCriteria;
            this.Status = status;               
        }
    }
}