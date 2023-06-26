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

using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using Minio.DataModel.Encryption;

namespace Minio.DataModel.Select;

[Serializable]
[XmlRoot(ElementName = "SelectObjectContentRequest")]
public class SelectObjectOptions
{
    [XmlIgnore] public IServerSideEncryption SSE { get; set; }

    public string Expression { get; set; }

    [XmlElement("ExpressionType")] public QueryExpressionType ExpressionType { get; set; }

    public SelectObjectInputSerialization InputSerialization { get; set; }
    public SelectObjectOutputSerialization OutputSerialization { get; set; }
    public RequestProgress RequestProgress { get; set; }

    public string MarshalXML()
    {
        XmlWriter xw = null;

        var str = string.Empty;

        try
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true
            };
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            using var sw = new StringWriter(CultureInfo.InvariantCulture);

            var xs = new XmlSerializer(typeof(SelectObjectOptions));
            using (xw = XmlWriter.Create(sw, settings))
            {
                xs.Serialize(xw, this, ns);
                xw.Flush();

                str = sw.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            xw?.Close();
        }

        return str;
    }
}