using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Minio.Api.DataModel;
using RestSharp;
using System.IO;
using System.Xml.Serialization;

namespace Minio.Api
{
    internal class BucketOperations : IBucketOperations
    {
        internal static readonly ApiResponseErrorHandlingDelegate NoSuchImageHandler = (response) =>
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
              //  throw new MinioBucketNotFoundException(response);
            }
        };

        private const string RegistryAuthHeaderKey = "X-Registry-Auth";

        private readonly MinioRestClient _client;

        internal BucketOperations(MinioRestClient client)
        {
            this._client = client;
        }

        public void  ListBucketsAsync(Action<ListAllMyBucketsResult> callback)
        {
            var request = new RestRequest("/", Method.GET);
            this._client.ExecuteAsync(request, (response) => {
                if (HttpStatusCode.OK.Equals(response.StatusCode))
                {
                    var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                    var stream = new MemoryStream(contentBytes);
                    ListAllMyBucketsResult bucketList = (ListAllMyBucketsResult)(new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream));
                    callback(bucketList);
                }

              //TODO  throw ParseError(response);

              });

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

