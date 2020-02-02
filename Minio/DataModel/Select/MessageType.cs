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
    public sealed class MessageType
    {
        // Constants for JSONTypes.
        public static readonly MessageType Error = new MessageType("error");
        public static readonly MessageType Event = new MessageType("event");
        [XmlText]
        public String Type;
        
        public MessageType()
        {
        }
        
        public MessageType(string value)
        {
            this.Type = value;
        }
    }
}
