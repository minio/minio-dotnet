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

namespace Minio.DataModel;

/// <summary>
///     NotificationConfig - represents one single notification configuration
///     such as topic, queue or lambda configuration
/// </summary>
public class NotificationConfiguration
{
    [XmlElement] public string Id { get; set; }
    [XmlElement("Event")] public List<EventType> Events { get; set; }
    [XmlElement("Filter")] public Filter Filter;

    public NotificationConfiguration()
    {
        Arn = null;
        Events = new List<EventType>();
    }

    public NotificationConfiguration(string arn)
    {
        Arn = new Arn(arn);
    }

    public NotificationConfiguration(Arn arn)
    {
        Arn = arn;
    }


    private Arn Arn { get; }


    public void AddEvents(List<EventType> evnt)
    {
        if (Events == null) Events = new List<EventType>();

        Events.AddRange(evnt);
    }

    /// <summary>
    ///     AddFilterSuffix sets the suffix configuration to the current notification config
    /// </summary>
    /// <param name="suffix"></param>
    public void AddFilterSuffix(string suffix)
    {
        if (Filter == null) Filter = new Filter();

        var newFilterRule = new FilterRule("suffix", suffix);
        // Replace any suffix rule if existing and add to the list otherwise
        for (var i = 0; i < Filter.S3Key.FilterRules.Count; i++)
            if (Filter.S3Key.FilterRules[i].Equals("suffix"))
            {
                Filter.S3Key.FilterRules[i] = newFilterRule;
                return;
            }

        Filter.S3Key.FilterRules.Add(newFilterRule);
    }

    /// <summary>
    ///     AddFilterPrefix sets the prefix configuration to the current notification config
    /// </summary>
    /// <param name="prefix"></param>
    public void AddFilterPrefix(string prefix)
    {
        if (Filter == null) Filter = new Filter();

        var newFilterRule = new FilterRule("prefix", prefix);
        // Replace any prefix rule if existing and add to the list otherwise
        for (var i = 0; i < Filter.S3Key.FilterRules.Count; i++)
            if (Filter.S3Key.FilterRules[i].Equals("prefix"))
            {
                Filter.S3Key.FilterRules[i] = newFilterRule;
                return;
            }

        Filter.S3Key.FilterRules.Add(newFilterRule);
    }

    public bool ShouldSerializeFilter()
    {
        return Filter != null;
    }

    public bool ShouldSerializeId()
    {
        return Id != null;
    }

    public bool ShouldSerializeEvents()
    {
        return Events != null && Events.Count > 0;
    }

    internal bool IsIdSet()
    {
        return Id != null;
    }
}