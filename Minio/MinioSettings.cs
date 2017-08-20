namespace Minio
{
    using System;
    using System.Net.Http;

    public class MinioSettings
    {
        public MinioSettings(string endpoint, string accessKey = "", string secretKey = "")
        {
            this.Endpoint = endpoint;
            this.AccessKey = accessKey;
            this.SecretKey = secretKey;
        }

        /// <summary>
        /// Location of the server, supports HTTP and HTTPS
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Access Key for authenticated requests (Optional,can be omitted for anonymous requests)
        /// </summary>
        public string AccessKey { get; }

        /// <summary>
        /// Secret Key for authenticated requests (Optional,can be omitted for anonymous requests)
        /// </summary>
        public string SecretKey { get; }

        /// <summary>
        /// Create HttpClientHandler Func
        /// </summary>
        public Func<HttpClientHandler> CreateHttpClientHandlerFunc { get; set; }

        /// <summary>
        /// http or https?
        /// </summary>
        public bool Secure { get; set; }
    }
}