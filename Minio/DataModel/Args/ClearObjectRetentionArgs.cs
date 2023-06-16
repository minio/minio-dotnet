/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
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

using Minio.Helper;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Minio.DataModel.Args;

public class ClearObjectRetentionArgs : ObjectVersionArgs<ClearObjectRetentionArgs>
{
    public ClearObjectRetentionArgs()
    {
        RequestMethod = HttpMethod.Put;
    }

    public static string EmptyRetentionConfigXML()
    {
        using var sw = new StringWriter(CultureInfo.InvariantCulture);
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true
        };
        using var xw = XmlWriter.Create(sw, settings);
        xw.WriteStartElement("Retention");
        xw.WriteString("");
        xw.WriteFullEndElement();
        xw.Flush();
        return sw.ToString();
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)
    {
        requestMessageBuilder.AddQueryParameter("retention", "");
        if (!string.IsNullOrEmpty(VersionId)) requestMessageBuilder.AddQueryParameter("versionId", VersionId);
        // Required for Clear Object Retention.
        requestMessageBuilder.AddOrUpdateHeaderParameter("x-amz-bypass-governance-retention", "true");
        var body = EmptyRetentionConfigXML();
        requestMessageBuilder.AddXmlBody(body);
        requestMessageBuilder.AddOrUpdateHeaderParameter("Content-Md5",
            Utils.GetMD5SumStr(Encoding.UTF8.GetBytes(body)));
        return requestMessageBuilder;
    }
}
