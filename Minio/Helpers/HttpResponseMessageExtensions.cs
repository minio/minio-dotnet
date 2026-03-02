namespace Minio.Helpers;

internal static class HttpResponseMessageExtensions
{
    public static string? GetHeaderValue(this HttpResponseMessage response, string header)
    {
        return response.Headers.TryGetValues(header, out var values) ? values.FirstOrDefault() : null;
    }
}