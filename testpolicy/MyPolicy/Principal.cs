using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace testpolicy.MyPolicy
{
    class Principal
    {
        [JsonProperty("AWS")]
        public ISet<string> awsSet { get; set; }

        [JsonProperty("CanonicalUser")]
        private ISet<string> canonicalUser;
        public Principal()
        {

        }
        public Principal(string aws = null)
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
