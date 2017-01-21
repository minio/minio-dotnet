using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Exceptions
{
    class InvalidObjectNameException : ClientException
    {
        private string objectName;

        public InvalidObjectNameException(string objectName, string message) : base(message)
        {
            this.objectName = objectName;
        }

        public override string ToString()
        {
            return this.objectName + ": " + base.ToString();
        }
    }
}
