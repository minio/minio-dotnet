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
using System.Xml;
using System.Globalization;

using System.Xml.Serialization;
using System.IO;

namespace Minio.DataModel
{
    [Serializable]
    [XmlRoot(ElementName = "SelectObjectContentRequest")]
    public class SelectObjectOptions
    {
        [XmlIgnore]
        public  ServerSideEncryption SSE{ get; set; }
        
        public String Expression { get; set; }
        
        [XmlElement("ExpressionType")]
        public QueryExpressionType ExpressionType { get; set; }
        public SelectObjectInputSerialization InputSerialization { get; set; }
        public SelectObjectOutputSerialization OutputSerialization { get; set; }
        public RequestProgress RequestProgress { get; set; }

        public SelectObjectOptions()
        {
        }
        public string MarshalXML()
        {
            XmlSerializer xs = null;
            XmlWriterSettings settings = null;
            XmlSerializerNamespaces ns = null;

            XmlWriter xw = null;

            String str = String.Empty;

            try
            {
                settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true;

                ns = new XmlSerializerNamespaces();
                ns.Add("", "");

                StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);

                xs = new XmlSerializer(typeof(SelectObjectOptions));
                xw = XmlWriter.Create(sw, settings);
                xs.Serialize(xw, this, ns);
                xw.Flush();

                str = sw.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (xw != null)
                {
                    xw.Close();
                }
            }
            Console.WriteLine(str);
            return str;
        }
    }
}
