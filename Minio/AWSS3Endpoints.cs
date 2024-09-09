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

using System.Collections.Concurrent;

namespace Minio;

/// <summary>
///     Amazon AWS S3 endpoints for various regions.
/// </summary>
public sealed class AWSS3Endpoints
{
    private static readonly Lazy<AWSS3Endpoints> lazy = new(() => new AWSS3Endpoints());

    private readonly ConcurrentDictionary<string, string> endpoints;

    private AWSS3Endpoints()
    {
        endpoints = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        // af-south-1
        _ = endpoints.TryAdd("af-south-1", "s3.af-south-1.amazonaws.com");
        // ap-east-1
        _ = endpoints.TryAdd("ap-east-1", "s3.ap-east-1.amazonaws.com");
        // ap-northeast-1
        _ = endpoints.TryAdd("ap-northeast-1", "s3-ap-northeast-1.amazonaws.com");
        // ap-northeast-2
        _ = endpoints.TryAdd("ap-northeast-2", "s3-ap-northeast-2.amazonaws.com");
        // ap-northeast-3
        _ = endpoints.TryAdd("ap-northeast-3", "s3.ap-northeast-3.amazonaws.com");
        // ap-south-1
        _ = endpoints.TryAdd("ap-south-1", "s3-ap-south-1.amazonaws.com");
        // ap-south-2
        _ = endpoints.TryAdd("ap-south-2", "s3.ap-south-2.amazonaws.com");
        // ap-southeast-1
        _ = endpoints.TryAdd("ap-southeast-1", "s3-ap-southeast-1.amazonaws.com");
        // ap-southeast-2
        _ = endpoints.TryAdd("ap-southeast-2", "s3-ap-southeast-2.amazonaws.com");
        // ap-southeast-3
        _ = endpoints.TryAdd("ap-southeast-3", "s3.ap-southeast-3.amazonaws.com");
        // ap-southeast-4
        _ = endpoints.TryAdd("ap-southeast-4", "s3.ap-southeast-4.amazonaws.com");
        // ap-southeast-5
        _ = endpoints.TryAdd("ap-southeast-5", "s3-ap-southeast-5.amazonaws.com");
        // ca-central-1
        _ = endpoints.TryAdd("ca-central-1", "s3.ca-central-1.amazonaws.com");
        // ca-west-1
        _ = endpoints.TryAdd("ca-west-1", "s3.ca-west-1.amazonaws.com.cn");
        // cn-north-1
        _ = endpoints.TryAdd("cn-north-1", "s3.cn-north-1.amazonaws.com");
        // cn-northwest-1
        _ = endpoints.TryAdd("cn-northwest-1", "s3.cn-northwest-1.amazonaws.com.cn");
        // eu-central-1
        _ = endpoints.TryAdd("eu-central-1", "s3-eu-central-1.amazonaws.com");
        // eu-central-2
        _ = endpoints.TryAdd("eu-central-2", "s3.eu-central-2.amazonaws.com");
        // eu-north-1
        _ = endpoints.TryAdd("eu-north-1", "s3.eu-north-1.amazonaws.com");
        // eu-south-1
        _ = endpoints.TryAdd("eu-south-1", "s3.eu-south-1.amazonaws.com");
        // eu-south-2
        _ = endpoints.TryAdd("eu-south-2", "s3.eu-south-2.amazonaws.com");
        // eu-west-1
        _ = endpoints.TryAdd("eu-west-1", "s3-eu-west-1.amazonaws.com");
        // eu-west-2
        _ = endpoints.TryAdd("eu-west-2", "s3-eu-west-2.amazonaws.com");
        // eu-west-3
        _ = endpoints.TryAdd("eu-west-3", "s3.eu-west-3.amazonaws.com");
        // il-central-1
        _ = endpoints.TryAdd("il-central-1", "s3.il-central-1.amazonaws.com");
        // me-central-1
        _ = endpoints.TryAdd("me-central-1", "s3.me-central-1.amazonaws.com");
        // me-south-1
        _ = endpoints.TryAdd("me-south-1", "s3.me-south-1.amazonaws.com");
        // sa-east-1
        _ = endpoints.TryAdd("sa-east-1", "s3-sa-east-1.amazonaws.com");
        // us-east-1
        _ = endpoints.TryAdd("us-east-1", "s3.amazonaws.com");
        // us-east-2
        _ = endpoints.TryAdd("us-east-2", "s3-us-east-2.amazonaws.com");
        // us-gov-east-1
        _ = endpoints.TryAdd("us-gov-east-1", "s3.us-gov-east-1.amazonaws.com");
        // us-gov-west-1
        _ = endpoints.TryAdd("us-gov-west-1", "s3.us-gov-west-1.amazonaws.com");
        // us-west-1
        _ = endpoints.TryAdd("us-west-1", "s3-us-west-1.amazonaws.com");
        // us-west-2
        _ = endpoints.TryAdd("us-west-2", "s3-us-west-2.amazonaws.com");
    }

    public static AWSS3Endpoints Instance => lazy.Value;

    /// <summary>
    ///     Gets Amazon S3 endpoint for the relevant region.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public static string Endpoint(string region)
    {
        string endpoint = null;
        if (region is not null) _ = Instance.endpoints.TryGetValue(region, out endpoint);
        endpoint ??= "s3.amazonaws.com";
        return endpoint;
    }
}
