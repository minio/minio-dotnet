/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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

using System.Xml.Serialization;

namespace Minio.DataModel
{
    /// <summary>
    /// EventType is a S3 notification event associated to the bucket notification configuration
    /// </summary>
    public sealed class EventType
    {
        [XmlText]
        public string value;
        private EventType()
        {
            this.value = null;
        }

        public EventType(string value)
        {
            this.value = value;
        }

        // Valid Event types as described in:
        // http://docs.aws.amazon.com/AmazonS3/latest/dev/NotificationHowTo.html#notification-how-to-event-types-and-destinations

        public static readonly EventType ObjectCreatedAll = new EventType("s3:ObjectCreated:*");
        public static readonly EventType ObjectCreatedPut = new EventType("s3:ObjectCreated:Put");
        public static readonly EventType ObjectCreatedPost = new EventType("s3:ObjectCreated:Post");
        public static readonly EventType ObjectCreatedCopy = new EventType("s3:ObjectCreated:Copy");
        public static readonly EventType ObjectCreatedCompleteMultipartUpload = new EventType("s3:ObjectCreated:CompleteMultipartUpload");
        public static readonly EventType ObjectAccessedGet = new EventType("s3:ObjectAccessed:Get");
        public static readonly EventType ObjectAccessedHead = new EventType("s3:ObjectAccessed:Head");
        public static readonly EventType ObjectAccessedAll = new EventType("s3:ObjectAccessed:*");
        public static readonly EventType ObjectRemovedAll = new EventType("s3:ObjectRemoved:*");
        public static readonly EventType ObjectRemovedDelete = new EventType("s3:ObjectRemoved:Delete");
        public static readonly EventType ObjectRemovedDeleteMarkerCreated = new EventType("s3:ObjectRemoved:DeleteMarkerCreated");
        public static readonly EventType ReducedRedundancyLostObject = new EventType("s3:ReducedRedundancyLostObject");
    }
}
