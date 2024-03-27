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

using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Minio.Credentials;
using Minio.DataModel;
using Minio.DataModel.Result;
using Minio.Exceptions;
using Minio.Handlers;
using Minio.Helper;

namespace Minio;

public static class MinioClientExtensions
{
    internal static string SystemUserAgent
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var release = $"minio-dotnet/{version}";
#if NET46
		string arch = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
		return $"MinIO ({Environment.OSVersion};{arch}) {release}";
#else
            var arch = RuntimeInformation.OSArchitecture.ToString();
            return $"MinIO ({RuntimeInformation.OSDescription};{arch}) {release}";
#endif
        }
    }

    public static IMinioClient WithEndpoint(this IMinioClient minioClient, string endpoint)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.Config.Endpoint = endpoint;
        minioClient.SetBaseURL(GetBaseUrl(endpoint));
        return minioClient;
    }

    public static IMinioClient WithEndpoint(this IMinioClient minioClient, string endpoint, int port)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        if (port is < 1 or > 65535)
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "Port {0} is not a number between 1 and 65535", port),
                nameof(port));
        endpoint = endpoint + ":" + port.ToString(CultureInfo.InvariantCulture);
        minioClient.Config.Endpoint = endpoint;
        minioClient.SetBaseURL(GetBaseUrl(endpoint));
        return minioClient;
    }

    public static IMinioClient WithEndpoint(this IMinioClient minioClient, Uri url)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        if (url is null) throw new ArgumentNullException(nameof(url));
        minioClient.SetBaseURL(url);
        minioClient.Config.Endpoint = url.AbsoluteUri;

        return minioClient;
    }

    public static IMinioClient WithRegion(this IMinioClient minioClient, string region)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        if (string.IsNullOrEmpty(region))
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "{0} the region value can't be null or empty.", region),
                nameof(region));

        minioClient.Config.Region = region;
        return minioClient;
    }

    public static IMinioClient WithRegion(this IMinioClient minioClient)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));
        // Set region to its default value if empty or null
        minioClient.Config.Region ??= "us-east-1";
        return minioClient;
    }

    public static IMinioClient WithCredentials(this IMinioClient minioClient, string accessKey, string secretKey)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.Config.AccessKey = accessKey;
        minioClient.Config.SecretKey = secretKey;
        return minioClient;
    }

    public static IMinioClient WithSessionToken(this IMinioClient minioClient, string st)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.Config.SessionToken = st;
        return minioClient;
    }

    /// <summary>
    ///     Connects to Cloud Storage with HTTPS if this method is invoked on client object
    /// </summary>
    /// <returns></returns>
    public static IMinioClient WithSSL(this IMinioClient minioClient, bool secure = true)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));
        minioClient.Config.Secure = secure;
        return minioClient;
    }

    /// <summary>
    ///     Uses webproxy for all requests if this method is invoked on client object.
    /// </summary>
    /// <param name="minioClient">The MinioClient instance used</param>
    /// <param name="proxy">Information on the proxy server in the setup.</param>
    /// <returns></returns>
    public static IMinioClient WithProxy(this IMinioClient minioClient, IWebProxy proxy)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.Config.Proxy = proxy;
        return minioClient;
    }

    /// <summary>
    ///     Uses the set timeout for all requests if this method is invoked on client object
    /// </summary>
    /// <param name="minioClient">The MinioClient instance used</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <returns></returns>
    public static IMinioClient WithTimeout(this IMinioClient minioClient, int timeout)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.Config.RequestTimeout = timeout;
        return minioClient;
    }

    /// <summary>
    ///     Allows to add retry policy handler
    /// </summary>
    /// <param name="minioClient">The MinioClient instance used</param>
    /// <param name="retryPolicyHandler">Delegate that will wrap execution of http client requests.</param>
    /// <returns></returns>
    public static IMinioClient WithRetryPolicy(this IMinioClient minioClient,
        IRetryPolicyHandler retryPolicyHandler)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.Config.RetryPolicyHandler = retryPolicyHandler;
        return minioClient;
    }

    /// <summary>
    ///     Allows to add retry policy handler
    /// </summary>
    /// <param name="minioClient">The MinioClient instance used</param>
    /// <param name="retryPolicyHandler">Delegate that will wrap execution of http client requests.</param>
    /// <returns></returns>
    public static IMinioClient WithRetryPolicy(this IMinioClient minioClient,
        Func<Func<Task<ResponseResult>>, Task<ResponseResult>> retryPolicyHandler)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        _ = minioClient.WithRetryPolicy(new DefaultRetryPolicyHandler(retryPolicyHandler));
        return minioClient;
    }

    /// <summary>
    ///     Allows end user to define the Http server and pass it as a parameter
    /// </summary>
    /// <param name="minioClient">The MinioClient instance used</param>
    /// <param name="httpClient"> Instance of HttpClient</param>
    /// <param name="disposeHttpClient"> Dispose the HttpClient when leaving</param>
    /// <returns></returns>
    public static IMinioClient WithHttpClient(this IMinioClient minioClient, HttpClient httpClient,
        bool disposeHttpClient = false)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        if (httpClient is not null) minioClient.Config.HttpClient = httpClient;
        minioClient.Config.DisposeHttpClient = disposeHttpClient;
        return minioClient;
    }

    /// <summary>
    ///     With provider for credentials and session token if being used
    /// </summary>
    /// <returns></returns>
    public static IMinioClient WithCredentialsProvider(this IMinioClient minioClient, IClientProvider provider)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.Config.Provider = provider;
        AccessCredentials credentials;
        if (minioClient.Config.Provider is IAMAWSProvider)
            // Empty object, we need the Minio client completely
            credentials = new AccessCredentials();
        else
            credentials = minioClient.Config.Provider.GetCredentials();

        if (credentials is null)
            // Unable to fetch credentials.
            return minioClient;

        minioClient.Config.AccessKey = credentials.AccessKey;
        minioClient.Config.SecretKey = credentials.SecretKey;
        var isSessionTokenAvailable = !string.IsNullOrEmpty(credentials.SessionToken);
        if ((minioClient.Config.Provider is AWSEnvironmentProvider ||
             minioClient.Config.Provider is IAMAWSProvider ||
             minioClient.Config.Provider is CertificateIdentityProvider ||
             (minioClient.Config.Provider is ChainedProvider chainedProvider &&
              chainedProvider.CurrentProvider is AWSEnvironmentProvider))
            && isSessionTokenAvailable)
            minioClient.Config.SessionToken = credentials.SessionToken;

        return minioClient;
    }

    public static IMinioClient Build(this IMinioClient minioClient)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        // Instantiate a region cache
        minioClient.Config.RegionCache = BucketRegionCache.Instance;
        if (string.IsNullOrEmpty(minioClient.Config.BaseUrl)) throw new MinioException("Endpoint not initialized.");
        if (minioClient.Config.Provider is not null &&
            minioClient.Config.Provider.GetType() != typeof(ChainedProvider) &&
            minioClient.Config.SessionToken is null)
            throw new MinioException("User Access Credentials Provider not initialized correctly.");
        if (minioClient.Config.Provider is null &&
            (string.IsNullOrEmpty(minioClient.Config.AccessKey) || string.IsNullOrEmpty(minioClient.Config.SecretKey)))
            throw new MinioException("User Access Credentials not initialized.");

        var host = minioClient.Config.BaseUrl;

        var scheme = minioClient.Config.Secure ? Utils.UrlEncode("https") : Utils.UrlEncode("http");

        if (!minioClient.Config.BaseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            minioClient.Config.Endpoint = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", scheme, host);
        else
            minioClient.Config.Endpoint = host;
         
        minioClient.Config.HttpClient ??= minioClient.Config.Proxy is null
            ? new HttpClient()
            : new HttpClient(new HttpClientHandler { Proxy = minioClient.Config.Proxy });
        _ = minioClient.Config.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            minioClient.Config.FullUserAgent);
        minioClient.Config.HttpClient.Timeout = TimeSpan.FromMinutes(30);
        return minioClient;
    }

    /// <summary>
    ///     Sets app version and name. Used for constructing User-Agent header in all HTTP requests
    /// </summary>
    /// <param name="minioClient"></param>
    /// <param name="appName"></param>
    /// <param name="appVersion"></param>
    public static IMinioClient SetAppInfo(this IMinioClient minioClient, string appName, string appVersion)
    {
        if (string.IsNullOrEmpty(appName))
            throw new ArgumentException("Appname cannot be null or empty", nameof(appName));

        if (string.IsNullOrEmpty(appVersion))
            throw new ArgumentException("Appversion cannot be null or empty", nameof(appVersion));

        minioClient.Config.CustomUserAgent = $"{appName}/{appVersion}";

        return minioClient;
    }

    internal static Uri GetBaseUrl(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture,
                    "{0} is the value of the endpoint. It can't be null or empty.", endpoint),
                nameof(endpoint));

        if (endpoint.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            endpoint = endpoint[..^1];
        if (!BuilderUtil.IsValidHostnameOrIPAddress(endpoint))
            throw new InvalidEndpointException(
                string.Format(CultureInfo.InvariantCulture, "{0} is invalid hostname.", endpoint), "endpoint");
        string conn_url;
        if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            throw new InvalidEndpointException(
                string.Format(CultureInfo.InvariantCulture,
                    "{0} the value of the endpoint has the scheme (http/https) in it.", endpoint),
                "endpoint");

        var enable_https = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
        var scheme = enable_https?.Equals("1", StringComparison.OrdinalIgnoreCase) == true ? "https://" : "http://";
        conn_url = scheme + endpoint;
        var url = new Uri(conn_url);
        var hostnameOfUri = url.Authority;
        if (!string.IsNullOrEmpty(hostnameOfUri) && !BuilderUtil.IsValidHostnameOrIPAddress(hostnameOfUri))
            throw new InvalidEndpointException(
                string.Format(CultureInfo.InvariantCulture, "{0}, {1} is invalid hostname.", endpoint, hostnameOfUri),
                "endpoint");

        return url;
    }

    internal static void SetBaseURL(this IMinioClient minioClient, Uri url)
    {
        if (url.IsDefaultPort)
            minioClient.Config.BaseUrl = url.Host;
        else
            minioClient.Config.BaseUrl = url.Host + ":" + url.Port;
    }
}
