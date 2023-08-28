using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Minio.Credentials;
using Minio.Handlers;

namespace Minio;
public class MinioConfig
{
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
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string BaseUrl { get; set; }

    // Reconstructed endpoint with scheme and host.In the case of Amazon, this url
    // is the virtual style path or location based endpoint
    public string Endpoint { get; set; }
    public string SessionToken { get; set; }

    // Indicates if we are using HTTPS or not
    public bool Secure { get; set; }

    public string Region { get; set; }

    public int RequestTimeout { get; set; }

    public string CustomUserAgent { get; internal set; } = string.Empty;

    /// <summary>
    ///     Returns the User-Agent header for the request
    /// </summary>
    public string FullUserAgent => $"{MinioClientExtensions.SystemUserAgent} {CustomUserAgent}";
}
