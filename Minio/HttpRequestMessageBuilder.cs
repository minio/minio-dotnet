using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;

namespace Minio
{
    internal class HttpRequestMessageBuilder
    {
        public Uri RequestUri { get;  }
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

                    if (this.BodyParameters.Any() )
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

        public Dictionary<string,string> BodyParameters { get; }

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