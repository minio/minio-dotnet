namespace Minio.Rest
{
    using System;
    using System.Net.Http;
    using RestSharp.Portable;
    using RestClientExtensions = Minio.Rest.Extensions.RestClientExtensions;

    internal class RestClient : RestClientBase
    {
        private RestClient(Func<HttpClientHandler> createHttpHandlerFunc)
            : base(new MinioHttpClientFactory(createHttpHandlerFunc))
        {
        }

        public RestClient(Uri baseUrl, Func<HttpClientHandler> createHttpHandlerFunc = null)
            : this(createHttpHandlerFunc)
        {
            this.BaseUrl = baseUrl;
        }

        public RestClient(string baseUrl, Func<HttpClientHandler> createHttpHandlerFunc = null)
            : this(new Uri(baseUrl), createHttpHandlerFunc)
        {
        }

        protected override IHttpContent GetContent(IRestRequest request, RequestParameters parameters)
        {
            return RestClientExtensions.GetContent(this, request, parameters);
        }
    }
}