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
<<<<<<< HEAD
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Minio.DataModel.Policy
{
    public class StatementJsonConverter : JsonConverter
=======

namespace MinioCore2.DataModel.Policy
{
    class StatementJsonConverter : JsonConverter
>>>>>>> netcore
    {
        
        public override bool CanConvert(Type objectType)
        {
            return typeof(Statement).GetTypeInfo().IsInstanceOfType(objectType);
<<<<<<< HEAD
         
        }
        public override bool CanRead { get { return false; } }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite {  get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
           // throw new  NotImplementedException();
            JArray array = new JArray();
            IList<Statement> list = (IList<Statement>)value;
            //var list = ilist.SelectMany(i => i);
            /*
            var grouped = list.GroupBy(x => new { x.resources, x.effect, x.principal, x.sid, x.conditions})
                             .Select(grp => grp.ToList())
                             .ToList();
            StringBuilder stmtstring = new StringBuilder();
            string json = JsonConvert.SerializeObject(grouped);
            writer.WriteValue(json);
            */
            var grouped1 = list.GroupBy(x => new { x.resources })
                 .Select(grp => grp.ToList())
                 .ToList();
            StringBuilder stmtstring = new StringBuilder();
            string json = JsonConvert.SerializeObject(grouped1);
            writer.WriteValue(json);

            var grouped2 = list.GroupBy(x => new { x.resources, x.effect })
                 .Select(grp => grp.ToList())
                 .ToList();
            stmtstring = new StringBuilder();
            json = JsonConvert.SerializeObject(grouped2);
            writer.WriteValue(json);

            var grouped3 = list.GroupBy(x => new { x.resources, x.effect, x.principal })
                 .Select(grp => grp.ToList())
                 .ToList();
            stmtstring = new StringBuilder();
            json = JsonConvert.SerializeObject(grouped3);
            writer.WriteValue(json);

            var grouped4 = list.GroupBy(x => new { x.resources, x.effect, x.principal, x.sid })
                 .Select(grp => grp.ToList())
                 .ToList();
            stmtstring = new StringBuilder();
            json = JsonConvert.SerializeObject(grouped4);

            var grouped5 = list.GroupBy(x => new { x.resources, x.effect, x.principal, x.sid, x.conditions })
                .Select(grp => grp.ToList())
                .ToList();
            stmtstring = new StringBuilder();
            json = JsonConvert.SerializeObject(grouped5);
            writer.WriteValue(json);
            /*
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
            */
            array.WriteTo(writer);
=======
            
           //KP return typeof(Statement).IsAssignableFrom(objectType);
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            var properties = jsonObject.Properties().ToList();
            return new StatementJsonConverter
            {
                //Name = properties[0].Name.Replace("$", ""),
                //Value = (string)properties[0].Value
            };

        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
>>>>>>> netcore
        }
    }
}
