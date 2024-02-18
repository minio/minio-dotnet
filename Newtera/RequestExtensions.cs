using System.Diagnostics.CodeAnalysis;
using System.Net;
using Newtera.Credentials;
using Newtera.DataModel;
using Newtera.DataModel.Args;
using Newtera.DataModel.Result;
using Newtera.Exceptions;
using Newtera.Handlers;
using Newtera.Helper;

namespace Newtera;

public static class RequestExtensions
{
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
        Justification = "This is done in the interface. String is provided here for convenience")]
    public static Task<HttpResponseMessage> WrapperGetAsync(this INewteraClient newteraClient, string url)
    {
        return newteraClient is null
            ? throw new ArgumentNullException(nameof(newteraClient))
            : newteraClient.WrapperGetAsync(new Uri(url));
    }

    /// <summary>
    ///     Runs httpClient's PutObjectAsync method
    /// </summary>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
        Justification = "This is done in the interface. String is provided here for convenience")]
    public static Task WrapperPutAsync(this INewteraClient newteraClient, string url, StreamContent strm)
    {
        return newteraClient is null
            ? throw new ArgumentNullException(nameof(newteraClient))
            : newteraClient.WrapperPutAsync(new Uri(url), strm);
    }

    /// <summary>
    ///     Actual doer that executes the request on the server
    /// </summary>
    /// <param name="newteraClient"></param>
    /// <param name="requestMessageBuilder">The build of HttpRequestMessageBuilder </param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>ResponseResult</returns>
    internal static Task<ResponseResult> ExecuteTaskAsync(this INewteraClient newteraClient,
        HttpRequestMessageBuilder requestMessageBuilder,
        CancellationToken cancellationToken = default)
    {
        Task<ResponseResult> responseResult;
        try
        {
            if (newteraClient.Config.RequestTimeout > 0)
            {
                using var internalTokenSource =
                    new CancellationTokenSource(new TimeSpan(0, 0, 0, 0, newteraClient.Config.RequestTimeout));
                using var timeoutTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(internalTokenSource.Token, cancellationToken);
                cancellationToken = timeoutTokenSource.Token;
            }

            responseResult = newteraClient.ExecuteWithRetry(
                async () => await newteraClient.ExecuteTaskCoreAsync(requestMessageBuilder, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n\n   *** ExecuteTaskAsync::Threw an exception => {ex.Message}");
            throw;
        }

        return responseResult;
    }

    private static async Task<ResponseResult> ExecuteTaskCoreAsync(this INewteraClient newteraClient,
        HttpRequestMessageBuilder requestMessageBuilder,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        /*
        var v4Authenticator = new V4Authenticator(newteraClient.Config.Secure,
            newteraClient.Config.AccessKey, newteraClient.Config.SecretKey, newteraClient.Config.Region,
            newteraClient.Config.SessionToken);

        requestMessageBuilder.AddOrUpdateHeaderParameter("Authorization",
            v4Authenticator.Authenticate(requestMessageBuilder, isSts));
        */

        var request = requestMessageBuilder.Request;

        var responseResult = new ResponseResult(request, response: null);
        try
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            var response = await newteraClient.Config.HttpClient.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            responseResult = new ResponseResult(request, response);
            if (requestMessageBuilder.ResponseWriter is not null)
                await requestMessageBuilder.ResponseWriter(responseResult.ContentStream, cancellationToken)
                    .ConfigureAwait(false);

            var path = request.RequestUri.LocalPath.TrimStart('/').TrimEnd('/').Split('/');
            if (responseResult.Response.StatusCode == HttpStatusCode.NotFound)
            {
                if (request.Method == HttpMethod.Head)
                {
                    if (responseResult.Exception?.GetType().Equals(typeof(BucketNotFoundException)) == true ||
                        path?.ToList().Count == 1)
                        responseResult.Exception = new BucketNotFoundException();

                    if (path?.ToList().Count > 1)
                    {
                        var found = await newteraClient
                            .BucketExistsAsync(new BucketExistsArgs().WithBucket(path.ToList()[0]), cancellationToken)
                            .ConfigureAwait(false);
                        responseResult.Exception = !found
                            ? new Exception("ThrowBucketNotFoundException")
                            : new ObjectNotFoundException();
                        throw responseResult.Exception;
                    }
                }
            }

            return responseResult;
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or
                                       ObjectNotFoundException))
        {
            if (ex.Message.Equals("ThrowBucketNotFoundException", StringComparison.Ordinal))
                throw new BucketNotFoundException();

            if (responseResult is not null)
                responseResult.Exception = ex;
            else
                responseResult = new ResponseResult(request, ex);
            return responseResult;
        }
    }

    private static Task<ResponseResult> ExecuteWithRetry(this INewteraClient newteraClient,
        Func<Task<ResponseResult>> executeRequestCallback)
    {
        return newteraClient.Config.RetryPolicyHandler is null
            ? executeRequestCallback()
            : newteraClient.Config.RetryPolicyHandler.Handle(executeRequestCallback);
    }

    /// <summary>
    ///     Constructs a HttpRequestMessageBuilder using bucket/object names from Args.
    ///     Calls overloaded CreateRequest method.
    /// </summary>
    /// <param name="newteraClient"></param>
    /// <param name="args">The direct descendant of BucketArgs class, args with populated values from Input</param>
    /// <returns>A HttpRequestMessageBuilder</returns>
    internal static async Task<HttpRequestMessageBuilder> CreateRequest<T>(this INewteraClient newteraClient,
        BucketArgs<T> args) where T : BucketArgs<T>
    {
        ArgsCheck(args);
        var requestMessageBuilder =
            await newteraClient.CreateRequest(args.RequestMethod, args.RequestPath, args.BucketName, headerMap: args.Headers).ConfigureAwait(false);
        return args.BuildRequest(requestMessageBuilder);
    }

    /// <summary>
    ///     Constructs a HttpRequestMessage using bucket/object names from Args.
    ///     Calls overloaded CreateRequest method.
    /// </summary>
    /// <param name="newteraClient"></param>
    /// <param name="args">The direct descendant of ObjectArgs class, args with populated values from Input</param>
    /// <returns>A HttpRequestMessage</returns>
    internal static async Task<HttpRequestMessageBuilder> CreateRequest<T>(this INewteraClient newteraClient,
        ObjectArgs<T> args) where T : ObjectArgs<T>
    {
        ArgsCheck(args);

        var contentType = "application/octet-stream";
        _ = args.Headers?.TryGetValue("Content-Type", out contentType);
        var requestMessageBuilder =
            await newteraClient.CreateRequest(args.RequestMethod,
                args.RequestPath,
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
    /// <param name="newteraClient"></param>
    /// <param name="method">HTTP method</param>
    /// <param name="bucketName">Bucket Name</param>
    /// <param name="objectName">Object Name</param>
    /// <param name="headerMap">headerMap</param>
    /// <param name="contentType">Content Type</param>
    /// <param name="body">request body</param>
    /// <param name="resourcePath">query string</param>
    /// <returns>A HttpRequestMessage builder</returns>
    /// <exception cref="BucketNotFoundException">When bucketName is invalid</exception>
    internal static async Task<HttpRequestMessageBuilder> CreateRequest(this INewteraClient newteraClient,
        HttpMethod method,
        string requestPath = null,
        string bucketName = null,
        string objectName = null,
        IDictionary<string, string> headerMap = null,
        string contentType = "application/octet-stream",
        ReadOnlyMemory<byte> body = default,
        string resourcePath = null)
    {
        if (bucketName is not null)
        {
            Utils.ValidateBucketName(bucketName);
        }

        if (objectName is not null) Utils.ValidateObjectName(objectName);

        if (newteraClient.Config.Provider is not null)
        {
            AccessCredentials creds;
            creds = await newteraClient.Config.Provider.GetCredentialsAsync().ConfigureAwait(false);

            if (creds is not null)
            {
                newteraClient.Config.AccessKey = creds.AccessKey;
                newteraClient.Config.SecretKey = creds.SecretKey;
            }
        }

        var resource = string.Empty;

        // Set Target URL
        var requestUrl = RequestUtil.MakeTargetURL(newteraClient.Config.BaseUrl, newteraClient.Config.Secure, requestPath, bucketName);

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
    ///     Delegate errors to handlers
    /// </summary>
    /// <param name="newteraClient"></param>
    /// <param name="response"></param>
    /// <param name="handlers"></param>
    /// <param name="startTime"></param>
    private static void HandleIfErrorResponse(this INewteraClient newteraClient, ResponseResult response,
        IEnumerable<IApiResponseErrorHandler> handlers,
        DateTime startTime)
    {
        // Logs Response if HTTP tracing is enabled
        if (newteraClient.Config.TraceHttp)
        {
            var now = DateTime.Now;
            newteraClient.LogRequest(response.Request, response, (now - startTime).TotalMilliseconds);
        }

        if (response.Exception is null)
            throw response.Exception;

        if (handlers.Any())
            // Run through handlers passed to take up error handling
            foreach (var handler in handlers)
                handler.Handle(response);
        else
            newteraClient.DefaultErrorHandler.Handle(response);
    }
}
