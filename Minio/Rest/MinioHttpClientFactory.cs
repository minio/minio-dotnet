namespace Minio.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using Minio.Rest.Impl;
    using RestSharp.Portable;

    internal sealed class MinioHttpClientFactory : IHttpClientFactory
    {
        private Func<HttpClientHandler> CreateHttpHandlerFunc { get; }
        
        private readonly bool setCredentials;

        public MinioHttpClientFactory(Func<HttpClientHandler> createHttpHandlerFunc = null)
            : this(true) => this.CreateHttpHandlerFunc = createHttpHandlerFunc ?? (() => new HttpClientHandler());

        private MinioHttpClientFactory(bool setCredentials)
        {
            this.setCredentials = setCredentials;
        }

        public IHttpClient CreateClient(IRestClient client)
        {
            var handler = this.CreateMessageHandler(client);

            var httpClient = new HttpClient(handler, true)
            {
                BaseAddress = GetBaseAddress(client)
            };

            var timeout = client.Timeout;
            if (timeout.HasValue)
            {
                httpClient.Timeout = timeout.Value;
            }

            return new MinioHttpClient(httpClient, client.CookieContainer);
        }

        public IHttpRequestMessage CreateRequestMessage(IRestClient client, IRestRequest request,
            IList<Parameter> parameters)
        {
            var address = GetMessageAddress(client, request);
            var method = GetHttpMethod(client, request).ToHttpMethod();
            var message = new HttpRequestMessage(method, address);
            message = AddHttpHeaderParameters(message, parameters);
            return new MinioHttpRequestMessage(message);
        }

        private static Uri GetBaseAddress(IRestClient client)
        {
            return client.BuildUri(null, false);
        }

        private static Uri GetMessageAddress(IRestClient client, IRestRequest request)
        {
            var fullUrl = client.BuildUri(request);
            var url = client.BuildUri(null, false).MakeRelativeUri(fullUrl);
            return url;
        }

        private static Method GetHttpMethod(IRestClient client, IRestRequest request)
        {
            return client.GetEffectiveHttpMethod(request);
        }

        private static HttpRequestMessage AddHttpHeaderParameters(HttpRequestMessage message, IEnumerable<Parameter> parameters)
        {
            foreach (var param in parameters.Where(x => x.Type == ParameterType.HttpHeader))
            {
                if (message.Headers.Contains(param.Name))
                {
                    message.Headers.Remove(param.Name);
                }

                var paramValue = param.ToRequestString();
                if (param.ValidateOnAdd)
                {
                    message.Headers.Add(param.Name, paramValue);
                }
                else
                {
                    message.Headers.TryAddWithoutValidation(param.Name, paramValue);
                }
            }

            return message;
        }

        private HttpMessageHandler CreateMessageHandler(IRestClient client)
        {
            var handler = this.CreateHttpHandlerFunc();
            if (client.CookieContainer != null)
            {
                handler.UseCookies = true;
                handler.CookieContainer = client.CookieContainer;
            }

            if (this.setCredentials)
            {
                var credentials = client.Credentials;
                if (credentials != null)
                {
                    handler.Credentials = credentials;
                }
            }

            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            }

            return handler;
        }
    }
}