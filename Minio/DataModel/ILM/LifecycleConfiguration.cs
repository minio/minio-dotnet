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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Minio.DataModel
{
    [Serializable]
    [XmlRoot(ElementName = "LifecycleConfiguration")]
    public class LifecycleConfiguration
    {
        [XmlElement("Rule")]
        public List<LifecycleRule> Rules { get; set; }

        public LifecycleConfiguration()
        {
        }

        public LifecycleConfiguration(List<LifecycleRule> rules)
        {
            if (rules == null || rules.Count <= 0)
            {
                throw new ArgumentNullException(nameof(Rules), "Rules object cannot be empty. A finite set of Lifecycle Rules are needed for LifecycleConfiguration.");
            }
            this.Rules = new List<LifecycleRule>(rules);
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
                ns.Add(string.Empty, string.Empty);

                StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);

                xs = new XmlSerializer(typeof(LifecycleConfiguration), "");
                xw = XmlWriter.Create(sw, settings);
                xs.Serialize(xw, this, ns);
                xw.Flush();

                // We'll need to remove the namespace attribute inserted in the serialize configuration
                const RegexOptions regexOptions =
                            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;
                str = sw.ToString();
                str = Regex.Replace(
                    str,
                    @"<\w+\s+\w+:nil=""true""(\s+xmlns:\w+=""http://www.w3.org/2001/XMLSchema-instance"")?\s*/>",
                    string.Empty,
                    regexOptions
                );
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
            return str;
        }
    }
}