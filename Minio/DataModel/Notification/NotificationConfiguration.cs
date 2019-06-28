/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Minio.DataModel
{
    /// <summary>
    /// NotificationConfig - represents one single notification configuration
    /// such as topic, queue or lambda configuration
    /// </summary>
    public class NotificationConfiguration
    {
        [XmlElement]
        public string Id { get; set; }
        private Arn Arn { get; set; }
        [XmlElement("Event")]
        public List<EventType> Events { get; set; }
        [XmlElement("Filter")]
        public Filter Filter;

        public NotificationConfiguration()
        {
            this.Arn = null;
            this.Events = new List<EventType>();
        }

        public NotificationConfiguration(string arn)
        {
            this.Arn = new Arn(arn);
        }

        public NotificationConfiguration(Arn arn)
        {
            this.Arn = arn;
        }

        public void AddEvents(List<EventType> evnt)
        {
            if (this.Events == null)
            {
                this.Events = new List<EventType>();
            }

            this.Events.AddRange(evnt);
        }

        /// <summary>
        /// AddFilterSuffix sets the suffix configuration to the current notification config
        /// </summary>
        /// <param name="suffix"></param>
        public void AddFilterSuffix(string suffix)
        {
            if (this.Filter == null)
            {
                this.Filter = new Filter();
            }

            FilterRule newFilterRule = new FilterRule("suffix", suffix);
            // Replace any suffix rule if existing and add to the list otherwise
            for (int i = 0; i < this.Filter.S3Key.FilterRules.Count; i++)
            {
                if (this.Filter.S3Key.FilterRules[i].Equals("suffix"))
                {
                    this.Filter.S3Key.FilterRules[i] = newFilterRule;
                    return;
                }
            }
            this.Filter.S3Key.FilterRules.Add(newFilterRule);
        }

        /// <summary>
        /// AddFilterPrefix sets the prefix configuration to the current notification config
        /// </summary>
        /// <param name="prefix"></param>
        public void AddFilterPrefix(string prefix)
        {
            if (this.Filter == null)
            {
                this.Filter = new Filter();
            }

            FilterRule newFilterRule = new FilterRule("prefix", prefix);
            // Replace any prefix rule if existing and add to the list otherwise
            for (int i = 0; i < this.Filter.S3Key.FilterRules.Count; i++)
            {
                if (this.Filter.S3Key.FilterRules[i].Equals("prefix"))
                {
                    this.Filter.S3Key.FilterRules[i] = newFilterRule;
                    return;
                }
            }
            this.Filter.S3Key.FilterRules.Add(newFilterRule);
        }

        public bool ShouldSerializeFilter() => this.Filter != null;

        public bool ShouldSerializeId() => Id != null;

        public bool ShouldSerializeEvents()
        {
            return this.Events != null && this.Events.Count > 0;
        }

        internal bool IsIdSet() => this.Id != null;
    }
}
