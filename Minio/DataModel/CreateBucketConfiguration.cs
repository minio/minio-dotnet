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

using System.Xml;
using System.Xml.Serialization;
using Minio.DataModel.Notification;

namespace Minio.DataModel;

[XmlRoot(ElementName = "CreateBucketConfiguration", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
public class CreateBucketConfiguration
{
    public CreateBucketConfiguration()
    {
        LocationConstraint = null;
    }

    public CreateBucketConfiguration(string location = null)
    {
        LocationConstraint = location;
    }

    [XmlElement(ElementName = "LocationConstraint", IsNullable = true)]
    public string LocationConstraint { get; set; }

    public string ToXml()
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true
        };
        using var ms = new MemoryStream();
        using var writer = XmlWriter.Create(ms, settings);
        var names = new XmlSerializerNamespaces();
        names.Add(string.Empty, "http://s3.amazonaws.com/doc/2006-03-01/");

        var cs = new XmlSerializer(typeof(BucketNotification));
        cs.Serialize(writer, this, names);

        ms.Flush();
        _ = ms.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(ms);
        return sr.ReadToEnd();
    }
}
