using System.Diagnostics.CodeAnalysis;
using System.Net;
using Minio.Credentials;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Result;
using Minio.Exceptions;
using Minio.Handlers;
using Minio.Helper;

namespace Minio;

public static class RequestExtensions
{
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
        Justification = "This is done in the interface. String is provided here for convenience")]
    public static Task<HttpResponseMessage> WrapperGetAsync(this IMinioClient minioClient, string url)
    {
        return minioClient is null
            ? throw new ArgumentNullException(nameof(minioClient))
            : minioClient.WrapperGetAsync(new Uri(url));
    }

    /// <summary>
    ///     Runs httpClient's PutObjectAsync method
    /// </summary>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
        Justification = "This is done in the interface. String is provided here for convenience")]
    public static Task WrapperPutAsync(this IMinioClient minioClient, string url, StreamContent strm)
    {
        return minioClient is null
            ? throw new ArgumentNullException(nameof(minioClient))
            : minioClient.WrapperPutAsync(new Uri(url), strm);
    }

    /// <summary>
    ///     Actual doer that executes the request on the server
    /// </summary>
    /// <param name="minioClient"></param>
    /// <param name="requestMessageBuilder">The build of HttpRequestMessageBuilder </param>
    /// <param name="ignoreExceptionType">any type of Exception; if an exception type is going to be ignored</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>ResponseResult</returns>
    internal static async Task<ResponseResult> ExecuteTaskAsync(this IMinioClient minioClient,
        HttpRequestMessageBuilder requestMessageBuilder,
        Type ignoreExceptionType = null,
        bool isSts = false,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var responseResult = new ResponseResult(requestMessageBuilder.Request, response: null);
        try
        {
            if (minioClient.Config.RequestTimeout > 0)
            {
                using var internalTokenSource =
                    new CancellationTokenSource(new TimeSpan(0, 0, 0, 0, minioClient.Config.RequestTimeout));
                using var timeoutTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(internalTokenSource.Token, cancellationToken);
                cancellationToken = timeoutTokenSource.Token;
            }

            responseResult = await minioClient.ExecuteWithRetry(
                async Task<ResponseResult> () => await minioClient.ExecuteTaskCoreAsync(
                    requestMessageBuilder,
                    isSts, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
            if (responseResult is not null &&
                (responseResult.Exception?.GetType().Equals(ignoreExceptionType) == false ||
                 responseResult.StatusCode != HttpStatusCode.OK))
            {
                var handler = new DefaultErrorHandler();
                handler.Handle(responseResult);
            }

            return responseResult;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            responseResult.Exception ??= ignoreExceptionType is not null &&
                                         ex.GetType() == ignoreExceptionType
                ? null
                : ex;
            return responseResult;
        }
    }

    private static async Task<ResponseResult> ExecuteTaskCoreAsync(this IMinioClient minioClient,
        // IEnumerable<IApiResponseErrorHandler> errorHandlers,
        HttpRequestMessageBuilder requestMessageBuilder,
        bool isSts = false,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var v4Authenticator = new V4Authenticator(minioClient.Config.Secure,
            minioClient.Config.AccessKey, minioClient.Config.SecretKey, minioClient.Config.Region,
            minioClient.Config.SessionToken);

        requestMessageBuilder.AddOrUpdateHeaderParameter("Authorization",
            v4Authenticator.Authenticate(requestMessageBuilder, isSts));

        var request = requestMessageBuilder.Request;
        var responseResult = new ResponseResult(request, new HttpResponseMessage());
        try
        {
            var response = await minioClient.Config.HttpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            responseResult = new ResponseResult(request, response);
            if (requestMessageBuilder.ResponseWriter is not null)
                await requestMessageBuilder.ResponseWriter(responseResult.ContentStream, cancellationToken)
                    .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            responseResult.Exception = ex;
        }

        return responseResult;
    }

    private static async Task<ResponseResult> ExecuteWithRetry(this IMinioClient minioClient,
        Func<Task<ResponseResult>> executeRequestCallback)
    {
        return minioClient.Config.RetryPolicyHandler is null
            ? await executeRequestCallback().ConfigureAwait(false)
            : await minioClient.Config.RetryPolicyHandler.Handle(executeRequestCallback).ConfigureAwait(false);
    }

    /// <summary>
    ///     Constructs a HttpRequestMessageBuilder using bucket/object names from Args.
    ///     Calls overloaded CreateRequest method.
    /// </summary>
    /// <param name="minioClient"></param>
    /// <param name="args">The direct descendant of BucketArgs class, args with populated values from Input</param>
    /// <returns>A HttpRequestMessageBuilder</returns>
    internal static async Task<HttpRequestMessageBuilder> CreateRequest<T>(this IMinioClient minioClient,
        BucketArgs<T> args) where T : BucketArgs<T>
    {
        ArgsCheck(args);
        var requestMessageBuilder =
            await minioClient.CreateRequest(args.RequestMethod, args.BucketName, headerMap: args.Headers,
                isBucketCreationRequest: args.IsBucketCreationRequest).ConfigureAwait(false);
        return args.BuildRequest(requestMessageBuilder);
    }

    /// <summary>
    ///     Constructs a HttpRequestMessage using bucket/object names from Args.
    ///     Calls overloaded CreateRequest method.
    /// </summary>
    /// <param name="minioClient"></param>
    /// <param name="args">The direct descendant of ObjectArgs class, args with populated values from Input</param>
    /// <returns>A HttpRequestMessage</returns>
    internal static async Task<HttpRequestMessageBuilder> CreateRequest<T>(this IMinioClient minioClient,
        ObjectArgs<T> args) where T : ObjectArgs<T>
    {
        ArgsCheck(args);

        var contentType = "application/octet-stream";
        _ = args.Headers?.TryGetValue("Content-Type", out contentType);
        var requestMessageBuilder =
            await minioClient.CreateRequest(args.RequestMethod,
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
    /// <param name="minioClient"></param>
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
    internal static async Task<HttpRequestMessageBuilder> CreateRequest(this IMinioClient minioClient,
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
                region = await minioClient.GetRegion(bucketName).ConfigureAwait(false);
        }

        if (objectName is not null) Utils.ValidateObjectName(objectName);

        if (minioClient.Config.Provider is not null)
        {
            var isAWSEnvProvider = minioClient.Config.Provider is AWSEnvironmentProvider ||
                                   (minioClient.Config.Provider is ChainedProvider ch &&
                                    ch.CurrentProvider is AWSEnvironmentProvider);

            var isIAMAWSProvider = minioClient.Config.Provider is IAMAWSProvider ||
                                   (minioClient.Config.Provider is ChainedProvider chained &&
                                    chained.CurrentProvider is AWSEnvironmentProvider);

            AccessCredentials creds;
            if (isAWSEnvProvider)
            {
                var aWSEnvProvider = (AWSEnvironmentProvider)minioClient.Config.Provider;
                creds = await aWSEnvProvider.GetCredentialsAsync().ConfigureAwait(false);
            }
            else if (isIAMAWSProvider)
            {
                var iamAWSProvider = (IAMAWSProvider)minioClient.Config.Provider;
                creds = iamAWSProvider.Credentials;
            }
            else
            {
                creds = await minioClient.Config.Provider.GetCredentialsAsync().ConfigureAwait(false);
            }

            if (creds is not null)
            {
                minioClient.Config.AccessKey = creds.AccessKey;
                minioClient.Config.SecretKey = creds.SecretKey;
            }
        }

        // This section reconstructs the url with scheme followed by location specific endpoint (s3.region.amazonaws.com)
        // or Virtual Host styled endpoint (bucketname.s3.region.amazonaws.com) for Amazon requests.
        var resource = string.Empty;
        var usePathStyle = false;

        if (!string.IsNullOrEmpty(bucketName) && S3utils.IsAmazonEndPoint(minioClient.Config.BaseUrl))
        {
            if (method == HttpMethod.Put && objectName is null && resourcePath is null)
                // use path style for make bucket to workaround "AuthorizationHeaderMalformed" error from s3.amazonaws.com
                usePathStyle = true;
            else if (resourcePath?.Contains("location", StringComparison.OrdinalIgnoreCase) == true)
                // use path style for location query
                usePathStyle = true;
            else if (bucketName.Contains('.', StringComparison.Ordinal) && minioClient.Config.Secure)
                // use path style where '.' in bucketName causes SSL certificate validation error
                usePathStyle = true;

            if (usePathStyle) resource += Utils.UrlEncode(bucketName) + "/";
        }

        // Set Target URL
        var requestUrl = RequestUtil.MakeTargetURL(minioClient.Config.BaseUrl, minioClient.Config.Secure, bucketName,
            region, usePathStyle);

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

        if (headerMap?.Count > 0)
        {
            if (headerMap.TryGetValue(messageBuilder.ContentTypeKey, out var value) && !string.IsNullOrEmpty(value))
                headerMap[messageBuilder.ContentTypeKey] = contentType;

            foreach (var entry in headerMap) messageBuilder.AddOrUpdateHeaderParameter(entry.Key, entry.Value);
        }

        return messageBuilder;
    }

    /// <summary>
    ///     Null Check for Args object.
    ///     Expected to be called from CreateRequest
    /// </summary>
    /// <param name="args">The child object of Args class</param>
    private static void ArgsCheck(RequestArgs args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args),
                "Args object cannot be null. It needs to be assigned to an instantiated child object of Args.");
    }

    /// <summary>
    ///     Resolve region of the bucket.
    /// </summary>
    /// <param name="minioClient"></param>
    /// <param name="bucketName"></param>
    /// <returns></returns>
    internal static async Task<string> GetRegion(this IMinioClient minioClient, string bucketName)
    {
        var rgn = "";
        // Use user specified region in client constructor if present
        if (!string.IsNullOrEmpty(minioClient.Config.Region)) return minioClient.Config.Region;

        // pick region from endpoint if present
        if (!string.IsNullOrEmpty(minioClient.Config.Endpoint))
            rgn = RegionHelper.GetRegionFromEndpoint(minioClient.Config.Endpoint);

        // Pick region from location HEAD request
        if (rgn?.Length == 0)
            rgn = BucketRegionCache.Instance.Exists(bucketName)
                ? await BucketRegionCache.Update(minioClient, bucketName).ConfigureAwait(false)
                : BucketRegionCache.Instance.Region(bucketName);

        // Defaults to us-east-1 if region could not be found
        return rgn?.Length == 0 ? "us-east-1" : rgn;
    }

    /// <summary>
    ///     Delegate errors to handlers
    /// </summary>
    /// <param name="minioClient"></param>
    /// <param name="response"></param>
    /// <param name="handlers"></param>
    /// <param name="startTime"></param>
    /// <param name="ignoreExceptionType"></param>
    private static void HandleIfErrorResponse(this IMinioClient minioClient, ResponseResult response,
        IEnumerable<IApiResponseErrorHandler> handlers,
        DateTime startTime,
        Type ignoreExceptionType = null)
    {
        // Logs Response if HTTP tracing is enabled
        if (minioClient.Config.TraceHttp)
        {
            var now = DateTime.Now;
            minioClient.LogRequest(response.Request, response, (now - startTime).TotalMilliseconds);
        }

        if (response.Exception is not null)
        {
            if (response.Exception?.GetType() == ignoreExceptionType)
            {
                response.Exception = null;
            }
            else
            {
                if (handlers.Any())
                    // Run through handlers passed to take up error handling
                    foreach (var handler in handlers)
                        handler.Handle(response);
                else
                    minioClient.DefaultErrorHandler.Handle(response);
            }
        }
    }
}
