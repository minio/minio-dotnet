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
///     S3Key - child of Filter, a tag in the notification xml which carries suffix/prefix
///     filters and allows filtering event notifications based on S3 Object key's name
/// </summary>
[Serializable]
public class S3Key
{
    private List<FilterRule> filterRules;

    [XmlElement("FilterRule")]
    public List<FilterRule> FilterRules
    {
        get
        {
            filterRules ??= new List<FilterRule>();

            return filterRules;
        }
        set => filterRules = value;
    }

    internal bool IsFilterRulesSet()
    {
        return filterRules?.Count > 0;
    }

    public bool ShouldSerializeFilterRules()
    {
        return filterRules.Count > 0;
    }
}