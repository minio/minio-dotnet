using System;
using System.Collections.Generic;

namespace Minio.DataModel.Tracing
{
    public sealed class RequestToLog
    {
        public string resource { get; internal set; }
        public IEnumerable<RequestParameter> parameters { get; internal set; }
        public string method { get; internal set; }
        public Uri uri { get; internal set; }
    }
}