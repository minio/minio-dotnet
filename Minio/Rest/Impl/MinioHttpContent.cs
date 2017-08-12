namespace Minio.Rest.Impl
{
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Minio.Rest.Extensions;
    using RestSharp.Portable;

    internal sealed class MinioHttpContent : IHttpContent
    {
        private bool isDisposed;

        public MinioHttpContent(HttpContent content)
        {
            this.Content = content;
            this.Headers = content.Headers.AsRestHeaders();
        }

        public HttpContent Content { get; }

        public IHttpHeaders Headers { get; }

        public Task CopyToAsync(Stream stream)
        {
            return this.Content.CopyToAsync(stream);
        }

        public Task LoadIntoBufferAsync(long maxBufferSize)
        {
            return this.Content.LoadIntoBufferAsync(maxBufferSize);
        }

        public Task<Stream> ReadAsStreamAsync()
        {
            return this.Content.ReadAsStreamAsync();
        }

        public Task<byte[]> ReadAsByteArrayAsync()
        {
            return this.Content.ReadAsByteArrayAsync();
        }

        public Task<string> ReadAsStringAsync()
        {
            return this.Content.ReadAsStringAsync();
        }

        public bool TryComputeLength(out long length)
        {
            length = long.MaxValue;
            return false;
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
            this.Content.Dispose();
        }
    }
}