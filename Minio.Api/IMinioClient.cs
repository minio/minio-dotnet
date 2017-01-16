using Minio.Api.ApiEndpoints;
using System;


namespace Minio.Api
{
    public interface IMinioClient 
    {
        IBucketOperations Buckets { get; }
    }
}
