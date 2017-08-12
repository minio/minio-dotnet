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

namespace Minio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using RestSharp.Portable;

    /// <summary>
    ///     V4Authenticator implements IAuthenticator interface.
    /// </summary>
    internal class V4Authenticator : IAuthenticator
    {
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
        private static readonly HashSet<string> IgnoredHeaders = new HashSet<string>
        {
            "authorization",
            "content-length",
            "content-type",
            "user-agent"
        };

        private readonly string accessKey;

		private readonly DefaultMinioClient minioClient;
        private readonly string secretKey;

        /// <summary>
        ///     Authenticator constructor.
        /// </summary>
		/// <param name="minioClient">minio client</param>
		public V4Authenticator(DefaultMinioClient minioClient)
        {
			this.minioClient = minioClient;
            this.IsSecure = minioClient.Secure;
            this.accessKey = minioClient.AccessKey;
            this.secretKey = minioClient.SecretKey;
            this.IsAnonymous = string.IsNullOrEmpty(this.accessKey) && string.IsNullOrEmpty(this.secretKey);
        }

        internal bool IsAnonymous { get; }
        internal bool IsSecure { get; }

        public bool CanPreAuthenticate(IRestClient client, IRestRequest request, ICredentials credentials)
        {
            return true;
        }

        public bool CanPreAuthenticate(IHttpClient client, IHttpRequestMessage request, ICredentials credentials)
        {
            return false;
        }

        public bool CanHandleChallenge(IHttpClient client, IHttpRequestMessage request, ICredentials credentials,
            IHttpResponseMessage response)
        {
            return false;
        }

        public Task PreAuthenticate(IRestClient client, IRestRequest request, ICredentials credentials)
        {
            var signingDate = DateTime.UtcNow;
            this.SetContentMd5(request);
            this.SetContentSha256(request);
            SetHostHeader(request, client.BaseUrl.Host + ":" + client.BaseUrl.Port);
            SetDateHeader(request, signingDate);
            var headersToSign = GetHeadersToSign(request);
            var signedHeaders = GetSignedHeaders(headersToSign);
            var region = Regions.GetRegion(client.BaseUrl.Host);
            var canonicalRequest = GetCanonicalRequest(client, request, headersToSign);
            var canonicalRequestBytes = Encoding.UTF8.GetBytes(canonicalRequest);
            var canonicalRequestHash = this.BytesToHex(this.ComputeSha256(canonicalRequestBytes));
            var stringToSign = GetStringToSign(region, signingDate, canonicalRequestHash);
            var signingKey = this.GenerateSigningKey(region, signingDate);

            var stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);

            var signatureBytes = this.SignHmac(signingKey, stringToSignBytes);

            var signature = this.BytesToHex(signatureBytes);

            var authorization = this.GetAuthorizationHeader(signedHeaders, signature, signingDate, region);
            // add without validation
            request.AddParameter(new Parameter
            {
                Name = "Authorization",
                Value = authorization,
                Type = ParameterType.HttpHeader,
                ValidateOnAdd = false
            });

            return Task.FromResult(true);
        }

        public Task PreAuthenticate(IHttpClient client, IHttpRequestMessage request, ICredentials credentials)
        {
            throw new NotImplementedException();
        }

        public Task HandleChallenge(IHttpClient client, IHttpRequestMessage request, ICredentials credentials,
            IHttpResponseMessage response)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Get credential string of form <ACCESSID />/date/region/s3/aws4_request.
        /// </summary>
        /// <param name="signingDate">Signature initated date</param>
        /// <param name="region">Region for the credential string</param>
        /// <returns>Credential string for the authorization header</returns>
        public string GetCredentialString(DateTime signingDate, string region)
        {
            return this.accessKey + "/" + GetScope(region, signingDate);
        }

        /// <summary>
        ///     Constructs an authorization header.
        /// </summary>
        /// <param name="signedHeaders">All signed http headers</param>
        /// <param name="signature">Hexadecimally encoded computed signature</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <param name="region">Requested region</param>
        /// <returns>Fully formed authorization header</returns>
        private string GetAuthorizationHeader(string signedHeaders, string signature, DateTime signingDate,
            string region)
        {
            return "AWS4-HMAC-SHA256 Credential=" + this.accessKey + "/" + GetScope(region, signingDate) +
                   ", SignedHeaders=" + signedHeaders + ", Signature=" + signature;
        }

        /// <summary>
        ///     Concatenates sorted list of signed http headers.
        /// </summary>
        /// <param name="headersToSign">Sorted dictionary of headers to be signed</param>
        /// <returns>All signed headers</returns>
        private static string GetSignedHeaders(SortedDictionary<string, string> headersToSign)
        {
            return string.Join(";", headersToSign.Keys);
        }

        /// <summary>
        ///     Generates signing key based on the region and date.
        /// </summary>
        /// <param name="region">Requested region</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <returns>bytes of computed hmac</returns>
        private byte[] GenerateSigningKey(string region, DateTime signingDate)
        {
            var formattedDateBytes = Encoding.UTF8.GetBytes(signingDate.ToString("yyyMMdd"));
            var formattedKeyBytes = Encoding.UTF8.GetBytes("AWS4" + this.secretKey);
            var dateKey = this.SignHmac(formattedKeyBytes, formattedDateBytes);

            var regionBytes = Encoding.UTF8.GetBytes(region);
            var dateRegionKey = this.SignHmac(dateKey, regionBytes);

            var serviceBytes = Encoding.UTF8.GetBytes("s3");
            var dateRegionServiceKey = this.SignHmac(dateRegionKey, serviceBytes);

            var requestBytes = Encoding.UTF8.GetBytes("aws4_request");
            return this.SignHmac(dateRegionServiceKey, requestBytes);
        }

        /// <summary>
        ///     Compute hmac of input content with key.
        /// </summary>
        /// <param name="key">Hmac key</param>
        /// <param name="content">Bytes to be hmac computed</param>
        /// <returns>Computed hmac of input content</returns>
        private byte[] SignHmac(byte[] key, byte[] content)
        {
			return this.minioClient.CryptoProvider.Hmacsha.ComputeHash(key, content);
        }

        /// <summary>
        ///     Get string to sign.
        /// </summary>
        /// <param name="region">Requested region</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <param name="canonicalRequestHash">Hexadecimal encoded sha256 checksum of canonicalRequest</param>
        /// <returns>String to sign</returns>
        private static string GetStringToSign(string region, DateTime signingDate, string canonicalRequestHash)
        {
            return "AWS4-HMAC-SHA256\n" +
                   signingDate.ToString("yyyyMMddTHHmmssZ") + "\n" + GetScope(region, signingDate) + "\n" +
                   canonicalRequestHash;
        }

        /// <summary>
        ///     Get scope.
        /// </summary>
        /// <param name="region">Requested region</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        /// <returns>Scope string</returns>
        private static string GetScope(string region, DateTime signingDate)
        {
            var formattedDate = signingDate.ToString("yyyyMMdd");
            return formattedDate + "/" + region + "/s3/aws4_request";
        }

        /// <summary>
        ///     Compute sha256 checksum.
        /// </summary>
        /// <param name="body">Bytes body</param>
        /// <returns>Bytes of sha256 checksum</returns>
        private byte[] ComputeSha256(byte[] body)
        {
			return this.minioClient.CryptoProvider.Sha256.ComputeHash(body);
        }

        /// <summary>
        ///     Convert bytes to hexadecimal string.
        /// </summary>
        /// <param name="checkSum">Bytes of any checksum</param>
        /// <returns>Hexlified string of input bytes</returns>
        private string BytesToHex(byte[] checkSum)
        {
            return BitConverter.ToString(checkSum).Replace("-", string.Empty).ToLower();
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
            var signingKey = this.GenerateSigningKey(region, signingDate);
            var stringToSignBytes = Encoding.UTF8.GetBytes(policyBase64);

            var signatureBytes = this.SignHmac(signingKey, stringToSignBytes);
            var signature = this.BytesToHex(signatureBytes);

            return signature;
        }

        /// <summary>
        ///     Presigns any input client object with a requested expiry.
        /// </summary>
        /// <param name="client">Instantiated client</param>
        /// <param name="request">Instantiated request</param>
        /// <param name="expires">Expiration in seconds</param>
        /// <returns>Presigned url</returns>
        public string PresignURL(IRestClient client, IRestRequest request, int expires)
        {
            var signingDate = DateTime.UtcNow;
            var region = Regions.GetRegion(client.BaseUrl.Host);
            var path = request.Resource;

            var requestQuery = "X-Amz-Algorithm=AWS4-HMAC-SHA256&";
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

            var canonicalRequest = GetPresignCanonicalRequest(client, request, requestQuery);
            var canonicalRequestBytes = Encoding.UTF8.GetBytes(canonicalRequest);
            var canonicalRequestHash = this.BytesToHex(this.ComputeSha256(canonicalRequestBytes));
            var stringToSign = GetStringToSign(region, signingDate, canonicalRequestHash);
            var signingKey = this.GenerateSigningKey(region, signingDate);
            var stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);
            var signatureBytes = this.SignHmac(signingKey, stringToSignBytes);
            var signature = this.BytesToHex(signatureBytes);

            // Return presigned url.
            return client.BaseUrl + path + "?" + requestQuery + "&X-Amz-Signature=" + signature;
        }

        /// <summary>
        ///     Get presign canonical request.
        /// </summary>
        /// <param name="client">Instantiated client object</param>
        /// <param name="request">Instantiated request object</param>
        /// <param name="requestQuery">Additional request query params</param>
        /// <returns>Presigned canonical request</returns>
        private static string GetPresignCanonicalRequest(IRestClient client, IRestRequest request, string requestQuery)
        {
            var canonicalStringList = new LinkedList<string>();
            // METHOD
            canonicalStringList.AddLast(request.Method.ToString());

            var path = request.Resource;
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            canonicalStringList.AddLast(path);
            canonicalStringList.AddLast(requestQuery);
            if (client.BaseUrl.Port > 0 && client.BaseUrl.Port != 80 && client.BaseUrl.Port != 443)
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
        ///     Get canonical request.
        /// </summary>
        /// <param name="client">Instantiated client object</param>
        /// <param name="request">Instantiated request object</param>
        /// <param name="headersToSign">Dictionary of http headers to be signed</param>
        /// <returns>Canonical Request</returns>
        private static string GetCanonicalRequest(IRestClient client, IRestRequest request,
            SortedDictionary<string, string> headersToSign)
        {
            var canonicalStringList = new LinkedList<string>();
            // METHOD
            canonicalStringList.AddLast(request.Method.ToString());

            var path = request.Resource.Split(new[] {'?'}, 2);
			if (!path[0].StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                path[0] = "/" + path[0];
            }
            canonicalStringList.AddLast(path[0]);

            var query = "";
            // QUERY
            if (path.Length == 2)
            {
                var parameterString = path[1];
                var parameterList = parameterString.Split('&');
                var sortedQueries = new SortedSet<string>();
                foreach (var individualParameterString in parameterList)
                {
                    if (individualParameterString.Contains("="))
                    {
                        var splitQuery = individualParameterString.Split(new[] {'='}, 2);
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

            foreach (var header in headersToSign.Keys)
            {
                canonicalStringList.AddLast(header + ":" + headersToSign[header]);
            }
            canonicalStringList.AddLast("");
            canonicalStringList.AddLast(string.Join(";", headersToSign.Keys));
            canonicalStringList.AddLast(headersToSign.Keys.Contains("x-amz-content-sha256")
                ? headersToSign["x-amz-content-sha256"]
                : "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");

            return string.Join("\n", canonicalStringList);
        }

        /// <summary>
        ///     Get headers to be signed.
        /// </summary>
        /// <param name="request">Instantiated requesst</param>
        /// <returns>Sorted dictionary of headers to be signed</returns>
        private static SortedDictionary<string, string> GetHeadersToSign(IRestRequest request)
        {
            var headers = request.Parameters.Where(p => p.Type.Equals(ParameterType.HttpHeader)).ToList();

            var sortedHeaders = new SortedDictionary<string, string>();
            foreach (var header in headers)
            {
                var headerName = header.Name.ToLower();
                var headerValue = header.Value.ToString();
                if (headerName.Equals("host"))
                {
                    var host = headerValue.Split(':')[0];
                    var port = headerValue.Split(':')[1];
                    if (port.Equals("80") || port.Equals("443"))
                    {
                        sortedHeaders.Add(headerName, host);
                    }
                    else
                    {
                        sortedHeaders.Add(headerName, headerValue);
                    }
                }
                else if (!IgnoredHeaders.Contains(headerName))
                {
                    sortedHeaders.Add(headerName, headerValue);
                }
            }
            return sortedHeaders;
        }

        /// <summary>
        ///     Sets 'x-amz-date' http header.
        /// </summary>
        /// <param name="request">Instantiated request object</param>
        /// <param name="signingDate">Date for signature to be signed</param>
        private static void SetDateHeader(IRestRequest request, DateTime signingDate)
        {
            request.AddHeader("x-amz-date", signingDate.ToString("yyyyMMddTHHmmssZ"));
        }

        /// <summary>
        ///     Set 'Host' http header.
        /// </summary>
        /// <param name="request">Instantiated request object</param>
        /// <param name="hostUrl">Host url</param>
        private static void SetHostHeader(IRestRequest request, string hostUrl)
        {
            request.AddHeader("Host", hostUrl);
        }

		private void AddSha256Header(IRestRequest request, string value)
		{
			if(this.minioClient.Trace)
			{
				this.minioClient.LogProvider.Trace($"HEADER: X-AMZ-CONTENT-SHA256, {value}");
			}

			request.AddHeader("x-amz-content-sha256", value);
		}

        /// <summary>
        ///     Set 'x-amz-content-sha256' http header.
        /// </summary>
        /// <param name="request">Instantiated request object</param>
        private void SetContentSha256(IRestRequest request)
        {
            if (this.IsAnonymous)
            {
                return;
            }
            // No need to compute SHA256 if endpoint scheme is https
            if (this.IsSecure)
            {
				this.AddSha256Header(request, "UNSIGNED-PAYLOAD");
                return;
			}

            if (request.Method == Method.PUT || request.Method.Equals(Method.POST))
            {
                var bodyParameter = request.Parameters.FirstOrDefault(p => p.Type.Equals(ParameterType.RequestBody));
                if (bodyParameter == null)
                {
					this.AddSha256Header(request, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
                    return;
                }
                byte[] body = null;
                if (bodyParameter.Value is string valueString)
                {
                    body = Encoding.UTF8.GetBytes(valueString);
                }
                if (bodyParameter.Value is byte[] valueBytes)
                {
                    body = valueBytes;
                }
                if (body == null)
                {
                    body = new byte[0];
                }

                var hash = this.ComputeSha256(body);
                var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
				this.AddSha256Header(request, hex);
            }
            else
            {
				this.AddSha256Header(request, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            }
		}

        /// <summary>
        ///     Set 'Content-MD5' http header.
        /// </summary>
        /// <param name="request">Instantiated request object</param>
        private void SetContentMd5(IRestRequest request)
        {
            if (request.Method == Method.PUT || request.Method.Equals(Method.POST))
            {
                var bodyParameter = request.Parameters.FirstOrDefault(p => p.Type.Equals(ParameterType.RequestBody));
                if (bodyParameter == null)
                {
                    return;
                }
                // For insecure, authenticated requests set sha256 header instead of MD5.
                if (!this.IsSecure && !this.IsAnonymous)
                {
                    return;
                }
                // All anonymous access requests get Content-MD5 header set.
                byte[] body = null;
                if (bodyParameter.Value is string valueString)
                {
                    body = Encoding.UTF8.GetBytes(valueString);
                }
                if (bodyParameter.Value is byte[] valueBytes)
                {
                    body = valueBytes;
                }
                if (body == null)
                {
                    body = new byte[0];
                }

				var hash = this.minioClient.CryptoProvider.Md5.ComputeHash(body);
                var base64 = Convert.ToBase64String(hash);
                request.AddHeader("Content-MD5", base64);
            }
        }
    }
}