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
using System.Xml.Serialization;

namespace Minio.DataModel
{
    [Serializable]
    [XmlRoot(ElementName = "AccessControlTranslation")]
    public class AccessControlTranslation
    {
        [XmlElement("Owner")]
        public string Owner { get; set; }

        public AccessControlTranslation(string owner) 
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrWhiteSpace(owner))
            {
                throw new ArgumentNullException(nameof(Owner) + " cannot be empty.");
            }
            this.Owner = owner;   
        }

        public AccessControlTranslation()
        {
        }
    }
}