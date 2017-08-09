namespace Minio.Rest.Impl
{
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Minio.Rest.Extensions;
    using RestSharp.Portable;

    internal class HttpContentWrapper : HttpContent
    {
        private bool isDisposed;

        public HttpContentWrapper(IHttpContent content)
        {
            this.Content = content;
            content.Headers.CopyTo(this.Headers);
        }

        public IHttpContent Content { get; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return this.Content.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            return this.Content.TryComputeLength(out length);
        }

        protected override void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = false;
            base.Dispose(disposing);
            this.Content.Dispose();
        }
    }
}