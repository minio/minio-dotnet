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
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers,request);

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
        /// <summary>
        /// List all objects non-recursively in a bucket with a given prefix, optionally emulating a directory
        /// </summary>
        /// <param name="bucketName">Bucket to list objects from</param>
        /// <param name="prefix">Filters all objects not beginning with a given prefix</param>
        /// <param name="recursive">Set to false to emulate a directory</param>
        /// <returns>A iterator lazily populated with objects</returns>
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
                      else
                      {
                          marker = lastItem.Key;
                      }
                      isRunning = result.Item1.IsTruncated;
                  }
              });
        }
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
            var request = new RestRequest(path, Method.GET);
            var response = await this._client.ExecuteTaskAsync(this._client.NoErrorHandlers, request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                this._client.ParseError(response);
            }
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
       
    }
}

