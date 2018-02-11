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

using Minio.Helper;
using RestSharp;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Minio
{
    // A singleton bucket/region cache map.
    public sealed class BucketRegionCache
    {
        private static readonly Lazy<BucketRegionCache> lazy =
            new Lazy<BucketRegionCache>(() => new BucketRegionCache());

        private ConcurrentDictionary<string, string> regionMap;

        public static BucketRegionCache Instance
        {
            get { return lazy.Value; }
        }
        private BucketRegionCache()
        {
            regionMap = new ConcurrentDictionary<string, string>();
        }
        /**
         * Returns AWS region for given bucket name.
        */
        public string Region(string bucketName)
        {
            string value = null;
            this.regionMap.TryGetValue(bucketName, out value);
            return value != null ? value : "us-east-1";
        }


        /**
         * Adds bucket name and its region to BucketRegionCache.
         */
        public void Add(string bucketName, string region)
        {
            this.regionMap.TryAdd(bucketName, region);
        }


        /**
         * Removes region cache of the bucket if any.
         */
        public void Remove(string bucketName)
        {
            string value;
            this.regionMap.TryRemove(bucketName, out value);
        }


        /**
         * Returns true if given bucket name is in the map else false.
         */
        public bool Exists(String bucketName)
        {
            string value = null;
            this.regionMap.TryGetValue(bucketName, out value);
            return value != null;

        }

        /// <summary>
        /// Updates Region cache for given bucket.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        internal async Task<string> Update(MinioClient client, string bucketName)
        {
            string region = null;

            if (bucketName != null && client.AccessKey != null
            && client.SecretKey != null && !Instance.Exists(bucketName))
            {
                string location = null;
                var path = utils.UrlEncode(bucketName) + "?location";
                // Initialize client
                Uri requestUrl = RequestUtil.MakeTargetURL(client.BaseUrl, client.Secure);
                client.SetTargetURL(requestUrl);

                var request = new RestRequest(path, Method.GET);

                var response = await client.ExecuteTaskAsync(client.NoErrorHandlers, request);

                if (HttpStatusCode.OK.Equals(response.StatusCode))
                {
                    var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                    var stream = new MemoryStream(contentBytes);
                    XDocument root = XDocument.Parse(response.Content);
                    location = root.Root.Value;

                }
                if (location == null || location == "")
                {
                    region = "us-east-1";
                }
                else
                {
                    // eu-west-1 can be sometimes 'EU'.
                    if ("EU".Equals(location))
                    {
                        region = "eu-west-1";
                    }
                    else
                    {
                        region = location;
                    }
                }

                // Add the new location.
                Instance.Add(bucketName, region);
            }
            return region;

        }

    }
}