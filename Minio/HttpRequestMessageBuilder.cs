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
using System.Text;
using System.Web;

namespace Minio
{
    internal class HttpRequestMessageBuilder
    {
        public Uri RequestUri { get; }
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

                    if (this.BodyParameters.Any())
                    {
                        foreach (var parameter in this.BodyParameters)
                        {
                            request.Content.Headers.TryAddWithoutValidation(parameter.Key, parameter.Value);
                        }
                    }
                }

                return request;
            }
        }
        public HttpRequestMessageBuilder(HttpMethod method, Uri host, string path)
            : this(method,
                new UriBuilder(host)
                {
                    Path = path
                }.Uri
            )
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

            this.QueryParameters = new Dictionary<string, string>();
            this.HeaderParameters = new Dictionary<string, string>();
            this.BodyParameters = new Dictionary<string, string>();
        }

        public Dictionary<string, string> QueryParameters { get; }

        public Dictionary<string, string> HeaderParameters { get; }

        public Dictionary<string, string> BodyParameters { get; }

        public byte[] Content { get; private set; }

        public void AddHeaderParameter(string key, string value)
        {
            this.HeaderParameters[key] = value;
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
            this.BodyParameters.Add("Content-Type", "application/xml");
        }

        public void AddJsonBody(string body)
        {
            this.SetBody(Encoding.UTF8.GetBytes(body));
            this.BodyParameters.Add("Content-Type", "application/json");
        }
    }
}
