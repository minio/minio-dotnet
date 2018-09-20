using System;
using System.Collections.Generic;
using System.Text;

namespace Minio.DataModel.Tracing
{
    public sealed class RequestParameter
    {
        public string name { get; internal set; }
        public object value { get; internal set; }
        public string type { get; internal set; }
    }
}
