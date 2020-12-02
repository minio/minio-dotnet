/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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
using System.Xml.Serialization;

namespace Minio
{
    public enum RetentionMode
    {
        GOVERNANCE,
        COMPLIANCE
    }
    [Serializable]
    [XmlRoot(ElementName = "Retention", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class ObjectRetentionConfiguration
    {
        [XmlElement("Mode")]
        public RetentionMode Mode { get; set; }

        [XmlElement("RetainUntilDate")]
        public string RetainUntilDate { get; set; }

        public ObjectRetentionConfiguration()
        {
            this.Mode = RetentionMode.GOVERNANCE;
            this.RetainUntilDate = utils.To8601String(DateTime.Now);
        }

        public ObjectRetentionConfiguration(DateTime date, RetentionMode mode = RetentionMode.GOVERNANCE)
        {
            this.RetainUntilDate = utils.To8601String(date);
            this.Mode = mode;
        }

        public ObjectRetentionConfiguration(uint numOfDays, RetentionMode mode = RetentionMode.GOVERNANCE)
        {
            DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            this.RetainUntilDate = utils.To8601String(today.AddDays(numOfDays));
            this.Mode = mode;
        }
    }
}