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

namespace Minio.DataModel.Policy
{
    using System.Collections.Generic;

    public class ConditionMap : Dictionary<string, ConditionKeyMap>
    {
        public ConditionMap() : base() { }
        public ConditionMap(ConditionMap map = null) : base(map) { }
        public ConditionMap(string key = null, ConditionKeyMap value = null) : base()
        {
            if (key != null && value != null)
            {
                this.Add(key, value);
            }
        }
        // Merge Condition Key map values.
        public ConditionKeyMap Put(string key, ConditionKeyMap value)
        {
            ConditionKeyMap existingValue;
            base.TryGetValue(key, out existingValue);
            if (existingValue == null)
            {
                existingValue = new ConditionKeyMap(value);
            }
            else
            {
                foreach (var item in value)
                {
                    existingValue.Put(item.Key, item.Value);
                }
            }
            this[key] = existingValue;
            return existingValue;
        }
        public void PutAll(ConditionMap cmap)
        {
            foreach (var item in cmap)
            {
                this.Put(item.Key, item.Value);

            }
        }

    }
}
