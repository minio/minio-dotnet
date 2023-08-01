/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2021 MinIO, Inc.
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

using System.Xml;
using System.Xml.Serialization;

namespace Minio.Credentials;

[Serializable]
[XmlRoot(ElementName = "AssumeRoleResponse", Namespace = "https://sts.amazonaws.com/doc/2011-06-15/")]
public class AssumeRoleResponse
{
    [XmlElement(ElementName = "AssumeRoleResult")]
    public AssumeRoleResult AssumeRole { get; set; }

    public string ToXML()
    {
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
        using var ms = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(ms, settings);
        var names = new XmlSerializerNamespaces();
        names.Add(string.Empty, "https://sts.amazonaws.com/doc/2011-06-15/");

        var cs = new XmlSerializer(typeof(CertificateResponse));
        cs.Serialize(xmlWriter, this, names);

        ms.Flush();
        _ = ms.Seek(0, SeekOrigin.Begin);
        using var streamReader = new StreamReader(ms);
        return streamReader.ReadToEnd();
    }
}
