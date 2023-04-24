using Minio.Credentials;
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
            throw new ArgumentException(string.Format("Port {0} is not a number between 1 and 65535", port),
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
            throw new ArgumentException(string.Format("{0} the region value can't be null or empty.", region),
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

    public static MinioClient Build(this MinioClient minioClient)
    {
        if (minioClient is null) throw new ArgumentNullException(nameof(minioClient));
        // Instantiate a region cache
        minioClient.regionCache = BucketRegionCache.Instance;
        if (string.IsNullOrEmpty(minioClient.BaseUrl)) throw new MinioException("Endpoint not initialized.");
        if (minioClient.Provider != null && minioClient.Provider.GetType() != typeof(ChainedProvider) &&
            minioClient.SessionToken == null)
            throw new MinioException("User Access Credentials Provider not initialized correctly.");
        if (minioClient.Provider == null &&
            (string.IsNullOrEmpty(minioClient.AccessKey) || string.IsNullOrEmpty(minioClient.SecretKey)))
            throw new MinioException("User Access Credentials not initialized.");

        var host = minioClient.BaseUrl;

        var scheme = minioClient.Secure ? Utils.UrlEncode("https") : Utils.UrlEncode("http");

        if (!minioClient.BaseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            minioClient.Endpoint = string.Format("{0}://{1}", scheme, host);
        else
            minioClient.Endpoint = host;

        minioClient.HttpClient ??= minioClient.Proxy is null
            ? new HttpClient()
            : new HttpClient(new HttpClientHandler { Proxy = minioClient.Proxy });
        minioClient.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", minioClient.FullUserAgent);
        return minioClient;
    }

    internal static Uri GetBaseUrl(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException(
                string.Format("{0} is the value of the endpoint. It can't be null or empty.", endpoint),
                nameof(endpoint));

        if (endpoint.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            endpoint = endpoint.Substring(0, endpoint.Length - 1);
        if (!BuilderUtil.IsValidHostnameOrIPAddress(endpoint))
            throw new InvalidEndpointException(string.Format("{0} is invalid hostname.", endpoint), "endpoint");
        string conn_url;
        if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            throw new InvalidEndpointException(
                string.Format("{0} the value of the endpoint has the scheme (http/https) in it.", endpoint),
                "endpoint");

        var enable_https = Environment.GetEnvironmentVariable("ENABLE_HTTPS");
        var scheme = enable_https?.Equals("1", StringComparison.OrdinalIgnoreCase) == true ? "https://" : "http://";
        conn_url = scheme + endpoint;
        var url = new Uri(conn_url);
        var hostnameOfUri = url.Authority;
        if (!string.IsNullOrEmpty(hostnameOfUri) && !BuilderUtil.IsValidHostnameOrIPAddress(hostnameOfUri))
            throw new InvalidEndpointException(string.Format("{0}, {1} is invalid hostname.", endpoint, hostnameOfUri),
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