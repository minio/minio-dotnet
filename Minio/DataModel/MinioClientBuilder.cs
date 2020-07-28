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
using Minio.Exceptions;

namespace Minio
{
    public interface IMinioClient
    {
        MinioClient Build();
        MinioClient WithCredentials(string accessKey, string secretKey);
        MinioClient WithRegion(string region);
        MinioClient WithEndpoint(Uri url);
        MinioClient WithEndpoint(string endpoint, int port, bool secure);
        MinioClient WithEndpoint(string endpoint);
    }

    public partial class MinioClient : IMinioClient
    {
        internal bool IsAwsHost { get; private set; }
        internal bool IsAwsChinaHost { get; private set; }
        internal bool IsAcceleratedHost { get; private set; }
        internal bool IsDualStackHost { get; private set; }
        internal bool UseVirtualStyle { get; private set; }
        internal string RegionInUrl { get; private set; }
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
            this.IsAwsHost = BuilderUtil.IsAwsEndpoint(url.Host);
            this.IsAwsChinaHost = false;
            if ( this.IsAwsHost )
            {
                this.IsAcceleratedHost = BuilderUtil.IsAwsAccelerateEndpoint(url.Host);
                this.IsDualStackHost = BuilderUtil.IsAwsDualStackEndpoint(url.Host);
                this.RegionInUrl = BuilderUtil.ExtractRegion(url.Host);
                this.UseVirtualStyle = true;
                this.IsAwsChinaHost = BuilderUtil.IsChineseDomain(url.Host);
            }
        }
        private Uri GetBaseUrl(string endpoint)
        {
            if (String.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentException(String.Format("{0} is the value of the endpoint. It can't be null or empty.", endpoint),"endpoint");
            }
            if (!endpoint.Contains("http") && !BuilderUtil.IsValidHostnameOrIPAddress(endpoint))
            {
                throw new InvalidEndpointException(String.Format("{0} is invalid hostname.", endpoint),"endpoint");
            }

            if (endpoint.EndsWith("/"))
            {
                endpoint = endpoint.Substring(0, endpoint.Length - 1);
            }
            string conn_url;
            if (endpoint.Contains("http"))
            {
                throw new InvalidEndpointException(String.Format("{0} the value of the endpoint has the scheme (http/https) in it.", endpoint),"endpoint");
            }
            string enable_https = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
            string scheme = (enable_https != null && enable_https.Equals("1"))? "https://":"http://";
            conn_url = scheme + endpoint;
            Uri url = null;
            try
            {
                url = new Uri(conn_url);
                string hostnameOfUri = url.Authority;

                if (!BuilderUtil.IsValidHostnameOrIPAddress(hostnameOfUri))
                {
                    throw new InvalidEndpointException(String.Format("{0}, {1} is invalid hostname.", endpoint, hostnameOfUri),"endpoint");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" - " + ex.GetType().ToString());
            }
            return url;
        }

        public MinioClient WithEndpoint(string endpoint)
        {
            this.BaseUrl = endpoint;
            SetBaseURL(GetBaseUrl(endpoint));
            if (!InitDone)
            {
                InitClientBuilder();
            }
            return this;
        }

        public MinioClient WithEndpoint(string endpoint, int port, bool secure)
        {
            if (port < 1 || port > 65535)
            {
                throw new ArgumentException(String.Format("Port {0} is not a number between 1 and 65535",port), "port");
            }
            var url = GetBaseUrl(endpoint + ":" + port);
            SetBaseURL(url);
            this.Secure = secure;
            if (!InitDone)
            {
                InitClientBuilder();
            }
            return this;
        }

        public MinioClient WithEndpoint(Uri url)
        {
            if (url != null )
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
            this.RegionInUrl = region;
            if (!InitDone)
            {
                InitClientBuilder();
            }
            return this;
        }

        public MinioClient WithCredentials(String accessKey, String secretKey)
        {
            this.AccessKey = accessKey;
            this.SecretKey = secretKey;
            if (!InitDone)
            {
                InitClientBuilder();
            }
            return this;
        }

        public MinioClient Build()
        {
            if (!InitDone)
            {
                InitClientBuilder();
            }
            return this;
        }

    }
}