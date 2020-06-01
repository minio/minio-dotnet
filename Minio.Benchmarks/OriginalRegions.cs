﻿/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
* (C) 2017, 2018, 2019, 2020 MinIO, Inc.
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

using System.Text.RegularExpressions;

namespace Minio.Benchmarks
{
    public static class OriginalRegions
    {
        /// <summary>
        /// Get corresponding region for input host.
        /// </summary>
        /// <param name="endpoint">S3 API endpoint</param>
        /// <returns>Region corresponding to the endpoint. Default is 'us-east-1'</returns>
        public static string GetRegionFromEndpoint(string endpoint)
        {
            string region = null;
            Regex endpointrgx = new Regex("^([a-z0-9][a-z0-9\\.\\-]{1,61}[a-z0-9])*?.?s3[.\\-]?(.*?)\\.amazonaws\\.com$", RegexOptions.IgnoreCase);
            Regex regionrgx = new Regex("^(s3[.\\-])?(.*?)$");
            MatchCollection matches = endpointrgx.Matches(endpoint);
            if (matches.Count > 0 && matches[0].Groups.Count > 1)
            {
                string regionStr = matches[0].Groups[2].Value;
                matches = regionrgx.Matches(regionStr);
                if (matches.Count > 0 && matches[0].Groups.Count > 1)
                {
                    region = matches[0].Groups[0].Value;
                }
            }
            return region ?? string.Empty;
        }
    }
}