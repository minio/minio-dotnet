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
using System.Xml;
using System.Xml.Serialization;

/*
 * Object representation of request XML used in these calls - PutBucketLifecycleConfiguration, GetBucketLifecycleConfiguration.
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutBucketLifecycleConfiguration.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketLifecycleConfiguration.html
 *
 */

namespace Minio.DataModel.ILM
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
                str = utils.RemoveNamespaceInXML(sw.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // throw ex;
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