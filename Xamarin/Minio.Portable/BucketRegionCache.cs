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

namespace Minio
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Helper;
    using RestSharp.Portable;

    // A singleton bucket/region cache map.
    internal sealed class BucketRegionCache
    {
        private static readonly Lazy<BucketRegionCache> Lazy =
            new Lazy<BucketRegionCache>(() => new BucketRegionCache());

        private readonly ConcurrentDictionary<string, string> regionMap;

        private BucketRegionCache()
        {
            this.regionMap = new ConcurrentDictionary<string, string>();
        }

        public static BucketRegionCache Instance => Lazy.Value;

        /**
         * Returns AWS region for given bucket name.
        */
        public string Region(string bucketName)
        {
            string value;
            this.regionMap.TryGetValue(bucketName, out value);
            return value ?? "us-east-1";
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
        public bool Exists(string bucketName)
        {
            string value;
            this.regionMap.TryGetValue(bucketName, out value);
            return value != null;
        }

        /// <summary>
        ///     Updates Region cache for given bucket.
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="bucketName"></param>
        internal async Task<string> Update(AbstractMinioClient client, string bucketName)
        {
			string region = string.Empty;

            if (bucketName != null && S3Utils.IsAmazonEndPoint(client.BaseUrl) && client.AccessKey != null
                && client.SecretKey != null && !Instance.Exists(bucketName))
            {
                string location = null;
                var path = Utils.UrlEncode(bucketName) + "?location";
                // Initialize client
                var requestUrl = RequestUtil.MakeTargetUrl(client.BaseUrl, client.Secure);
                client.SetTargetUrl(requestUrl);

                var request = new RestRequest(path, Method.GET);

                var response = await client.ExecuteTaskAsync(client.NoErrorHandlers, request);

                if (HttpStatusCode.OK.Equals(response.StatusCode))
                {
                    var root = XDocument.Parse(response.Content);
                    location = root.Root?.Value;
                }
                if (string.IsNullOrEmpty(location))
                {
                    region = "us-east-1";
                }
                else
                {
                    // eu-west-1 can be sometimes 'EU'.
                    region = "EU".Equals(location) ? "eu-west-1" : location;
                }

                // Add the new location.
                Instance.Add(bucketName, region);
            }
            return region;
        }
    }
}