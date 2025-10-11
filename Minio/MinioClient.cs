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

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minio.DataModel.Result;
using Minio.Exceptions;
using Minio.Handlers;
using Minio.Helper;

namespace Minio;

public partial class MinioClient : IMinioClient
{
    private static readonly char[] separator = { '/' };

    private bool disposedValue;

    /// <summary>
    ///     Creates and returns an MinIO Client
    /// </summary>
    /// <returns>Client with no arguments to be used with other builder methods</returns>
    public MinioClient() { }

    public MinioConfig Config { get; } = new();

    public IEnumerable<IApiResponseErrorHandler> ResponseErrorHandlers { get; internal set; } =
        Enumerable.Empty<IApiResponseErrorHandler>();

    /// <summary>
    ///     Default error handling delegate
    /// </summary>
    public IApiResponseErrorHandler DefaultErrorHandler { get; internal set; } =
        new DefaultErrorHandler();

    public IRequestLogger RequestLogger { get; internal set; }

    /// <summary>
    ///     Runs httpClient's GetAsync method
    /// </summary>
    public Task<HttpResponseMessage> WrapperGetAsync(Uri uri)
    {
        return Config.HttpClient.GetAsync(uri);
    }

    /// <summary>
    ///     Runs httpClient's PutObjectAsync method
    /// </summary>
    public Task WrapperPutAsync(Uri uri, StreamContent strm)
    {
        return Task.Run(async () =>
            await Config.HttpClient.PutAsync(uri, strm).ConfigureAwait(false)
        );
    }

    /// <summary>
    ///     Sets HTTP tracing On.Writes output to Console
    /// </summary>
    public void SetTraceOn(IRequestLogger requestLogger = null)
    {
        var logger = Config?.ServiceProvider?.GetRequiredService<ILogger<DefaultRequestLogger>>();
        RequestLogger = requestLogger ?? new DefaultRequestLogger(logger);
        Config.TraceHttp = true;
    }

    /// <summary>
    ///     Sets HTTP tracing Off.
    /// </summary>
    public void SetTraceOff()
    {
        Config.TraceHttp = false;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Parse response errors if any and return relevant error messages
    /// </summary>
    /// <param name="response"></param>
    internal static void ParseError(ResponseResult response)
    {
        if (response is null)
            throw new ConnectionException(
                "Response is nil. Please report this issue https://github.com/minio/minio-dotnet/issues",
                response
            );

        if (
            HttpStatusCode.Redirect == response.StatusCode
            || HttpStatusCode.TemporaryRedirect == response.StatusCode
            || HttpStatusCode.MovedPermanently == response.StatusCode
        )
            throw new RedirectionException(
                "Redirection detected. Please report this issue https://github.com/minio/minio-dotnet/issues"
            );

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            ParseErrorNoContent(response);
            return;
        }

        ParseErrorFromContent(response);
    }

    private static void ParseErrorNoContent(ResponseResult response)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));
        var statusCodeStrs = new[]
        {
            nameof(HttpStatusCode.Forbidden),
            nameof(HttpStatusCode.BadRequest),
            nameof(HttpStatusCode.NotFound),
            nameof(HttpStatusCode.MethodNotAllowed),
            nameof(HttpStatusCode.NotImplemented),
        };

        if (
            response.Exception != null
            || !string.IsNullOrEmpty(response.ErrorMessage)
            || response.Headers is not null
        )
            foreach (var exception in statusCodeStrs)
            {
                if (response.StatusCode is not HttpStatusCode.OK)
                {
                    ParseWellKnownErrorNoContent(response);
                    break;
                }

                if (
                    response.Headers.TryGetValue("X-Minio-Error-Code", out var value1)
                    && value1 is not null
                    && response.Headers.TryGetValue("X-Minio-Error-Desc", out var value2)
                    && value2 is not null
                )
                    throw new Exception(value1 + ": " + value2);
            }

        if (statusCodeStrs.Contains(response.StatusCode.ToString(), StringComparer.Ordinal))
            ParseWellKnownErrorNoContent(response);
#pragma warning disable MA0099 // Use Explicit enum value instead of 0
        if (
            response.ErrorMessage.Contains(
                "Name or service not known",
                StringComparison.OrdinalIgnoreCase
            )
            || response.StatusCode == 0
        )
            throw new ConnectionException("Connection error: " + response.ErrorMessage);
#pragma warning disable MA0099 // Use Explicit enum value instead of 0
        if (response.ErrorMessage.Contains("No route to host", StringComparison.OrdinalIgnoreCase))
            throw new ConnectionException(
                "Connection error:"
                    + "Name or service not known ("
                    + response.Request.RequestUri.Authority
                    + ")"
            );

        if (
            response.ErrorMessage.Contains(
                "The Access Key Id you provided does not exist in our records.",
                StringComparison.OrdinalIgnoreCase
            )
        )
            throw new AccessDeniedException(
                "Access denied error: " + response.ErrorMessage,
                response
            );

        if (
            response.ErrorMessage.Contains(
                "The request signature we calculated does not match the signature you provided. Check your key and signing method.",
                StringComparison.OrdinalIgnoreCase
            ) || string.IsNullOrWhiteSpace(response.Content)
        )
            throw new AccessDeniedException(
                "Access denied error: " + response.ErrorMessage,
                response
            );

        if (response.Exception.GetType() == typeof(TaskCanceledException))
            throw response.Exception;

        if (response.StatusCode == HttpStatusCode.PartialContent)
            throw response.Exception;

        throw new InternalClientException(
            "Unsuccessful response from server without XML:" + response.ErrorMessage,
            response
        );
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
        var resourceSplits = pathAndQuery.Split(
            separator,
            2,
            StringSplitOptions.RemoveEmptyEntries
        );

        if (response.StatusCode == HttpStatusCode.PartialContent)
        {
            errorResponse.Code = "PartialContent";
            error = new PartialContentException();
        }

        if (
            response.StatusCode == HttpStatusCode.NotFound
            || string.Equals(
                response.Response.ReasonPhrase.Replace(" ", "", StringComparison.OrdinalIgnoreCase),
                nameof(HttpStatusCode.NotFound),
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            var pathLength = resourceSplits.Length;
            var isAWS = host.EndsWith("s3.amazonaws.com", StringComparison.OrdinalIgnoreCase);
            var isVirtual =
                isAWS && !host.StartsWith("s3.amazonaws.com", StringComparison.OrdinalIgnoreCase);

            if (pathLength > 1)
            {
                var objectName = resourceSplits[1];
                errorResponse.Code = "NoSuchKey";
                error = new ObjectNotFoundException(objectName);
            }
            else if (pathLength == 1)
            {
                var resource = resourceSplits[0];

                if (isAWS && isVirtual)
                {
                    errorResponse.Code = "NoSuchKey";
                    error = new ObjectNotFoundException(resource);
                }
                else
                {
                    errorResponse.Code = "NoSuchBucket";
                    BucketRegionCache.Instance.Remove(resource);
                    error = new BucketNotFoundException(resource);
                }
            }
            else
            {
                error = new InternalClientException(
                    "404 without body resulted in path with less than two components",
                    response
                );
            }
        }
        else if (HttpStatusCode.BadRequest == response.StatusCode)
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
                error = new InternalClientException(
                    "400 without body resulted in path with less than two components",
                    response
                );
            }
        }
        else if (HttpStatusCode.Forbidden == response.StatusCode)
        {
            errorResponse.Code = "Forbidden";
            error = new AccessDeniedException("Access denied on the resource: " + pathAndQuery);
        }

        response.Exception = error;
        throw error;
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "TODO")]
    private static void ParseErrorFromContent(ResponseResult response)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        if (
            response
                .StatusCode.ToString()
                .Contains(nameof(HttpStatusCode.NotFound), StringComparison.OrdinalIgnoreCase)
            && response.Request.RequestUri.PathAndQuery.EndsWith(
                "?location",
                StringComparison.OrdinalIgnoreCase
            )
            && response.Request.Method.Equals(HttpMethod.Get)
        )
        {
            var bucketName = response.Request.RequestUri.PathAndQuery.Split('?')[0];
            BucketRegionCache.Instance.Remove(bucketName);
            throw new BucketNotFoundException(bucketName);
        }

        var errResponse = Utils.DeserializeXml<ErrorResponse>(response.Content);

        if (
            response.StatusCode == HttpStatusCode.Forbidden
            && (
                errResponse.Code.Equals("SignatureDoesNotMatch", StringComparison.OrdinalIgnoreCase)
                || errResponse.Code.Equals("InvalidAccessKeyId", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            throw new AuthorizationException(
                errResponse.Resource,
                errResponse.BucketName,
                errResponse.Message
            );
        }

        // Handle XML response for Bucket Policy not found case
        if (
            response
                .StatusCode.ToString()
                .Contains(nameof(HttpStatusCode.NotFound), StringComparison.OrdinalIgnoreCase)
            && response.Request.RequestUri.PathAndQuery.EndsWith(
                "?policy",
                StringComparison.OrdinalIgnoreCase
            )
            && response.Request.Method.Equals(HttpMethod.Get)
            && string.Equals(
                errResponse.Code,
                "NoSuchBucketPolicy",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new ErrorResponseException(errResponse, response) { XmlError = response.Content };
        }

        if (
            response
                .StatusCode.ToString()
                .Contains(nameof(HttpStatusCode.NotFound), StringComparison.OrdinalIgnoreCase)
            && string.Equals(errResponse.Code, "NoSuchBucket", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new BucketNotFoundException(errResponse.BucketName);
        }

        if (
            response
                .StatusCode.ToString()
                .Contains(nameof(HttpStatusCode.NotFound), StringComparison.OrdinalIgnoreCase)
            && string.Equals(errResponse.Code, "NoSuchKey", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new ObjectNotFoundException(errResponse.BucketName);
        }

        if (
            response
                .StatusCode.ToString()
                .Contains(nameof(HttpStatusCode.NotFound), StringComparison.OrdinalIgnoreCase)
            && errResponse.Code.Equals("MalformedXML", StringComparison.OrdinalIgnoreCase)
        )
            throw new MalFormedXMLException(
                errResponse.Resource,
                errResponse.BucketName,
                errResponse.Message,
                errResponse.Key
            );

        if (
            response
                .StatusCode.ToString()
                .Contains(nameof(HttpStatusCode.NotImplemented), StringComparison.OrdinalIgnoreCase)
            && errResponse.Code.Equals("NotImplemented", StringComparison.OrdinalIgnoreCase)
        )
        {
#pragma warning disable MA0025 // Implement the functionality instead of throwing NotImplementedException
            throw new NotImplementedException(errResponse.Message);
        }
#pragma warning restore MA0025 // Implement the functionality instead of throwing NotImplementedException

        if (
            response.StatusCode == HttpStatusCode.BadRequest
            && errResponse.Code.Equals("InvalidRequest", StringComparison.OrdinalIgnoreCase)
        )
        {
            _ = new Dictionary<string, string>(StringComparer.Ordinal) { { "legal-hold", "" } };
            if (
                response.Request.RequestUri.Query.Contains(
                    "legalHold",
                    StringComparison.OrdinalIgnoreCase
                )
            )
                throw new MissingObjectLockConfigurationException(
                    errResponse.BucketName,
                    errResponse.Message
                );
        }

        if (
            response
                .StatusCode.ToString()
                .Contains(nameof(HttpStatusCode.NotFound), StringComparison.OrdinalIgnoreCase)
            && errResponse.Code.Equals(
                "ObjectLockConfigurationNotFoundError",
                StringComparison.OrdinalIgnoreCase
            )
        )
            throw new MissingObjectLockConfigurationException(
                errResponse.BucketName,
                errResponse.Message
            );

        if (
            response
                .StatusCode.ToString()
                .Contains(nameof(HttpStatusCode.NotFound), StringComparison.OrdinalIgnoreCase)
            && errResponse.Code.Equals(
                "ReplicationConfigurationNotFoundError",
                StringComparison.OrdinalIgnoreCase
            )
        )
            throw new MissingBucketReplicationConfigurationException(
                errResponse.BucketName,
                errResponse.Message
            );

        if (
            response.StatusCode == HttpStatusCode.Conflict
            && errResponse.Code.Equals(
                "BucketAlreadyOwnedByYou",
                StringComparison.OrdinalIgnoreCase
            )
        )
            throw new ArgumentException(
                "Bucket already owned by you: " + errResponse.BucketName,
                nameof(response)
            );

        if (
            response.StatusCode == HttpStatusCode.PreconditionFailed
            && errResponse.Code.Equals("PreconditionFailed", StringComparison.OrdinalIgnoreCase)
        )
            throw new PreconditionFailedException(
                "At least one of the pre-conditions you "
                    + "specified did not hold for object: \""
                    + errResponse.Resource
                    + "\""
            );

        throw new UnexpectedMinioException(errResponse.Message)
        {
            Response = errResponse,
            XmlError = response.Content,
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing && Config.DisposeHttpClient)
                Config.HttpClient?.Dispose();
            disposedValue = true;
        }
    }
}
