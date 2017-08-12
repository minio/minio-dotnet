namespace Minio
{
    using System;

    public interface IMinioClient : IObjectOperations, IBucketOperations
    {
        string AccessKey { get; }

        string SecretKey { get; }

        string BaseUrl { get; }

        /// <summary>
        /// Sets app version and name. Used by RestSharp for constructing User-Agent header in all HTTP requests.
        /// </summary>
        /// <param name="customUserAgent"></param>
        void SetCustomUserAgent(string customUserAgent);

        /// <summary>
        /// Connects to Cloud Storage with HTTPS if this method is invoked on client object
        /// </summary>
        /// <returns></returns>
        IMinioClient WithSsl();

        /// <summary>
        /// Sets HTTP tracing On.Writes output to Console
        /// </summary>
        bool Trace { get; set; }

        /// <summary>
        /// Sets endpoint URL on the client object that request will be made against
        /// </summary>
        /// <param name="baseUrl"></param>
        void SetTargetUrl(Uri baseUrl);
    }
}