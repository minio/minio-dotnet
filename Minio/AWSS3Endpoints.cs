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

using System;
using System.Collections.Concurrent;

namespace Minio
{
    /// <summary>
    /// Amazon AWS S3 endpoints for various regions.
    /// </summary>
    public sealed class AWSS3Endpoints
    {
        private static readonly Lazy<AWSS3Endpoints> lazy =
            new Lazy<AWSS3Endpoints>(() => new AWSS3Endpoints());

        private readonly ConcurrentDictionary<string, string> endpoints;

        public static AWSS3Endpoints Instance => lazy.Value;

        private AWSS3Endpoints()
        {
            endpoints = new ConcurrentDictionary<string, string>();
            // ap-northeast-1
            endpoints.TryAdd("ap-northeast-1", "s3-ap-northeast-1.amazonaws.com");
            // ap-northeast-2
            endpoints.TryAdd("ap-northeast-2", "s3-ap-northeast-2.amazonaws.com");
            // ap-south-1
            endpoints.TryAdd("ap-south-1", "s3-ap-south-1.amazonaws.com");
            // ap-southeast-1
            endpoints.TryAdd("ap-southeast-1", "s3-ap-southeast-1.amazonaws.com");
            // ap-southeast-2
            endpoints.TryAdd("ap-southeast-2", "s3-ap-southeast-2.amazonaws.com");
            // eu-central-1
            endpoints.TryAdd("eu-central-1", "s3-eu-central-1.amazonaws.com");
            // eu-west-1
            endpoints.TryAdd("eu-west-1", "s3-eu-west-1.amazonaws.com");
            // eu-west-2
            endpoints.TryAdd("eu-west-2", "s3-eu-west-2.amazonaws.com");
            // sa-east-1
            endpoints.TryAdd("sa-east-1", "s3-sa-east-1.amazonaws.com");
            // us-west-1
            endpoints.TryAdd("us-west-1", "s3-us-west-1.amazonaws.com");
            // us-west-2
            endpoints.TryAdd("us-west-2", "s3-us-west-2.amazonaws.com");
            // us-east-1
            endpoints.TryAdd("us-east-1", "s3.amazonaws.com");
            // us-east-2
            endpoints.TryAdd("us-east-2", "s3-us-east-2.amazonaws.com");
            // ca-central-1
            endpoints.TryAdd("ca-central-1", "s3.ca-central-1.amazonaws.com");
            // cn-north-1
            endpoints.TryAdd("cn-north-1", "s3.cn-north-1.amazonaws.com.cn");
        }

        /// <summary>
        /// Gets Amazon S3 endpoint for the relevant region.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public string Endpoint(string region)
        {
            string endpoint = null;
            if (region != null)
            {
                Instance.endpoints.TryGetValue(region, out endpoint);
            }
            if (endpoint == null)
            {
                endpoint = "s3.amazonaws.com";
            }
            return endpoint;
        }
    }
}
