/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Net;
using Minio.Exceptions;

namespace Minio
{
    public interface IBuilder
    {
        MinioClient Build();
        Task<MinioClient> BuildAsync();
        MinioClient WithHttpClient(HttpClient httpClient);
        MinioClient WithCredentials(string accessKey, string secretKey);
        MinioClient WithRegion(string region);
        MinioClient WithEndpoint(Uri url);
        MinioClient WithEndpoint(string endpoint, int port, bool secure);
        MinioClient WithEndpoint(string endpoint);
    }

    public interface IClientArgs
    {
        string GetSecretKey();
        string GetAccessKey();
        string GetBaseUrl();
        string GetEndpoint();
        BucketRegionCache GetRegionCache();
        IRequestLogger GetLogger();
        string GetRegion();
        Uri GetURI();
        bool GetIfSecure();

        void SetAccessKey(string a);
        void SetSecretKey(string s);
        void SetBaseUrl(string b);
        void SetEndpoint(string e);
        void SetRegionCache(BucketRegionCache b);
        void SetLogger(IRequestLogger l);
        void SetRegion(string r);
        void SetURI(Uri u);
        void SetIfSecure(bool s);

    }

    public class MinioClientArgs : IClientArgs
    {
        public BucketRegionCache GetRegionCache()
        {
            return RegionCache;
        }
        public IRequestLogger GetLogger()
        {
            return Logger;
        }
        public string GetAccessKey()
        {
            return AccessKey;
        }
        public string GetSecretKey()
        {
            return SecretKey;
        }
        public string GetBaseUrl()
        {
            return this.BaseUrl;
        }
        public string GetEndpoint()
        {
            return this.Endpoint;
        }
        public string GetRegion()
        {
            return this.Region;
        }
        public Uri GetURI()
        {
            return this.uri;
        }
        public bool GetIfSecure()
        {
            return this.Secure;
        }

        public void SetBaseUrl(string b)
        {
            this.BaseUrl = b;
        }
        public void SetEndpoint(string e)
        {
            this.Endpoint = e;
        }
        public void SetRegion(string r)
        {
            this.Region = r;
        }
        public void SetURI(Uri u)
        {
            this.uri = u;
        }
        public void SetIfSecure(bool s)
        {
            this.Secure = s;
        }
        public void SetRegionCache(BucketRegionCache b)
        {
            this.RegionCache = b;
        }
        public void SetLogger(IRequestLogger l)
        {
            this.Logger = l;
        }

        public void SetSecretKey(string s)
        {
            this.SecretKey = s;
        }

        public void SetAccessKey(string a)
        {
            this.AccessKey = a;
        }

        // Cache holding bucket to region mapping for buckets seen so far.
        internal BucketRegionCache RegionCache { get; set; }
        internal IRequestLogger Logger { get; set; }
        // Save Credentials from user
        internal string AccessKey { get; set; }
        internal string SecretKey { get; set; }

        internal string BaseUrl { get; set; }
        // Reconstructed endpoint with scheme and host.In the case of Amazon, this url
        // is the virtual style path or location based endpoint
        internal string Endpoint { get; set; }
        internal string Region;
        // Corresponding URI for above endpoint
        internal Uri uri;

        // Indicates if we are using HTTPS or not
        internal bool Secure { get; set; }

        internal void DeepCopy(MinioClientArgs minioClientArgs)
        {
            this.SetBaseUrl(minioClientArgs.GetBaseUrl());
            this.SetSecretKey(minioClientArgs.GetSecretKey());
            this.SetAccessKey(minioClientArgs.GetAccessKey());
            this.SetEndpoint(minioClientArgs.GetEndpoint());
            this.SetIfSecure(minioClientArgs.GetIfSecure());
            this.SetLogger(minioClientArgs.GetLogger());
            this.SetRegion(minioClientArgs.GetRegion());
            this.SetRegionCache(minioClientArgs.GetRegionCache());
            this.SetURI(minioClientArgs.GetURI());
        }
    }

    public partial class MinioClient: IBuilder 
    {
        internal HttpClient TheHttpClient { get; private set; }
        internal bool IsAwsHost { get; private set; }
        internal bool IsAwsChinaHost { get; private set; }
        internal bool IsAcceleratedHost { get; private set; }
        internal bool IsDualStackHost { get; private set; } 
        internal bool UseVirtualStyle { get; private set; }
        internal string RegionInUrl { get; private set; }
        internal IWebProxy Proxy { get; private set; }

        private void SetBaseURL(Uri url)
        {
            string host = url.Host;
            MinioClientArgs mca;
            if ( (mca = GetMinioClientArgs()) != null )
            {
                mca.BaseUrl = url.Host;
            }
            else
            {
                this.MinioClientArgs = new MinioClientArgs();
                this.MinioClientArgs.BaseUrl = url.Host;
            }
            this.IsAwsHost = BuilderUtil.IsAwsEndpoint(host);
            this.IsAwsChinaHost = false;
            if ( this.IsAwsHost )
            {
                this.IsAcceleratedHost = BuilderUtil.IsAwsAccelerateEndpoint(host);
                this.IsDualStackHost = BuilderUtil.IsAwsDualStackEndpoint(host);
                this.RegionInUrl = BuilderUtil.ExtractRegion(host);
                this.UseVirtualStyle = true;
                this.IsAwsChinaHost = BuilderUtil.IsChineseDomain(host);
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
            string conn_url;
            if (endpoint.Contains("http"))
            {
                throw new InvalidEndpointException(String.Format("{0} the value of the endpoint has the scheme (http/https) in it.", endpoint),"endpoint");
            }
            string enable_https = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
            string scheme = (enable_https != null && enable_https.Equals("1"))? "https://":"http://";
            conn_url = scheme + endpoint;
            Uri url = new Uri(conn_url);
            string hostnameOfUri = url.Authority;

            if (!BuilderUtil.IsValidHostnameOrIPAddress(hostnameOfUri))
            {
                throw new ArgumentException(String.Format("{0}, {1} is invalid hostname.", endpoint, hostnameOfUri),"endpoint");
            }
            return url;
        }

        public MinioClient WithEndpoint(string endpoint)
        {
            if ( this.GetMinioClientArgs()  == null )
            {
                this.MinioClientArgs = new MinioClientArgs();
                this.MinioClientArgs.Endpoint = endpoint;
                this.MinioClientArgs.BaseUrl = endpoint;
            }
            else
            {
                this.GetMinioClientArgs().SetEndpoint(endpoint);
            }
            SetBaseURL(GetBaseUrl(endpoint));
            if (!initDone)
            {
                Console.WriteLine("WithEndpoint InitClient");
                InitClient();
            }
            return this;
        }

        internal MinioClientArgs GetMinioClientArgs()
        {
            if ( this.MinioClientArgs != null )
            {
                return this.MinioClientArgs;
            }

            if ( this.BucketMinioClientArgs != null )
            {
                return this.BucketMinioClientArgs.ClientArgs;
            }

            if ( this.ObjectMinioClientArgs != null )
            {
                return this.ObjectMinioClientArgs.ClientArgs;
            }

            if ( this.EncryptionMinioClientArgs != null )
            {
                return this.EncryptionMinioClientArgs.ClientArgs;
            }

            if ( this.ListenNotificationMinioClientArgs != null )
            {
                return this.ListenNotificationMinioClientArgs.ClientArgs;
            }

            if ( this.ObjectReadPropertiesClientArgs != null )
            {
                return this.ObjectReadPropertiesClientArgs.ClientArgs;
            }

            return null;
        }

        public MinioClient WithEndpoint(string endpoint, int port, bool secure)
        {
            MinioClientArgs mca;
            if ( (mca = GetMinioClientArgs()) == null )
            {
                this.MinioClientArgs = new MinioClientArgs();
                this.MinioClientArgs.Endpoint = endpoint + ":" + port;
                this.MinioClientArgs.Secure = secure;
                mca = this.MinioClientArgs;
            }
            else
            {
                mca.Endpoint = endpoint + ":" + port;
                mca.Secure = secure;
            }
            if (port < 1 || port > 65535) {
                throw new ArgumentException(String.Format("Port {0} is not a number between 1 and 65535",port), "port");
            }
            var url = GetBaseUrl(endpoint + ":" + port);
            SetBaseURL(url);
            mca.Endpoint = endpoint + ":" + port;
            mca.Secure = secure;
            if (!initDone)
            {
                InitClient();
            }
            return this;
        }

        public MinioClient WithEndpoint(Uri url)
        {
            MinioClientArgs mca;
            if ( (mca = GetMinioClientArgs()) == null )
            {
                this.MinioClientArgs = new MinioClientArgs();
            }
            if (url != null )
            {
                throw new ArgumentException(String.Format("URL is null. Can't create endpoint."));
            }
            // TODO: Check.
            return WithEndpoint(url.AbsoluteUri);
        }

        public MinioClient WithRegion(string region)
        {
            MinioClientArgs mca;
            if ( (mca = GetMinioClientArgs()) == null )
            {
                this.MinioClientArgs = new MinioClientArgs();
                this.MinioClientArgs.Region = region;
            }
            else
            {
                mca.Region = region;
            }
            if (String.IsNullOrEmpty(region))
            {
                throw new ArgumentException(String.Format("{0} the region value can't be null or empty.", region),"region");
            }
            if (!initDone)
            {
                InitClient();
            }
            this.RegionInUrl = region;
            return this;
        }

        public MinioClient WithHttpClient(HttpClient httpClient)
        {
            if (this.MinioClientArgs == null)
            {
                this.MinioClientArgs = new MinioClientArgs();
            }
            if (httpClient == null)
            {
                throw new ArgumentException(String.Format("{0} the HTTP client value can't be null or empty.", httpClient),"httpClient");
            }
            this.TheHttpClient = httpClient;
            if (!initDone)
            {
                InitClient();
            }
            return this;
        }

        public MinioClient WithCredentials(String accessKey, String secretKey)
        {
            MinioClientArgs mca = this.GetMinioClientArgs();
            if (mca == null)
            {
                this.MinioClientArgs = new MinioClientArgs();
                mca = this.MinioClientArgs;
            }
            mca.AccessKey = accessKey;
            mca.SecretKey = secretKey;
            if (!initDone)
            {
                InitClient();
            }
            return this;
        }

        public MinioClient Build()
        {
            // TODO: If it is already instantiated.
            if (!initDone)
            {
                InitClient();
            }
            if (this.TheHttpClient == null)
            {
                // TODO: Check. TheHttpClient is a little complicated.
                // TODO: Make sure to initialize that member variable.
                string filename = Environment.GetEnvironmentVariable("SSL_CERT_FILE");
                if (!String.IsNullOrEmpty(filename))
                {
                    // TODO: enable external certificates
                    var handler = new HttpClientHandler();
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    handler.SslProtocols = SslProtocols.Tls12;
                    handler.ClientCertificates.Add(new X509Certificate2(filename));
                    using(this.TheHttpClient = new HttpClient(handler))
                    this.TheHttpClient.DefaultRequestHeaders.TransferEncodingChunked = true;
                }
                else
                {
                    using(this.TheHttpClient = new HttpClient())
                    this.TheHttpClient.DefaultRequestHeaders.TransferEncodingChunked = true;
                }
            }
            
            return this;
        }
        public async Task<MinioClient> BuildAsync()
        {
            MinioClientArgs mca = this.GetMinioClientArgs();
            if (!initDone)
            {
                InitClient();
            }
            if (string.IsNullOrEmpty(mca.BaseUrl))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }
            if (this.TheHttpClient == null)
            {
                // TODO: Check. TheHttpClient is a little complicated.
                // TODO: Make sure to initialize that member variable.
                string filename = Environment.GetEnvironmentVariable("SSL_CERT_FILE");
                if (!String.IsNullOrEmpty(filename))
                {
                    // TODO: Test external certificates
                    var handler = new HttpClientHandler();
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    handler.SslProtocols = SslProtocols.Tls12;
                    handler.ClientCertificates.Add(new X509Certificate2(filename));
                    using(this.TheHttpClient = new HttpClient(handler))
                    this.TheHttpClient.BaseAddress = new Uri(mca.BaseUrl);
                    this.TheHttpClient.DefaultRequestHeaders.TransferEncodingChunked = true;
                }
                else
                {
                    using(this.TheHttpClient = new HttpClient())
                    {
                        this.TheHttpClient.BaseAddress = RequestUtil.MakeTargetURL(mca.BaseUrl, mca.Secure);
                        this.TheHttpClient.DefaultRequestHeaders.TransferEncodingChunked = true;
                    }
                }
            }

            return this;
        }
        protected MinioClient()
        {
            initDone = false;
        }

        public static MinioClient NewClient()
        {
            return new MinioClient();
        }

    }
}
