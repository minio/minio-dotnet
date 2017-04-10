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
using Minio.Exceptions;
using Minio.Helper;
using System;
using System.Text.RegularExpressions;

namespace Minio
{
    public class RequestHelper
    {
        private static String GetTargetURL(string baseURL, bool secure, string bucketName = null, string region = null, bool usePathStyle = true)
        {
            string endPoint = null;
            string host = baseURL;
            // For Amazon S3 endpoint, try to fetch location based endpoint.
            if (s3utils.IsAmazonEndPoint(baseURL))
            {
                // Fetch new host based on the bucket location.
                host = AWSS3Endpoints.Instance.endpoint(region);
                if (!usePathStyle)
                {
                    host = utils.UrlEncode(bucketName) + "." + utils.UrlEncode(host) + "/";
                }
            }
            var scheme = secure ? utils.UrlEncode("https") : utils.UrlEncode("http");

            // This is the actual url pointed to for all HTTP requests
            endPoint = string.Format("{0}://{1}", scheme, host);
            
            return endPoint;
        }
        private static Uri GetEndpointURL(string baseURL,bool secure, string bucketName = null, string region = null, bool usePathStyle = true)
        {
            if (string.IsNullOrEmpty(baseURL))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }

            string endPoint = null;
            string host = baseURL;
            string amzHost = baseURL;
            if ((amzHost.EndsWith(".amazonaws.com", StringComparison.CurrentCultureIgnoreCase))
                 && !(amzHost.Equals("s3.amazonaws.com", StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new InvalidEndpointException(amzHost, "For Amazon S3, host should be \'s3.amazonaws.com\' in endpoint.");
            }
            //// For Amazon S3 endpoint, try to fetch location based endpoint.
            //if (s3utils.IsAmazonEndPoint(baseURL))
            //{
            //    // Fetch new host based on the bucket location.
            //    host = AWSS3Endpoints.Instance.endpoint(region);
            //    if (!usePathStyle)
            //    {
            //        host = utils.UrlEncode(bucketName) + "." + utils.UrlEncode(host) + "/";
            //    }
            //}
            var scheme = secure ? utils.UrlEncode("https") : utils.UrlEncode("http");

            // This is the actual url pointed to for all HTTP requests
            endPoint = string.Format("{0}://{1}", scheme, host);
            Uri uri = TryCreateUri(endPoint);
            _validateEndpoint(uri);

            return uri;
        }
        private static Uri TryCreateUri(string endpoint)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(endpoint);
            }
            catch (UriFormatException e)
            {
                throw new InvalidEndpointException(e.Message);
            }
            return uri;
        }

        /// <summary>
        /// Validates URI to check if it is well formed. Otherwise cry foul.
        /// </summary>
        private static void _validateEndpoint(Uri uri)
        {
            if (string.IsNullOrEmpty(uri.OriginalString))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }
            string endpoint = uri.Host;

            if (!isValidEndpoint(uri.Host))
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
            if ((!uri.Scheme.ToLowerInvariant().Equals("https")) && (!uri.Scheme.ToLowerInvariant().Equals("http")))
            //kp if (!(this.uri.Scheme.Equals(Uri.UriSchemeHttp) || this.uri.Scheme.Equals(Uri.UriSchemeHttps)))
            {
                throw new InvalidEndpointException(endpoint, "Invalid scheme detected in endpoint.");
            }
           
        }

        /// <summary>
        /// Validate Url endpoint 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns>true/false</returns>
        private static bool isValidEndpoint(string endpoint)
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

                Regex validLabel = new Regex("^[a-zA-Z0-9][a-zA-Z0-9-]*");
                Regex validEndpoint = new Regex(".*[a-zA-Z0-9]$");

                if (!(validLabel.IsMatch(label) && validEndpoint.IsMatch(endpoint)))
                {
                    return false;
                }
            }

            return true;
        }

    }
}

