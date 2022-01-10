/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2020 MinIO, Inc.
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
using System.Net;
using Minio.Credentials;
using Minio.Exceptions;

namespace Minio
{
    public interface IMinioClient
    {
        MinioClient Build();
        MinioClient WithCredentials(string accessKey, string secretKey);
        MinioClient WithRegion(string region);
        MinioClient WithEndpoint(Uri url);
        MinioClient WithEndpoint(string endpoint, int port);
        MinioClient WithEndpoint(string endpoint);
        MinioClient WithSessionToken(string sessiontoken);
    }

    public partial class MinioClient : IMinioClient
    {
        internal IWebProxy Proxy { get; private set; }

        private void SetBaseURL(Uri url)
        {
            if ( url.IsDefaultPort )
            {
                this.BaseUrl = url.Host;
            }
            else
            {
                this.BaseUrl = url.Host + ":" + url.Port;
            }
        }
        private Uri GetBaseUrl(string endpoint)
        {
            if (String.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentException(String.Format("{0} is the value of the endpoint. It can't be null or empty.", endpoint),"endpoint");
            }
            if (endpoint.EndsWith("/"))
            {
                endpoint = endpoint.Substring(0, endpoint.Length - 1);
            }
            if (!endpoint.StartsWith("http") && !BuilderUtil.IsValidHostnameOrIPAddress(endpoint))
            {
                throw new InvalidEndpointException(String.Format("{0} is invalid hostname.", endpoint),"endpoint");
            }
            string conn_url;
            if (endpoint.StartsWith("http"))
            {
                throw new InvalidEndpointException(String.Format("{0} the value of the endpoint has the scheme (http/https) in it.", endpoint),"endpoint");
            }
            string enable_https = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
            string scheme = (enable_https != null && enable_https.Equals("1"))? "https://":"http://";
            conn_url = scheme + endpoint;
            string hostnameOfUri = string.Empty;
            Uri url = null;
            try
            {
                url = new Uri(conn_url);
                hostnameOfUri = url.Authority;
            }
            catch (Exception)
            {
                throw;
            }
            if ( !String.IsNullOrEmpty(hostnameOfUri) && !BuilderUtil.IsValidHostnameOrIPAddress(hostnameOfUri))
            {
                throw new InvalidEndpointException(String.Format("{0}, {1} is invalid hostname.", endpoint, hostnameOfUri),"endpoint");
            }

            return url;
        }

        public MinioClient WithEndpoint(string endpoint)
        {
            this.BaseUrl = endpoint;
            SetBaseURL(GetBaseUrl(endpoint));
            return this;
        }

        public MinioClient WithEndpoint(string endpoint, int port)
        {
            if (port < 1 || port > 65535)
            {
                throw new ArgumentException(String.Format("Port {0} is not a number between 1 and 65535",port), "port");
            }
            return WithEndpoint(endpoint + ":" + port);
        }

        public MinioClient WithEndpoint(Uri url)
        {
            if (url == null )
            {
                throw new ArgumentException(String.Format("URL is null. Can't create endpoint."));
            }
            return WithEndpoint(url.AbsoluteUri);
        }

        public MinioClient WithRegion(string region)
        {
            if (String.IsNullOrEmpty(region))
            {
                throw new ArgumentException(String.Format("{0} the region value can't be null or empty.", region),"region");
            }
            this.Region = region;
            return this;
        }

        public MinioClient WithCredentials(string accessKey, string secretKey)
        {
            this.AccessKey = accessKey;
            this.SecretKey = secretKey;
            return this;
        }

        public MinioClient WithSessionToken(string st)
        {
            this.SessionToken = st;
            return this;
        }

        public MinioClient Build()
        {
            // Instantiate a region cache
            this.regionCache = BucketRegionCache.Instance;
            if (string.IsNullOrEmpty(this.BaseUrl))
            {
                throw new MinioException("Endpoint not initialized.");
            }
            if (this.Provider != null && this.Provider.GetType() != (typeof(ChainedProvider)) && this.SessionToken == null)
            {
                throw new MinioException("User Access Credentials Provider not initialized correctly.");
            }
            if (this.Provider == null && (string.IsNullOrEmpty(this.AccessKey) || string.IsNullOrEmpty(this.SecretKey)))
            {
                throw new MinioException("User Access Credentials not initialized.");
            }

            string host = this.BaseUrl;

            var scheme = this.Secure ? utils.UrlEncode("https") : utils.UrlEncode("http");

            if ( !this.BaseUrl.StartsWith("http") )
            {
               this.Endpoint = string.Format("{0}://{1}", scheme, host);
            }
            else
            {
                this.Endpoint = host;
            }
            Init();
            return this;
        }
    }
}