using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minio.Api.DataModel;
using RestSharp;
namespace Minio.Api
{
    public interface IBucketOperations
    {
        Task<ListAllMyBucketsResult> ListBucketsAsync();
   /*
        Task MakeBucketAsync(string bucketName, string location= "us-east-1");

        Task<bool> BucketExistsAsync(string bucketName);

        Task RemoveBucketAsync(string bucketName); //returns err in go-sdk <===

        Task<IEnumerable<Item>> ListObjectsAsync(string bucketName, string prefix,bool recursive);

        Task<IEnumerable<Upload>> ListIncompleteUploadsAsync(string bucketName,string prefix,bool recursive);
*/

    }
}