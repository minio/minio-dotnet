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
    [XmlRoot(ElementName = "ReplicationConfiguration")]
    public class ReplicationConfiguration
    {
        [XmlElement("Role")]
        public string Role { get; set; }
        [XmlElement("Rule")]
        public List<ReplicationRule> Rules { get; set; }

        public ReplicationConfiguration() 
        {               
        }

        public ReplicationConfiguration(string role, List<ReplicationRule> rules) 
        {
            if (string.IsNullOrEmpty(role) || string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentNullException(nameof(this.Role) + " member cannot be empty.");
            }
            if (rules == null || rules.Count == 0)
            {
                throw new ArgumentNullException(nameof(this.Rules) + " member cannot be an empty list.");
            }
            if (rules.Count >= 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(this.Rules) + " Count of rules cannot exceed maximum limit of 1000.");
            }
            this.Role = role;
            this.Rules = new List<ReplicationRule>(rules);
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

                xs = new XmlSerializer(typeof(ReplicationConfiguration), "");
                xw = XmlWriter.Create(sw, settings);
                xs.Serialize(xw, this, ns);
                xw.Flush();

                str = sw.ToString();
                // We'll need to remove the namespace attribute inserted in the serialize configuration
                const RegexOptions regexOptions =
                            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;
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