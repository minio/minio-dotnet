using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Minio.DataModel.Policy;
namespace Minio.DataModel.Policy
{
    class ResourceJsonConverter : JsonConverter
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
                Resources instance = (Resources)serializer.Deserialize(reader, typeof(Resources));
                retVal =  instance ;

            }
            else if (reader.TokenType == JsonToken.String)
            {
                Resources instance = new Resources();
                instance.Add(reader.Value.ToString());
                retVal = instance;
                
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                // retVal = serializer.Deserialize(reader, objectType);
                JArray array = JArray.Load(reader);
                var rs = array.ToObject<ISet<string>>();
                Resources instance = new Resources();
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
