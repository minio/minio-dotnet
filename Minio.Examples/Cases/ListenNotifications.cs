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

using Minio.DataModel.Args;
using Minio.DataModel.Notification;

namespace Minio.Examples.Cases;

internal static class ListenNotifications
{
    // Listen for gloabal notifications (a Minio-only extension)
    public static void Run(IMinioClient minio,
        List<EventType> events = null)
    {
        try
        {
            Console.WriteLine("Running example for API: ListenNotifications");
            Console.WriteLine();
            events ??= new List<EventType> { EventType.BucketCreatedAll };
            var args = new ListenBucketNotificationsArgs().WithEvents(events);
            var observable = minio.ListenNotificationsAsync(events);

            var subscription = observable.Subscribe(
                notification => Console.WriteLine($"Notification: {notification.Json}"),
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
