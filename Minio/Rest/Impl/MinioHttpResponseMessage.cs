namespace Minio.Rest.Impl
{
    using System.Net;
    using System.Net.Http;
    using Minio.Rest.Extensions;
    using RestSharp.Portable;
    using RestSharp.Portable.Impl;

    internal sealed class MinioHttpResponseMessage : IHttpResponseMessage
    {
        public MinioHttpResponseMessage(HttpRequestMessage requestMessage, HttpResponseMessage responseMessage,
            CookieContainer cookies)
        {
            this.ResponseMessage = responseMessage;
            this.RequestMessage = new MinioHttpRequestMessage(requestMessage);

            if (responseMessage != null)
            {
                this.Content = responseMessage.Content.AsRestHttpContent();
                this.Headers = new MinioHttpHeaders(responseMessage.Headers);
            }
            else
            {
                this.Content = null;
                this.Headers = new GenericHttpHeaders();
            }

            this.Cookies = cookies;
        }

        private HttpResponseMessage ResponseMessage { get; }

        public CookieContainer Cookies { get; }

        public IHttpHeaders Headers { get; }

        public bool IsSuccessStatusCode => this.ResponseMessage?.IsSuccessStatusCode ?? false;

        public string ReasonPhrase => this.ResponseMessage?.ReasonPhrase ?? "Unknown error";

        public IHttpRequestMessage RequestMessage { get; }

        public HttpStatusCode StatusCode => this.ResponseMessage?.StatusCode ?? HttpStatusCode.InternalServerError;

        public IHttpContent Content { get; }

        public void EnsureSuccessStatusCode()
        {
            if (this.ResponseMessage == null)
            {
                throw new HttpRequestException(this.ReasonPhrase);
            }

            this.ResponseMessage?.EnsureSuccessStatusCode();
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

            this.ResponseMessage?.Dispose();
            this.RequestMessage.Dispose();
        }
    }
}