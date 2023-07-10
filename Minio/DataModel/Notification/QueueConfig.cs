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

namespace Minio.DataModel.Notification;

/// <summary>
///     QueueConfig carries one single queue notification configuration
/// </summary>
[Serializable]
public class QueueConfig : NotificationConfiguration
{
    public QueueConfig()
    {
    }

    public QueueConfig(string arn) : base(arn)
    {
        Queue = arn;
    }

    public QueueConfig(Arn arn) : base(arn)
    {
        if (arn is null) throw new ArgumentNullException(nameof(arn));

        Queue = arn.ToString();
    }

    public string Queue { get; set; }

    // Implement equality for this object
    public override bool Equals(object obj)
    {
        var other = (QueueConfig)obj;
        // If parameter is null return false.
        if (other is null) return false;
        return other.Queue.Equals(Queue, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Queue);
    }
}
