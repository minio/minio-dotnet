using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.Exceptions;
using System.Net;

namespace Minio
{
    public partial class ClientApiOperations
    {
        internal MinioRestClient client;
        private const string RegistryAuthHeaderKey = "X-Registry-Auth";
        internal static readonly ApiResponseErrorHandlingDelegate NoSuchBucketHandler = (response) =>
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BucketNotFoundException();
            }
        };
        public ClientApiOperations(MinioRestClient client)
        {
            this.client = client;
        }
    }
}

