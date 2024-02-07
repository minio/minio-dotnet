/*
 * Newtera .NET Library for Newtera TDM, (C) 2017 Newtera, Inc.
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
using Newtera.Credentials;
using Newtera.DataModel;
using Newtera.DataModel.Result;
using Newtera.Exceptions;
using Newtera.Handlers;
using Newtera.Helper;

namespace Newtera;

public static class NewteraClientExtensions
{
    internal static string SystemUserAgent
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var release = $"newtera-dotnet/{version}";
#if NET46
		string arch = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
		return $"Newtera ({Environment.OSVersion};{arch}) {release}";
#else
            var arch = RuntimeInformation.OSArchitecture.ToString();
            return $"Newtera ({RuntimeInformation.OSDescription};{arch}) {release}";
#endif
        }
    }

    public static INewteraClient WithEndpoint(this INewteraClient newteraClient, string endpoint)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        newteraClient.Config.Endpoint = endpoint;
        newteraClient.SetBaseURL(GetBaseUrl(endpoint));
        return newteraClient;
    }

    public static INewteraClient WithEndpoint(this INewteraClient newteraClient, string endpoint, int port)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        if (port is < 1 or > 65535)
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "Port {0} is not a number between 1 and 65535", port),
                nameof(port));
        endpoint = endpoint + ":" + port.ToString(CultureInfo.InvariantCulture);
        newteraClient.Config.Endpoint = endpoint;
        newteraClient.SetBaseURL(GetBaseUrl(endpoint));
        return newteraClient;
    }

    public static INewteraClient WithEndpoint(this INewteraClient newteraClient, Uri url)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        if (url is null) throw new ArgumentNullException(nameof(url));
        newteraClient.SetBaseURL(url);
        newteraClient.Config.Endpoint = url.AbsoluteUri;

        return newteraClient;
    }

    public static INewteraClient WithCredentials(this INewteraClient newteraClient, string accessKey, string secretKey)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        newteraClient.Config.AccessKey = accessKey;
        newteraClient.Config.SecretKey = secretKey;
        return newteraClient;
    }

    public static INewteraClient WithSessionToken(this INewteraClient newteraClient, string st)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        newteraClient.Config.SessionToken = st;
        return newteraClient;
    }

    /// <summary>
    ///     Connects to Cloud Storage with HTTPS if this method is invoked on client object
    /// </summary>
    /// <returns></returns>
    public static INewteraClient WithSSL(this INewteraClient newteraClient, bool secure = true)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));
        newteraClient.Config.Secure = secure;
        return newteraClient;
    }

    /// <summary>
    ///     Uses webproxy for all requests if this method is invoked on client object.
    /// </summary>
    /// <param name="newteraClient">The NewteraClient instance used</param>
    /// <param name="proxy">Information on the proxy server in the setup.</param>
    /// <returns></returns>
    public static INewteraClient WithProxy(this INewteraClient newteraClient, IWebProxy proxy)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        newteraClient.Config.Proxy = proxy;
        return newteraClient;
    }

    /// <summary>
    ///     Uses the set timeout for all requests if this method is invoked on client object
    /// </summary>
    /// <param name="newteraClient">The NewteraClient instance used</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <returns></returns>
    public static INewteraClient WithTimeout(this INewteraClient newteraClient, int timeout)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        newteraClient.Config.RequestTimeout = timeout;
        return newteraClient;
    }

    /// <summary>
    ///     Allows to add retry policy handler
    /// </summary>
    /// <param name="newteraClient">The NewteraClient instance used</param>
    /// <param name="retryPolicyHandler">Delegate that will wrap execution of http client requests.</param>
    /// <returns></returns>
    public static INewteraClient WithRetryPolicy(this INewteraClient newteraClient,
        IRetryPolicyHandler retryPolicyHandler)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        newteraClient.Config.RetryPolicyHandler = retryPolicyHandler;
        return newteraClient;
    }

    /// <summary>
    ///     Allows to add retry policy handler
    /// </summary>
    /// <param name="newteraClient">The NewteraClient instance used</param>
    /// <param name="retryPolicyHandler">Delegate that will wrap execution of http client requests.</param>
    /// <returns></returns>
    public static INewteraClient WithRetryPolicy(this INewteraClient newteraClient,
        Func<Func<Task<ResponseResult>>, Task<ResponseResult>> retryPolicyHandler)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        _ = newteraClient.WithRetryPolicy(new DefaultRetryPolicyHandler(retryPolicyHandler));
        return newteraClient;
    }

    /// <summary>
    ///     Allows end user to define the Http server and pass it as a parameter
    /// </summary>
    /// <param name="newteraClient">The NewteraClient instance used</param>
    /// <param name="httpClient"> Instance of HttpClient</param>
    /// <param name="disposeHttpClient"> Dispose the HttpClient when leaving</param>
    /// <returns></returns>
    public static INewteraClient WithHttpClient(this INewteraClient newteraClient, HttpClient httpClient,
        bool disposeHttpClient = false)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        if (httpClient is not null) newteraClient.Config.HttpClient = httpClient;
        newteraClient.Config.DisposeHttpClient = disposeHttpClient;
        return newteraClient;
    }

    /// <summary>
    ///     With provider for credentials and session token if being used
    /// </summary>
    /// <returns></returns>
    public static INewteraClient WithCredentialsProvider(this INewteraClient newteraClient, IClientProvider provider)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        newteraClient.Config.Provider = provider;
        AccessCredentials credentials;
        credentials = newteraClient.Config.Provider.GetCredentials();

        if (credentials is null)
            // Unable to fetch credentials.
            return newteraClient;

        newteraClient.Config.AccessKey = credentials.AccessKey;
        newteraClient.Config.SecretKey = credentials.SecretKey;

        return newteraClient;
    }

    public static INewteraClient Build(this INewteraClient newteraClient)
    {
        if (newteraClient is null) throw new ArgumentNullException(nameof(newteraClient));

        if (string.IsNullOrEmpty(newteraClient.Config.BaseUrl)) throw new NewteraException("Endpoint not initialized.");
        if (newteraClient.Config.Provider is null &&
            (string.IsNullOrEmpty(newteraClient.Config.AccessKey) || string.IsNullOrEmpty(newteraClient.Config.SecretKey)))
            throw new NewteraException("User Access Credentials not initialized.");

        var host = newteraClient.Config.BaseUrl;

        var scheme = newteraClient.Config.Secure ? Utils.UrlEncode("https") : Utils.UrlEncode("http");

        if (!newteraClient.Config.BaseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            newteraClient.Config.Endpoint = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", scheme, host);
        else
            newteraClient.Config.Endpoint = host;

        var httpClientHandler = new HttpClientHandler { Proxy = newteraClient.Config.Proxy };
        newteraClient.Config.HttpClient ??= newteraClient.Config.Proxy is null
            ? new HttpClient()
            : new HttpClient(httpClientHandler);
        _ = newteraClient.Config.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            newteraClient.Config.FullUserAgent);
        newteraClient.Config.HttpClient.Timeout = TimeSpan.FromMinutes(30);
        return newteraClient;
    }

    /// <summary>
    ///     Sets app version and name. Used for constructing User-Agent header in all HTTP requests
    /// </summary>
    /// <param name="newteraClient"></param>
    /// <param name="appName"></param>
    /// <param name="appVersion"></param>
    public static INewteraClient SetAppInfo(this INewteraClient newteraClient, string appName, string appVersion)
    {
        if (string.IsNullOrEmpty(appName))
            throw new ArgumentException("Appname cannot be null or empty", nameof(appName));

        if (string.IsNullOrEmpty(appVersion))
            throw new ArgumentException("Appversion cannot be null or empty", nameof(appVersion));

        newteraClient.Config.CustomUserAgent = $"{appName}/{appVersion}";

        return newteraClient;
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

    internal static void SetBaseURL(this INewteraClient newteraClient, Uri url)
    {
        if (url.IsDefaultPort)
            newteraClient.Config.BaseUrl = url.Host;
        else
            newteraClient.Config.BaseUrl = url.Host + ":" + url.Port;
    }
}
