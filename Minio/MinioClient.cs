/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017-2021 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Minio.Credentials;
using Minio.DataModel;
using Minio.DataModel.Tracing;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio;

public class InnerItemType
{
    public int sortOrder { get; set; }
    public string value { get; set; }
}

public partial class MinioClient : IMinioClient
{
    private const string RegistryAuthHeaderKey = "X-Registry-Auth";

    /// <summary>
    ///     Default error handling delegate
    /// </summary>
    private readonly ApiResponseErrorHandlingDelegate _defaultErrorHandlingDelegate = response =>
    {
        if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.BadRequest)
            ParseError(response);
    };

    internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers =
        Enumerable.Empty<ApiResponseErrorHandlingDelegate>();

    private string CustomUserAgent = string.Empty;

    private bool disposeHttpClient = true;

    private IRequestLogger logger;

    internal ClientProvider Provider;
    internal string Region;

    // Cache holding bucket to region mapping for buckets seen so far.
    internal BucketRegionCache regionCache;

    private int requestTimeout;

    // Handler for task retry policy
    internal RetryPolicyHandlingDelegate retryPolicyHandler;

    // Enables HTTP tracing if set to true
    private bool trace;

    // Corresponding URI for above endpoint
    internal Uri uri;

    /// <summary>
    ///     Creates and returns an MinIO Client
    /// </summary>
    /// <returns>Client with no arguments to be used with other builder methods</returns>
    public MinioClient()
    {
        Region = "";
        SessionToken = "";
        Provider = null;
    }

    /// <summary>
    ///     Creates and returns an MinIO Client with custom HTTP Client
    /// </summary>
    /// <returns>Client with no arguments to be used with other builder methods</returns>
    [Obsolete("Use MinioClient() and Builder method .WithHttpClient(httpClient)")]
    public MinioClient(HttpClient httpClient)
    {
        Region = "";
        SessionToken = "";
        Provider = null;
        HTTPClient = httpClient;
    }

    /// <summary>
    ///     Creates and returns a MinIO Client
    /// </summary>
    /// <param name="endpoint">Location of the server, supports HTTP and HTTPS</param>
    /// <param name="accessKey">Access Key for authenticated requests (Optional, can be omitted for anonymous requests)</param>
    /// <param name="secretKey">Secret Key for authenticated requests (Optional, can be omitted for anonymous requests)</param>
    /// <param name="region">Optional custom region</param>
    /// <param name="sessionToken">Optional session token</param>
    /// <returns>Client initialized with user credentials</returns>
    [Obsolete("Use appropriate Builder object and call Build() or BuildAsync()")]
    public MinioClient(string endpoint, string accessKey = "",
        string secretKey = "", string region = "", string sessionToken = "")
    {
        Secure = false;

        // Save user entered credentials
        BaseUrl = endpoint;
        AccessKey = accessKey;
        SecretKey = secretKey;
        SessionToken = sessionToken;
        Region = region;
        // Instantiate a region cache
        regionCache = BucketRegionCache.Instance;

        if (string.IsNullOrEmpty(BaseUrl)) throw new InvalidEndpointException("Endpoint cannot be empty.");

        var host = BaseUrl;
        var scheme = Secure ? utils.UrlEncode("https") : utils.UrlEncode("http");
        // This is the actual url pointed to for all HTTP requests
        Endpoint = string.Format("{0}://{1}", scheme, host);
        uri = RequestUtil.GetEndpointURL(BaseUrl, Secure);
        RequestUtil.ValidateEndpoint(uri, Endpoint);

        HTTPClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", FullUserAgent);
    }

    // Save Credentials from user
    internal string AccessKey { get; private set; }
    internal string SecretKey { get; private set; }
    internal string BaseUrl { get; private set; }

    // Reconstructed endpoint with scheme and host.In the case of Amazon, this url
    // is the virtual style path or location based endpoint
    internal string Endpoint { get; private set; }
    internal string SessionToken { get; private set; }

    // Indicates if we are using HTTPS or not
    internal bool Secure { get; private set; }

    internal HttpClient HTTPClient { get; private set; }

    private static string SystemUserAgent
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

    /// <summary>
    ///     Returns the User-Agent header for the request
    /// </summary>
    private string FullUserAgent => $"{SystemUserAgent} {CustomUserAgent}";

    /// <summary>
    ///     Runs httpClient's GetAsync method
    /// </summary>
    public async Task<HttpResponseMessage> WrapperGetAsync(string url)
    {
        var response = await HTTPClient.GetAsync(url).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    ///     Runs httpClient's PutObjectAsync method
    /// </summary>
    public async Task WrapperPutAsync(string url, StreamContent strm)
    {
        await Task.Run(async () => await HTTPClient.PutAsync(url, strm).ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <summary>
    ///     Sets app version and name. Used for constructing User-Agent header in all HTTP requests
    /// </summary>
    /// <param name="appName"></param>
    /// <param name="appVersion"></param>
    public void SetAppInfo(string appName, string appVersion)
    {
        if (string.IsNullOrEmpty(appName))
            throw new ArgumentException("Appname cannot be null or empty", nameof(appName));

        if (string.IsNullOrEmpty(appVersion))
            throw new ArgumentException("Appversion cannot be null or empty", nameof(appVersion));

        CustomUserAgent = $"{appName}/{appVersion}";
    }

    /// <summary>
    ///     Sets HTTP tracing On.Writes output to Console
    /// </summary>
    public void SetTraceOn(IRequestLogger logger = null)
    {
        this.logger = logger ?? new DefaultRequestLogger();
        trace = true;
    }

    /// <summary>
    ///     Sets HTTP tracing Off.
    /// </summary>
    public void SetTraceOff()
    {
        trace = false;
    }

    public void Dispose()
    {
        if (disposeHttpClient) HTTPClient?.Dispose();
    }

    /// <summary>
    ///     Resolve region of the bucket.
    /// </summary>
    /// <param name="bucketName"></param>
    /// <returns></returns>
    private async Task<string> GetRegion(string bucketName)
    {
        var rgn = "";
        // Use user specified region in client constructor if present
        if (Region != string.Empty) return Region;

        // pick region from endpoint if present
        if (!string.IsNullOrEmpty(Endpoint))
            rgn = Regions.GetRegionFromEndpoint(Endpoint);

        // Pick region from location HEAD request
        if (rgn == string.Empty)
        {
            if (!BucketRegionCache.Instance.Exists(bucketName))
                rgn = await BucketRegionCache.Instance.Update(this, bucketName).ConfigureAwait(false);
            else
                rgn = BucketRegionCache.Instance.Region(bucketName);
        }

        // Defaults to us-east-1 if region could not be found
        return rgn == string.Empty ? "us-east-1" : rgn;
    }

    /// <summary>
    ///     Null Check for Args object.
    ///     Expected to be called from CreateRequest
    /// </summary>
    /// <param name="args">The child object of Args class</param>
    private void ArgsCheck(Args args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args),
                "Args object cannot be null. It needs to be assigned to an instantiated child object of Args.");
    }

    /// <summary>
    ///     Constructs a HttpRequestMessageBuilder using bucket/object names from Args.
    ///     Calls overloaded CreateRequest method.
    /// </summary>
    /// <param name="args">The direct descendant of BucketArgs class, args with populated values from Input</param>
    /// <returns>A HttpRequestMessageBuilder</returns>
    internal async Task<HttpRequestMessageBuilder> CreateRequest<T>(BucketArgs<T> args) where T : BucketArgs<T>
    {
        ArgsCheck(args);
        var requestMessageBuilder =
            await CreateRequest(args.RequestMethod, args.BucketName, headerMap: args.Headers,
                isBucketCreationRequest: args.IsBucketCreationRequest).ConfigureAwait(false);
        return args.BuildRequest(requestMessageBuilder);
    }

    /// <summary>
    ///     Constructs a HttpRequestMessage using bucket/object names from Args.
    ///     Calls overloaded CreateRequest method.
    /// </summary>
    /// <param name="args">The direct descendant of ObjectArgs class, args with populated values from Input</param>
    /// <returns>A HttpRequestMessage</returns>
    internal async Task<HttpRequestMessageBuilder> CreateRequest<T>(ObjectArgs<T> args) where T : ObjectArgs<T>
    {
        ArgsCheck(args);

        var contentType = "application/octet-stream";
        args.Headers?.TryGetValue("Content-Type", out contentType);
        var requestMessageBuilder =
            await CreateRequest(args.RequestMethod,
                args.BucketName,
                args.ObjectName,
                args.Headers,
                contentType,
                args.RequestBody).ConfigureAwait(false);
        return args.BuildRequest(requestMessageBuilder);
    }

    /// <summary>
    ///     Constructs an HttpRequestMessage builder. For AWS, this function
    ///     has the side-effect of overriding the baseUrl in the HttpClient
    ///     with region specific host path or virtual style path.
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="bucketName">Bucket Name</param>
    /// <param name="objectName">Object Name</param>
    /// <param name="headerMap">headerMap</param>
    /// <param name="contentType">Content Type</param>
    /// <param name="body">request body</param>
    /// <param name="resourcePath">query string</param>
    /// <param name="isBucketCreationRequest">boolean to define bucket creation</param>
    /// <returns>A HttpRequestMessage builder</returns>
    /// <exception cref="BucketNotFoundException">When bucketName is invalid</exception>
    internal async Task<HttpRequestMessageBuilder> CreateRequest(
        HttpMethod method,
        string bucketName = null,
        string objectName = null,
        Dictionary<string, string> headerMap = null,
        string contentType = "application/octet-stream",
        byte[] body = null,
        string resourcePath = null,
        bool isBucketCreationRequest = false)
    {
        var region = string.Empty;
        if (bucketName != null)
        {
            utils.ValidateBucketName(bucketName);
            // Fetch correct region for bucket if this is not a bucket creation
            if (!isBucketCreationRequest)
                region = await GetRegion(bucketName).ConfigureAwait(false);
        }

        if (objectName != null) utils.ValidateObjectName(objectName);

        if (Provider != null)
        {
            var isAWSEnvProvider = Provider is AWSEnvironmentProvider ||
                                   (Provider is ChainedProvider ch &&
                                    ch.CurrentProvider is AWSEnvironmentProvider);

            var isIAMAWSProvider = Provider is IAMAWSProvider ||
                                   (Provider is ChainedProvider chained &&
                                    chained.CurrentProvider is AWSEnvironmentProvider);

            AccessCredentials creds = null;

            if (isAWSEnvProvider)
            {
                var aWSEnvProvider = (AWSEnvironmentProvider)Provider;
                creds = await aWSEnvProvider.GetCredentialsAsync();
            }
            else if (isIAMAWSProvider)
            {
                var iamAWSProvider = (IAMAWSProvider)Provider;
                creds = iamAWSProvider.Credentials;
            }
            else
            {
                creds = await Provider.GetCredentialsAsync();
            }

            if (creds != null)
            {
                AccessKey = creds.AccessKey;
                SecretKey = creds.SecretKey;
            }
        }

        // This section reconstructs the url with scheme followed by location specific endpoint (s3.region.amazonaws.com)
        // or Virtual Host styled endpoint (bucketname.s3.region.amazonaws.com) for Amazon requests.
        var resource = string.Empty;
        var usePathStyle = false;

        if (bucketName != null)
            if (s3utils.IsAmazonEndPoint(BaseUrl))
            {
                if (method == HttpMethod.Put && objectName == null && resourcePath == null)
                    // use path style for make bucket to workaround "AuthorizationHeaderMalformed" error from s3.amazonaws.com
                    usePathStyle = true;
                else if (resourcePath != null && resourcePath.Contains("location"))
                    // use path style for location query
                    usePathStyle = true;
                else if (bucketName != null && bucketName.Contains(".") && Secure)
                    // use path style where '.' in bucketName causes SSL certificate validation error
                    usePathStyle = true;

                if (usePathStyle) resource += utils.UrlEncode(bucketName) + "/";
            }

        // Set Target URL
        var requestUrl = RequestUtil.MakeTargetURL(BaseUrl, Secure, bucketName, region, usePathStyle);

        if (objectName != null) resource += utils.EncodePath(objectName);

        // Append query string passed in
        if (resourcePath != null) resource += resourcePath;


        HttpRequestMessageBuilder messageBuilder;
        if (!string.IsNullOrEmpty(resource))
            messageBuilder = new HttpRequestMessageBuilder(method, requestUrl, resource);
        else
            messageBuilder = new HttpRequestMessageBuilder(method, requestUrl);
        if (body != null)
        {
            messageBuilder.SetBody(body);
            messageBuilder.AddOrUpdateHeaderParameter("Content-Type", contentType);
        }

        if (headerMap != null)
        {
            if (headerMap.ContainsKey(messageBuilder.ContentTypeKey) &&
                !string.IsNullOrEmpty(headerMap[messageBuilder.ContentTypeKey]))
                headerMap[messageBuilder.ContentTypeKey] = contentType;
            foreach (var entry in headerMap) messageBuilder.AddOrUpdateHeaderParameter(entry.Key, entry.Value);
        }

        return messageBuilder;
    }

    /// <summary>
    ///     Connects to Cloud Storage with HTTPS if this method is invoked on client object
    /// </summary>
    /// <returns></returns>
    public MinioClient WithSSL(bool secure = true)
    {
        if (secure)
        {
            Secure = true;
            if (string.IsNullOrEmpty(BaseUrl)) return this;
            var secureUrl = RequestUtil.MakeTargetURL(BaseUrl, Secure);
        }

        return this;
    }

    /// <summary>
    ///     Uses webproxy for all requests if this method is invoked on client object.
    /// </summary>
    /// <remarks>
    ///     This setting will be ignored when injecting an external <see cref="HttpClient" /> instance with
    ///     <see cref="MinioClient(HttpClient)" /> <see cref="WithHttpClient(HttpClient, bool)" />.
    /// </remarks>
    /// <returns></returns>
    public MinioClient WithProxy(IWebProxy proxy)
    {
        Proxy = proxy;
        return this;
    }

    /// <summary>
    ///     Uses the set timeout for all requests if this method is invoked on client object
    /// </summary>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <returns></returns>
    public MinioClient WithTimeout(int timeout)
    {
        requestTimeout = timeout;
        return this;
    }

    /// <summary>
    ///     Allows to add retry policy handler
    /// </summary>
    /// <param name="retryPolicyHandler">Delegate that will wrap execution of http client requests.</param>
    /// <returns></returns>
    public MinioClient WithRetryPolicy(RetryPolicyHandlingDelegate retryPolicyHandler)
    {
        this.retryPolicyHandler = retryPolicyHandler;
        return this;
    }

    /// <summary>
    ///     Allows end user to define the Http server and pass it as a parameter
    /// </summary>
    /// <param name="httpClient"> Instance of HttpClient</param>
    /// <param name="disposeHttpClient"> Dispose the HttpClient when leaving</param>
    /// <returns></returns>
    public MinioClient WithHttpClient(HttpClient httpClient, bool disposeHttpClient = false)
    {
        if (httpClient != null) HTTPClient = httpClient;
        this.disposeHttpClient = disposeHttpClient;
        return this;
    }

    /// <summary>
    ///     With provider for credentials and session token if being used
    /// </summary>
    /// <returns></returns>
    public MinioClient WithCredentialsProvider(ClientProvider provider)
    {
        Provider = provider;
        AccessCredentials credentials = null;
        if (Provider is IAMAWSProvider iAMAWSProvider)
            // Empty object, we need the Minio client completely
            credentials = new AccessCredentials();
        else
            credentials = Provider.GetCredentials();

        if (credentials == null)
            // Unable to fetch credentials.
            return this;

        AccessKey = credentials.AccessKey;
        SecretKey = credentials.SecretKey;
        var isSessionTokenAvailable = !string.IsNullOrEmpty(credentials.SessionToken);
        if ((Provider is AWSEnvironmentProvider ||
             Provider is IAMAWSProvider ||
             Provider is CertificateIdentityProvider ||
             (Provider is ChainedProvider chainedProvider && chainedProvider.CurrentProvider is AWSEnvironmentProvider))
            && isSessionTokenAvailable)
            SessionToken = credentials.SessionToken;
        return this;
    }

    /// <summary>
    ///     Actual doer that executes the request on the server
    /// </summary>
    /// <param name="errorHandlers">List of handlers to override default handling</param>
    /// <param name="requestMessageBuilder">The build of HttpRequestMessageBuilder </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>ResponseResult</returns>
    internal Task<ResponseResult> ExecuteTaskAsync(
        IEnumerable<ApiResponseErrorHandlingDelegate> errorHandlers,
        HttpRequestMessageBuilder requestMessageBuilder,
        CancellationToken cancellationToken = default,
        bool isSts = false)
    {
        if (requestTimeout > 0)
        {
            var internalTokenSource = new CancellationTokenSource(new TimeSpan(0, 0, 0, 0, requestTimeout));
            var timeoutTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(internalTokenSource.Token, cancellationToken);
            cancellationToken = timeoutTokenSource.Token;
        }

        return ExecuteWithRetry(
            () => ExecuteTaskCoreAsync(errorHandlers, requestMessageBuilder,
                cancellationToken, isSts));
    }

    private async Task<ResponseResult> ExecuteTaskCoreAsync(
        IEnumerable<ApiResponseErrorHandlingDelegate> errorHandlers,
        HttpRequestMessageBuilder requestMessageBuilder,
        CancellationToken cancellationToken = default,
        bool isSts = false)
    {
        var startTime = DateTime.Now;
        // Logs full url when HTTPtracing is enabled.
        if (trace)
        {
            var fullUrl = requestMessageBuilder.RequestUri;
        }

        var v4Authenticator = new V4Authenticator(Secure,
            AccessKey, SecretKey, Region,
            SessionToken);

        requestMessageBuilder.AddOrUpdateHeaderParameter("Authorization",
            v4Authenticator.Authenticate(requestMessageBuilder, isSts));

        var request = requestMessageBuilder.Request;

        ResponseResult responseResult;
        try
        {
            var response = await HTTPClient.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            responseResult = new ResponseResult(request, response);
            if (requestMessageBuilder.ResponseWriter != null)
                requestMessageBuilder.ResponseWriter(responseResult.ContentStream);
            if (requestMessageBuilder.FunctionResponseWriter != null)
                await requestMessageBuilder.FunctionResponseWriter(responseResult.ContentStream,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            responseResult = new ResponseResult(request, e);
        }

        HandleIfErrorResponse(responseResult, errorHandlers, startTime);
        return responseResult;
    }

    /// <summary>
    ///     Parse response errors if any and return relevant error messages
    /// </summary>
    /// <param name="response"></param>
    internal static void ParseError(ResponseResult response)
    {
        if (response == null)
            throw new ConnectionException(
                "Response is nil. Please report this issue https://github.com/minio/minio-dotnet/issues", response);

        if (HttpStatusCode.Redirect.Equals(response.StatusCode) ||
            HttpStatusCode.TemporaryRedirect.Equals(response.StatusCode) ||
            HttpStatusCode.MovedPermanently.Equals(response.StatusCode))
            throw new RedirectionException(
                "Redirection detected. Please report this issue https://github.com/minio/minio-dotnet/issues");

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            ParseErrorNoContent(response);
            return;
        }

        ParseErrorFromContent(response);
    }

    private static void ParseErrorNoContent(ResponseResult response)
    {
        if (HttpStatusCode.Forbidden.Equals(response.StatusCode)
            || HttpStatusCode.BadRequest.Equals(response.StatusCode)
            || HttpStatusCode.NotFound.Equals(response.StatusCode)
            || HttpStatusCode.MethodNotAllowed.Equals(response.StatusCode)
            || HttpStatusCode.NotImplemented.Equals(response.StatusCode))
            ParseWellKnownErrorNoContent(response);

        if (response.StatusCode == 0)
            throw new ConnectionException("Connection error:" + response.ErrorMessage, response);
        throw new InternalClientException(
            "Unsuccessful response from server without XML:" + response.ErrorMessage, response);
    }

    private static void ParseWellKnownErrorNoContent(ResponseResult response)
    {
        MinioException error = null;
        var errorResponse = new ErrorResponse();

        foreach (var parameter in response.Headers)
        {
            if (parameter.Key.Equals("x-amz-id-2", StringComparison.CurrentCultureIgnoreCase))
                errorResponse.HostId = parameter.Value;

            if (parameter.Key.Equals("x-amz-request-id", StringComparison.CurrentCultureIgnoreCase))
                errorResponse.RequestId = parameter.Value;

            if (parameter.Key.Equals("x-amz-bucket-region", StringComparison.CurrentCultureIgnoreCase))
                errorResponse.BucketRegion = parameter.Value;
        }

        var pathAndQuery = response.Request.RequestUri.PathAndQuery;
        var host = response.Request.RequestUri.Host;
        errorResponse.Resource = pathAndQuery;

        // zero, one or two segments
        var resourceSplits = pathAndQuery.Split(new[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);

        if (HttpStatusCode.NotFound.Equals(response.StatusCode))
        {
            var pathLength = resourceSplits.Length;
            var isAWS = host.EndsWith("s3.amazonaws.com");
            var isVirtual = isAWS && !host.StartsWith("s3.amazonaws.com");

            if (pathLength > 1)
            {
                var objectName = resourceSplits[1];
                errorResponse.Code = "NoSuchKey";
                error = new ObjectNotFoundException(objectName, "Not found.");
            }
            else if (pathLength == 1)
            {
                var resource = resourceSplits[0];

                if (isAWS && isVirtual && pathAndQuery != string.Empty)
                {
                    errorResponse.Code = "NoSuchKey";
                    error = new ObjectNotFoundException(resource, "Not found.");
                }
                else
                {
                    errorResponse.Code = "NoSuchBucket";
                    BucketRegionCache.Instance.Remove(resource);
                    error = new BucketNotFoundException(resource, "Not found.");
                }
            }
            else
            {
                error = new InternalClientException("404 without body resulted in path with less than two components",
                    response);
            }
        }
        else if (HttpStatusCode.BadRequest.Equals(response.StatusCode))
        {
            var pathLength = resourceSplits.Length;

            if (pathLength > 1)
            {
                var objectName = resourceSplits[1];
                errorResponse.Code = "InvalidObjectName";
                error = new InvalidObjectNameException(objectName, "Invalid object name.");
            }
            else
            {
                error = new InternalClientException("400 without body resulted in path with less than two components",
                    response);
            }
        }
        else if (HttpStatusCode.Forbidden.Equals(response.StatusCode))
        {
            errorResponse.Code = "Forbidden";
            error = new AccessDeniedException("Access denied on the resource: " + pathAndQuery);
        }

        error.Response = errorResponse;
        throw error;
    }

    private static void ParseErrorFromContent(ResponseResult response)
    {
        if (response.StatusCode.Equals(HttpStatusCode.NotFound)
            && response.Request.RequestUri.PathAndQuery.EndsWith("?location")
            && response.Request.Method.Equals(HttpMethod.Get))
        {
            var bucketName = response.Request.RequestUri.PathAndQuery.Split('?')[0];
            BucketRegionCache.Instance.Remove(bucketName);
            throw new BucketNotFoundException(bucketName, "Not found.");
        }

        var contentBytes = Encoding.UTF8.GetBytes(response.Content);
        var stream = new MemoryStream(contentBytes);
        var errResponse = (ErrorResponse)new XmlSerializer(typeof(ErrorResponse)).Deserialize(stream);

        if (response.StatusCode.Equals(HttpStatusCode.Forbidden)
            && (errResponse.Code.Equals("SignatureDoesNotMatch") || errResponse.Code.Equals("InvalidAccessKeyId")))
            throw new AuthorizationException(errResponse.Resource, errResponse.BucketName, errResponse.Message);

        // Handle XML response for Bucket Policy not found case
        if (response.StatusCode.Equals(HttpStatusCode.NotFound)
            && response.Request.RequestUri.PathAndQuery.EndsWith("?policy")
            && response.Request.Method.Equals(HttpMethod.Get)
            && errResponse.Code == "NoSuchBucketPolicy")
            throw new ErrorResponseException(errResponse, response)
            {
                XmlError = response.Content
            };

        if (response.StatusCode.Equals(HttpStatusCode.NotFound)
            && errResponse.Code == "NoSuchBucket")
            throw new BucketNotFoundException(errResponse.BucketName, "Not found.");

        if (response.StatusCode.Equals(HttpStatusCode.BadRequest)
            && errResponse.Code.Equals("MalformedXML"))
            throw new MalFormedXMLException(errResponse.Resource, errResponse.BucketName, errResponse.Message,
                errResponse.Key);

        if (response.StatusCode.Equals(HttpStatusCode.NotImplemented)
            && errResponse.Code.Equals("NotImplemented"))
            throw new NotImplementedException(errResponse.Message);

        if (response.StatusCode.Equals(HttpStatusCode.BadRequest)
            && errResponse.Code.Equals("InvalidRequest"))
        {
            var legalHold = new Dictionary<string, string> { { "legal-hold", "" } };
            if (response.Request.RequestUri.Query.Contains("legalHold"))
                throw new MissingObjectLockConfigurationException(errResponse.BucketName, errResponse.Message);
        }

        if (response.StatusCode.Equals(HttpStatusCode.NotFound)
            && errResponse.Code.Equals("ObjectLockConfigurationNotFoundError"))
            throw new MissingObjectLockConfigurationException(errResponse.BucketName, errResponse.Message);

        if (response.StatusCode.Equals(HttpStatusCode.NotFound)
            && errResponse.Code.Equals("ReplicationConfigurationNotFoundError"))
            throw new MissingBucketReplicationConfigurationException(errResponse.BucketName, errResponse.Message);

        if (response.StatusCode.Equals(HttpStatusCode.Conflict)
            && errResponse.Code.Equals("BucketAlreadyOwnedByYou"))
            throw new Exception("Bucket already owned by you: " + errResponse.BucketName);

        throw new UnexpectedMinioException(errResponse.Message)
        {
            Response = errResponse,
            XmlError = response.Content
        };
    }

    /// <summary>
    ///     Delegate errors to handlers
    /// </summary>
    /// <param name="response"></param>
    /// <param name="handlers"></param>
    /// <param name="startTime"></param>
    private void HandleIfErrorResponse(ResponseResult response, IEnumerable<ApiResponseErrorHandlingDelegate> handlers,
        DateTime startTime)
    {
        // Logs Response if HTTP tracing is enabled
        if (trace)
        {
            var now = DateTime.Now;
            LogRequest(response.Request, response, (now - startTime).TotalMilliseconds);
        }

        if (handlers == null) throw new ArgumentNullException(nameof(handlers));

        // Run through handlers passed to take up error handling
        foreach (var handler in handlers) handler(response);

        // Fall back default error handler
        _defaultErrorHandlingDelegate(response);
    }

    /// <summary>
    ///     Logs the request sent to server and corresponding response
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <param name="durationMs"></param>
    private void LogRequest(HttpRequestMessage request, ResponseResult response, double durationMs)
    {
        var requestToLog = new RequestToLog
        {
            resource = request.RequestUri.PathAndQuery,
            // Parameters are custom anonymous objects in order to have the parameter type as a nice string
            // otherwise it will just show the enum value
            parameters = request.Headers.Select(parameter => new RequestParameter
            {
                name = parameter.Key,
                value = parameter.Value,
                type = parameter.GetType().ToString()
            }),
            // ToString() here to have the method as a nice string otherwise it will just show the enum value
            method = request.Method.ToString(),
            // This will generate the actual Uri used in the request
            uri = request.RequestUri
        };

        var responseToLog = new ResponseToLog
        {
            statusCode = response.StatusCode,
            content = response.Content,
            headers = response.Headers.ToDictionary(o => o.Key, o => string.Join(Environment.NewLine, o.Value)),
            // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
            responseUri = response.Request.RequestUri,
            errorMessage = response.ErrorMessage,
            durationMs = durationMs
        };

        logger.LogRequest(requestToLog, responseToLog, durationMs);
    }

    private Task<ResponseResult> ExecuteWithRetry(
        Func<Task<ResponseResult>> executeRequestCallback)
    {
        return retryPolicyHandler == null
            ? executeRequestCallback()
            : retryPolicyHandler(executeRequestCallback);
    }
}

internal delegate void ApiResponseErrorHandlingDelegate(ResponseResult response);

public delegate Task<ResponseResult> RetryPolicyHandlingDelegate(
    Func<Task<ResponseResult>> executeRequestCallback);