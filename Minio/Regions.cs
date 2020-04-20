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

using System.Text.RegularExpressions;

namespace Minio
{
	public static class Regions
	{
		private static readonly Regex endpointRegex = new Regex(@"s3[.\-](.*?)\.amazonaws\.com$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.RightToLeft);

        /// <summary>
        /// Get corresponding region for input host.
        /// </summary>
        /// <param name="endpoint">S3 API endpoint</param>
        /// <returns>Region corresponding to the endpoint. Default is 'us-east-1'</returns>
        public static string GetRegionFromEndpoint(string endpoint)
        {
            Match match = endpointRegex.Match(endpoint);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }
    }
}
