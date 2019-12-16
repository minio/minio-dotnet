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
    public sealed class SelectEventType
    {
        // Constants for EventType.
        public static readonly SelectEventType SelectEndEvent = new SelectEventType("End");
        public static readonly SelectEventType SelectRecordsEvent = new SelectEventType("Records");
        public static readonly SelectEventType SelectProgressEvent = new SelectEventType("Progress");
        public static readonly SelectEventType SelectStatsEvent = new SelectEventType("Stats");

        private String Type;
        public SelectEventType()
        {
        }
        
        public SelectEventType(string value)
        {
            this.Type = value;
        }
    }
}
