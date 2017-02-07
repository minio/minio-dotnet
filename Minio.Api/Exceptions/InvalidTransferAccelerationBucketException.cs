using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Exceptions
{
    class InvalidTransferAccelerationBucketException : MinioException
    {
        private string bucketName;
        public InvalidTransferAccelerationBucketException()
        {

        }
        public InvalidTransferAccelerationBucketException(string bucketName, string message = null) : base(message)

        {
            this.bucketName = bucketName;
        }
        public override string ToString()
        {
            return this.bucketName + ": The name of the bucket used for Transfer Acceleration must be DNS-compliant and must not contain periods \".\"";
        }
    }
}
