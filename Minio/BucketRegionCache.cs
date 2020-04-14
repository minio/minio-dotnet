/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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

using RestSharp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Minio
{
    /// <summary>
    /// A singleton bucket/region cache map.
    /// </summary>
    public sealed class BucketRegionCache
    {
        private static readonly Lazy<BucketRegionCache> lazy =
            new Lazy<BucketRegionCache>(() => new BucketRegionCache());

        private readonly ConcurrentDictionary<string, string> regionMap;

        public static BucketRegionCache Instance => lazy.Value;
        private BucketRegionCache()
        {
            this.regionMap = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Returns AWS region for given bucket name.
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public string Region(string bucketName)
        {
            this.regionMap.TryGetValue(bucketName, out string value);
            return value ?? "us-east-1";
        }

        /// <summary>
        /// Adds bucket name and its region to BucketRegionCache.
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="region"></param>
        public void Add(string bucketName, string region)
        {
            this.regionMap.TryAdd(bucketName, region);
        }

        /// <summary>
        /// Removes region cache of the bucket if any.
        /// </summary>
        /// <param name="bucketName"></param>
        public void Remove(string bucketName)
        {
            this.regionMap.TryRemove(bucketName, out string value);
        }

        /// <summary>
        /// Returns true if given bucket name is in the map else false.
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public bool Exists(string bucketName)
        {
            this.regionMap.TryGetValue(bucketName, out string value);
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
                var path = utils.UrlEncode(bucketName);
                // Initialize client
                Uri requestUrl = RequestUtil.MakeTargetURL(client.BaseUrl, client.Secure);
                client.SetTargetURL(requestUrl);

                var request = new RestRequest(path, Method.GET);
                request.AddQueryParameter("location","");
                var response = await client.ExecuteTaskAsync(client.NoErrorHandlers, request).ConfigureAwait(false);

                if (HttpStatusCode.OK.Equals(response.StatusCode))
                {
                    var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                    var stream = new MemoryStream(contentBytes);
                    XDocument root = XDocument.Parse(response.Content);
                    location = root.Root.Value;
                }
                if (string.IsNullOrEmpty(location))
                {
                    region = "us-east-1";
                }
                else
                {
                    // eu-west-1 can be sometimes 'EU'.
                    if (location == "EU")
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
