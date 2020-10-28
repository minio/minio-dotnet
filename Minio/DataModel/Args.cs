/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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

using System.Collections.Generic;
using RestSharp;
using System.Linq;

namespace Minio
{
    public abstract class Args
    {
        internal Dictionary<string, string> ExtraHeaders { get; set; }
        internal Dictionary<string, string> ExtraQueryParams { get; set; }

        // RequestMethod will be the HTTP Method for request variable which is of type RestRequest.
        // Will be one of the type - HEAD, GET, PUT, DELETE. etc.
        internal Method RequestMethod { get; set; }

        public static Dictionary<string, string> CloneDictionary(Dictionary<string, string> h)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> entry in h)
            {
                ret.Add(entry.Key, entry.Value);
            }
            return ret;
        }
        public Args()
        {
        }
        public Args WithExtraHeaders(Dictionary<string, string> h)
        {
            this.ExtraHeaders = this.ExtraHeaders ?? new Dictionary<string, string>();
            if (h != null)
            {
                this.ExtraHeaders = this.ExtraHeaders.Concat(h).GroupBy(ele => ele.Key).ToDictionary(ele => ele.Key, ele => ele.First().Value);
            }
            return this;
        }
        public Args WithExtraQueryParams(Dictionary<string, string> h)
        {
            this.ExtraHeaders = this.ExtraHeaders.Concat(h).GroupBy(ele => ele.Key).ToDictionary(ele => ele.Key, ele => ele.First().Value);
            return this;
        }

        public virtual RestRequest BuildRequest(RestRequest request)
        {
            return request;
        }
    }
}