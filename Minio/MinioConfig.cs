using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Minio.Credentials;
using Minio.Handlers;

namespace Minio;

public class MinioConfig
{
    internal ServiceProvider ServiceProvider { get; set; }

    // Cache holding bucket to region mapping for buckets seen so far.
    public BucketRegionCache RegionCache { get; internal set; }

    public IClientProvider Provider { get; internal set; }
    public HttpClient HttpClient { get; internal set; }
    public IWebProxy Proxy { get; internal set; }

    // Handler for task retry policy
    public IRetryPolicyHandler RetryPolicyHandler { get; internal set; }

    //TODO: Should be removed?
    // Corresponding URI for above endpoint
    public Uri Uri { get; internal set; }

    public bool DisposeHttpClient { get; internal set; } = true;

    // Save Credentials from user
    public string AccessKey { get; internal set; }
    public string SecretKey { get; internal set; }
    public string BaseUrl { get; internal set; }

    // Reconstructed endpoint with scheme and host.In the case of Amazon, this url
    // is the virtual style path or location based endpoint
    public string Endpoint { get; internal set; }
    public string SessionToken { get; internal set; }

    // Indicates if we are using HTTPS or not
    public bool Secure { get; internal set; }

    public string Region { get; internal set; }

    public int RequestTimeout { get; internal set; }

    // Enables HTTP tracing if set to true
    public bool TraceHttp { get; internal set; }

    public string CustomUserAgent { get; internal set; } = string.Empty;

    /// <summary>
    ///     Returns the User-Agent header for the request
    /// </summary>
    public string FullUserAgent => $"{MinioClientExtensions.SystemUserAgent} {CustomUserAgent}";
}
