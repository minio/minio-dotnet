using Minio.Api.DataModel;
using System;
using System.Collections.Generic;

namespace Minio.Api
{
    /// <summary>
    /// Minio API call result 
    public class BucketResult : MinioBase
    {
        /// <summary>
        /// List of Bucket results returned from Twilio API call.
        /// </summary>
        public List<Bucket> Buckets { get; set; }
    }
}

