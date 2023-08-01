using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using Minio.Credentials;
using Minio.DataModel;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio;

public static class MinioClientExtensions
{
    public static MinioClient WithEndpoint(this MinioClient minioClient, string endpoint)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.BaseUrl = endpoint;
        minioClient.SetBaseURL(GetBaseUrl(endpoint));
        return minioClient;
    }

    public static MinioClient WithEndpoint(this MinioClient minioClient, string endpoint, int port)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        if (port < 1 || port > 65535)
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "Port {0} is not a number between 1 and 65535", port),
                nameof(port));
        return minioClient.WithEndpoint(endpoint + ":" + port);
    }

    public static MinioClient WithEndpoint(this MinioClient minioClient, Uri url)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        if (url is null) throw new ArgumentNullException(nameof(url));

        return minioClient.WithEndpoint(url.AbsoluteUri);
    }

    public static MinioClient WithRegion(this MinioClient minioClient, string region)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        if (string.IsNullOrEmpty(region))
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "{0} the region value can't be null or empty.", region),
                nameof(region));

        minioClient.Region = region;
        return minioClient;
    }

    public static MinioClient WithRegion(this MinioClient minioClient)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));
        // Set region to its default value if empty or null
        minioClient.Region = "us-east-1";
        return minioClient;
    }

    public static MinioClient WithCredentials(this MinioClient minioClient, string accessKey, string secretKey)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.AccessKey = accessKey;
        minioClient.SecretKey = secretKey;
        return minioClient;
    }

    public static MinioClient WithSessionToken(this MinioClient minioClient, string st)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.SessionToken = st;
        return minioClient;
    }

    /// <summary>
    ///     Connects to Cloud Storage with HTTPS if this method is invoked on client object
    /// </summary>
    /// <returns></returns>
    public static MinioClient WithSSL(this MinioClient minioClient, bool secure = true)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        if (secure)
        {
            minioClient.Secure = true;
            if (string.IsNullOrEmpty(minioClient.BaseUrl))
                return minioClient;
            var secureUrl = RequestUtil.MakeTargetURL(minioClient.BaseUrl, minioClient.Secure);
        }

        return minioClient;
    }

    /// <summary>
    ///     Uses webproxy for all requests if this method is invoked on client object.
    /// </summary>
    /// <param name="minioClient">The MinioClient instance used</param>
    /// <param name="proxy">Information on the proxy server in the setup.</param>
    /// <returns></returns>
    public static MinioClient WithProxy(this MinioClient minioClient, IWebProxy proxy)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.Proxy = proxy;
        return minioClient;
    }

    /// <summary>
    ///     Uses the set timeout for all requests if this method is invoked on client object
    /// </summary>
    /// <param name="minioClient">The MinioClient instance used</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <returns></returns>
    public static MinioClient WithTimeout(this MinioClient minioClient, int timeout)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.RequestTimeout = timeout;
        return minioClient;
    }

    /// <summary>
    ///     Allows to add retry policy handler
    /// </summary>
    /// <param name="minioClient">The MinioClient instance used</param>
    /// <param name="retryPolicyHandler">Delegate that will wrap execution of http client requests.</param>
    /// <returns></returns>
    public static MinioClient WithRetryPolicy(this MinioClient minioClient,
        RetryPolicyHandler retryPolicyHandler)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.RetryPolicyHandler = retryPolicyHandler;
        return minioClient;
    }

    /// <summary>
    ///     Allows end user to define the Http server and pass it as a parameter
    /// </summary>
    /// <param name="minioClient">The MinioClient instance used</param>
    /// <param name="httpClient"> Instance of HttpClient</param>
    /// <param name="disposeHttpClient"> Dispose the HttpClient when leaving</param>
    /// <returns></returns>
    public static MinioClient WithHttpClient(this MinioClient minioClient, HttpClient httpClient,
        bool disposeHttpClient = false)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        if (httpClient is not null) minioClient.HttpClient = httpClient;
        minioClient.DisposeHttpClient = disposeHttpClient;
        return minioClient;
    }

    /// <summary>
    ///     With provider for credentials and session token if being used
    /// </summary>
    /// <returns></returns>
    public static MinioClient WithCredentialsProvider(this MinioClient minioClient, IClientProvider provider)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        minioClient.Provider = provider;
        AccessCredentials credentials;
        if (minioClient.Provider is IAMAWSProvider)
            // Empty object, we need the Minio client completely
            credentials = new AccessCredentials();
        else
            credentials = minioClient.Provider.GetCredentials();

        if (credentials is null)
            // Unable to fetch credentials.
            return minioClient;

        minioClient.AccessKey = credentials.AccessKey;
        minioClient.SecretKey = credentials.SecretKey;
        var isSessionTokenAvailable = !string.IsNullOrEmpty(credentials.SessionToken);
        if ((minioClient.Provider is AWSEnvironmentProvider ||
             minioClient.Provider is IAMAWSProvider ||
             minioClient.Provider is CertificateIdentityProvider ||
             (minioClient.Provider is ChainedProvider chainedProvider &&
              chainedProvider.CurrentProvider is AWSEnvironmentProvider))
            && isSessionTokenAvailable)
            minioClient.SessionToken = credentials.SessionToken;

        return minioClient;
    }

    public static MinioClient Build(this MinioClient minioClient)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        // Instantiate a region cache
        minioClient.regionCache = BucketRegionCache.Instance;
        if (string.IsNullOrEmpty(minioClient.BaseUrl)) throw new MinioException("Endpoint not initialized.");
        if (minioClient.Provider is not null && minioClient.Provider.GetType() != typeof(ChainedProvider) &&
            minioClient.SessionToken is null)
            throw new MinioException("User Access Credentials Provider not initialized correctly.");
        if (minioClient.Provider is null &&
            (string.IsNullOrEmpty(minioClient.AccessKey) || string.IsNullOrEmpty(minioClient.SecretKey)))
            throw new MinioException("User Access Credentials not initialized.");

        var host = minioClient.BaseUrl;

        var scheme = minioClient.Secure ? Utils.UrlEncode("https") : Utils.UrlEncode("http");

        if (!minioClient.BaseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            minioClient.Endpoint = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", scheme, host);
        else
            minioClient.Endpoint = host;

        var httpClientHandler = new HttpClientHandler { Proxy = minioClient.Proxy };
        minioClient.HttpClient ??= minioClient.Proxy is null
            ? new HttpClient()
            : new HttpClient(httpClientHandler);
        minioClient.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", minioClient.FullUserAgent);
        minioClient.HttpClient.Timeout = TimeSpan.FromMinutes(30);
        return minioClient;
    }

    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
        Justification = "This is done in the interface. String is provided here for convenience")]
    public static Task<HttpResponseMessage> WrapperGetAsync(this IMinioClient minioClient, string url)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        return minioClient.WrapperGetAsync(new Uri(url));
    }

    /// <summary>
    ///     Runs httpClient's PutObjectAsync method
    /// </summary>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
        Justification = "This is done in the interface. String is provided here for convenience")]
    public static Task WrapperPutAsync(this IMinioClient minioClient, string url, StreamContent strm)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));

        return minioClient.WrapperPutAsync(new Uri(url), strm);
    }

    internal static Uri GetBaseUrl(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture,
                    "{0} is the value of the endpoint. It can't be null or empty.", endpoint),
                nameof(endpoint));

        if (endpoint.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            endpoint = endpoint.Substring(0, endpoint.Length - 1);
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

    internal static void SetBaseURL(this MinioClient minioClient, Uri url)
    {
        if (url.IsDefaultPort)
            minioClient.BaseUrl = url.Host;
        else
            minioClient.BaseUrl = url.Host + ":" + url.Port;
    }
}
