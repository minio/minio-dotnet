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

using System.Globalization;
using System.Xml.Serialization;

namespace Minio.DataModel.Notification;

/// <summary>
///     EventType is a S3 notification event associated to the bucket notification configuration
/// </summary>
public sealed class EventType
{
    // Valid Event types as described in:
    // http://docs.aws.amazon.com/AmazonS3/latest/dev/NotificationHowTo.html#notification-how-to-event-types-and-destinations

    public static readonly EventType ObjectCreatedAll = new("s3:ObjectCreated:*");
    public static readonly EventType ObjectCreatedPut = new("s3:ObjectCreated:Put");
    public static readonly EventType ObjectCreatedPost = new("s3:ObjectCreated:Post");
    public static readonly EventType ObjectCreatedCopy = new("s3:ObjectCreated:Copy");

    public static readonly EventType ObjectCreatedCompleteMultipartUpload =
        new("s3:ObjectCreated:CompleteMultipartUpload");

    public static readonly EventType ObjectAccessedGet = new("s3:ObjectAccessed:Get");
    public static readonly EventType ObjectAccessedHead = new("s3:ObjectAccessed:Head");
    public static readonly EventType ObjectAccessedAll = new("s3:ObjectAccessed:*");
    public static readonly EventType ObjectRemovedAll = new("s3:ObjectRemoved:*");
    public static readonly EventType ObjectRemovedDelete = new("s3:ObjectRemoved:Delete");
    public static readonly EventType ObjectRemovedDeleteMarkerCreated = new("s3:ObjectRemoved:DeleteMarkerCreated");
    public static readonly EventType ReducedRedundancyLostObject = new("s3:ReducedRedundancyLostObject");

    private EventType()
    {
        Value = null;
    }

    public EventType(string value)
    {
        Value = value;
    }

    [XmlText] public string Value { get; set; }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "EventType= {0}", Value);
    }
}
