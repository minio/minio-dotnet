namespace Minio.Rest.Impl
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Net.Http.Headers;
    using RestSharp.Portable;

    internal class MinioHttpHeaders : IHttpHeaders
    {
        public MinioHttpHeaders(HttpHeaders headers)
        {
            this.Headers = headers;
        }

        private HttpHeaders Headers { get; }

        public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
        {
            return this.Headers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(string name, IEnumerable<string> values)
        {
            this.Headers.Add(name, values);
        }

        public void Add(string name, string value)
        {
            this.Headers.Add(name, value);
        }

        public void Clear()
        {
            this.Headers.Clear();
        }

        public bool Contains(string name)
        {
            return this.Headers.Contains(name);
        }

        public IEnumerable<string> GetValues(string name)
        {
            return this.Headers.GetValues(name);
        }

        public bool Remove(string name)
        {
            return this.Headers.Remove(name);
        }

        public bool TryGetValues(string name, out IEnumerable<string> values)
        {
            return this.Headers.TryGetValues(name, out values);
        }

        public bool TryAddWithoutValidation(string name, IEnumerable<string> values)
        {
            return this.Headers.TryAddWithoutValidation(name, values);
        }

        public bool TryAddWithoutValidation(string name, string value)
        {
            return this.Headers.TryAddWithoutValidation(name, value);
        }
    }
}