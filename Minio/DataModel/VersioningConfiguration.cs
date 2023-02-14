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

using System.Xml.Serialization;

namespace Minio.DataModel;

[Serializable]
[XmlRoot(ElementName = "VersioningConfiguration")]
public class VersioningConfiguration
{
    public VersioningConfiguration()
    {
        Status = "Off";
        MfaDelete = "Disabled";
    }

    public VersioningConfiguration(bool enableVersioning = true)
    {
        if (enableVersioning)
            Status = "Enabled";
        else
            Status = "Suspended";
    }

    public VersioningConfiguration(VersioningConfiguration vc)
    {
        if (vc is null) throw new ArgumentNullException(nameof(vc));

        Status = vc.Status;
        MfaDelete = vc.MfaDelete;
    }

    [XmlElement] public string Status { get; set; }

    public string MfaDelete { get; set; }
}
