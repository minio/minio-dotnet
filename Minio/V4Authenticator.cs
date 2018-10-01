/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Minio
{
    /// <summary>
    /// V4Authenticator implements IAuthenticator interface.
    /// </summary>
    internal class V4Authenticator : IAuthenticator
    {
        private readonly string accessKey;
        private readonly string secretKey;
        private string Region;
        internal bool isAnonymous { get; private set; }
        internal bool isSecure { get; private set; }
        //
        // Excerpts from @lsegal - https://github.com/aws/aws-sdk-js/issues/659#issuecomment-120477258
        //
        //  User-Agent:
        //
        //      This is ignored from signing because signing this causes problems with generating pre-signed URLs
        //      (that are executed by other agents) or when customers pass requests through proxies, which may
        //      modify the user-agent.
        //
        //  Content-Length:
        //
        //      This is ignored from signing because generating a pre-signed URL should not provide a content-length
        //      constraint, specifically when vending a S3 pre-signed PUT URL. The corollary to this is that when
        //      sending regular requests (non-pre-signed), the signature contains a checksum of the body, which
        //      implicitly validates the payload length (since changing the number of bytes would change the checksum)
        //      and therefore this header is not valuable in the signature.
        //
        //  Content-Type:
        //
        //      Signing this header causes quite a number of problems in browser environments, where browsers
        //      like to modify and normalize the content-type header in different ways. There is more information
        //      on this in https://github.com/aws/aws-sdk-js/issues/244. Avoiding this field simplifies logic
        //      and reduces the possibility of future bugs
        //
        //  Authorization:
        //
        //      Is skipped for obvious reasons
        //
        private static HashSet<string> ignoredHeaders = new HashSet<string>() {
            "authorization",
            "content-length",
            "content-type",
            "user-agent"
        };

        /// <summary>
        /// Authenticator constructor.
        /// </summary>
        /// <param name="secure"></param>
        /// <param name="accessKey">Access key id</param>
        /// <param name="secretKey">Secret access key</param>
        public V4Authenticator(bool secure,string accessKey, string secretKey)
        {
            this.isSecure = secure;
            this.accessKey = accessKey;
            this.secretKey = secretKey;
            this.isAnonymous = String.IsNullOrEmpty(accessKey) && String.IsNullOrEmpty(secretKey);
        }

        private String getRegion(string url)
        {
            string region = Regions.GetRegionFromEndpoint(url);
            return (region == "") ? "us-east-1" : region;
        }
      
        /// <summary>
        /// Implements Authenticate interface method for IAuthenticator.
        /// </summary>
        /// <param name="client">Instantiated IRestClient object</param>
        /// <param name="request">Instantiated IRestRequest object</param>
        public void Authenticate(IRestClient client, IRestRequest request)
        {
            DateTime signingDate = DateTime.UtcNow;
            SetContentMd5(request);
            SetContentSha256(request);
            SetHostHeader(request, client.BaseUrl.Host + ":" + client.BaseUrl.Port);
            SetDateHeader(request, signingDate);
            SortedDictionary<string, string> headersToSign = GetHeadersToSign(request);
            string signedHeaders = GetSignedHeaders(headersToSign);
            string canonicalRequest = GetCanonicalRequest(request, headersToSign);
            byte[] canonicalRequestBytes = System.Text.Encoding.UTF8.GetBytes(canonicalRequest);
            string canonicalRequestHash = BytesToHex(ComputeSha256(canonicalRequestBytes));
            string region = this.getRegion(client.BaseUrl.Host);
            string stringToSign = GetStringToSign(region, signingDate, canonicalRequestHash);
            byte[] signingKey = GenerateSigningKey(region, signingDate);

            byte[] stringToSignBytes = System.Text.Encoding.UTF8.GetBytes(stringToSign);

            byte[] signatureBytes = SignHmac(signingKey, stringToSignBytes);

            string signature = BytesToHex(signatureBytes);
          
            string authorization = GetAuthorizationHeader(signedHeaders, signature, signingDate, region);
            request.AddHeader("Authorization", authorization);

        }

        /// <summary>
        /// Get credential string of form {ACCESSID}/date/region/s3/aws4_request. 
        /// </summary>
        /// <param name="signingDate">Signature initated date</param>
        /// <param name="region">Region for the credential string</param>
        /// <returns>Credential string for the authorization header</returns>
        public string GetCredentialString(DateTime signingDate, string region)
        {
                return this.accessKey + "/" + GetScope(region, signingDate);
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
            return "AWS4-HMAC-SHA256 Credential=" + this.accessKey + "/" + GetScope(region, signingDate) +
                ", SignedHeaders=" + signedHeaders + ", Signature=" + signature;
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
            byte[] formattedDateBytes = System.Text.Encoding.UTF8.GetBytes(signingDate.ToString("yyyMMdd"));
            byte[] formattedKeyBytes = System.Text.Encoding.UTF8.GetBytes("AWS4" + this.secretKey);
            byte[] dateKey = SignHmac(formattedKeyBytes, formattedDateBytes);

            byte[] regionBytes = System.Text.Encoding.UTF8.GetBytes(region);
            byte[] dateRegionKey = SignHmac(dateKey, regionBytes);

            byte[] serviceBytes = System.Text.Encoding.UTF8.GetBytes("s3");
            byte[] dateRegionServiceKey = SignHmac(dateRegionKey, serviceBytes);

            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes("aws4_request");
            return SignHmac(dateRegionServiceKey, requestBytes);
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
            return "AWS4-HMAC-SHA256\n" +
                signingDate.ToString("yyyyMMddTHHmmssZ") + "\n" +
                GetScope(region, signingDate) + "\n" +
                canonicalRequestHash;
        }

        /// <summary>
        /// Get scope.
        /// </summary>
        /// <param name="region">Requested region</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <returns>Scope string</returns>
        private string GetScope(string region, DateTime signingDate)
        {
            string formattedDate = signingDate.ToString("yyyyMMdd");
            return formattedDate + "/" + region + "/s3/aws4_request";
        }

        /// <summary>
        /// Compute sha256 checksum.
        /// </summary>
        /// <param name="body">Bytes body</param>
        /// <returns>Bytes of sha256 checksum</returns>
        private byte[] ComputeSha256(byte[] body)
        {

            SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
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

            byte[] signatureBytes = SignHmac(signingKey, stringToSignBytes);
            string signature = BytesToHex(signatureBytes);
                
            return signature;
        }
        
        /// <summary>
        /// Presigns any input client object with a requested expiry.
        /// </summary>
        /// <param name="client">Instantiated client</param>
        /// <param name="request">Instantiated request</param>
        /// <param name="expires">Expiration in seconds</param>
        /// <returns>Presigned url</returns>      
        public string PresignURL(IRestClient client, IRestRequest request, int expires)
        {
            DateTime signingDate = DateTime.UtcNow;
            string requestQuery = "";
            string path = request.Resource;
            string region = this.getRegion(client.BaseUrl.Host);

            requestQuery = "X-Amz-Algorithm=AWS4-HMAC-SHA256&";
            requestQuery += "X-Amz-Credential="
                + this.accessKey
                + Uri.EscapeDataString("/" + GetScope(region, signingDate))
                + "&";
            requestQuery += "X-Amz-Date="
                + signingDate.ToString("yyyyMMddTHHmmssZ")
                + "&";
            requestQuery += "X-Amz-Expires="
                + expires
                + "&";
            requestQuery += "X-Amz-SignedHeaders=host";

            SortedDictionary<string,string> headersToSign = GetHeadersToSign(request);
            string canonicalRequest = GetPresignCanonicalRequest(client, request, requestQuery, headersToSign);
            string headers = string.Join("&", headersToSign.Select(p => p.Key + "=" + utils.UrlEncode(p.Value)));
            byte[] canonicalRequestBytes = System.Text.Encoding.UTF8.GetBytes(canonicalRequest);
            string canonicalRequestHash = BytesToHex(ComputeSha256(canonicalRequestBytes));
            string stringToSign = GetStringToSign(region, signingDate, canonicalRequestHash);
            byte[] signingKey = GenerateSigningKey(region, signingDate);
            byte[] stringToSignBytes = System.Text.Encoding.UTF8.GetBytes(stringToSign);
            byte[] signatureBytes = SignHmac(signingKey, stringToSignBytes);
            string signature = BytesToHex(signatureBytes);

            // Return presigned url.
            return client.BaseUrl + path + "?" + requestQuery + "&" + headers + "&X-Amz-Signature=" + signature;
        }

        /// <summary>
        /// Get presign canonical request.
        /// </summary>
        /// <param name="client">Instantiated client object</param>
        /// <param name="request">Instantiated request object</param>
        /// <param name="requestQuery">Additional request query params</param>
        /// <param name="headersToSign"></param>
        /// <returns>Presigned canonical request</returns>
        private string GetPresignCanonicalRequest(IRestClient client, IRestRequest request, string requestQuery,  SortedDictionary<string,string> headersToSign)
        {
            LinkedList<string> canonicalStringList = new LinkedList<string>();
            // METHOD
            canonicalStringList.AddLast(request.Method.ToString());

            string path = request.Resource;
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            canonicalStringList.AddLast(path);
            String query = headersToSign.Aggregate(requestQuery, (pv, cv) => $"{pv}&{utils.UrlEncode(cv.Key)}={utils.UrlEncode(cv.Value)}");
            canonicalStringList.AddLast(query);
            if (client.BaseUrl.Port > 0 && (client.BaseUrl.Port != 80 && client.BaseUrl.Port != 443))
            {
                canonicalStringList.AddLast("host:" + client.BaseUrl.Host + ":" + client.BaseUrl.Port);
            }
            else
            {
                canonicalStringList.AddLast("host:" + client.BaseUrl.Host);
            }

            canonicalStringList.AddLast("");
            canonicalStringList.AddLast("host");
            canonicalStringList.AddLast("UNSIGNED-PAYLOAD");

            return string.Join("\n", canonicalStringList);
        }

        /// <summary>
        /// Get canonical request.
        /// </summary>
        /// <param name="client">Instantiated client object</param>
        /// <param name="request">Instantiated request object</param>
        /// <param name="headersToSign">Dictionary of http headers to be signed</param>
        /// <returns>Canonical Request</returns>
        private string GetCanonicalRequest(IRestRequest request,
            SortedDictionary<string, string> headersToSign)
        {
            LinkedList<string> canonicalStringList = new LinkedList<string>();
            // METHOD
            canonicalStringList.AddLast(request.Method.ToString());

            string[] path = request.Resource.Split(new char[] { '?' }, 2);
            if (!path[0].StartsWith("/"))
            {
                path[0] = "/" + path[0];
            }
            canonicalStringList.AddLast(path[0]);

            string query = "";
            // QUERY
            if (path.Length == 2)
            {
                var parameterString = path[1];
                var parameterList = parameterString.Split('&');
                SortedSet<string> sortedQueries = new SortedSet<string>();
                foreach (string individualParameterString in parameterList)
                {
                    if (individualParameterString.Contains('='))
                    {
                        string[] splitQuery = individualParameterString.Split(new char[] { '=' }, 2);
                        sortedQueries.Add(splitQuery[0] + "=" + splitQuery[1]);
                    }
                    else
                    {
                        sortedQueries.Add(individualParameterString + "=");
                    }
                }
                query = string.Join("&", sortedQueries);
            }
            canonicalStringList.AddLast(query);

            foreach (string header in headersToSign.Keys)
            {
                canonicalStringList.AddLast(header + ":" + headersToSign[header]);
            }
            canonicalStringList.AddLast("");
            canonicalStringList.AddLast(string.Join(";", headersToSign.Keys));
            if (headersToSign.Keys.Contains("x-amz-content-sha256"))
            {
                canonicalStringList.AddLast(headersToSign["x-amz-content-sha256"]);
            }
            else
            {
                canonicalStringList.AddLast("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            }

            return string.Join("\n", canonicalStringList);
        }

        /// <summary>
        /// Get headers to be signed.
        /// </summary>
        /// <param name="request">Instantiated requesst</param>
        /// <returns>Sorted dictionary of headers to be signed</returns>
        private SortedDictionary<string, string> GetHeadersToSign(IRestRequest request)
        {
            var headers = request.Parameters.Where(p => p.Type.Equals(ParameterType.HttpHeader)).ToList();

            SortedDictionary<string, string> sortedHeaders = new SortedDictionary<string, string>();
            foreach (Parameter header in headers)
            {
                string headerName = header.Name.ToLower();
                string headerValue = header.Value.ToString();
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
        /// <param name="request">Instantiated request object</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        private void SetDateHeader(IRestRequest request, DateTime signingDate)
        {
            request.AddHeader("x-amz-date", signingDate.ToString("yyyyMMddTHHmmssZ"));
        }

        /// <summary>
        /// Set 'Host' http header.
        /// </summary>
        /// <param name="request">Instantiated request object</param>
        /// <param name="hostUrl">Host url</param>
        private void SetHostHeader(IRestRequest request, string hostUrl)
        {
            request.AddHeader("Host", hostUrl);
        }

        /// <summary>
        /// Set 'x-amz-content-sha256' http header.
        /// </summary>
        /// <param name="request">Instantiated request object</param>
        private void SetContentSha256(IRestRequest request)
        {
            if (this.isAnonymous)
                return;
            // No need to compute SHA256 if endpoint scheme is https
            if (isSecure)
            {
                request.AddHeader("x-amz-content-sha256","UNSIGNED-PAYLOAD");
                return;
            }
            if (request.Method == Method.PUT || request.Method.Equals(Method.POST))
            {
                var bodyParameter = request.Parameters.FirstOrDefault(p => p.Type.Equals(ParameterType.RequestBody));
                if (bodyParameter == null)
                {
                    request.AddHeader("x-amz-content-sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
                    return;
                }
                byte[] body = null;
                if (bodyParameter.Value is string)
                {
                    body = System.Text.Encoding.UTF8.GetBytes(bodyParameter.Value as string);
                }
                if (bodyParameter.Value is byte[])
                {
                    body = bodyParameter.Value as byte[];
                }
                if (body == null)
                {
                    body = new byte[0];
                }
                SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
                byte[] hash = sha256.ComputeHash(body);
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                request.AddHeader("x-amz-content-sha256", hex);
            }
            else
            {
                request.AddHeader("x-amz-content-sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            }
        }

        /// <summary>
        /// Set 'Content-MD5' http header.
        /// </summary>
        /// <param name="request">Instantiated request object</param>
        private void SetContentMd5(IRestRequest request)
        {
            if (request.Method == Method.PUT || request.Method.Equals(Method.POST))
            {
                var bodyParameter = request.Parameters.Where(p => p.Type.Equals(ParameterType.RequestBody)).FirstOrDefault();
                if (bodyParameter == null)
                {
                    return;
                }
                bool isMultiDeleteRequest = false;
                if (request.Method == Method.POST && request.Resource.EndsWith("?delete"))
                {
                    isMultiDeleteRequest = true;
                }
                // For insecure, authenticated requests set sha256 header instead of MD5.
                if (!isSecure && !isAnonymous && !isMultiDeleteRequest)
                    return;
                // All anonymous access requests get Content-MD5 header set.
                byte[] body = null;
                if (bodyParameter.Value is string)
                {
                    body = System.Text.Encoding.UTF8.GetBytes(bodyParameter.Value as string);
                }
                if (bodyParameter.Value is byte[])
                {
                    body = bodyParameter.Value as byte[];
                }
                if (body == null)
                {
                    body = new byte[0];
                }
                MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] hash = md5.ComputeHash(body);

                string base64 = Convert.ToBase64String(hash);
                request.AddHeader("Content-MD5", base64);
            }
        }
    }
}
