/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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
using Minio.DataModel.Policy;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Minio.DataModel
{
    [JsonConverter(typeof(ConditionKeyMapConverter))]
    public class ConditionKeyMap : Dictionary<string, ISet<string>>
    {
        public ConditionKeyMap() : base() { }
        public ConditionKeyMap(ConditionKeyMap map = null) : base(map) { }

        public ConditionKeyMap(string key, string value)
        {
            ISet<string> values = new HashSet<string>();
            values.Add(value);
            this.Add(key, values);
        }

        public ConditionKeyMap(string key, ISet<string> value)
        {
            this.Add(key, value);
        }

        public ISet<string> Put(string key, string value)
        {
            ISet<string> existingValue;
            this.TryGetValue(key, out existingValue);
            if (existingValue == null)
            {
                existingValue = new HashSet<string>();
            }
            existingValue.Add(value);
            this.Add(key, existingValue);
            return existingValue;
        }
        public ISet<string> Put(string key, ISet<string> value)
        {
            ISet<string> existingValue;
            this.TryGetValue(key, out existingValue);
            if (existingValue == null)
            {
                existingValue = new HashSet<string>();
            }
            existingValue.UnionWith(value);
            this[key] = existingValue;
            return existingValue;
        }

        public void remove(string key, ISet<string> value)
        {
            ISet<string> existingValue;
            this.TryGetValue(key, out existingValue);
            if (existingValue == null)
            {
                return;
            }
            existingValue.ExceptWith(value);
            if (existingValue.Count == 0)
            {
                this.Remove(key);
            }
            else
            {
                this[key] = existingValue;
            }
        }

    }
}
