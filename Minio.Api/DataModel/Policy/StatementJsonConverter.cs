using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Minio.DataModel.Policy
{
    class StatementJsonConverter : JsonConverter
    {
        
        public override bool CanConvert(Type objectType)
        {
            return typeof(Statement).IsAssignableFrom(objectType);
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
        }
    }
}
