/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Globalization;
using System.Net;

namespace Minio.Helper;

public static class BuilderUtil
{
    public static bool IsAwsDualStackEndpoint(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException($"'{nameof(endpoint)}' cannot be null or empty.", nameof(endpoint));

        return endpoint.Contains(".dualstack.");
    }

    public static bool IsAwsAccelerateEndpoint(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException($"'{nameof(endpoint)}' cannot be null or empty.", nameof(endpoint));

        return endpoint.StartsWith("s3-accelerate.", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAwsEndpoint(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException($"'{nameof(endpoint)}' cannot be null or empty.", nameof(endpoint));

        return (endpoint.StartsWith("s3.", StringComparison.OrdinalIgnoreCase) ||
                IsAwsAccelerateEndpoint(endpoint)) &&
               (endpoint.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase) ||
                endpoint.EndsWith(".amazonaws.com.cn", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsChineseDomain(string host)
    {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentException($"'{nameof(host)}' cannot be null or empty.", nameof(host));

        return host.EndsWith(".cn", StringComparison.OrdinalIgnoreCase);
    }

    public static string ExtractRegion(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException($"'{nameof(endpoint)}' cannot be null or empty.", nameof(endpoint));

        var tokens = endpoint.Split('.');
        if (tokens.Length < 2)
            return null;
        var token = tokens[1];

        // If token is "dualstack", then region might be in next token.
        if (token.Equals("dualstack", StringComparison.OrdinalIgnoreCase) && tokens.Length >= 3)
            token = tokens[2];

        // If token is equal to "amazonaws", region is not passed in the endpoint.
        if (token.Equals("amazonaws", StringComparison.OrdinalIgnoreCase))
            return null;

        // Return token as region.
        return token;
    }

    private static bool IsValidSmallInt(string val)
    {
        return byte.TryParse(val, out _);
    }

    private static bool IsValidOctetVal(string val)
    {
        const byte uLimit = 255;
        return byte.Parse(val, NumberStyles.Integer, CultureInfo.InvariantCulture) <= uLimit;
    }

    private static bool IsValidIPv4(string ip)
    {
        var posColon = ip.LastIndexOf(':');
        if (posColon != -1) ip = ip.Substring(0, posColon);
        var octetsStr = ip.Split('.');
        if (octetsStr.Length != 4) return false;
        var isValidSmallInt = Array.TrueForAll(octetsStr, IsValidSmallInt);
        if (!isValidSmallInt) return false;
        return Array.TrueForAll(octetsStr, IsValidOctetVal);
    }

    private static bool IsValidIP(string host)
    {
        return IPAddress.TryParse(host, out _);
    }

    public static bool IsValidHostnameOrIPAddress(string host)
    {
        // Let's do IP address check first.
        if (string.IsNullOrWhiteSpace(host)) return false;
        // IPv4 first
        if (IsValidIPv4(host)) return true;
        // IPv6 or other IP address format
        if (IsValidIP(host)) return true;
        // Remove any port in endpoint, in such a case.
        var posColon = host.LastIndexOf(':');
        if (posColon != -1)
        {
            try
            {
                var port = int.Parse(host.Substring(posColon + 1, host.Length - posColon - 1),
                    CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return false;
            }

            host = host.Substring(0, posColon);
        }

        // Check host if it is a hostname.
        return Uri.CheckHostName(host).ToString().Equals("dns", StringComparison.OrdinalIgnoreCase);
    }
}