﻿/*
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

namespace Minio.Helper
{
    using System;
    using System.Text.RegularExpressions;
    using Exceptions;

    internal static class RequestUtil
    {
        internal static Uri GetEndpointUrl(string endPoint, bool secure)
        {
            if (endPoint.Contains(":"))
            {
                var parts = endPoint.Split(':');
                var host = parts[0];
                if (!S3Utils.IsValidIp(host) && !IsValidEndpoint(host))
                {
                    throw new InvalidEndpointException("Endpoint: " + endPoint +
                                                       " does not follow ip address or domain name standards.");
                }
            }
            else
            {
                if (!S3Utils.IsValidIp(endPoint) && !IsValidEndpoint(endPoint))
                {
                    throw new InvalidEndpointException("Endpoint: " + endPoint +
                                                       " does not follow ip address or domain name standards.");
                }
            }

            var uri = TryCreateUri(endPoint, secure);
            ValidateEndpoint(uri, endPoint);
            return uri;
        }

        internal static Uri MakeTargetUrl(string endPoint, bool secure, string bucketName = null, string region = null,
            bool usePathStyle = true)
        {
            // For Amazon S3 endpoint, try to fetch location based endpoint.
            var host = endPoint;
            if (S3Utils.IsAmazonEndPoint(endPoint))
            {
                // Fetch new host based on the bucket location.
                host = Awss3Endpoints.Endpoint(region);
                if (!usePathStyle)
                {
                    host = Utils.UrlEncode(bucketName) + "." + Utils.UrlEncode(host) + "/";
                }
            }
            var uri = TryCreateUri(host, secure);
            return uri;
        }

        internal static Uri TryCreateUri(string endpoint, bool secure)
        {
            var scheme = secure ? Utils.UrlEncode("https") : Utils.UrlEncode("http");
            // This is the actual url pointed to for all HTTP requests
            var endpointUrl = $"{scheme}://{endpoint}";
            Uri uri;
            try
            {
                uri = new Uri(endpointUrl);
            }
            catch (Exception e)
            {
                throw new InvalidEndpointException(e.Message);
            }

            return uri;
        }

        /// <summary>
        ///     Validates URI to check if it is well formed. Otherwise cry foul.
        /// </summary>
        internal static void ValidateEndpoint(Uri uri, string endpoint)
        {
            if (string.IsNullOrEmpty(uri.OriginalString))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }

            if (!IsValidEndpoint(uri.Host))
            {
                throw new InvalidEndpointException(endpoint, "Invalid endpoint.");
            }
            if (!uri.AbsolutePath.Equals("/", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new InvalidEndpointException(endpoint, "No path allowed in endpoint.");
            }

            if (!string.IsNullOrEmpty(uri.Query))
            {
                throw new InvalidEndpointException(endpoint, "No query parameter allowed in endpoint.");
            }
            if (!uri.Scheme.ToLowerInvariant().Equals("https") && !uri.Scheme.ToLowerInvariant().Equals("http"))
            {
                throw new InvalidEndpointException(endpoint, "Invalid scheme detected in endpoint.");
            }
            var amzHost = uri.Host;
            if (Utils.CaseInsensitiveContains(amzHost, ".amazonaws.com")
                && !S3Utils.IsAmazonEndPoint(uri.Host))
            {
                throw new InvalidEndpointException(amzHost,
                    "For Amazon S3, host should be \'s3.amazonaws.com\' in endpoint.");
            }
        }

        /// <summary>
        ///     Validate Url endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns>true/false</returns>
        internal static bool IsValidEndpoint(string endpoint)
        {
            // endpoint may be a hostname
            // refer https://en.wikipedia.org/wiki/Hostname#Restrictions_on_valid_host_names
            // why checks are as shown below.
            if (endpoint.Length < 1 || endpoint.Length > 253)
            {
                return false;
            }

            foreach (var label in endpoint.Split('.'))
            {
                if (label.Length < 1 || label.Length > 63)
                {
                    return false;
                }

                var validLabel = new Regex("^[a-zA-Z0-9]([A-Za-z0-9-]*[a-zA-Z0-9])?$");

                if (!validLabel.IsMatch(label))
                {
                    return false;
                }
            }
            return true;
        }
    }
}