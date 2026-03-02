using System.Globalization;

namespace Minio.Helpers;

internal static class StringExtensions
{
    private static readonly string[] Iso8601Formats =
    {
        "yyyy-MM-ddTHH:mm:ss.FFFzzz",
        "yyyy-MM-ddTHH:mm:ss.FFFZ",
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-ddTHH:mm:ssZ",
    };
    
    public static DateTimeOffset? ParseIsoTimestamp(this string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        if (!DateTimeOffset.TryParseExact(text, Iso8601Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
            return null;

        return timestamp;
    }

    public static string ToIsoTimestamp(this DateTime timestamp)
    {
        return timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFF", CultureInfo.InvariantCulture);
    }

    public static string ToIsoTimestamp(this DateTimeOffset timestamp)
    {
        return timestamp.ToString("yyyy-MM-ddTHH:mm:ss.FFFzzz", CultureInfo.InvariantCulture);
    }
}