using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Exceptions
{
    class InvalidContentLengthException :ClientException
    {
        private string bucketName;
        private string objectName;
        public InvalidContentLengthException(string bucketName, string objectName, string message) : base(message)
        {
            this.bucketName = bucketName;
            this.objectName = objectName;
        }

        public override string ToString()
        {
            return this.bucketName + " :" + this.objectName + ": " + base.ToString();
        }
    }
}
