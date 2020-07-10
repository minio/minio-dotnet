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

namespace Minio.DataModel
{
    [Serializable]
    [XmlRoot(ElementName = "VersioningConfiguration", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class VersioningConfiguration
    {
        public VersioningConfiguration()
        {
            this.Status = null;
            this.MfaDelete = null;
        }

        public VersioningConfiguration(bool enable = true )
        {
            if (enable)
            {
                this.Status = "Enabled";
            }
            else
            {
                this.Status = "Suspended";
            }
            this.MfaDelete = "Disabled";
        }

        public VersioningConfiguration(VersioningConfiguration vc)
        {
            this.Status = vc.Status;
            this.MfaDelete = vc.MfaDelete;
        }

        [XmlElement]
        public string Status { get; set; }
        public string MfaDelete { get; set; }

        public bool IsNotVersioned()
        {
            return this.Status == null;
        }

        public bool IsVersioningEnabled()
        {
            return this.Status != null && this.Status.ToLower().Equals("enabled");
        }

        public bool IsVersioningSuspended()
        {
            return this.Status != null && this.Status.ToLower().Equals("suspended");
        }
    }
}
