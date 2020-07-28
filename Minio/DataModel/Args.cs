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

using System.Collections;
using System.Collections.Generic;
using RestSharp;

namespace Minio
{
    public class Args
    {
        internal Hashtable Headers { get; set; }
        internal Hashtable QueryParams { get; set; }

        public static Hashtable CloneHashTable(Hashtable h)
        {
            Hashtable ret = new Hashtable();
            foreach (KeyValuePair<string, string> entry in h)
            {
                ret.Add(entry.Key.Clone(), entry.Value.Clone());
            }
            return ret;
        }
        public Args()
        {
        }
        public Args WithHeaders(Hashtable h)
        {
            this.Headers = CloneHashTable(h);
            return this;
        }
        public Args WithQueryParams(Hashtable h)
        {
            this.QueryParams = CloneHashTable(h);
            return this;
        }
        public void Validate()
        {
        }

        public RestRequest GetRequest()
        {
            return null;
        }
        public RestRequest GetRequest(string baseUrl, RestSharp.Authenticators.IAuthenticator authenticator)
        {
            return null;
        }
        public void ProcessResponse()
        {
        }
    }
}