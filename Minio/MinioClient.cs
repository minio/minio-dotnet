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

using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Minio.Credentials;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Tracing;
using Minio.Exceptions;
using Minio.Helper;

namespace Minio;

public partial class MinioClient : IMinioClient
{
    private static readonly char[] separator = { '/' };

    internal readonly IEnumerable<IApiResponseErrorHandler> NoErrorHandlers =
        Enumerable.Empty<IApiResponseErrorHandler>();

    private string customUserAgent = string.Empty;
    private bool disposedValue;

    internal bool DisposeHttpClient = true;

    private IRequestLogger logger;

    internal IClientProvider Provider;
    internal string Region;

    // Cache holding bucket to region mapping for buckets seen so far.
    internal BucketRegionCache regionCache;

    internal int RequestTimeout;

    // Handler for task retry policy
    internal IRetryPolicyHandler RetryPolicyHandler;

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
    ///     Default error handling delegate
    /// </summary>
    private IApiResponseErrorHandler DefaultErrorHandlingDelegate { get; } = new DefaultErrorHandler();

    // Save Credentials from user
    internal string AccessKey { get; set; }
    internal string SecretKey { get; set; }
    internal string BaseUrl { get; set; }

    // Reconstructed endpoint with scheme and host.In the case of Amazon, this url
    // is the virtual style path or location based endpoint
    internal string Endpoint { get; set; }
    internal string SessionToken { get; set; }

    // Indicates if we are using HTTPS or not
    internal bool Secure { get; set; }

    internal HttpClient HttpClient { get; set; }

    internal IWebProxy Proxy { get; set; }

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
    internal string FullUserAgent => $"{SystemUserAgent} {customUserAgent}";

    /// <summary>
    ///     Runs httpClient's GetAsync method
    /// </summary>
    public Task<HttpResponseMessage> WrapperGetAsync(Uri uri)
    {
        return HttpClient.GetAsync(uri);
    }

    /// <summary>
    ///     Runs httpClient's PutObjectAsync method
    /// </summary>
    public Task WrapperPutAsync(Uri uri, StreamContent strm)
    {
        return Task.Run(async () => await HttpClient.PutAsync(uri, strm).ConfigureAwait(false));
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

        customUserAgent = $"{appName}/{appVersion}";
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
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
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
        if (!string.IsNullOrEmpty(Region)) return Region;

        // pick region from endpoint if present
        if (!string.IsNullOrEmpty(Endpoint))
            rgn = Regions.GetRegionFromEndpoint(Endpoint);

        // Pick region from location HEAD request
        if (rgn?.Length == 0)
        {
            if (!BucketRegionCache.Instance.Exists(bucketName))
                rgn = await BucketRegionCache.Update(this, bucketName).ConfigureAwait(false);
            else
                rgn = BucketRegionCache.Instance.Region(bucketName);
        }

        // Defaults to us-east-1 if region could not be found
        return rgn?.Length == 0 ? "us-east-1" : rgn;
    }

    /// <summary>
    ///     Null Check for Args object.
    ///     Expected to be called from CreateRequest
    /// </summary>
    /// <param name="args">The child object of Args class</param>
    private void ArgsCheck(RequestArgs args)
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
        _ = args.Headers?.TryGetValue("Content-Type", out contentType);
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
        IDictionary<string, string> headerMap = null,
        string contentType = "application/octet-stream",
        ReadOnlyMemory<byte> body = default,
        string resourcePath = null,
        bool isBucketCreationRequest = false)
    {
        var region = string.Empty;
        if (bucketName is not null)
        {
            Utils.ValidateBucketName(bucketName);
            // Fetch correct region for bucket if this is not a bucket creation
            if (!isBucketCreationRequest)
                region = await GetRegion(bucketName).ConfigureAwait(false);
        }

        if (objectName is not null) Utils.ValidateObjectName(objectName);

        if (Provider is not null)
        {
            var isAWSEnvProvider = Provider is AWSEnvironmentProvider ||
                                   (Provider is ChainedProvider ch &&
                                    ch.CurrentProvider is AWSEnvironmentProvider);

            var isIAMAWSProvider = Provider is IAMAWSProvider ||
                                   (Provider is ChainedProvider chained &&
                                    chained.CurrentProvider is AWSEnvironmentProvider);

            AccessCredentials creds;
            if (isAWSEnvProvider)
            {
                var aWSEnvProvider = (AWSEnvironmentProvider)Provider;
                creds = await aWSEnvProvider.GetCredentialsAsync().ConfigureAwait(false);
            }
            else if (isIAMAWSProvider)
            {
                var iamAWSProvider = (IAMAWSProvider)Provider;
                creds = iamAWSProvider.Credentials;
            }
            else
            {
                creds = await Provider.GetCredentialsAsync().ConfigureAwait(false);
            }

            if (creds is not null)
            {
                AccessKey = creds.AccessKey;
                SecretKey = creds.SecretKey;
            }
        }

        // This section reconstructs the url with scheme followed by location specific endpoint (s3.region.amazonaws.com)
        // or Virtual Host styled endpoint (bucketname.s3.region.amazonaws.com) for Amazon requests.
        var resource = string.Empty;
        var usePathStyle = false;

        if (!string.IsNullOrEmpty(bucketName) && S3utils.IsAmazonEndPoint(BaseUrl))
        {
            if (method == HttpMethod.Put && objectName is null && resourcePath is null)
                // use path style for make bucket to workaround "AuthorizationHeaderMalformed" error from s3.amazonaws.com
                usePathStyle = true;
            else if (resourcePath?.Contains("location") == true)
                // use path style for location query
                usePathStyle = true;
            else if (bucketName.Contains('.', StringComparison.Ordinal) && Secure)
                // use path style where '.' in bucketName causes SSL certificate validation error
                usePathStyle = true;

            if (usePathStyle) resource += Utils.UrlEncode(bucketName) + "/";
        }

        // Set Target URL
        var requestUrl = RequestUtil.MakeTargetURL(BaseUrl, Secure, bucketName, region, usePathStyle);

        if (objectName is not null) resource += Utils.EncodePath(objectName);

        // Append query string passed in
        if (resourcePath is not null) resource += resourcePath;

        HttpRequestMessageBuilder messageBuilder;
        if (!string.IsNullOrEmpty(resource))
            messageBuilder = new HttpRequestMessageBuilder(method, requestUrl, resource);
        else
            messageBuilder = new HttpRequestMessageBuilder(method, requestUrl);
        if (!body.IsEmpty)
        {
            messageBuilder.SetBody(body);
            messageBuilder.AddOrUpdateHeaderParameter("Content-Type", contentType);
        }

        if (headerMap is not null)
        {
            if (headerMap.TryGetValue(messageBuilder.ContentTypeKey, out var value) && !string.IsNullOrEmpty(value))
                headerMap[messageBuilder.ContentTypeKey] = contentType;

            foreach (var entry in headerMap) messageBuilder.AddOrUpdateHeaderParameter(entry.Key, entry.Value);
        }

        return messageBuilder;
    }

    /// <summary>
    ///     Actual doer that executes the request on the server
    /// </summary>
    /// <param name="errorHandlers">List of handlers to override default handling</param>
    /// <param name="requestMessageBuilder">The build of HttpRequestMessageBuilder </param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>ResponseResult</returns>
    internal Task<ResponseResult> ExecuteTaskAsync(
        IEnumerable<IApiResponseErrorHandler> errorHandlers,
        HttpRequestMessageBuilder requestMessageBuilder,
        bool isSts = false,
        CancellationToken cancellationToken = default)
    {
        if (RequestTimeout > 0)
        {
            using var internalTokenSource = new CancellationTokenSource(new TimeSpan(0, 0, 0, 0, RequestTimeout));
            using var timeoutTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(internalTokenSource.Token, cancellationToken);
            cancellationToken = timeoutTokenSource.Token;
        }

        return ExecuteWithRetry(
            () => ExecuteTaskCoreAsync(errorHandlers, requestMessageBuilder,
                isSts, cancellationToken));
    }

    private async Task<ResponseResult> ExecuteTaskCoreAsync(
        IEnumerable<IApiResponseErrorHandler> errorHandlers,
        HttpRequestMessageBuilder requestMessageBuilder,
        bool isSts = false,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        var v4Authenticator = new V4Authenticator(Secure,
            AccessKey, SecretKey, Region,
            SessionToken);

        requestMessageBuilder.AddOrUpdateHeaderParameter("Authorization",
            v4Authenticator.Authenticate(requestMessageBuilder, isSts));

        var request = requestMessageBuilder.Request;

        ResponseResult responseResult = null;
        try
        {
            var response = await HttpClient.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            responseResult = new ResponseResult(request, response);
            if (requestMessageBuilder.ResponseWriter is not null)
                requestMessageBuilder.ResponseWriter(responseResult.ContentStream);
            if (requestMessageBuilder.FunctionResponseWriter is not null)
                await requestMessageBuilder.FunctionResponseWriter(responseResult.ContentStream, cancellationToken)
                    .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            responseResult?.Dispose();
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
        if (response is null)
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

#pragma warning disable MA0099 // Use Explicit enum value instead of 0
        if (response.StatusCode == 0)
            throw new ConnectionException("Connection error:" + response.ErrorMessage, response);
#pragma warning restore MA0099 // Use Explicit enum value instead of 0
        throw new InternalClientException(
            "Unsuccessful response from server without XML:" + response.ErrorMessage, response);
    }

    private static void ParseWellKnownErrorNoContent(ResponseResult response)
    {
        MinioException error = null;
        var errorResponse = new ErrorResponse();

        foreach (var parameter in response.Headers)
        {
            if (parameter.Key.Equals("x-amz-id-2", StringComparison.OrdinalIgnoreCase))
                errorResponse.HostId = parameter.Value;

            if (parameter.Key.Equals("x-amz-request-id", StringComparison.OrdinalIgnoreCase))
                errorResponse.RequestId = parameter.Value;

            if (parameter.Key.Equals("x-amz-bucket-region", StringComparison.OrdinalIgnoreCase))
                errorResponse.BucketRegion = parameter.Value;
        }

        var pathAndQuery = response.Request.RequestUri.PathAndQuery;
        var host = response.Request.RequestUri.Host;
        errorResponse.Resource = pathAndQuery;

        // zero, one or two segments
        var resourceSplits = pathAndQuery.Split(separator, 2, StringSplitOptions.RemoveEmptyEntries);

        if (HttpStatusCode.NotFound.Equals(response.StatusCode))
        {
            var pathLength = resourceSplits.Length;
            var isAWS = host.EndsWith("s3.amazonaws.com", StringComparison.OrdinalIgnoreCase);
            var isVirtual = isAWS && !host.StartsWith("s3.amazonaws.com", StringComparison.OrdinalIgnoreCase);

            if (pathLength > 1)
            {
                var objectName = resourceSplits[1];
                errorResponse.Code = "NoSuchKey";
                error = new ObjectNotFoundException(objectName, "Not found.");
            }
            else if (pathLength == 1)
            {
                var resource = resourceSplits[0];

                if (isAWS && isVirtual && !string.IsNullOrEmpty(pathAndQuery))
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
            && response.Request.RequestUri.PathAndQuery.EndsWith("?location", StringComparison.OrdinalIgnoreCase)
            && response.Request.Method.Equals(HttpMethod.Get))
        {
            var bucketName = response.Request.RequestUri.PathAndQuery.Split('?')[0];
            BucketRegionCache.Instance.Remove(bucketName);
            throw new BucketNotFoundException(bucketName, "Not found.");
        }

        using var stream = Encoding.UTF8.GetBytes(response.Content).AsMemory().AsStream();
        var errResponse = Utils.DeserializeXml<ErrorResponse>(stream);

        if (response.StatusCode.Equals(HttpStatusCode.Forbidden)
            && (errResponse.Code.Equals("SignatureDoesNotMatch", StringComparison.OrdinalIgnoreCase) ||
                errResponse.Code.Equals("InvalidAccessKeyId", StringComparison.OrdinalIgnoreCase)))
            throw new AuthorizationException(errResponse.Resource, errResponse.BucketName, errResponse.Message);

        // Handle XML response for Bucket Policy not found case
        if (response.StatusCode.Equals(HttpStatusCode.NotFound)
            && response.Request.RequestUri.PathAndQuery.EndsWith("?policy", StringComparison.OrdinalIgnoreCase)
            && response.Request.Method.Equals(HttpMethod.Get)
            && string.Equals(errResponse.Code, "NoSuchBucketPolicy", StringComparison.OrdinalIgnoreCase))
            throw new ErrorResponseException(errResponse, response) { XmlError = response.Content };

        if (response.StatusCode.Equals(HttpStatusCode.NotFound)
            && string.Equals(errResponse.Code, "NoSuchBucket", StringComparison.OrdinalIgnoreCase))
            throw new BucketNotFoundException(errResponse.BucketName, "Not found.");

        if (response.StatusCode.Equals(HttpStatusCode.BadRequest)
            && errResponse.Code.Equals("MalformedXML", StringComparison.OrdinalIgnoreCase))
            throw new MalFormedXMLException(errResponse.Resource, errResponse.BucketName, errResponse.Message,
                errResponse.Key);

        if (response.StatusCode.Equals(HttpStatusCode.NotImplemented)
            && errResponse.Code.Equals("NotImplemented", StringComparison.OrdinalIgnoreCase))
#pragma warning disable MA0025 // Implement the functionality instead of throwing NotImplementedException
            throw new NotImplementedException(errResponse.Message);
#pragma warning restore MA0025 // Implement the functionality instead of throwing NotImplementedException

        if (response.StatusCode.Equals(HttpStatusCode.BadRequest)
            && errResponse.Code.Equals("InvalidRequest", StringComparison.OrdinalIgnoreCase))
        {
            var legalHold = new Dictionary<string, string>(StringComparer.Ordinal) { { "legal-hold", "" } };
            if (response.Request.RequestUri.Query.Contains("legalHold"))
                throw new MissingObjectLockConfigurationException(errResponse.BucketName, errResponse.Message);
        }

        if (response.StatusCode.Equals(HttpStatusCode.NotFound)
            && errResponse.Code.Equals("ObjectLockConfigurationNotFoundError", StringComparison.OrdinalIgnoreCase))
            throw new MissingObjectLockConfigurationException(errResponse.BucketName, errResponse.Message);

        if (response.StatusCode.Equals(HttpStatusCode.NotFound)
            && errResponse.Code.Equals("ReplicationConfigurationNotFoundError", StringComparison.OrdinalIgnoreCase))
            throw new MissingBucketReplicationConfigurationException(errResponse.BucketName, errResponse.Message);

        if (response.StatusCode.Equals(HttpStatusCode.Conflict)
            && errResponse.Code.Equals("BucketAlreadyOwnedByYou", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Bucket already owned by you: " + errResponse.BucketName,
                nameof(response));

        throw new UnexpectedMinioException(errResponse.Message) { Response = errResponse, XmlError = response.Content };
    }

    /// <summary>
    ///     Delegate errors to handlers
    /// </summary>
    /// <param name="response"></param>
    /// <param name="handlers"></param>
    /// <param name="startTime"></param>
    private void HandleIfErrorResponse(ResponseResult response, IEnumerable<IApiResponseErrorHandler> handlers,
        DateTime startTime)
    {
        // Logs Response if HTTP tracing is enabled
        if (trace)
        {
            var now = DateTime.Now;
            LogRequest(response.Request, response, (now - startTime).TotalMilliseconds);
        }

        if (handlers is null) throw new ArgumentNullException(nameof(handlers));

        // Run through handlers passed to take up error handling
        foreach (var handler in handlers) handler.Handle(response);

        // Fall back default error handler
        DefaultErrorHandlingDelegate.Handle(response);
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
            Resource = request.RequestUri.PathAndQuery,
            // Parameters are custom anonymous objects in order to have the parameter type as a nice string
            // otherwise it will just show the enum value
            Parameters = request.Headers.Select(parameter => new RequestParameter
            {
                Name = parameter.Key,
                Value = parameter.Value,
                Type = typeof(KeyValuePair<string, IEnumerable<string>>).ToString()
            }),
            // ToString() here to have the method as a nice string otherwise it will just show the enum value
            Method = request.Method.ToString(),
            // This will generate the actual Uri used in the request
            Uri = request.RequestUri
        };

        var responseToLog = new ResponseToLog
        {
            StatusCode = response.StatusCode,
            Content = response.Content,
            Headers = response.Headers.ToDictionary(o => o.Key, o => string.Join(Environment.NewLine, o.Value),
                StringComparer.Ordinal),
            // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
            ResponseUri = response.Request.RequestUri,
            ErrorMessage = response.ErrorMessage,
            DurationMs = durationMs
        };

        logger.LogRequest(requestToLog, responseToLog, durationMs);
    }

    private Task<ResponseResult> ExecuteWithRetry(
        Func<Task<ResponseResult>> executeRequestCallback)
    {
        return RetryPolicyHandler is null
            ? executeRequestCallback()
            : RetryPolicyHandler.Handle(executeRequestCallback);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                if (DisposeHttpClient)
                    HttpClient?.Dispose();
            disposedValue = true;
        }
    }
}
