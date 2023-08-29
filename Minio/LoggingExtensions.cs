using Minio.DataModel.Result;
using Minio.DataModel.Tracing;

namespace Minio;

public static class LoggingExtensions
{
    /// <summary>
    ///     Logs the request sent to server and corresponding response
    /// </summary>
    /// <param name="minioClient"></param>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <param name="durationMs"></param>
    internal static void LogRequest(this IMinioClient minioClient, HttpRequestMessage request, ResponseResult response,
        double durationMs)
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

        minioClient.RequestLogger.LogRequest(requestToLog, responseToLog, durationMs);
    }
}
