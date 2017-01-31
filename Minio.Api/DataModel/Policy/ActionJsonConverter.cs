using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Minio.DataModel.Policy;
namespace Minio.DataModel.Policy
{
    class ActionJsonConverter : JsonConverter
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
            throw new NotImplementedException();
        }
    }
}
