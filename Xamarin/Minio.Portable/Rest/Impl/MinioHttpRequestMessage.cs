namespace Minio.Rest.Impl
{
    using System;
    using System.Net.Http;
    using Minio.Rest.Extensions;
    using RestSharp.Portable;

    internal sealed class MinioHttpRequestMessage : IHttpRequestMessage
    {
        private readonly MinioHttpHeaders requestHttpHeaders;

        private IHttpContent content;

        private bool isDisposed;

        public MinioHttpRequestMessage(HttpRequestMessage requestMessage)
        {
            this.RequestMessage = requestMessage;
            this.requestHttpHeaders = new MinioHttpHeaders(requestMessage.Headers);
            this.content = requestMessage.Content.AsRestHttpContent();
        }

        public HttpRequestMessage RequestMessage { get; }

        public IHttpHeaders Headers => this.requestHttpHeaders;

        public Method Method
        {
            get => this.RequestMessage.Method.ToMethod();
            set => this.RequestMessage.Method = value.ToHttpMethod();
        }

        public Uri RequestUri
        {
            get => this.RequestMessage.RequestUri;
            set => this.RequestMessage.RequestUri = value;
        }

        public Version Version
        {
            get => this.RequestMessage.Version;
            set => this.RequestMessage.Version = value;
        }

        public IHttpContent Content
        {
            get => this.content;
            set
            {
                this.content = value;
                this.RequestMessage.Content = value.AsHttpContent();
            }
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

            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            if (this.Content != null)
            {
                this.Content.Dispose();
                this.RequestMessage.Content = null;
            }
            this.RequestMessage.Dispose();
        }
    }
}