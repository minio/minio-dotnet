using System;
using System.Collections.Generic;
using System.Net;
using RestSharp;

namespace Minio.DataModel.Tracing
{
    public sealed class ResponseToLog
    {
        public string content { get; internal set; }
        public IEnumerable<Parameter> headers { get; internal set; }
        public HttpStatusCode statusCode { get; internal set; }
        public Uri responseUri { get; internal set; }
        public string errorMessage { get; internal set; }
        public double durationMs { get; internal set; }
    }
}