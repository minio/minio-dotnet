/*
1;3803;0c * Minio .NET Library for Amazon S3 compatible cloud storage, (C) 2015 Minio, Inc.
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

using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Minio
{
    class V4Authenticator : IAuthenticator
    {
        private readonly string accessKey;
        private readonly string secretKey;

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

        public V4Authenticator(string accessKey, string secretKey)
        {
            this.accessKey = accessKey;
            this.secretKey = secretKey;
        }

        public void Authenticate(IRestClient client, IRestRequest request)
        {
            DateTime signingDate = DateTime.UtcNow;
            SetContentMd5(request);
            SetContentSha256(request);
            SetHostHeader(client, request);
            SetDateHeader(request, signingDate);
            SortedDictionary<string, string> headersToSign = GetHeadersToSign(request);
            string signedHeaders = GetSignedHeaders(headersToSign);
            string region = Regions.GetRegion(client.BaseUrl.Host);
            string canonicalRequest = GetCanonicalRequest(client, request, headersToSign);
            byte[] canonicalRequestBytes = System.Text.Encoding.UTF8.GetBytes(canonicalRequest);
            string canonicalRequestHash = BytesToHex(ComputeSha256(canonicalRequestBytes));
            string stringToSign = GetStringToSign(region, canonicalRequestHash, signingDate);
            byte[] signingKey = GenerateSigningKey(region, signingDate);

            byte[] stringToSignBytes = System.Text.Encoding.UTF8.GetBytes(stringToSign);

            byte[] signatureBytes = SignHmac(signingKey, stringToSignBytes);

            string signature = BytesToHex(signatureBytes);

            string authorization = GetAuthorizationHeader(signedHeaders, signature, signingDate, region);
            request.AddHeader("Authorization", authorization);

        }

        private string GetAuthorizationHeader(string signedHeaders, string signature, DateTime signingDate, string region)
        {
            return "AWS4-HMAC-SHA256 Credential=" + this.accessKey + "/" + GetScope(region, signingDate) +
                ", SignedHeaders=" + signedHeaders + ", Signature=" + signature;
        }

        private string GetSignedHeaders(SortedDictionary<string, string> headersToSign)
        {
            return string.Join(";", headersToSign.Keys);
        }

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

        private byte[] SignHmac(byte[] key, byte[] content)
        {
            HMACSHA256 hmac = new HMACSHA256(key);
            hmac.Initialize();
            return hmac.ComputeHash(content);
        }

        private string GetStringToSign(string region, string canonicalRequestHash, DateTime signingDate)
        {
            return "AWS4-HMAC-SHA256\n" +
                signingDate.ToString("yyyyMMddTHHmmssZ") + "\n" +
                GetScope(region, signingDate) + "\n" +
                canonicalRequestHash;
        }

        private string GetScope(string region, DateTime signingDate)
        {
            string formattedDate = signingDate.ToString("yyyyMMdd");
            return formattedDate + "/" + region + "/s3/aws4_request";
        }

        private byte[] ComputeSha256(byte[] body)
        {

            SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(body);
        }

        private string BytesToHex(byte[] body)
        {
            return BitConverter.ToString(body).Replace("-", string.Empty).ToLower();
        }

        private string GetCanonicalRequest(IRestClient client, IRestRequest request, SortedDictionary<string, string> headersToSign)
        {
            LinkedList<string> canonicalStringList = new LinkedList<string>();
            // METHOD
            canonicalStringList.AddLast(request.Method.ToString());

            //// PATH
            //var pathParameter = request.Parameters.Where(p => p.Type.Equals(ParameterType.UrlSegment)).FirstOrDefault();
            //if (pathParameter == null)
            //{
            //    throw new NullReferenceException();
            //}
            //var path = pathParameter.Value.ToString();
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

        private string GetCanonicalRequest(IRestRequest request)
        {
            throw new NotImplementedException();
        }

        private void SetDateHeader(IRestRequest request, DateTime signingDate)
        {
            request.AddHeader("x-amz-date", signingDate.ToString("yyyyMMddTHHmmssZ"));
        }

        private void SetHostHeader(IRestClient client, IRestRequest request)
        {
            request.AddHeader("Host", client.BaseUrl.Host + ":" + client.BaseUrl.Port);
        }

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

        private void SetContentSha256(IRestRequest request)
        {
            if (request.Method == Method.PUT || request.Method.Equals(Method.POST))
            {
                var bodyParameter = request.Parameters.Where(p => p.Type.Equals(ParameterType.RequestBody)).FirstOrDefault();
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

        private void SetContentMd5(IRestRequest request)
        {
            if (request.Method == Method.PUT || request.Method.Equals(Method.POST))
            {
                var bodyParameter = request.Parameters.Where(p => p.Type.Equals(ParameterType.RequestBody)).FirstOrDefault();
                if (bodyParameter == null)
                {
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
                MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] hash = md5.ComputeHash(body);

                string base64 = Convert.ToBase64String(hash);
                request.AddHeader("Content-MD5", base64);
            }
        }
    }
}
