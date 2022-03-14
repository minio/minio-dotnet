/*
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

using Minio.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Minio
{
    /// <summary>
    /// V4Authenticator implements IAuthenticator interface.
    /// </summary>
    internal class V4Authenticator
    {
        private readonly string accessKey;
        private readonly string secretKey;
        private readonly string region;
        private readonly string sessionToken;

        private readonly string sha256EmptyFileHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        internal bool isAnonymous { get; private set; }
        internal bool isSecure { get; private set; }
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
        private static HashSet<string> ignoredHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authorization",
            "user-agent"
        };

        /// <summary>
        /// Authenticator constructor.
        /// </summary>
        /// <param name="secure"></param>
        /// <param name="accessKey">Access key id</param>
        /// <param name="secretKey">Secret access key</param>
        /// <param name="region">Region if specifically set</param>
        /// <param name="sessionToken">sessionToken</param>
        public V4Authenticator(bool secure, string accessKey, string secretKey, string region = "", string sessionToken = "")
        {
            this.isSecure = secure;
            this.accessKey = accessKey;
            this.secretKey = secretKey;
            this.isAnonymous = utils.IsAnonymousClient(accessKey, secretKey);
            this.region = region;
            this.sessionToken = sessionToken;
        }

        private string GetRegion(string endpoint)
        {
            if (!string.IsNullOrEmpty(this.region))
            {
                return this.region;
            }

            string region = Regions.GetRegionFromEndpoint(endpoint);
            return (region == string.Empty) ? "us-east-1" : region;
        }

        /// <summary>
        /// Implements Authenticate interface method for IAuthenticator.
        /// </summary>
        /// <param name="requestBuilder">Instantiated IRestRequest object</param>
        public string Authenticate(HttpRequestMessageBuilder requestBuilder)
        {
            DateTime signingDate = DateTime.UtcNow;

            this.SetContentSha256(requestBuilder);

            requestBuilder.RequestUri = requestBuilder.Request.RequestUri;
            var requestUri = requestBuilder.RequestUri;

            if (requestUri.Port == 80 || requestUri.Port == 443)
            {
                this.SetHostHeader(requestBuilder, requestUri.Host);
            }
            else
            {
                this.SetHostHeader(requestBuilder, requestUri.Host + ":" + requestUri.Port);
            }
            this.SetDateHeader(requestBuilder, signingDate);
            this.SetSessionTokenHeader(requestBuilder, this.sessionToken);
            SortedDictionary<string, string> headersToSign = this.GetHeadersToSign(requestBuilder);
            string signedHeaders = this.GetSignedHeaders(headersToSign);
            string canonicalRequest = this.GetCanonicalRequest(requestBuilder, headersToSign);
            byte[] canonicalRequestBytes = System.Text.Encoding.UTF8.GetBytes(canonicalRequest);
            string canonicalRequestHash = this.BytesToHex(this.ComputeSha256(canonicalRequestBytes));
            string region = this.GetRegion(requestUri.Host);
            string stringToSign = this.GetStringToSign(region, signingDate, canonicalRequestHash);

            byte[] signingKey = this.GenerateSigningKey(region, signingDate);

            byte[] stringToSignBytes = System.Text.Encoding.UTF8.GetBytes(stringToSign);

            byte[] signatureBytes = this.SignHmac(signingKey, stringToSignBytes);

            string signature = this.BytesToHex(signatureBytes);

            string authorization = this.GetAuthorizationHeader(signedHeaders, signature, signingDate, region);
            return authorization;
        }

        /// <summary>
        /// Get credential string of form {ACCESSID}/date/region/s3/aws4_request.
        /// </summary>
        /// <param name="signingDate">Signature initiated date</param>
        /// <param name="region">Region for the credential string</param>
        /// <returns>Credential string for the authorization header</returns>
        public string GetCredentialString(DateTime signingDate, string region)
        {
            var scope = this.GetScope(region, signingDate);
            return $"{this.accessKey}/{scope}";
        }

        /// <summary>
        /// Constructs an authorization header.
        /// </summary>
        /// <param name="signedHeaders">All signed http headers</param>
        /// <param name="signature">Hexadecimally encoded computed signature</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <param name="region">Requested region</param>
        /// <returns>Fully formed authorization header</returns>
        private string GetAuthorizationHeader(string signedHeaders, string signature, DateTime signingDate, string region)
        {
            var scope = this.GetScope(region, signingDate);
            return $"AWS4-HMAC-SHA256 Credential={this.accessKey}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}";
        }

        /// <summary>
        /// Concatenates sorted list of signed http headers.
        /// </summary>
        /// <param name="headersToSign">Sorted dictionary of headers to be signed</param>
        /// <returns>All signed headers</returns>
        private string GetSignedHeaders(SortedDictionary<string, string> headersToSign)
        {
            return string.Join(";", headersToSign.Keys);
        }

        /// <summary>
        /// Generates signing key based on the region and date.
        /// </summary>
        /// <param name="region">Requested region</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <returns>bytes of computed hmac</returns>
        private byte[] GenerateSigningKey(string region, DateTime signingDate)
        {
            byte[] formattedDateBytes = System.Text.Encoding.UTF8.GetBytes(signingDate.ToString("yyyyMMdd"));
            byte[] formattedKeyBytes = System.Text.Encoding.UTF8.GetBytes($"AWS4{this.secretKey}");
            byte[] dateKey = this.SignHmac(formattedKeyBytes, formattedDateBytes);

            byte[] regionBytes = System.Text.Encoding.UTF8.GetBytes(region);
            byte[] dateRegionKey = this.SignHmac(dateKey, regionBytes);

            byte[] serviceBytes = System.Text.Encoding.UTF8.GetBytes("s3");
            byte[] dateRegionServiceKey = this.SignHmac(dateRegionKey, serviceBytes);

            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes("aws4_request");
            return this.SignHmac(dateRegionServiceKey, requestBytes);
        }

        /// <summary>
        /// Compute hmac of input content with key.
        /// </summary>
        /// <param name="key">Hmac key</param>
        /// <param name="content">Bytes to be hmac computed</param>
        /// <returns>Computed hmac of input content</returns>
        private byte[] SignHmac(byte[] key, byte[] content)
        {
            HMACSHA256 hmac = new HMACSHA256(key);
            hmac.Initialize();
            return hmac.ComputeHash(content);
        }

        /// <summary>
        /// Get string to sign.
        /// </summary>
        /// <param name="region">Requested region</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <param name="canonicalRequestHash">Hexadecimal encoded sha256 checksum of canonicalRequest</param>
        /// <returns>String to sign</returns>
        private string GetStringToSign(string region, DateTime signingDate, string canonicalRequestHash)
        {
            var scope = this.GetScope(region, signingDate);
            return $"AWS4-HMAC-SHA256\n{signingDate:yyyyMMddTHHmmssZ}\n{scope}\n{canonicalRequestHash}";
        }

        /// <summary>
        /// Get scope.
        /// </summary>
        /// <param name="region">Requested region</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <returns>Scope string</returns>
        private string GetScope(string region, DateTime signingDate)
        {
            return $"{signingDate:yyyyMMdd}/{region}/s3/aws4_request";
        }

        /// <summary>
        /// Compute sha256 checksum.
        /// </summary>
        /// <param name="body">Bytes body</param>
        /// <returns>Bytes of sha256 checksum</returns>
        private byte[] ComputeSha256(byte[] body)
        {
            var sha256 = SHA256.Create();
            return sha256.ComputeHash(body);
        }

        /// <summary>
        /// Convert bytes to hexadecimal string.
        /// </summary>
        /// <param name="checkSum">Bytes of any checksum</param>
        /// <returns>Hexlified string of input bytes</returns>
        private string BytesToHex(byte[] checkSum)
        {
            return BitConverter.ToString(checkSum).Replace("-", string.Empty).ToLower();
        }

        /// <summary>
        /// Generate signature for post policy.
        /// </summary>
        /// <param name="region">Requested region</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <param name="policyBase64">Base64 encoded policy JSON</param>
        /// <returns>Computed signature</returns>
        public string PresignPostSignature(string region, DateTime signingDate, string policyBase64)
        {
            byte[] signingKey = this.GenerateSigningKey(region, signingDate);
            byte[] stringToSignBytes = System.Text.Encoding.UTF8.GetBytes(policyBase64);

            byte[] signatureBytes = this.SignHmac(signingKey, stringToSignBytes);
            string signature = this.BytesToHex(signatureBytes);

            return signature;
        }

        /// <summary>
        /// Presigns any input client object with a requested expiry.
        /// </summary>
        /// <param name="requestBuilder">Instantiated requestBuilder</param>
        /// <param name="expires">Expiration in seconds</param>
        /// <param name="region">Region of storage</param>
        /// <param name="sessionToken">Value for session token</param>
        /// <param name="reqDate"> Optional requestBuilder date and time in UTC</param>
        /// <returns>Presigned url</returns>
        internal string PresignURL(HttpRequestMessageBuilder requestBuilder, int expires, string region = "", string sessionToken = "", DateTime? reqDate = null)
        {
            var signingDate = reqDate ?? DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(region))
            {
                region = this.GetRegion(requestBuilder.RequestUri.Host);
            }

            Uri requestUri = requestBuilder.RequestUri;
            string requestQuery = requestUri.Query;

            SortedDictionary<string, string> headersToSign = this.GetHeadersToSign(requestBuilder);
            if (!string.IsNullOrEmpty(sessionToken))
            {
                headersToSign["X-Amz-Security-Token"] = sessionToken;
            }

            if (requestQuery.Length > 0)
            {
                requestQuery += "&";
            }
            requestQuery += "X-Amz-Algorithm=AWS4-HMAC-SHA256&";
            requestQuery += "X-Amz-Credential="
                + Uri.EscapeDataString(this.accessKey + "/" + this.GetScope(region, signingDate))
                + "&";
            requestQuery += "X-Amz-Date="
                + signingDate.ToString("yyyyMMddTHHmmssZ")
                + "&";
            requestQuery += "X-Amz-Expires="
                + expires
                + "&";
            requestQuery += "X-Amz-SignedHeaders=host";

            var presignUri = new UriBuilder(requestUri) { Query = requestQuery }.Uri;
            string canonicalRequest = this.GetPresignCanonicalRequest(requestBuilder.Method, presignUri, headersToSign);
            string headers = string.Concat(headersToSign.Select(p => $"&{p.Key}={utils.UrlEncode(p.Value)}"));
            byte[] canonicalRequestBytes = System.Text.Encoding.UTF8.GetBytes(canonicalRequest);
            string canonicalRequestHash = this.BytesToHex(ComputeSha256(canonicalRequestBytes));
            string stringToSign = this.GetStringToSign(region, signingDate, canonicalRequestHash);
            byte[] signingKey = this.GenerateSigningKey(region, signingDate);
            byte[] stringToSignBytes = System.Text.Encoding.UTF8.GetBytes(stringToSign);
            byte[] signatureBytes = this.SignHmac(signingKey, stringToSignBytes);
            string signature = this.BytesToHex(signatureBytes);

            // Return presigned url.
            var signedUri = new UriBuilder(presignUri) { Query = $"{requestQuery}{headers}&X-Amz-Signature={signature}" };
            if (signedUri.Uri.IsDefaultPort)
            {
                signedUri.Port = -1;
            }
            return Convert.ToString(signedUri);
        }

        /// <summary>
        /// Get presign canonical requestBuilder.
        /// </summary>
        /// <param name="requestMethod">HTTP method used for this requestBuilder</param>
        /// <param name="uri">Full url for this requestBuilder, including all query parameters except for headers and X-Amz-Signature</param>
        /// <param name="headersToSign">The key-value of headers.</param>
        /// <returns>Presigned canonical requestBuilder</returns>
        internal string GetPresignCanonicalRequest(HttpMethod requestMethod, Uri uri, SortedDictionary<string, string> headersToSign)
        {
            var canonicalStringList = new LinkedList<string>();
            canonicalStringList.AddLast(requestMethod.ToString());

            string path = uri.AbsolutePath;

            canonicalStringList.AddLast(path);
            var queryParams = uri.Query.TrimStart('?').Split('&').ToList();
            queryParams.AddRange(headersToSign.Select(cv =>
                $"{utils.UrlEncode(cv.Key)}={utils.UrlEncode(s3utils.TrimAll(cv.Value))}"));
            queryParams.Sort(StringComparer.Ordinal);
            string query = string.Join("&", queryParams);
            canonicalStringList.AddLast(query);
            var canonicalHost = GetCanonicalHost(uri);
            canonicalStringList.AddLast($"host:{canonicalHost}");

            canonicalStringList.AddLast(string.Empty);
            canonicalStringList.AddLast("host");
            canonicalStringList.AddLast("UNSIGNED-PAYLOAD");

            return string.Join("\n", canonicalStringList);
        }

        private static string GetCanonicalHost(Uri url)
        {
            string canonicalHost;
            if (url.Port > 0 && (url.Port != 80 && url.Port != 443))
            {
                canonicalHost = $"{url.Host}:{url.Port}";
            }
            else
            {
                canonicalHost = url.Host;
            }

            return canonicalHost;
        }

        /// <summary>
        /// Get canonical requestBuilder.
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

            var resource = requestBuilder.RequestUri.PathAndQuery;
            string[] path = resource.Split(new char[] { '?' }, 2);
            if (!path[0].StartsWith("/"))
            {
                path[0] = $"/{path[0]}";
            }
            canonicalStringList.AddLast(path[0]);
            Dictionary<string, string> queryParams =
                requestBuilder.QueryParameters.ToDictionary(o => o.Key, o => Uri.EscapeDataString(o.Value));
            var sb1 = new StringBuilder();
            var queryKeys = new List<string>(queryParams.Keys);
            queryKeys.Sort(StringComparer.Ordinal);
            foreach (var p in queryKeys)
            {
                if (sb1.Length > 0)
                    sb1.Append("&");
                sb1.AppendFormat("{0}={1}", p, queryParams[p]);
            }
            var query = sb1.ToString();
            canonicalStringList.AddLast(query);

            foreach (string header in headersToSign.Keys)
            {
                canonicalStringList.AddLast(header + ":" + s3utils.TrimAll(headersToSign[header]));
            }
            canonicalStringList.AddLast(string.Empty);
            canonicalStringList.AddLast(string.Join(";", headersToSign.Keys));
            if (headersToSign.Keys.Contains("x-amz-content-sha256"))
            {
                canonicalStringList.AddLast(headersToSign["x-amz-content-sha256"]);
            }
            else
            {
                canonicalStringList.AddLast(sha256EmptyFileHash);
            }

            return string.Join("\n", canonicalStringList);
        }

        public static Dictionary<string, TValue> ToDictionary<TValue>(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, TValue>>(json);
            return dictionary;
        }

        /// <summary>
        /// Get headers to be signed.
        /// </summary>
        /// <param name="requestBuilder">Instantiated requesst</param>
        /// <returns>Sorted dictionary of headers to be signed</returns>
        private SortedDictionary<string, string> GetHeadersToSign(HttpRequestMessageBuilder requestBuilder)
        {

            var headers = requestBuilder.HeaderParameters.ToList();
            var sortedHeaders = new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (var header in headers)
            {
                string headerName = header.Key.ToLower();
                string headerValue = header.Value;

                if (!ignoredHeaders.Contains(headerName))
                {
                    sortedHeaders.Add(headerName, headerValue);
                }
            }

            return sortedHeaders;
        }

        /// <summary>
        /// Sets 'x-amz-date' http header.
        /// </summary>
        /// <param name="requestBuilder">Instantiated requestBuilder object</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        private void SetDateHeader(HttpRequestMessageBuilder requestBuilder, DateTime signingDate)
        {
            requestBuilder.AddOrUpdateHeaderParameter("x-amz-date", signingDate.ToString("yyyyMMddTHHmmssZ"));
        }

        /// <summary>
        /// Set 'Host' http header.
        /// </summary>
        /// <param name="requestBuilder">Instantiated requestBuilder object</param>
        /// <param name="hostUrl">Host url</param>
        private void SetHostHeader(HttpRequestMessageBuilder requestBuilder, string hostUrl)
        {
            requestBuilder.AddOrUpdateHeaderParameter("Host", hostUrl);
        }

        /// <summary>
        /// Set 'X-Amz-Security-Token' http header.
        /// </summary>
        /// <param name="requestBuilder">Instantiated requestBuilder object</param>
        /// <param name="sessionToken">session token</param>
        private void SetSessionTokenHeader(HttpRequestMessageBuilder requestBuilder, string sessionToken)
        {
            if (!string.IsNullOrEmpty(sessionToken))
            {
                requestBuilder.AddOrUpdateHeaderParameter("X-Amz-Security-Token", sessionToken);
            }
        }

        /// <summary>
        /// Set 'x-amz-content-sha256' http header.
        /// </summary>
        /// <param name="requestBuilder">Instantiated requestBuilder object</param>
        private void SetContentSha256(HttpRequestMessageBuilder requestBuilder)
        {
            if (this.isAnonymous)
                return;
            // No need to compute SHA256 if the endpoint scheme is https
            // or the command method is not a Post to delete multiple files
            var isMultiDeleteRequest = false;
            if (requestBuilder.Method == HttpMethod.Post)
            {
                isMultiDeleteRequest = requestBuilder.QueryParameters.Any(p => p.Key.Equals("delete", StringComparison.OrdinalIgnoreCase));
            }
            if (isSecure || isMultiDeleteRequest)
            {
                requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", "UNSIGNED-PAYLOAD");
                return;
            }
            // For insecure, authenticated requests set sha256 header instead of MD5.
            if (requestBuilder.Method.Equals(HttpMethod.Put) ||
                requestBuilder.Method.Equals(HttpMethod.Post))
            {
                var body = requestBuilder.Content;
                if (body == null)
                {
                    requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", sha256EmptyFileHash);
                    return;
                }
                var sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(body);
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", hex);
            }
            else if (!isSecure && requestBuilder.Content != null)
            {
                var md5 = MD5.Create();
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(requestBuilder.Content.ToString()));

                string base64 = Convert.ToBase64String(hash);
                requestBuilder.AddHeaderParameter("Content-Md5", base64);
            }
            else
            {
                requestBuilder.AddOrUpdateHeaderParameter("x-amz-content-sha256", sha256EmptyFileHash);
            }
        }
    }
}