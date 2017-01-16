using System;
using System.Collections.Generic;
namespace Minio.Api
{
 
    /// Minio API call result  
    public class MinioBase
    {
        public RestException RestException { get; set; }
        public Uri Uri { get; set; }

    }
}
