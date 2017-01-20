using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Minio.DataModel;
using RestSharp;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Minio.Exceptions;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Minio
{
    class ObjectOperations : IObjectOperations
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

        internal ObjectOperations(MinioRestClient client)
        {
            this._client = client;
        }
        public async Task GetObjectAsync(string bucketName, string objectName, Action<Stream> cb)
        {

            RestRequest request = new RestRequest(bucketName + "/" + UrlEncode(objectName), Method.GET);
            request.ResponseWriter = cb;
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
              
                this._client.ParseError(response);
            }
         
            return;
        }
     

      
    private static string UrlEncode(string input)
        {
            return Uri.EscapeDataString(input).Replace("%2F", "/");
        }

    }
}
