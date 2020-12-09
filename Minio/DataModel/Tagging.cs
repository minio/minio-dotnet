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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Minio.DataModel
{
    [Serializable]
    [XmlRoot(ElementName = "Tagging")]

    /*
    * References for Tagging.
    * https://docs.aws.amazon.com/AmazonS3/latest/dev/object-tagging.html
    * https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/Using_Tags.html#tag-restrictions
    */
    public class Tagging
    {
        internal const uint MAX_TAG_COUNT_PER_RESOURCE = 50;
        internal const uint MAX_TAG_COUNT_PER_OBJECT = 10;
        internal const uint MAX_TAG_KEY_LENGTH = 128;
        internal const uint MAX_TAG_VALUE_LENGTH = 256;

        [XmlElement("TagSet")]
        public TagSet TaggingSet { get; set; }

        public Tagging()
        {
            this.TaggingSet = null;
        }

        public Tagging(Dictionary<String, String> tags, bool isObjects)
        {
            if (tags == null)
            {
                this.TaggingSet = null;
                return;
            }
            uint tagging_upper_limit = isObjects ? MAX_TAG_COUNT_PER_OBJECT : MAX_TAG_COUNT_PER_RESOURCE;
            if (tags.Count > tagging_upper_limit)
            {
                throw new ArgumentOutOfRangeException(nameof(tags) + ". Count of tags exceeds maximum limit allowed for " + ((isObjects)?"objects.":"buckets."));
            }
            foreach (var tag in tags)
            {
                if (!validateTagKey(tag.Key))
                {
                    throw new ArgumentException("Invalid Tagging key " + tag.Key);
                }
                if (!validateTagValue(tag.Value))
                {
                    throw new ArgumentException("Invalid Tagging value " + tag.Value);
                }
            }
            this.TaggingSet = new TagSet(tags);
        }

        internal bool validateTagKey(string key)
        {
            if (string.IsNullOrEmpty(key) ||
                string.IsNullOrWhiteSpace(key) ||
                key.Length > MAX_TAG_KEY_LENGTH ||
                key.Contains("&"))
            {
                return false;
            }
            return true;
        }

        internal bool validateTagValue(string value)
        {
            if (value == null || // Empty or whitespace is allowed
                value.Length > MAX_TAG_VALUE_LENGTH ||
                value.Contains("&"))
            {
                return false;
            }
            return true;
        }

        public Dictionary<string, string> GetTags()
        {
            if (this.TaggingSet == null || this.TaggingSet.Tag.Count == 0)
            {
                return null;
            }
            Dictionary<string, string> tagMap = new Dictionary<string, string>();
            foreach (var tag in this.TaggingSet.Tag)
            {
                tagMap[tag.Key] = tag.Value;
            }
            return tagMap;
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
                // ns.Add("", "http://s3.amazonaws.com/doc/2006-03-01/");
                ns.Add(string.Empty, string.Empty);

                StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);

                xs = new XmlSerializer(typeof(Tagging), "");
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
            return str;
        }
        public static Tagging GetBucketTags(Dictionary<string, string> tags)
        {
            return new Tagging(tags, false);
        }

        public static Tagging GetObjectTags(Dictionary<string, string> tags)
        {
            return new Tagging(tags, true);
        }
    }
}