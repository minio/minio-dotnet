using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Client
{
    public class RequestException : Exception
    {
        public ErrorResponse Response { get; set; }
        public string XmlError { get; set; }
    }
}
