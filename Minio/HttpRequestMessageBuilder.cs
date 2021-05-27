/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
* (C) 2017, 2018, 2019, 2020 MinIO, Inc.
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

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

using Minio.Exceptions;


namespace Minio
{
    internal class HttpRequestMessageBuilder
    {
        internal HttpRequestMessageBuilder(Uri requestUri, HttpMethod method)
        {
            this.RequestUri = requestUri;
            this.Method = method;

        }
        public Uri RequestUri { get; set; }
        public Action<System.IO.Stream> ResponseWriter { get; set; }
        public HttpMethod Method { get; }

        public HttpRequestMessage Request
        {
            get
            {
                var requestUriBuilder = new UriBuilder(this.RequestUri);

                foreach (var queryParameter in this.QueryParameters)
                {
                    var query = HttpUtility.ParseQueryString(requestUriBuilder.Query);
                    query[queryParameter.Key] = queryParameter.Value;
                    requestUriBuilder.Query = query.ToString();

                }

                var requestUri = requestUriBuilder.Uri;
                var request = new HttpRequestMessage(this.Method, requestUri);

                if (this.Content != null)
                {
                    request.Content = new ByteArrayContent(this.Content);
                }

                foreach (var parameter in this.HeaderParameters)
                {
                    var key = parameter.Key.ToLower();
                    var val = parameter.Value;


                    bool addSuccess = request.Headers.TryAddWithoutValidation(key, val);
                    if (!addSuccess)
                    {
                        if (request.Content == null)
                            request.Content = new StringContent("");
                        switch (key)
                        {
                            case "content-type":
                                try
                                {
                                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(val);
                                }
                                catch
                                {
                                    bool success = request.Content.Headers.TryAddWithoutValidation(ContentTypeKey, val);
                                }
                                break;
                            case "content-length":
                                request.Content.Headers.ContentLength = Convert.ToInt32(val);
                                break;
                            case "content-md5":
                                request.Content.Headers.ContentMD5 = Convert.FromBase64String(val);
                                break;
                            default:
                                var errMessage = "Unsupported signed header: (" + key + ": " + val;
                                throw new Minio.Exceptions.UnexpectedMinioException(errMessage);
                        }
                    }
                }

                if (request.Content != null)
                {
                    var isMultiDeleteRequest = false;
                    if (this.Method == HttpMethod.Post)
                    {
                        isMultiDeleteRequest = this.QueryParameters.ContainsKey("delete");
                    }
                    bool isSecure = this.RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);

                    if (!isSecure && !isMultiDeleteRequest &&
                        this.BodyParameters.ContainsKey("Content-Md5") &&
                        this.BodyParameters["Content-Md5"] != null)
                    {
                        string returnValue = "";
                        this.BodyParameters.TryGetValue("Content-Md5", out returnValue);
                        request.Content.Headers.ContentMD5 = Convert.FromBase64String(returnValue);
                    }
                }
                return request;
            }
        }

        public HttpRequestMessageBuilder(HttpMethod method, Uri host, string path)
        : this(method, new UriBuilder(host) { Path = host.AbsolutePath + path }.Uri)
        {
        }

        public HttpRequestMessageBuilder(HttpMethod method, string requestUrl)
            : this(method, new Uri(requestUrl))
        {

        }

        public HttpRequestMessageBuilder(HttpMethod method, Uri requestUri)
        {
            this.Method = method;
            this.RequestUri = requestUri;

            this.QueryParameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            this.HeaderParameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            this.BodyParameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public Dictionary<string, string> QueryParameters { get; }

        public Dictionary<string, string> HeaderParameters { get; }

        public Dictionary<string, string> BodyParameters { get; }

        public byte[] Content { get; private set; }

        public void AddHeaderParameter(string key, string value)
        {
            StringComparison comparison = StringComparison.InvariantCultureIgnoreCase;
            if (key.StartsWith("content-", comparison) &&
               !string.IsNullOrEmpty(value) &&
               !this.BodyParameters.ContainsKey(key))
            {
                this.BodyParameters.Add(key, value);
            }
            this.HeaderParameters[key] = value;
        }

        public void AddOrUpdateHeaderParameter(string key, string value)
        {
            if (this.HeaderParameters.GetType().GetProperty(key) != null)
                this.HeaderParameters.Remove(key);
            this.HeaderParameters[key] = value;
        }

        public void AddBodyParameter(string key, string value)
        {
            this.BodyParameters.Add(key, value);
        }

        public void AddQueryParameter(string key, string value)
        {
            this.QueryParameters[key] = value;
        }

        public void SetBody(byte[] body)
        {
            this.Content = body;
        }

        public void AddXmlBody(string body)
        {
            this.SetBody(Encoding.UTF8.GetBytes(body));
            this.BodyParameters.Add(this.ContentTypeKey, "application/xml");
        }

        public void AddJsonBody(string body)
        {
            this.SetBody(Encoding.UTF8.GetBytes(body));
            this.BodyParameters.Add(this.ContentTypeKey, "application/json");
        }

        public string ContentTypeKey => "Content-Type";
    }
}
