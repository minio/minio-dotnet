using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Newtera.Credentials;
using Newtera.Handlers;

namespace Newtera;

public class NewteraConfig
{
    internal ServiceProvider ServiceProvider { get; set; }

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

    public int RequestTimeout { get; internal set; }

    // Enables HTTP tracing if set to true
    public bool TraceHttp { get; internal set; }
}
