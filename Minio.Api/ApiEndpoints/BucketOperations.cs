using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Minio.Api.DataModel;
using RestSharp;
using System.IO;
using System.Xml.Serialization;
using Minio.Api.Exceptions;

namespace Minio.Api
{
    internal class BucketOperations : IBucketOperations
    {
        internal static readonly ApiResponseErrorHandlingDelegate NoSuchBucketHandler = (response) =>
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
               throw new BucketNotFoundException();
            }
        };

        private const string RegistryAuthHeaderKey = "X-Registry-Auth";

        private readonly MinioRestClient _client;

        internal BucketOperations(MinioRestClient client)
        {
            this._client = client;
        }
      
        public async Task<ListAllMyBucketsResult>  ListBucketsAsync()
        {
            var request = new RestRequest("/", Method.GET);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
               this._client.ParseError(response);
            }
            ListAllMyBucketsResult bucketList = new ListAllMyBucketsResult();
            if (HttpStatusCode.OK.Equals(response.StatusCode))
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                var stream = new MemoryStream(contentBytes);
                bucketList = (ListAllMyBucketsResult)(new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream));
                return bucketList;

            }

            return bucketList;
           
        }


         
     



    }




    /*
    Task MakeBucketAsync(string bucketName, string location = "us-east-1");

    Task<bool> BucketExistsAsync(string bucketName);

    Task RemoveBucketAsync(string bucketName); //returns err in go-sdk <===

    Task<IEnumerable<Item>> ListObjectsAsync(string bucketName, string prefix, bool recursive);

    Task<IEnumerable<Upload>> ListIncompleteUploadsAsync(string bucketName, string prefix, bool recursive);
    */


}

