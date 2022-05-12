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

namespace Minio;

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

    public MinioClient WithEndpoint(string endpoint)
    {
        BaseUrl = endpoint;
        SetBaseURL(GetBaseUrl(endpoint));
        return this;
    }

    public MinioClient WithEndpoint(string endpoint, int port)
    {
        if (port < 1 || port > 65535)
            throw new ArgumentException(string.Format("Port {0} is not a number between 1 and 65535", port), "port");
        return WithEndpoint(endpoint + ":" + port);
    }

    public MinioClient WithEndpoint(Uri url)
    {
        if (url == null) throw new ArgumentException("URL is null. Can't create endpoint.");
        return WithEndpoint(url.AbsoluteUri);
    }

    public MinioClient WithRegion(string region)
    {
        if (string.IsNullOrEmpty(region))
            throw new ArgumentException(string.Format("{0} the region value can't be null or empty.", region),
                "region");
        Region = region;
        return this;
    }

    public MinioClient WithCredentials(string accessKey, string secretKey)
    {
        AccessKey = accessKey;
        SecretKey = secretKey;
        return this;
    }

    public MinioClient WithSessionToken(string st)
    {
        SessionToken = st;
        return this;
    }

    public MinioClient Build()
    {
        // Instantiate a region cache
        regionCache = BucketRegionCache.Instance;
        if (string.IsNullOrEmpty(BaseUrl)) throw new MinioException("Endpoint not initialized.");
        if (Provider != null && Provider.GetType() != typeof(ChainedProvider) && SessionToken == null)
            throw new MinioException("User Access Credentials Provider not initialized correctly.");
        if (Provider == null && (string.IsNullOrEmpty(AccessKey) || string.IsNullOrEmpty(SecretKey)))
            throw new MinioException("User Access Credentials not initialized.");

        var host = BaseUrl;

        var scheme = Secure ? utils.UrlEncode("https") : utils.UrlEncode("http");

        if (!BaseUrl.StartsWith("http"))
            Endpoint = string.Format("{0}://{1}", scheme, host);
        else
            Endpoint = host;
        return this;
    }

    private void SetBaseURL(Uri url)
    {
        if (url.IsDefaultPort)
            BaseUrl = url.Host;
        else
            BaseUrl = url.Host + ":" + url.Port;
    }

    private Uri GetBaseUrl(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException(
                string.Format("{0} is the value of the endpoint. It can't be null or empty.", endpoint), "endpoint");
        if (endpoint.EndsWith("/")) endpoint = endpoint.Substring(0, endpoint.Length - 1);
        if (!BuilderUtil.IsValidHostnameOrIPAddress(endpoint))
            throw new InvalidEndpointException(string.Format("{0} is invalid hostname.", endpoint), "endpoint");
        string conn_url;
        if (endpoint.StartsWith("http"))
            throw new InvalidEndpointException(
                string.Format("{0} the value of the endpoint has the scheme (http/https) in it.", endpoint),
                "endpoint");
        var enable_https = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
        var scheme = enable_https != null && enable_https.Equals("1") ? "https://" : "http://";
        conn_url = scheme + endpoint;
        var hostnameOfUri = string.Empty;
        Uri url = null;
        url = new Uri(conn_url);
        hostnameOfUri = url.Authority;
        if (!string.IsNullOrEmpty(hostnameOfUri) && !BuilderUtil.IsValidHostnameOrIPAddress(hostnameOfUri))
            throw new InvalidEndpointException(string.Format("{0}, {1} is invalid hostname.", endpoint, hostnameOfUri),
                "endpoint");

        return url;
    }

    public MinioClient WithRegion()
    {
        // Set region to its default value if empty or null
        Region = "us-east-1";
        return this;
    }
}