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
using System.Xml.Serialization;

namespace Minio.DataModel
{
    public sealed class CSVQuoteFields
    {
        // Constants for csv quote styles.
        public static readonly CSVQuoteFields Always = new CSVQuoteFields("Always");
        public static readonly CSVQuoteFields AsNeeded = new CSVQuoteFields("AsNeeded");

        [XmlText]
        public string QuoteFields;

        public CSVQuoteFields(string value)
        {
            this.QuoteFields = value;
        }
        public CSVQuoteFields()
        {

        }
    }
}
