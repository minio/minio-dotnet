﻿/*
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

    public class ResourceJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var retVal = new object();
            if (reader.TokenType == JsonToken.StartObject)
            {
                var instance = (Resources) serializer.Deserialize(reader, typeof(Resources));
                retVal = instance;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                var instance = new Resources {reader.Value.ToString()};
                retVal = instance;
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                // retVal = serializer.Deserialize(reader, objectType);
                var array = JArray.Load(reader);
                var rs = array.ToObject<ISet<string>>();
                var instance = new Resources();
                foreach (var el in rs)
                {
                    instance.Add(el);
                }
                retVal = instance;
            }
            return retVal;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null)
            {
                serializer.Serialize(writer, value);
            }
        }
    }
}