using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Exceptions
{
    class EntityTooLargeException : ClientException
    {

        public EntityTooLargeException(string message) : base(message)
        {
        }
    }
}
