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

using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Minio.DataModel
{
    [XmlRoot(ElementName = "CreateBucketConfiguration", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class CreateBucketConfiguration
    {
        public CreateBucketConfiguration()
        {
            this.LocationConstraint = null;
        }

        public CreateBucketConfiguration(string location = null)
        {
            this.LocationConstraint = location;
        }

        [XmlElement(ElementName = "LocationConstraint", IsNullable = true)]
        public string LocationConstraint { get; set; }

        public string ToXml()
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true
            };
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(ms, settings))
                {
                    XmlSerializerNamespaces names = new XmlSerializerNamespaces();
                    names.Add(string.Empty, "http://s3.amazonaws.com/doc/2006-03-01/");

                    XmlSerializer cs = new XmlSerializer(typeof(BucketNotification));
                    cs.Serialize(writer, this, names);

                    ms.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    using (StreamReader sr = new StreamReader(ms))
                    {
                        var xml = sr.ReadToEnd();
                        return xml;
                    }
                }
            }
        }
    }
}
