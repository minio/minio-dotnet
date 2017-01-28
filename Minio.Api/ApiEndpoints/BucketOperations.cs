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
using System.Reactive.Linq;

namespace Minio
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

        public async Task<ListAllMyBucketsResult> ListBucketsAsync()
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
        public async Task<bool> MakeBucketAsync(string bucketName, string location = "us-east-1")
        {
            var request = new RestRequest("/" + bucketName, Method.PUT);
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(location);
                request.AddBody(config);
            }
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            this._client.ParseError(response);
            return false;
        }

        public async Task<bool> BucketExistsAsync(string bucketName)
        {
            var request = new RestRequest(bucketName, Method.HEAD);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    this._client.ParseError(response);
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(BucketNotFoundException))
                    {
                        return false;
                    }
                    throw ex;
                }
            }

            return true;
        }


        public async Task RemoveBucketAsync(string bucketName)
        {
            var request = new RestRequest(bucketName, Method.DELETE);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (!response.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                this._client.ParseError(response);
            }
        }

        /**
         * Returns the parsed current bucket access policy.
         */
        private async Task<BucketPolicy> GetPolicyAsync(string bucketName)
        {
            BucketPolicy policy = null;
            IRestResponse response = null;
            var path =bucketName + "?policy";

            var request = new RestRequest(path, Method.GET);
            response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);
            Console.Out.WriteLine(response.ResponseUri);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                this._client.ParseError(response);
            }
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            policy = BucketPolicy.parseJson(stream, bucketName);
            if (policy == null)
            {
                policy = new BucketPolicy(bucketName);
            }

            return policy;
        }

        /**
         * Get bucket policy at given objectPrefix
         *
         * @param bucketName   Bucket name.
         * @param objectPrefix name of the object prefix
         *
         * </p><b>Example:</b><br>
         * <pre>{@code String policy = minioClient.getBucketPolicy("my-bucketname", "my-objectname");
         * System.out.println(policy); }</pre>
         */
        public async Task<PolicyType> GetPolicyAsync(String bucketName, String objectPrefix)
        {
            BucketPolicy policy = await GetPolicyAsync(bucketName);
            return policy.getPolicy(objectPrefix);
        }
        /**
         * Sets the bucket access policy.
         */
        private async Task setPolicyAsync(String bucketName, BucketPolicy policy)
        {
            var request = new RestRequest(bucketName, Method.PUT);
            request.AddHeader("Content-Type", "application/json");
            request.AddQueryParameter("policy", "");
            String policyJson = policy.getJson();
            request.AddJsonBody(policyJson);
            IRestResponse response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);
        }

        /**
         * Set policy on bucket and object prefix.
         *
         * @param bucketName   Bucket name.
         * @param objectPrefix Name of the object prefix.
         * @param policyType   Enum of {@link PolicyType}.
         *
         * </p><b>Example:</b><br>
         * <pre>{@code setBucketPolicy("my-bucketname", "my-objectname", BucketPolicy.ReadOnly); }</pre>
         */
        public async Task SetPolicyAsync(String bucketName, String objectPrefix, PolicyType policyType)
        {
            utils.validateObjectPrefix(objectPrefix);
            BucketPolicy policy = await GetPolicyAsync(bucketName);
            if (policyType == PolicyType.NONE && policy.Statements() == null)
            {
                // As the request is for removing policy and the bucket
                // has empty policy statements, just return success.
                return;
            }

            policy.setPolicy(policyType, objectPrefix);

            await setPolicyAsync(bucketName, policy);
        }
    }
}
   



    
  

   


