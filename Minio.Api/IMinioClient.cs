using Minio.ApiEndpoints;
using System;


namespace Minio
{
    public interface IMinioClient 
    {
        IBucketOperations Buckets { get; }
    }
}
