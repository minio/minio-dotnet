using System;
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio
{
    public interface IBucketOperations
    {

        Task<bool> MakeBucketAsync(string bucketName, string location = "us-east-1");

        Task<ListAllMyBucketsResult> ListBucketsAsync();

        Task<bool> BucketExistsAsync(string bucketName);

        Task RemoveBucketAsync(string bucketName);
        IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = true);

    }
}