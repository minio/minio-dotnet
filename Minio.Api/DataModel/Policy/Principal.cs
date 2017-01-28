using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json;

namespace Minio.DataModel
{
    [DataContract]
    internal class Principal
    {
        [JsonProperty("AWS")]
        private ISet<string> awsSet;
        [JsonProperty("CanonicalUser")]
        private ISet<string> canonicalUser;
        public Principal()
        {

        }
        public Principal(string aws=null)
        {
            this.awsSet = new HashSet<string>();
            if (aws != null)
            {
                this.awsSet.Add(aws);
            }
        }
        public ISet<string> aws()
        {
            return this.awsSet;
        }
    }
}
