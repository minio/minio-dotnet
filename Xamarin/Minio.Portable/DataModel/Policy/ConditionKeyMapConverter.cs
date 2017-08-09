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
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ConditionKeyMapConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IDictionary<,>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var isParsed = false;
            ISet<string> parseSet = new HashSet<string>();
            string key = null;
            ConditionKeyMap instance = null;
            if (reader.TokenType == JsonToken.StartObject)
            {
                instance = new ConditionKeyMap();
            }
            do
            {
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if (key == null)
                        {
                            key = reader.Value.ToString();
                        }
                    }
                    else if (reader.TokenType == JsonToken.String)
                    {
                        parseSet.Add(reader.Value.ToString());
                        instance?.Put(key, parseSet);
                        isParsed = true;
                    }
                    else if (reader.TokenType == JsonToken.StartArray)
                    {
                        var array = JArray.Load(reader);
                        var rs = array.ToObject<ISet<string>>();
                        parseSet = new HashSet<string>();
                        foreach (var el in rs)
                        {
                            parseSet.Add(el);
                        }
                        instance?.Put(key, parseSet);
                        isParsed = true;
                    }
                }
            } while (reader.Read() && !isParsed);
            return instance;
        }


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}