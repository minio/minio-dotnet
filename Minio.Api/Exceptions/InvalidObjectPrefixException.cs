using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Exceptions
{
    class InvalidObjectPrefixException : ClientException
    {
        private string objectPrefix;

        public InvalidObjectPrefixException(string objectPrefix, string message) : base(message)
        {
            this.objectPrefix = objectPrefix;
        }

        public override string ToString()
        {
            return this.objectPrefix + ": " + base.ToString();
        }
    }
}
