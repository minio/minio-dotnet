using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace testpolicy.MyPolicy
{
    internal class BucketPolicy
    {
        [JsonIgnore]
        private string bucketName { get; set; }
        public string Version { get; set; }
        public List<Statement> Statement { get; set; }

        /**
         * Reads JSON from given {@link Reader} and returns new {@link BucketPolicy} of given bucket name.
         */
        public static BucketPolicy parseJson(MemoryStream reader, String bucketName)
        {
            string toparse = new StreamReader(reader).ReadToEnd();
            JObject jsonData = JObject.Parse(toparse);


            Console.Out.WriteLine(toparse);
            BucketPolicy bucketPolicy = JsonConvert.DeserializeObject<BucketPolicy>(toparse);
            bucketPolicy.bucketName = bucketName;

            return bucketPolicy;
        }
        public bool ShouldSerializePrincipal()
        {
            return false;
        }
    }
}
