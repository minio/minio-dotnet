using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Minio.UnitTests.Helpers;

internal static class HttpAsserts
{
    private static readonly Regex HeaderRegex = new(@"^[a-z0-9_\-]+: [a-zA-Z0-9_ :;.,\/""'?!(){}\[\]@<>=\-+*#$&`|~^%]+$");

    public static void AssertHeaders(this HttpRequestMessage request, params string[] expectedHeaders)
    {
        AssertHeaders("Request header mismatch", request.Headers, expectedHeaders);
    }
    
    public static void AssertHeaders(this HttpResponseMessage response, params string[] expectedHeaders)
    {
        AssertHeaders("Response header mismatch", response.Headers, expectedHeaders);
    }
    
    private static void AssertHeaders(string errorMessage, HttpHeaders gotHeaders, params string[] expectedHeaders)
    {
#pragma warning disable CA1308  
        // Lower-case is easier to read and headers use plain ASCII anyway
        var expectedHeaderList = new List<string>(expectedHeaders);
        var gotHeaderList = new List<string>(
            from header in gotHeaders 
            from value in header.Value 
            select $"{header.Key.ToLowerInvariant()}: {value}");
#pragma warning restore CA1308

        expectedHeaderList.Sort();
        gotHeaderList.Sort();

        var invalidHeaders = expectedHeaders.Where(h => !HeaderRegex.IsMatch(h));
        var invalidHeadersText = string.Join('\n', invalidHeaders);
        if (!string.IsNullOrEmpty(invalidHeadersText))
            throw new ArgumentException($"Invalid expected header (invalid unit-test):\n{invalidHeadersText}", nameof(expectedHeaders));

        var (g,e) = (0,0);
        var error = new StringBuilder();
        while (g < gotHeaderList.Count || e < expectedHeaderList.Count)
        {
            var got = g < gotHeaderList.Count ? gotHeaderList[g] : string.Empty;
            var expected = e < expectedHeaderList.Count ? expectedHeaderList[e] : string.Empty;

            if (string.IsNullOrEmpty(expected) || string.Compare(got, expected, StringComparison.Ordinal) < 0)
            {
                error.AppendLine(CultureInfo.InvariantCulture, $"+ {got}");
                g++;
                continue;
            }

            if (string.IsNullOrEmpty(got) || string.Compare(got, expected, StringComparison.Ordinal) > 0)
            {
                error.AppendLine(CultureInfo.InvariantCulture, $"- {expected}");
                e++;
                continue;
            }

            g++;
            e++;
        }
        
        if (error.Length > 0)
            Assert.Fail($"{errorMessage}:\n{error}");
    }
}