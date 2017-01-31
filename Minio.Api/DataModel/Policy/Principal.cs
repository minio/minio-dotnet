using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json;
using Minio.DataModel.Policy;

namespace Minio.DataModel
{
    [DataContract]
    internal class Principal
    {
        [JsonProperty("AWS")]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]

        internal IList<string> awsList { get; set; }
        [JsonProperty("CanonicalUser")]
        internal IList<string> canonicalUser { get; set; }
        public Principal()
        {

        }
        public Principal(string aws=null)
        {
            this.awsList = new List<string>();
            if (aws != null)
            {
                this.awsList.Add(aws);
            }
        }
        public void CanonicalUser(string val)
        {
            this.canonicalUser = new List<string>();
            if (val != null)
            {
                this.canonicalUser.Add(val);
            }
        }
        public IList<string> aws()
        {
            return this.awsList;
        }
    }
}
