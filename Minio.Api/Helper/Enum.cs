using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio
{
    class Enum
    {
        /// <summary>
        /// HTTP method to use when making requests
        /// </summary>
        public enum Method
        {
            GET,
            POST,
            PUT,
            DELETE,
            HEAD,
            OPTIONS,
            PATCH,
            MERGE,
        }
    }
}
