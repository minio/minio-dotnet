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

using Minio.Exceptions;
using Minio.Helper;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Minio
{
    internal class RequestUtil
    {
        internal static Uri GetEndpointURL(string endPoint, bool secure)
        {
            if (endPoint.Contains(":"))
            {
                string[] parts = endPoint.Split(':');
                string host = parts[0];
                string port = parts[1];
                if (!s3utils.IsValidIP(host) && !IsValidEndpoint(host))
                {
                    throw new InvalidEndpointException("Endpoint: " + endPoint + " does not follow ip address or domain name standards.");
                }
            }
            else
            {
                if (!s3utils.IsValidIP(endPoint) && !IsValidEndpoint(endPoint))
                {
                    throw new InvalidEndpointException("Endpoint: " + endPoint + " does not follow ip address or domain name standards.");
                }
            }

            Uri uri = TryCreateUri(endPoint, secure);
            ValidateEndpoint(uri, endPoint);
            return uri;
        }

        internal static Uri MakeTargetURL(string endPoint, bool secure, string bucketName = null, string region = null, bool usePathStyle = true)
        {
            // For Amazon S3 endpoint, try to fetch location based endpoint.
            string host = endPoint;
            if (s3utils.IsAmazonEndPoint(endPoint))
            {
                // Fetch new host based on the bucket location.
                host = AWSS3Endpoints.Instance.Endpoint(region);
                if (!usePathStyle)
                {
                    string prefix = (bucketName != null) ? utils.UrlEncode(bucketName) + "." : "";
                    host = prefix + utils.UrlEncode(host) + "/";
                }
            }
            Uri uri = TryCreateUri(host, secure);
            return uri;
        }

        internal static Uri TryCreateUri(string endpoint, bool secure)
        {
            var scheme = secure ? utils.UrlEncode("https") : utils.UrlEncode("http");

            // This is the actual url pointed to for all HTTP requests
            string endpointURL = string.Format("{0}://{1}", scheme, endpoint);
            Uri uri = null;
            try
            {
                uri = new Uri(endpointURL);
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
        internal static void ValidateEndpoint(Uri uri, string endpoint)
        {
            if (string.IsNullOrEmpty(uri.OriginalString))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }
            string host = uri.Host;

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
            if ((!uri.Scheme.ToLowerInvariant().Equals("https")) && (!uri.Scheme.ToLowerInvariant().Equals("http")))
            {
                throw new InvalidEndpointException(endpoint, "Invalid scheme detected in endpoint.");
            }
        }

        /// <summary>
        /// Validate Url endpoint
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

                Regex validLabel = new Regex("^[a-zA-Z0-9]([A-Za-z0-9-_]*[a-zA-Z0-9])?$");

                if (!validLabel.IsMatch(label))
                {
                    return false;
                }
            }
            return true;
        }

        internal static RestRequest CreateRequest(string baseURL, RestSharp.Method method, RestSharp.Authenticators.IAuthenticator authenticator,
                        string bucketName = null, bool secure=false, string objectName = null,
                        Dictionary<string, string> headerMap = null,
                        string contentType = "application/octet-stream",
                        object body = null, string resourcePath = null)
        {
            utils.ValidateBucketName(bucketName);
            if (objectName != null)
            {
                utils.ValidateObjectName(objectName);
            }

            // Start with user specified endpoint
            string host = baseURL;

            string resource = string.Empty;
            bool usePathStyle = false;
            if (bucketName != null)
            {
                if (s3utils.IsAmazonEndPoint(baseURL))
                {
                    if (method == RestSharp.Method.PUT && objectName == null && resourcePath == null)
                    {
                        // use path style for make bucket to workaround "AuthorizationHeaderMalformed" error from s3.amazonaws.com
                        usePathStyle = true;
                    }
                    else if (resourcePath != null && resourcePath.Contains("location"))
                    {
                        // use path style for location query
                        usePathStyle = true;
                    }
                    else if (bucketName != null && bucketName.Contains(".") && secure)
                    {
                        // use path style where '.' in bucketName causes SSL certificate validation error
                        usePathStyle = true;
                    }
                    else if ( method == RestSharp.Method.HEAD && secure )
                    {
                        usePathStyle = true;
                    }

                    if (usePathStyle)
                    {
                        resource += utils.UrlEncode(bucketName) + "/";
                    }
                }
                else
                {
                    resource += utils.UrlEncode(bucketName) + "/";
                }
            }
            if (objectName != null)
            {
                resource += utils.EncodePath(objectName);
            }

            // Append query string passed in
            if (resourcePath != null)
            {
                resource += resourcePath;
            }

            RestRequest request = new RestRequest(resource, method);

            if (body != null)
            {
                request.AddParameter(contentType, body, RestSharp.ParameterType.RequestBody);
            }

            if (headerMap != null)
            {
                foreach (var entry in headerMap)
                {
                    request.AddHeader(entry.Key, entry.Value);
                }
            }
            return request;
        }
    }
}