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

using Minio.DataModel;

namespace Minio.Examples.Cases;

internal class ListenBucketNotifications
{
    // Listen for notifications from a specified bucket (a Minio-only extension)
    public static void Run(MinioClient minio,
        string bucketName = "my-bucket-name",
        List<EventType> events = null,
        string prefix = "",
        string suffix = "",
        bool recursive = true)
    {
        try
        {
            Console.WriteLine("Running example for API: ListenBucketNotifications");
            Console.WriteLine();
            events = events ?? new List<EventType> { EventType.ObjectCreatedAll };
            var args = new ListenBucketNotificationsArgs()
                .WithBucket(bucketName)
                .WithPrefix(prefix)
                .WithEvents(events)
                .WithSuffix(suffix);
            var observable = minio.ListenBucketNotificationsAsync(bucketName, events, prefix, suffix);

            var subscription = observable.Subscribe(
                notification => Console.WriteLine($"Notification: {notification.json}"),
                ex => Console.WriteLine($"OnError: {ex}"),
                () => Console.WriteLine("Stopped listening for bucket notifications\n"));

            // subscription.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}