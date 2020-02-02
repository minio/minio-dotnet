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
    public sealed class JSONType
    {
        // Constants for JSONTypes.
        public static readonly JSONType Document = new JSONType("DOCUMENT");
        public static readonly JSONType Lines = new JSONType("LINES");
        [XmlText]
        public string Type;
        public JSONType()
        {
        }
        
        public JSONType(string value)
        {
            this.Type = value;
        }
    }
}
