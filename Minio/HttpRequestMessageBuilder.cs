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

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Minio.Exceptions;

namespace Minio;

internal class HttpRequestMessageBuilder
{
    public const string ContentTypeKey = "Content-Type";
    public const string HostKey = "Host";

    internal HttpRequestMessageBuilder(Uri requestUri, HttpMethod method)
    {
        RequestUri = requestUri;
        Method = method;
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
        Method = method;
        RequestUri = requestUri;

        QueryParameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        HeaderParameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        BodyParameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }

    public Uri RequestUri { get; set; }
    public Func<Stream, CancellationToken, Task> ResponseWriter { get; set; }
    public HttpMethod Method { get; }

    public HttpRequestMessage Request
    {
        get
        {
            var requestUriBuilder = new UriBuilder(RequestUri);
            var requestUri = requestUriBuilder.Uri;
            var request = new HttpRequestMessage(Method, requestUri);

            if (!Content.IsEmpty) request.Content = new ReadOnlyMemoryContent(Content);

            foreach (var parameter in HeaderParameters)
            {
                var key = parameter.Key.ToLowerInvariant();
                var val = parameter.Value;

                var addSuccess = request.Headers.TryAddWithoutValidation(key, val);
                if (!addSuccess)
                {
                    request.Content ??= new StringContent("");
                    switch (key)
                    {
                        case "content-type":
                            try
                            {
                                request.Content.Headers.ContentType = new MediaTypeHeaderValue(val);
                            }
                            catch
                            {
                                var success = request.Content.Headers.TryAddWithoutValidation(ContentTypeKey, val);
                            }

                            break;

                        case "content-length":
                            request.Content.Headers.ContentLength = Convert.ToInt32(val, CultureInfo.InvariantCulture);
                            break;

                        case "content-md5":
                            request.Content.Headers.ContentMD5 = Convert.FromBase64String(val);
                            break;

                        default:
                            var errMessage = "Unsupported signed header: (" + key + ": " + val;
                            throw new UnexpectedMinioException(errMessage);
                    }
                }
            }

            if (request.Content is not null)
            {
                var isMultiDeleteRequest = false;
                if (Method == HttpMethod.Post) isMultiDeleteRequest = QueryParameters.ContainsKey("delete");
                var isSecure = RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);

                if (!isSecure && !isMultiDeleteRequest &&
                    BodyParameters.TryGetValue("Content-Md5", out var value) && value is not null)
                {
                    _ = BodyParameters.TryGetValue("Content-Md5", out var returnValue);
                    request.Content.Headers.ContentMD5 = Convert.FromBase64String(returnValue);
                }
            }

            return request;
        }
    }

    public Dictionary<string, string> QueryParameters { get; }

    public Dictionary<string, string> HeaderParameters { get; }

    public Dictionary<string, string> BodyParameters { get; }

    public ReadOnlyMemory<byte> Content { get; private set; }

    public void AddHeaderParameter(string key, string value)
    {
        if (key.StartsWith("content-", StringComparison.InvariantCultureIgnoreCase) &&
            !string.IsNullOrEmpty(value) &&
            !BodyParameters.ContainsKey(key))
            BodyParameters.Add(key, value);

        HeaderParameters[key] = value;
    }

    public void AddOrUpdateHeaderParameter(string key, string value)
    {
        if (HeaderParameters.GetType().GetProperty(key) is not null)
            _ = HeaderParameters.Remove(key);
        HeaderParameters[key] = value;
    }

    public void AddBodyParameter(string key, string value)
    {
        BodyParameters.Add(key, value);
    }

    public void AddQueryParameter(string key, string value)
    {
        QueryParameters[key] = value;
    }

    public void SetBody(ReadOnlyMemory<byte> body)
    {
        Content = body;
    }

    public void AddXmlBody(string body)
    {
        SetBody(Encoding.UTF8.GetBytes(body));
        BodyParameters.Add(ContentTypeKey, "application/xml");
    }

    public void AddJsonBody(string body)
    {
        SetBody(Encoding.UTF8.GetBytes(body));
        BodyParameters.Add(ContentTypeKey, "application/json");
    }
}
