﻿/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017, 2020 MinIO, Inc.
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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Minio.Helper;

namespace Minio;

/// <summary>
///     V4Authenticator implements IAuthenticator interface.
/// </summary>
internal class V4Authenticator
{
    //
    // Excerpts from @lsegal - https://github.com/aws/aws-sdk-js/issues/659#issuecomment-120477258
    //
    // User-Agent:
    //
    //     This is ignored from signing because signing this causes problems with generating pre-signed URLs
    //     (that are executed by other agents) or when customers pass requests through proxies, which may
    //     modify the user-agent.
    //
    // Authorization:
    //
    //     Is skipped for obvious reasons
    //
    private static readonly HashSet<string> ignoredHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "user-agent"
    };

    private readonly string accessKey;
    private readonly string region;
    private readonly string secretKey;
    private readonly string sessionToken;

    private readonly string sha256EmptyFileHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>
    ///     Authenticator constructor.
    /// </summary>
    /// <param name="secure"></param>
    /// <param name="accessKey">Access key id</param>
    /// <param name="secretKey">Secret access key</param>
    /// <param name="region">Region if specifically set</param>
    /// <param name="sessionToken">sessionToken</param>
    public V4Authenticator(bool secure, string accessKey, string secretKey, string region = "",
        string sessionToken = "")
    {
        IsSecure = secure;
        this.accessKey = accessKey;
        this.secretKey = secretKey;
        IsAnonymous = Utils.IsAnonymousClient(accessKey, secretKey);
        this.region = region;
        this.sessionToken = sessionToken;
    }

    internal bool IsAnonymous { get; }
    internal bool IsSecure { get; }

    private string GetRegion(string endpoint)
    {
        if (!string.IsNullOrEmpty(region)) return region;

        var endpointRegion = Regions.GetRegionFromEndpoint(endpoint);
        return string.IsNullOrEmpty(endpointRegion) ? "us-east-1" : endpointRegion;
    }

    /// <summary>
    ///     Implements Authenticate interface method for IAuthenticator.
    /// </summary>
    /// <param name="requestBuilder">Instantiated IRestRequest object</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    public string Authenticate(HttpRequestMessageBuilder requestBuilder, bool isSts = false)
    {
        var signingDate = DateTime.UtcNow;

        SetContentSha256(requestBuilder, isSts);

        requestBuilder.RequestUri = requestBuilder.Request.RequestUri;
        var requestUri = requestBuilder.RequestUri;

        if (requestUri.Port is 80 or 443)
            SetHostHeader(requestBuilder, requestUri.Host);
        else
            SetHostHeader(requestBuilder, requestUri.Host + ":" + requestUri.Port);
        SetDateHeader(requestBuilder, signingDate);
        SetSessionTokenHeader(requestBuilder, sessionToken);

        var headersToSign = GetHeadersToSign(requestBuilder);
        var signedHeaders = GetSignedHeaders(headersToSign);

        var canonicalRequest = GetCanonicalRequest(requestBuilder, headersToSign);
        ReadOnlySpan<byte> canonicalRequestBytes = Encoding.UTF8.GetBytes(canonicalRequest);
        var hash = ComputeSha256(canonicalRequestBytes);
        var canonicalRequestHash = BytesToHex(hash);
        var endpointRegion = GetRegion(requestUri.Host);
        var stringToSign = GetStringToSign(endpointRegion, signingDate, canonicalRequestHash, isSts);
        var signingKey = GenerateSigningKey(endpointRegion, signingDate, isSts);
        ReadOnlySpan<byte> stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);
        var signatureBytes = SignHmac(signingKey, stringToSignBytes);
        var signature = BytesToHex(signatureBytes);
        return GetAuthorizationHeader(signedHeaders, signature, signingDate, endpointRegion, isSts);
    }

    /// <summary>
    ///     Get credential string of form {ACCESSID}/date/region/serviceKind/aws4_request.
    /// </summary>
    /// <param name="signingDate">Signature initiated date</param>
    /// <param name="region">Region for the credential string</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>Credential string for the authorization header</returns>
    public string GetCredentialString(DateTime signingDate, string region, bool isSts = false)
    {
        var scope = GetScope(region, signingDate, isSts);
        return $"{accessKey}/{scope}";
    }

    /// <summary>
    ///     Constructs an authorization header.
    /// </summary>
    /// <param name="signedHeaders">All signed http headers</param>
    /// <param name="signature">Hexadecimally encoded computed signature</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="region">Requested region</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>Fully formed authorization header</returns>
    private string GetAuthorizationHeader(string signedHeaders, string signature, DateTime signingDate, string region,
        bool isSts = false)
    {
        var scope = GetScope(region, signingDate, isSts);
        return $"AWS4-HMAC-SHA256 Credential={accessKey}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}";
    }

    /// <summary>
    ///     Concatenates sorted list of signed http headers.
    /// </summary>
    /// <param name="headersToSign">Sorted dictionary of headers to be signed</param>
    /// <returns>All signed headers</returns>
    private string GetSignedHeaders(SortedDictionary<string, string> headersToSign)
    {
        return string.Join(";", headersToSign.Keys);
    }

    /// <summary>
    ///     Determines and returns the kind of service
    /// </summary>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>returns the kind of service as a string</returns>
    private string GetService(bool isSts)
    {
        return isSts ? "sts" : "s3";
    }

    /// <summary>
    ///     Generates signing key based on the region and date.
    /// </summary>
    /// <param name="region">Requested region</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>bytes of computed hmac</returns>
    private ReadOnlySpan<byte> GenerateSigningKey(string region, DateTime signingDate, bool isSts = false)
    {
        ReadOnlySpan<byte> dateRegionServiceKey;
        ReadOnlySpan<byte> requestBytes;

        ReadOnlySpan<byte> serviceBytes = Encoding.UTF8.GetBytes(GetService(isSts));
        ReadOnlySpan<byte> formattedDateBytes = Encoding.UTF8.GetBytes(signingDate.ToString("yyyyMMdd"));
        ReadOnlySpan<byte> formattedKeyBytes = Encoding.UTF8.GetBytes($"AWS4{secretKey}");
        var dateKey = SignHmac(formattedKeyBytes, formattedDateBytes);
        ReadOnlySpan<byte> regionBytes = Encoding.UTF8.GetBytes(region);
        var dateRegionKey = SignHmac(dateKey, regionBytes);
        dateRegionServiceKey = SignHmac(dateRegionKey, serviceBytes);
        requestBytes = Encoding.UTF8.GetBytes("aws4_request");
        var hmac = SignHmac(dateRegionServiceKey, requestBytes);
#if NETSTANDARD
        var signingKey = Encoding.UTF8.GetString(hmac.ToArray());
#else
        var signingKey = Encoding.UTF8.GetString(hmac);
#endif
        return SignHmac(dateRegionServiceKey, requestBytes);
    }

    /// <summary>
    ///     Compute hmac of input content with key.
    /// </summary>
    /// <param name="key">Hmac key</param>
    /// <param name="content">Bytes to be hmac computed</param>
    /// <returns>Computed hmac of input content</returns>
    private ReadOnlySpan<byte> SignHmac(ReadOnlySpan<byte> key, ReadOnlySpan<byte> content)
    {
#if NETSTANDARD
        using var hmac = new HMACSHA256(key.ToArray());
        hmac.Initialize();
        return hmac.ComputeHash(content.ToArray());
#else
        return HMACSHA256.HashData(key, content);
#endif
    }

    /// <summary>
    ///     Get string to sign.
    /// </summary>
    /// <param name="region">Requested region</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="canonicalRequestHash">Hexadecimal encoded sha256 checksum of canonicalRequest</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>String to sign</returns>
    private string GetStringToSign(string region, DateTime signingDate,
        string canonicalRequestHash, bool isSts = false)
    {
        var scope = GetScope(region, signingDate, isSts);
        return $"AWS4-HMAC-SHA256\n{signingDate:yyyyMMddTHHmmssZ}\n{scope}\n{canonicalRequestHash}";
    }

    /// <summary>
    ///     Get scope.
    /// </summary>
    /// <param name="region">Requested region</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    /// <returns>Scope string</returns>
    private string GetScope(string region, DateTime signingDate, bool isSts = false)
    {
        return $"{signingDate:yyyyMMdd}/{region}/{GetService(isSts)}/aws4_request";
    }

    /// <summary>
    ///     Compute sha256 checksum.
    /// </summary>
    /// <param name="body">Bytes body</param>
    /// <returns>Bytes of sha256 checksum</returns>
    private ReadOnlySpan<byte> ComputeSha256(ReadOnlySpan<byte> body)
    {
#if NETSTANDARD
        using var sha = SHA256.Create();
        ReadOnlySpan<byte> hash
            = sha.ComputeHash(body.ToArray());
#else
        ReadOnlySpan<byte> hash = SHA256.HashData(body);
#endif
        return hash;
    }

    /// <summary>
    ///     Convert bytes to hexadecimal string.
    /// </summary>
    /// <param name="checkSum">Bytes of any checksum</param>
    /// <returns>Hexlified string of input bytes</returns>
    private string BytesToHex(ReadOnlySpan<byte> checkSum)
    {
        return BitConverter.ToString(checkSum.ToArray()).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>
    ///     Generate signature for post policy.
    /// </summary>
    /// <param name="region">Requested region</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    /// <param name="policyBase64">Base64 encoded policy JSON</param>
    /// <returns>Computed signature</returns>
    public string PresignPostSignature(string region, DateTime signingDate, string policyBase64)
    {
        var signingKey = GenerateSigningKey(region, signingDate);
        ReadOnlySpan<byte> stringToSignBytes = Encoding.UTF8.GetBytes(policyBase64);
        var signatureBytes = SignHmac(signingKey, stringToSignBytes);
        var signature = BytesToHex(signatureBytes);
        return signature;
    }

    /// <summary>
    ///     Presigns any input client object with a requested expiry.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder</param>
    /// <param name="expires">Expiration in seconds</param>
    /// <param name="region">Region of storage</param>
    /// <param name="sessionToken">Value for session token</param>
    /// <param name="reqDate"> Optional requestBuilder date and time in UTC</param>
    /// <returns>Presigned url</returns>
    internal string PresignURL(HttpRequestMessageBuilder requestBuilder, int expires, string region = "",
        string sessionToken = "", DateTime? reqDate = null)
    {
        var signingDate = reqDate ?? DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(region)) region = GetRegion(requestBuilder.RequestUri.Host);

        var requestUri = requestBuilder.RequestUri;
        var requestQuery = requestUri.Query;

        var headersToSign = GetHeadersToSign(requestBuilder);
        if (!string.IsNullOrEmpty(sessionToken)) headersToSign["X-Amz-Security-Token"] = sessionToken;

        if (requestQuery.Length > 0) requestQuery += "&";
        requestQuery += "X-Amz-Algorithm=AWS4-HMAC-SHA256&";
        requestQuery += "X-Amz-Credential="
                        + Uri.EscapeDataString(accessKey + "/" + GetScope(region, signingDate))
                        + "&";
        requestQuery += "X-Amz-Date="
                        + signingDate.ToString("yyyyMMddTHHmmssZ")
                        + "&";
        requestQuery += "X-Amz-Expires="
                        + expires
                        + "&";
        requestQuery += "X-Amz-SignedHeaders=host";

        var presignUri = new UriBuilder(requestUri) { Query = requestQuery }.Uri;
        var canonicalRequest = GetPresignCanonicalRequest(requestBuilder.Method, presignUri, headersToSign);
        var headers = string.Concat(headersToSign.Select(p => $"&{p.Key}={Utils.UrlEncode(p.Value)}"));
        ReadOnlySpan<byte> canonicalRequestBytes = Encoding.UTF8.GetBytes(canonicalRequest);
        var canonicalRequestHash = BytesToHex(ComputeSha256(canonicalRequestBytes));
        var stringToSign = GetStringToSign(region, signingDate, canonicalRequestHash);
        var signingKey = GenerateSigningKey(region, signingDate);
        ReadOnlySpan<byte> stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);
        var signatureBytes = SignHmac(signingKey, stringToSignBytes);
        var signature = BytesToHex(signatureBytes);

        // Return presigned url.
        var signedUri = new UriBuilder(presignUri) { Query = $"{requestQuery}{headers}&X-Amz-Signature={signature}" };
        if (signedUri.Uri.IsDefaultPort) signedUri.Port = -1;
        return Convert.ToString(signedUri, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Get presign canonical requestBuilder.
    /// </summary>
    /// <param name="requestMethod">HTTP method used for this requestBuilder</param>
    /// <param name="uri">
    ///     Full url for this requestBuilder, including all query parameters except for headers and
    ///     X-Amz-Signature
    /// </param>
    /// <param name="headersToSign">The key-value of headers.</param>
    /// <returns>Presigned canonical requestBuilder</returns>
    internal string GetPresignCanonicalRequest(HttpMethod requestMethod, Uri uri,
        SortedDictionary<string, string> headersToSign)
    {
        var canonicalStringList = new LinkedList<string>();
        _ = canonicalStringList.AddLast(requestMethod.ToString());

        var path = uri.AbsolutePath;

        _ = canonicalStringList.AddLast(path);
        var queryParams = uri.Query.TrimStart('?').Split('&').ToList();
        queryParams.AddRange(headersToSign.Select(cv =>
            $"{Utils.UrlEncode(cv.Key)}={Utils.UrlEncode(cv.Value.Trim())}"));
        queryParams.Sort(StringComparer.Ordinal);
        var query = string.Join("&", queryParams);
        _ = canonicalStringList.AddLast(query);
        var canonicalHost = GetCanonicalHost(uri);
        _ = canonicalStringList.AddLast($"host:{canonicalHost}");

        _ = canonicalStringList.AddLast(string.Empty);
        _ = canonicalStringList.AddLast("host");
        _ = canonicalStringList.AddLast("UNSIGNED-PAYLOAD");

        return string.Join("\n", canonicalStringList);
    }

    private static string GetCanonicalHost(Uri url)
    {
        if (url.Port is > 0 and not 80 and not 443)
            return $"{url.Host}:{url.Port}";
        return url.Host;
    }

    /// <summary>
    ///     Get canonical requestBuilder.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="headersToSign">Dictionary of http headers to be signed</param>
    /// <returns>Canonical Request</returns>
    private string GetCanonicalRequest(HttpRequestMessageBuilder requestBuilder,
        SortedDictionary<string, string> headersToSign)
    {
        var canonicalStringList = new LinkedList<string>();
        // METHOD
        canonicalStringList.AddLast(requestBuilder.Method.ToString());

        var queryParamsDict = new Dictionary<string, string>(StringComparer.Ordinal);
        if (requestBuilder.QueryParameters is not null)
            foreach (var kvp in requestBuilder.QueryParameters)
                queryParamsDict[kvp.Key] = Uri.EscapeDataString(kvp.Value);

        var queryParams = "";
        if (queryParamsDict.Count > 0)
        {
            var sb1 = new StringBuilder();
            var queryKeys = new List<string>(queryParamsDict.Keys);
            queryKeys.Sort(StringComparer.Ordinal);
            foreach (var p in queryKeys)
            {
                if (sb1.Length > 0)
                    sb1.Append('&');
                sb1.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", p, queryParamsDict[p]);
            }

            queryParams = sb1.ToString();
        }

        var isFormData = false;
        if (requestBuilder.Request.Content?.Headers?.ContentType is not null)
            isFormData = string.Equals(requestBuilder.Request.Content.Headers.ContentType.ToString(),
                "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(queryParams) && isFormData)
        {
            // Convert stream content to byte[]
            var cntntByteData = Span<byte>.Empty;
            if (requestBuilder.Request.Content is not null)
                cntntByteData = requestBuilder.Request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

            // UTF conversion - String from bytes
#if NETSTANDARD
            queryParams = Encoding.UTF8.GetString(cntntByteData.ToArray(), 0, cntntByteData.Length);
#else
            queryParams = Encoding.UTF8.GetString(cntntByteData);
#endif
        }

        if (!string.IsNullOrEmpty(queryParams) &&
            !isFormData &&
            !string.Equals(requestBuilder.RequestUri.Query, "?location=", StringComparison.OrdinalIgnoreCase))
            requestBuilder.RequestUri = new Uri(requestBuilder.RequestUri + "?" + queryParams);

        canonicalStringList.AddLast(requestBuilder.RequestUri.AbsolutePath);
        canonicalStringList.AddLast(queryParams);

        // Headers to sign
        foreach (var header in headersToSign.Keys)
            canonicalStringList.AddLast(header + ":" + S3utils.TrimAll(headersToSign[header]));
        canonicalStringList.AddLast(string.Empty);
        canonicalStringList.AddLast(string.Join(";", headersToSign.Keys));
        if (headersToSign.TryGetValue("x-amz-content-sha256", out var value))
            canonicalStringList.AddLast(value);
        else
            canonicalStringList.AddLast(sha256EmptyFileHash);
        return string.Join("\n", canonicalStringList);
    }

    public static IDictionary<string, TValue> ToDictionary<TValue>(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, TValue>>(json);
        return dictionary;
    }

    /// <summary>
    ///     Get headers to be signed.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requesst</param>
    /// <returns>Sorted dictionary of headers to be signed</returns>
    private SortedDictionary<string, string> GetHeadersToSign(HttpRequestMessageBuilder requestBuilder)
    {
        var headers = requestBuilder.HeaderParameters.ToList();
        var sortedHeaders = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var header in headers)
        {
            var headerName = header.Key.ToLowerInvariant();
            var headerValue = header.Value;

            if (!ignoredHeaders.Contains(headerName)) sortedHeaders.Add(headerName, headerValue);
        }

        return sortedHeaders;
    }

    /// <summary>
    ///     Sets 'x-amz-date' http header.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="signingDate">Date for signature to be signed</param>
    private void SetDateHeader(HttpRequestMessageBuilder requestBuilder, DateTime signingDate)
    {
        requestBuilder.AddOrUpdateHeaderParameter("x-amz-date", signingDate.ToString("yyyyMMddTHHmmssZ"));
    }

    /// <summary>
    ///     Set 'Host' http header.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="hostUrl">Host url</param>
    private void SetHostHeader(HttpRequestMessageBuilder requestBuilder, string hostUrl)
    {
        requestBuilder.AddOrUpdateHeaderParameter("Host", hostUrl);
    }

    /// <summary>
    ///     Set 'X-Amz-Security-Token' http header.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="sessionToken">session token</param>
    private void SetSessionTokenHeader(HttpRequestMessageBuilder requestBuilder, string sessionToken)
    {
        if (!string.IsNullOrEmpty(sessionToken))
            requestBuilder.AddOrUpdateHeaderParameter("X-Amz-Security-Token", sessionToken);
    }

    /// <summary>
    ///     Set 'x-amz-content-sha256' http header.
    /// </summary>
    /// <param name="requestBuilder">Instantiated requestBuilder object</param>
    /// <param name="isSts">boolean; if true role credentials, otherwise IAM user</param>
    private void SetContentSha256(HttpRequestMessageBuilder requestBuilder, bool isSts = false)
    {
        if (IsAnonymous)
            return;
        // No need to compute SHA256 if the endpoint scheme is https
        // or the command method is not a Post to delete multiple files
        var isMultiDeleteRequest = false;
        if (requestBuilder.Method == HttpMethod.Post)
            isMultiDeleteRequest =
                requestBuilder.QueryParameters.Any(p => p.Key.Equals("delete", StringComparison.OrdinalIgnoreCase));

        if ((IsSecure && !isSts) || isMultiDeleteRequest)
        {
            requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", "UNSIGNED-PAYLOAD");
            return;
        }

        // For insecure, authenticated requests set sha256 header instead of MD5.
        if (requestBuilder.Method.Equals(HttpMethod.Put) ||
            requestBuilder.Method.Equals(HttpMethod.Post))
        {
            var body = requestBuilder.Content;
            if (body.IsEmpty)
            {
                requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", sha256EmptyFileHash);
                return;
            }
#if NETSTANDARD
            using var sha = SHA256.Create();
            var hash
                = sha.ComputeHash(body.ToArray());
#else
            var hash = SHA256.HashData(body.Span);
#endif
            var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", hex);
        }
        else if (!IsSecure && !requestBuilder.Content.IsEmpty)
        {
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(requestBuilder.Content.ToString());

#if NETSTANDARD
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
            using var md5
                = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
            var hash
                = md5.ComputeHash(bytes.ToArray());
#else
            ReadOnlySpan<byte> hash = MD5.HashData(bytes);
#endif
            var base64 = Convert.ToBase64String(hash);
            requestBuilder.AddHeaderParameter("Content-Md5", base64);
        }
        else
        {
            requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", sha256EmptyFileHash);
        }
    }
}
