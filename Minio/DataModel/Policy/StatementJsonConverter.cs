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

using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Minio.DataModel.Policy
{
    public class StatementJsonConverter : JsonConverter
    {
        
        public override bool CanConvert(Type objectType)
        {
            return typeof(Statement).GetTypeInfo().IsInstanceOfType(objectType);
         
        }
        public override bool CanRead { get { return false; } }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite {  get { return true; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
           // throw new  NotImplementedException();
            JArray array = new JArray();
            IList<Statement> list = (IList<Statement>)value;
            var grouped = list.GroupBy(x => new { x.resources , x.effect, x.sid, x.principal, x.conditions });
            StringBuilder stmtstring = new StringBuilder();
            string json = JsonConvert.SerializeObject(grouped);
            
            if (list.Count > 0)
            {
                JArray keys = new JArray();

                JObject first = JObject.FromObject(list[0], serializer);
                foreach (JProperty prop in first.Properties())
                {
                    keys.Add(new JValue(prop.Name));
                }
                array.Add(keys);

                foreach (object item in list)
                {
                    JObject obj = JObject.FromObject(item, serializer);
                    JArray itemValues = new JArray();
                    foreach (JProperty prop in obj.Properties())
                    {
                        itemValues.Add(prop.Value);
                    }
                    array.Add(itemValues);
                }
            }
            array.WriteTo(writer);
        }
    }
}
