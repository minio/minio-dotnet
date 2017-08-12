namespace Minio.Rest.Extensions
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Minio.Rest.Impl;
    using RestSharp.Portable;

    internal static class HttpExtensions
    {
        public static IHttpHeaders AsRestHeaders(this HttpHeaders headers)
        {
            return headers == null ? null : new MinioHttpHeaders(headers);
        }

        public static IHttpContent AsRestHttpContent(this HttpContent content)
        {
            if (content == null)
            {
                return null;
            }

            var wrapper = content as HttpContentWrapper;
            if (wrapper == null)
            {
                return new MinioHttpContent(content);
            }

            return wrapper.Content;
        }

        public static HttpContent AsHttpContent(this IHttpContent content)
        {
            if (content == null)
            {
                return null;
            }

            var defaultHttpContent = content as MinioHttpContent;
            return defaultHttpContent != null ? defaultHttpContent.Content : new HttpContentWrapper(content);
        }

        public static void CopyTo(this IHttpHeaders headers, HttpHeaders destination)
        {
            foreach (var httpHeader in headers)
            {
                destination.TryAddWithoutValidation(httpHeader.Key, httpHeader.Value);
            }
        }

        public static HttpRequestMessage AsHttpRequestMessage(this IHttpRequestMessage message)
        {
            var req = message as MinioHttpRequestMessage;
            if (req != null)
            {
                return req.RequestMessage;
            }

            var result = new HttpRequestMessage(message.Method.ToHttpMethod(), message.RequestUri);
            if (message.Version != null)
            {
                result.Version = message.Version;
            }

            message.Headers.CopyTo(result.Headers);
            if (message.Content != null)
            {
                result.Content = message.Content.AsHttpContent();
            }

            return result;
        }
    }
}