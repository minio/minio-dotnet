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
        public async Task MakeBucketAsync(string bucketName, string location = "us-east-1")
        {
            var request = new RestRequest("/" + bucketName, Method.PUT);
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(location);
                request.AddBody(config);
            }
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                this._client.ParseError(response);
            }
       
        }

        public async Task<bool> BucketExistsAsync(string bucketName)
        {
            var request = new RestRequest(bucketName, Method.HEAD);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            var ex = this._client.ParseError(response);
            if (ex.GetType() == typeof(BucketNotFoundException))
            {
                return false;
            }
            throw ex;
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

