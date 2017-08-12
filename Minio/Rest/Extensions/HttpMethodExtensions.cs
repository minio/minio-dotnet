namespace Minio.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using RestSharp.Portable;

    internal static class HttpMethodExtensions
    {
        private static readonly IDictionary<Method, HttpMethod> _methodsToHttpMethods = new Dictionary<Method, HttpMethod>
        {
            { Method.DELETE, HttpMethod.Delete },
            { Method.GET, HttpMethod.Get },
            { Method.HEAD, HttpMethod.Head },
            { Method.MERGE, new HttpMethod("MERGE") },
            { Method.OPTIONS, HttpMethod.Options },
            { Method.PATCH, new HttpMethod("PATCH") },
            { Method.POST, HttpMethod.Post },
            { Method.PUT, HttpMethod.Put },
        };

        private static readonly IDictionary<string, Method> _httpMethodsToMethods = _methodsToHttpMethods
            .ToDictionary(x => x.Value.Method, x => x.Key, StringComparer.OrdinalIgnoreCase);

        public static HttpMethod ToHttpMethod(this Method method)
        {
            HttpMethod result;
            if (_methodsToHttpMethods.TryGetValue(method, out result))
            {
                return result;
            }

            throw new NotSupportedException($"Unsupported HTTP method {method}");
        }

        public static Method ToMethod(this HttpMethod method)
        {
            Method result;
            if (_httpMethodsToMethods.TryGetValue(method.Method, out result))
            {
                return result;
            }

            throw new NotSupportedException($"Unsupported HTTP method {method.Method}");
        }
    }
}