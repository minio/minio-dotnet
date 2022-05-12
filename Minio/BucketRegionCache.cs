﻿/*
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

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Minio;

/// <summary>
///     A singleton bucket/region cache map.
/// </summary>
public sealed class BucketRegionCache
{
    private static readonly Lazy<BucketRegionCache> lazy = new(() => new BucketRegionCache());

    private readonly ConcurrentDictionary<string, string> regionMap;

    private BucketRegionCache()
    {
        regionMap = new ConcurrentDictionary<string, string>();
    }

    public static BucketRegionCache Instance => lazy.Value;

    /// <summary>
    ///     Returns AWS region for given bucket name.
    /// </summary>
    /// <param name="bucketName"></param>
    /// <returns></returns>
    public string Region(string bucketName)
    {
        regionMap.TryGetValue(bucketName, out var value);
        return value ?? "us-east-1";
    }

    /// <summary>
    ///     Adds bucket name and its region to BucketRegionCache.
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="region"></param>
    public void Add(string bucketName, string region)
    {
        regionMap.TryAdd(bucketName, region);
    }

    /// <summary>
    ///     Removes region cache of the bucket if any.
    /// </summary>
    /// <param name="bucketName"></param>
    public void Remove(string bucketName)
    {
        regionMap.TryRemove(bucketName, out var value);
    }

    /// <summary>
    ///     Returns true if given bucket name is in the map else false.
    /// </summary>
    /// <param name="bucketName"></param>
    /// <returns></returns>
    public bool Exists(string bucketName)
    {
        regionMap.TryGetValue(bucketName, out var value);
        return value != null;
    }

    /// <summary>
    ///     Updates Region cache for given bucket.
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
            var requestUrl = RequestUtil.MakeTargetURL(client.BaseUrl, client.Secure);

            var requestBuilder = new HttpRequestMessageBuilder(HttpMethod.Get, requestUrl, path);
            requestBuilder.AddQueryParameter("location", "");
            using var response =
                await client.ExecuteTaskAsync(client.NoErrorHandlers, requestBuilder).ConfigureAwait(false);

            if (response != null && HttpStatusCode.OK.Equals(response.StatusCode))
            {
                var root = XDocument.Parse(response.Content);
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
                    region = "eu-west-1";
                else
                    region = location;
            }

            // Add the new location.
            Instance.Add(bucketName, region);
        }

        return region;
    }
}