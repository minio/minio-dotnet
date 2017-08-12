namespace Minio.Rest
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Minio.Rest.Extensions;
    using Minio.Rest.Impl;
    using RestSharp.Portable;

    internal sealed class MinioHttpClient : IHttpClient
    {
        private readonly CookieContainer cookies;
        private readonly MinioHttpHeaders minioHeaders;

        public MinioHttpClient(HttpClient client, CookieContainer cookies)
        {
            this.cookies = cookies;
            this.Client = client;
            this.minioHeaders = new MinioHttpHeaders(client.DefaultRequestHeaders);
        }

        private HttpClient Client { get; }

        public Uri BaseAddress
        {
            get => this.Client.BaseAddress;
            set => this.Client.BaseAddress = value;
        }

        public IHttpHeaders DefaultRequestHeaders => this.minioHeaders;

        public TimeSpan Timeout
        {
            get => this.Client.Timeout;
            set => this.Client.Timeout = value;
        }

        public async Task<IHttpResponseMessage> SendAsync(IHttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var requestMessage = request.AsHttpRequestMessage();
            var response = await this.Client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            return new MinioHttpResponseMessage(requestMessage, response, this.cookies);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            this.Client.Dispose();
        }
    }
}