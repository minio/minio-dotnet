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

    /**
     * Amazon AWS S3 endpoints for various regions.
     */
    internal sealed class Awss3Endpoints
    {
        private static readonly Lazy<Awss3Endpoints> Lazy =
            new Lazy<Awss3Endpoints>(() => new Awss3Endpoints());

        private readonly ConcurrentDictionary<string, string> endpoints;

        private Awss3Endpoints()
        {
            this.endpoints = new ConcurrentDictionary<string, string>();
            // ap-northeast-1
            this.endpoints.TryAdd("ap-northeast-1", "s3-ap-northeast-1.amazonaws.com");
            // ap-northeast-2
            this.endpoints.TryAdd("ap-northeast-2", "s3-ap-northeast-2.amazonaws.com");
            //ap-south-1
            this.endpoints.TryAdd("ap-south-1", "s3-ap-south-1.amazonaws.com");
            // ap-southeast-1
            this.endpoints.TryAdd("ap-southeast-1", "s3-ap-southeast-1.amazonaws.com");
            // ap-southeast-2
            this.endpoints.TryAdd("ap-southeast-2", "s3-ap-southeast-2.amazonaws.com");
            // eu-central-1
            this.endpoints.TryAdd("eu-central-1", "s3-eu-central-1.amazonaws.com");
            // eu-west-1
            this.endpoints.TryAdd("eu-west-1", "s3-eu-west-1.amazonaws.com");
            // eu-west-2
            this.endpoints.TryAdd("eu-west-2", "s3-eu-west-2.amazonaws.com");
            // sa-east-1
            this.endpoints.TryAdd("sa-east-1", "s3-sa-east-1.amazonaws.com");
            // us-west-1
            this.endpoints.TryAdd("us-west-1", "s3-us-west-1.amazonaws.com");
            // us-west-2
            this.endpoints.TryAdd("us-west-2", "s3-us-west-2.amazonaws.com");
            // us-east-1
            this.endpoints.TryAdd("us-east-1", "s3.amazonaws.com");
            // us-east-2
            this.endpoints.TryAdd("us-east-2", "s3-us-east-2.amazonaws.com");
            //ca-central-1
            this.endpoints.TryAdd("ca-central-1", "s3.ca-central-1.amazonaws.com");
            // cn-north-1
            this.endpoints.TryAdd("cn-north-1", "s3.cn-north-1.amazonaws.com.cn");
        }

        public static Awss3Endpoints Instance => Lazy.Value;

        /**
         * Gets Amazon S3 endpoint for the relevant region.
         */
        public static string Endpoint(string region)
        {
            string endpoint = null;
            if (region != null)
            {
                Instance.endpoints.TryGetValue(region, out endpoint);
            }
            
            return endpoint ?? "s3.amazonaws.com";
        }
    }
}