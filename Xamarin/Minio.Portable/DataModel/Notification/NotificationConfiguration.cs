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
    using System.Xml.Serialization;

    // NotificationConfig - represents one single notification configuration
    // such as topic, queue or lambda configuration.
    public class NotificationConfiguration
    {
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

        [XmlElement]
        public string Id { get; set; }

        [XmlElement("Event")]
        public List<EventType> Events { get; set; }

        [XmlElement("Filter")]
        public Filter Filter { get; set; }

        private Arn Arn { get; }


        public void AddEvents(IEnumerable<EventType> evnt)
        {
            if (this.Events == null)
            {
                this.Events = new List<EventType>();
            }
            this.Events.AddRange(evnt);
        }

        /// <summary>
        ///     AddFilterSuffix sets the suffix configuration to the current notification config
        /// </summary>
        /// <param name="suffix"></param>
        public void AddFilterSuffix(string suffix)
        {
            if (this.Filter == null)
            {
                this.Filter = new Filter();
            }
            var newFilterRule = new FilterRule("suffix", suffix);
            // Replace any suffix rule if existing and add to the list otherwise
            for (var i = 0; i < this.Filter.S3Key.FilterRules.Count; i++)
            {
                if (this.Filter.S3Key.FilterRules[i].Value.Equals("suffix"))
                {
                    this.Filter.S3Key.FilterRules[i] = newFilterRule;
                    return;
                }
            }
            this.Filter.S3Key.FilterRules.Add(newFilterRule);
        }

        /// <summary>
        ///     AddFilterPrefix sets the prefix configuration to the current notification config
        /// </summary>
        public void AddFilterPrefix(string prefix)
        {
            if (this.Filter == null)
            {
                this.Filter = new Filter();
            }
            var newFilterRule = new FilterRule("prefix", prefix);
            // Replace any prefix rule if existing and add to the list otherwise
            for (var i = 0; i < this.Filter.S3Key.FilterRules.Count; i++)
            {
                if (!this.Filter.S3Key.FilterRules[i].Name.Equals("prefix"))
                {
                    continue;
                }

                this.Filter.S3Key.FilterRules[i] = newFilterRule;
                return;
            }
            this.Filter.S3Key.FilterRules.Add(newFilterRule);
        }

        public bool ShouldSerializeFilter()
        {
            return this.Filter != null;
        }

        public bool ShouldSerializeId()
        {
            return this.Id != null;
        }

        public bool ShouldSerializeEvents()
        {
            return this.Events != null && this.Events.Count > 0;
        }

        internal bool IsIdSet()
        {
            return this.Id != null;
        }
    }
}