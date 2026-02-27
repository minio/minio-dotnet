using System.Globalization;
using System.Text;
using Minio.Model;

namespace Minio;

#pragma warning disable CA1032  // Don't need the default exception constructors

/// <summary>
/// Exception thrown when a MinIO or S3-compatible server returns an unsuccessful HTTP response.
/// Provides a snapshot of the request method and URI, the <see cref="HttpResponseMessage"/>,
/// and the parsed S3 <see cref="ErrorResponse"/> body (when available).
/// </summary>
public class MinioHttpException : MinioException
{
    /// <summary>
    /// Gets a snapshot of the HTTP request that triggered this exception, containing the
    /// request method and URI. Only the <see cref="HttpRequestMessage.Method"/> and
    /// <see cref="HttpRequestMessage.RequestUri"/> are guaranteed to be populated; headers
    /// and content are not copied.
    /// </summary>
    public HttpRequestMessage Request { get; }

    /// <summary>
    /// Gets the HTTP response that contains the error status code and body.
    /// </summary>
    public HttpResponseMessage Response { get; }

    /// <summary>
    /// Gets the parsed S3 error response body, or <see langword="null"/> if the server
    /// did not return a structured XML error payload.
    /// </summary>
    public ErrorResponse? Error { get; }

    internal MinioHttpException(HttpRequestMessage request, HttpResponseMessage response, ErrorResponse? error)
        : base(GetMessage(request, response, error))
    {
        // Clone method and URI so this exception remains usable after the request is disposed
        // by its surrounding using-scope.
        Request = new HttpRequestMessage(request.Method, request.RequestUri);
        Response = response;
        Error = error;
    }

    private static string GetMessage(HttpRequestMessage request, HttpResponseMessage response, ErrorResponse? error)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"{request.Method} {request.RequestUri} returned HTTP status-code {(int)response.StatusCode} ({response.StatusCode})");
        if (!string.IsNullOrEmpty(error?.Message))
            sb.Append(CultureInfo.InvariantCulture, $": {error.Message}");
        return sb.ToString();
    }
}
