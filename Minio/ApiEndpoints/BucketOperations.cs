/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
using System.Globalization;
using System.Reactive.Linq;

namespace Minio
{
    public partial class MinioClient : IBucketOperations
    {

        /// <summary>
        /// List all objects in a bucket
        /// </summary>
        /// <param name="bucketName">Bucket to list objects from</param>
        /// <returns>An iterator lazily populated with objects</returns>
        public async Task<ListAllMyBucketsResult> ListBucketsAsync()
        {
            // Set Target URL
            Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure);
            SetTargetURL(requestUrl);
            // Initialize a new client 
            //PrepareClient();

            var request = new RestRequest("/", Method.GET);
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request);

            ListAllMyBucketsResult bucketList = new ListAllMyBucketsResult ();
            if (HttpStatusCode.OK.Equals(response.StatusCode))
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
                var stream = new MemoryStream(contentBytes);
                bucketList = (ListAllMyBucketsResult)(new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream));
                return bucketList;
            }
            return bucketList;
        }

        /// <summary>
        /// Create a private bucket with the given name.
        /// </summary>
        /// <param name="bucketName">Name of the new bucket</param>
        public async Task MakeBucketAsync(string bucketName, string location = "us-east-1")
        {
            
            // Set Target URL
            Uri requestUrl = RequestUtil.MakeTargetURL(this.BaseUrl, this.Secure);
            SetTargetURL(requestUrl);
           
            var request = new RestRequest("/" + bucketName, Method.PUT);
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            request.RequestFormat = DataFormat.Xml;
            // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
            if (location != "us-east-1")
            {
                CreateBucketConfiguration config = new CreateBucketConfiguration(location);
                request.AddBody(config);
            }
 
            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request);

        }


        /// <summary>
        /// Returns true if the specified bucketName exists, otherwise returns false.
        /// </summary>
        /// <param name="bucketName">Bucket to test existence of</param>
        /// <returns>true if exists and user has access</returns>
        public async Task<bool> BucketExistsAsync(string bucketName)
        {
            try
            {
                var request = await this.CreateRequest(Method.HEAD,
                                                   bucketName);
                var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(BucketNotFoundException))
                {
                    return false;
                }
                throw ex;
            }
            return true;
        }

        /// <summary>
        /// Remove a bucket
        /// </summary>
        /// <param name="bucketName">Name of bucket to remove</param>
        public async Task RemoveBucketAsync(string bucketName)
        {
            var request = await this.CreateRequest(Method.DELETE, bucketName, resourcePath:null);

            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request);

        }

        /// <summary>
        /// List all objects non-recursively in a bucket with a given prefix, optionally emulating a directory
        /// </summary>
        /// <param name="bucketName">Bucket to list objects from</param>
        /// <param name="prefix">Filters all objects not beginning with a given prefix</param>
        /// <param name="recursive">Set to false to emulate a directory</param>
        /// <returns>An observable of items that client can subscribe to</returns>
        public IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = true)
        {
            return Observable.Create<Item>(
              async obs =>
              {
                  bool isRunning = true;
                  string marker = null;
                  while (isRunning)
                  {
                      Tuple<ListBucketResult, List<Item>> result = await GetObjectListAsync(bucketName, prefix, recursive, marker);
                      Item lastItem = null;
                      foreach (Item item in result.Item2)
                      {
                          lastItem = item;
                          obs.OnNext(item);
                      }
                      if (result.Item1.NextMarker != null)
                      {
                          marker = result.Item1.NextMarker;
                      }
                      else if (lastItem != null)
                      {
                          marker = lastItem.Key;
                      }
                      isRunning = result.Item1.IsTruncated;
                  }
              });
        }

        /// <summary>
        /// Gets the list of objects in the bucket filtered by prefix
        /// </summary>
        /// <param name="bucketName">Bucket to list objects from</param>
        /// <param name="prefix">Filters all objects not beginning with a given prefix</param>
        /// <param name="recursive">Set to false to emulate a directory</param>
        /// <param name="marker">marks location in the iterator sequence</param>
        /// <returns>A tuple populated with objects</returns>
        private async Task<Tuple<ListBucketResult, List<Item>>> GetObjectListAsync(string bucketName, string prefix, bool recursive, string marker)
        {
            var queries = new List<string>();
            if (!recursive)
            {
                queries.Add("delimiter=%2F");
            }
            if (prefix != null)
            {
                queries.Add("prefix=" + Uri.EscapeDataString(prefix));
            }
            if (marker != null)
            {
                queries.Add("marker=" + Uri.EscapeDataString(marker));
            }
            queries.Add("max-keys=1000");
            string query = string.Join("&", queries);

            string path = bucketName;
            if (query.Length > 0)
            {
                path += "?" + query;
            }

            var request = await this.CreateRequest(Method.GET,
                                                     bucketName,
                                                     resourcePath: "?" + query);



            var response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request);

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ListBucketResult listBucketResult = (ListBucketResult)(new XmlSerializer(typeof(ListBucketResult)).Deserialize(stream));

            XDocument root = XDocument.Parse(response.Content);

            var items = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Contents")
                         select new Item()
                         {
                             Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Key").Value,
                             LastModified = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}LastModified").Value,
                             ETag = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}ETag").Value,
                             Size = UInt64.Parse(c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Size").Value, CultureInfo.CurrentCulture),
                             IsDir = false
                         });

            var prefixes = (from c in root.Root.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}CommonPrefixes")
                            select new Item()
                            {
                                Key = c.Element("{http://s3.amazonaws.com/doc/2006-03-01/}Prefix").Value,
                                IsDir = true
                            });

            items = items.Concat(prefixes);

            return new Tuple<ListBucketResult, List<Item>>(listBucketResult, items.ToList());
        }

        /// <summary>
        /// Returns current policy stored on the server for this bucket
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <returns>Returns the Bucket policy</returns>
        private async Task<BucketPolicy> GetPolicyAsync(string bucketName)
        {
            BucketPolicy policy = null;
            IRestResponse response = null;

            var path = bucketName + "?policy";

            var request = await this.CreateRequest(Method.GET, bucketName,
                                 contentType: "application/json",
                                 resourcePath: "?policy");
            try
            {
                response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request);
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);

                var stream = new MemoryStream(contentBytes);
                policy = BucketPolicy.parseJson(stream, bucketName);

            }
            catch (ErrorResponseException e)
            {
                // Ignore if there is 
                if (!e.Response.Code.Equals("NoSuchBucketPolicy"))
                {
                    throw e;
                }
            }
            finally
            {
                if (policy == null)
                {
                    policy = new BucketPolicy(bucketName);
                }
            }
            return policy;
        }


        /// <summary>
        /// Get bucket policy at given objectPrefix
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="objectPrefix">Name of the object prefix</param>
        /// <returns>Returns the PolicyType </returns>
        public async Task<PolicyType> GetPolicyAsync(string bucketName, string objectPrefix = "")
        {
            BucketPolicy policy = await GetPolicyAsync(bucketName);
            return policy.getPolicy(objectPrefix);
        }

        /// <summary>
        /// Internal method that sets the bucket access policy
        /// </summary>
        /// <param name="bucketName">Bucket Name.</param>
        /// <param name="policy">Valid Json policy object</param>
        /// <returns></returns>
        private async Task setPolicyAsync(string bucketName, BucketPolicy policy)
        {

            string policyJson = policy.getJson();
            var request = await this.CreateRequest(Method.PUT, bucketName,
                                           resourcePath: "?policy",
                                           contentType: "application/json",
                                           body: policyJson);

            IRestResponse response = await this.ExecuteTaskAsync(this.NoErrorHandlers, request);
        }

        /// <summary>
        /// Sets the current bucket policy
        /// </summary>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectPrefix">Name of the object prefix.</param>
        /// <param name="policyType">Desired Policy type change </param>
        /// <returns></returns>
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