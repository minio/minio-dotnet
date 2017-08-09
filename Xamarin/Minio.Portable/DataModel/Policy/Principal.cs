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

namespace Minio.DataModel.Policy
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class Principal
    {
        public Principal()
        {
        }

        public Principal(string aws = null)
        {
            this.AwsList = new List<string>();
            if (aws != null)
            {
                this.AwsList.Add(aws);
            }
        }

        [JsonProperty("AWS")]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]

        internal IList<string> AwsList { get; set; }

        [JsonProperty("CanonicalUser")]
        internal IList<string> CanonicalUser { get; set; }

        public void SetCanonicalUser(string val)
        {
            this.CanonicalUser = new List<string>();
            if (val != null)
            {
                this.CanonicalUser.Add(val);
            }
        }

        public IList<string> Aws()
        {
            return this.AwsList;
        }
    }
}