﻿/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2015 Minio, Inc.
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Minio.DataModel.Policy;
namespace Minio.DataModel.Policy
{
    class PrincipalJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object retVal = new Object();
           
            if (reader.TokenType == JsonToken.StartObject)
            {
                Principal instance = (Principal)serializer.Deserialize(reader, typeof(Principal));
                retVal =  instance ;

            }
            else if (reader.TokenType == JsonToken.String)
            {
                if (reader.Value.Equals("*"))
                {
                    Principal instance = new Principal("AWS");
                    instance.awsList.Add(reader.Value.ToString());
                    instance.CanonicalUser(reader.Value.ToString());
                    retVal = instance;
                }
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                retVal = serializer.Deserialize(reader, objectType);
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
