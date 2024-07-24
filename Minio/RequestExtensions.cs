using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Web;
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
    /// <param name="errorHandlers">List of handlers to override default handling</param>
    /// <param name="requestMessageBuilder">The build of HttpRequestMessageBuilder </param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>ResponseResult</returns>
    internal static Task<ResponseResult> ExecuteTaskAsync(this IMinioClient minioClient,
        IEnumerable<IApiResponseErrorHandler> errorHandlers,
        HttpRequestMessageBuilder requestMessageBuilder,
        bool isSts = false,
        CancellationToken cancellationToken = default)
    {
        Task<ResponseResult> responseResult;
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

            responseResult = minioClient.ExecuteWithRetry(
                async () => await minioClient.ExecuteTaskCoreAsync(errorHandlers, requestMessageBuilder,
                    isSts, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n\n   *** ExecuteTaskAsync::Threw an exception => {ex.Message}");
            throw;
        }

        return responseResult;
    }

    private static async Task<ResponseResult> ExecuteTaskCoreAsync(this IMinioClient minioClient,
        IEnumerable<IApiResponseErrorHandler> errorHandlers,
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

        var responseResult = new ResponseResult(request, response: null);
        try
        {
            var response = await minioClient.Config.HttpClient.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            responseResult = new ResponseResult(request, response);
            if (requestMessageBuilder.ResponseWriter is not null)
                await requestMessageBuilder.ResponseWriter(responseResult.ContentStream, cancellationToken)
                    .ConfigureAwait(false);

            var path = request.RequestUri.LocalPath.TrimStart('/').TrimEnd('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (responseResult.Response.StatusCode == HttpStatusCode.NotFound)
            {
                if (request.Method == HttpMethod.Get)
                {
                    var q = HttpUtility.ParseQueryString(request.RequestUri.Query);
                    if (q.Get("object-lock") != null)
                    {
                        responseResult.Exception = new MissingObjectLockConfigurationException();
                        return responseResult;
                    }
                }

                if (request.Method == HttpMethod.Head)
                {
                    if (responseResult.Exception is BucketNotFoundException || path.Length == 1)
                        responseResult.Exception = new BucketNotFoundException();

                    if (path.Length > 1)
                    {
                        var found = await minioClient
                            .BucketExistsAsync(new BucketExistsArgs().WithBucket(path[0]), cancellationToken)
                            .ConfigureAwait(false);
                        responseResult.Exception = !found
                            ? new Exception("ThrowBucketNotFoundException")
                            : new ObjectNotFoundException();
                        throw responseResult.Exception;
                    }
                }

                return responseResult;
            }

            minioClient.HandleIfErrorResponse(responseResult, errorHandlers, startTime);
            return responseResult;
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or
                                       ObjectNotFoundException))
        {
            if (ex.Message.Equals("ThrowBucketNotFoundException", StringComparison.Ordinal))
                throw new BucketNotFoundException();

            if (responseResult is not null)
            {
                responseResult.Exception = ex;
                throw;
            }

            responseResult = new ResponseResult(request, ex);

            return responseResult;
        }
    }

    private static Task<ResponseResult> ExecuteWithRetry(this IMinioClient minioClient,
        Func<Task<ResponseResult>> executeRequestCallback)
    {
        return minioClient.Config.RetryPolicyHandler is null
            ? executeRequestCallback()
            : minioClient.Config.RetryPolicyHandler.Handle(executeRequestCallback);
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

        if (headerMap is not null)
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
    private static void HandleIfErrorResponse(this IMinioClient minioClient, ResponseResult response,
        IEnumerable<IApiResponseErrorHandler> handlers,
        DateTime startTime)
    {
        // Logs Response if HTTP tracing is enabled
        if (minioClient.Config.TraceHttp)
        {
            var now = DateTime.Now;
            minioClient.LogRequest(response.Request, response, (now - startTime).TotalMilliseconds);
        }

        if (response.Exception is not null)
            throw response.Exception;

        if (handlers.Any())
            // Run through handlers passed to take up error handling
            foreach (var handler in handlers)
                handler.Handle(response);
        else
            minioClient.DefaultErrorHandler.Handle(response);
    }
}
