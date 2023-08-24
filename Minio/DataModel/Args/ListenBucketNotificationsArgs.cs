/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
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
using Minio.DataModel.Notification;
using Minio.Handlers;

namespace Minio.DataModel.Args;

public class ListenBucketNotificationsArgs : BucketArgs<ListenBucketNotificationsArgs>
{
    internal readonly IEnumerable<IApiResponseErrorHandler> NoErrorHandlers =
        Enumerable.Empty<IApiResponseErrorHandler>();

    public ListenBucketNotificationsArgs()
    {
        RequestMethod = HttpMethod.Get;
        EnableTrace = false;
        Events = new List<EventType>();
        Prefix = "";
        Suffix = "";
    }

    internal string Prefix { get; private set; }
    internal string Suffix { get; private set; }
    internal List<EventType> Events { get; }
    internal IObserver<MinioNotificationRaw> NotificationObserver { get; private set; }
    public bool EnableTrace { get; private set; }

    public override string ToString()
    {
        var str = string.Join("\n", string.Format(CultureInfo.InvariantCulture, "\nRequestMethod= {0}", RequestMethod),
            string.Format(CultureInfo.InvariantCulture, "EnableTrace= {0}", EnableTrace));

        var eventsAsStr = "";
        foreach (var eventType in Events)
        {
            if (!string.IsNullOrEmpty(eventsAsStr))
                eventsAsStr += ", ";
            eventsAsStr += eventType.Value;
        }

        return string.Join("\n", str, string.Format(CultureInfo.InvariantCulture, "Events= [{0}]", eventsAsStr),
            string.Format(CultureInfo.InvariantCulture, "Prefix= {0}", Prefix),
            string.Format(CultureInfo.InvariantCulture, "Suffix= {0}\n", Suffix));
    }

    public ListenBucketNotificationsArgs WithNotificationObserver(IObserver<MinioNotificationRaw> obs)
    {
        NotificationObserver = obs;
        return this;
    }

    internal override HttpRequestMessageBuilder BuildRequest(HttpRequestMessageBuilder requestMessageBuilder)

    {
        foreach (var eventType in Events) requestMessageBuilder.AddQueryParameter("events", eventType.Value);
        requestMessageBuilder.AddQueryParameter("prefix", Prefix);
        requestMessageBuilder.AddQueryParameter("suffix", Suffix);

        requestMessageBuilder.FunctionResponseWriter = async (responseStream, cancellationToken) =>
        {
            using var sr = new StreamReader(responseStream);
            while (!sr.EndOfStream)
                try
                {
                    var line = await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(line))
                        break;

                    if (EnableTrace)
                    {
                        Console.WriteLine("== ListenBucketNotificationsAsync read line ==");
                        Console.WriteLine(line);
                        Console.WriteLine("==============================================");
                    }

                    var trimmed = line.Trim();
                    if (trimmed.Length > 2) NotificationObserver.OnNext(new MinioNotificationRaw(trimmed));
                }
                catch
                {
                    break;
                }
        };
        return requestMessageBuilder;
    }

    internal ListenBucketNotificationsArgs WithEnableTrace(bool trace)
    {
        EnableTrace = trace;
        return this;
    }

    public ListenBucketNotificationsArgs WithPrefix(string prefix)
    {
        Prefix = prefix;
        return this;
    }

    public ListenBucketNotificationsArgs WithSuffix(string suffix)
    {
        Suffix = suffix;
        return this;
    }

    public ListenBucketNotificationsArgs WithEvents(IList<EventType> events)
    {
        Events.AddRange(events);
        return this;
    }
}
