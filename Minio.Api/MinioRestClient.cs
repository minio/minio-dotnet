/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2015 Minio, Inc.
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
using Minio.Exceptions;
using System.Text.RegularExpressions;
using RestSharp;
using System.Net;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Minio.Helper;
using Newtonsoft.Json; 

namespace Minio
{
 
    public sealed class MinioRestClient
 
    {
        public string AccessKey { get; private set; }
        public string SecretKey { get; private set; }
        public string Endpoint { get; private set; }
        internal string BaseUrl { get; private set; }
        internal bool Secure { get; private set; }
        internal bool Anonymous { get; }
        internal Uri uri;
        internal string s3AccelerateEndpoint;
        internal RestClient restClient;
        internal V4Authenticator authenticator;
        internal BucketRegionCache regionCache;
        public ClientApiOperations Api;

        internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers = Enumerable.Empty<ApiResponseErrorHandlingDelegate>();

        // Default error handling delegate
        private readonly ApiResponseErrorHandlingDelegate _defaultErrorHandlingDelegate = (response) =>
        {
            if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.BadRequest)
            {
                throw new ClientException(response);
            }
        };

        private static string SystemUserAgent
        {
            get
            {
                string arch = System.Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
                string release = "minio-dotnet/0.2.1";
                return String.Format("Minio ({0};{1}) {2}", System.Environment.OSVersion.ToString(), arch, release);
            }
        }

        private string CustomUserAgent = "";

        private string FullUserAgent
        {
            get
            {
                return SystemUserAgent + " " + CustomUserAgent;
            }

        }
        /// <summary>
        /// Constructs a RestRequest. For AWS, this function overrides the baseUrl in the RestClient
        /// with region specific host path or virtual style path.
        /// </summary>
        /// <param name="method">HTTP method</param>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectName">Object Name</param>
        /// <param name="region">Region - applies only to AWS</param>
        /// <param name="headerMap">unused headerMap</param>
        /// <param name="queryParamMap">unused queryParamMap</param>
        /// <param name="contentType">Content Type</param>
        /// <param name="body">request body</param>
        /// <param name="resourcePath">query string</param>
        /// <returns></returns>
        internal async Task<RestRequest> CreateRequest(Method method, string bucketName, string objectName = null,
                                string region = null, Dictionary<string, string> headerMap = null,
                                string contentType = "application/xml",
                                Object body = null, string resourcePath=null)
        {
            if (bucketName == null && objectName == null)
            {
                throw new InvalidBucketNameException(bucketName, "null bucket name for object '" + objectName + "'");
            }
            utils.validateBucketName(bucketName);
            if (objectName != null)
            {
                utils.validateObjectName(objectName);

            }
            string host = this.BaseUrl;

            //Fetch correct region for bucket
            if (!BucketRegionCache.Instance.Exists(bucketName))
            {
                region = await BucketRegionCache.Instance.Update(this, bucketName);
            }
           
            string baseUrl = null;               //Base url path
            string resource = null;              //Resource being requested  
            bool usePathStyle = false;
            if (s3utils.IsAmazonEndPoint(this.BaseUrl))
            {
                if (this.s3AccelerateEndpoint != null && bucketName != null)
                {
                    // http://docs.aws.amazon.com/AmazonS3/latest/dev/transfer-acceleration.html
                    // Disable transfer acceleration for non-compliant bucket names.
                    if (bucketName.Contains("."))
                    {
                        throw new InvalidTransferAccelerationBucketException(bucketName);
                    }
                    // If transfer acceleration is requested set new host.
                    // For more details about enabling transfer acceleration read here.
                    // http://docs.aws.amazon.com/AmazonS3/latest/dev/transfer-acceleration.html
                    host = s3AccelerateEndpoint;
                }
                else
                {
                    // Fetch new host based on the bucket location.
                    host = AWSS3Endpoints.Instance.endpoint(region);

                }

                usePathStyle = false;
                var scheme = this.Secure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

                if (method == Method.PUT && objectName == null && resourcePath == null)
                {
                    // use path style for make bucket to workaround "AuthorizationHeaderMalformed" error from s3.amazonaws.com
                    usePathStyle = true;
                }
                else if (resourcePath != null && resourcePath.Contains("location"))
                {
                    // use path style for location query
                    usePathStyle = true;
                }
                else if (bucketName.Contains(".") && this.Secure)
                {
                    // use path style where '.' in bucketName causes SSL certificate validation error
                    usePathStyle = true;
                }
                else if (method == Method.HEAD)
                {
                    usePathStyle = true;
                }
   
                if (usePathStyle)
                {
                    resource = utils.UrlEncode(bucketName) + "/";
                }
                else
                {
                    baseUrl = scheme + "://" + utils.UrlEncode(bucketName) + "." + utils.UrlEncode(host) + "/";
                    resource = "/";
                }
            }
            else
            {             
                resource = utils.UrlEncode(bucketName) + "/";
            }

            if (s3utils.IsAmazonEndPoint(this.BaseUrl) && region != null)
            {
                //override the baseUrl in RestClient with virtual style path or region 
                // specific deviation from AWS default url.
                _constructUri(region, bucketName);
                 this.restClient.BaseUrl = usePathStyle == true ? this.uri : new Uri(baseUrl);           

            }
            if (objectName != null)
            {
                // Limitation: OkHttp does not allow to add '.' and '..' as path segment.
                foreach (String pathSegment in objectName.Split('/'))
                {
                    resource += utils.UrlEncode(pathSegment);
                }

            }
            if (resourcePath != null)
            {
                resource += resourcePath;
            }

            RestRequest request = new RestRequest(resource,method);

            if (body != null)
            {
                request.AddParameter(contentType, body, RestSharp.ParameterType.RequestBody);

            }

            if (contentType != null)
            {
                request.AddHeader("Content-Type", contentType);
            }
            if (headerMap != null)
            {
                foreach (KeyValuePair<string, string> entry in headerMap)
                {
                    request.AddHeader(entry.Key, entry.Value);
                }
            }

            return request;
        }
      

        internal void ModifyAWSEndpointFor(string region, string bucketName = null)
        {
            if (region != null)
            {
                _constructUri(region, bucketName);
                this.restClient.BaseUrl = this.uri;
           
            }
        }
        internal async Task<string> ModifyTargetURL(IRestRequest request, string bucketName,bool usePathStyle=false)
        {
            var resource_url = this.Endpoint;

            if (s3utils.IsAmazonEndPoint(this.BaseUrl))
            {
                // ``us-east-1`` is not a valid location constraint according to amazon, so we skip it.
                string location = await BucketRegionCache.Instance.Update(this,bucketName);
                // if (location != "us-east-1")
                {
                    ModifyAWSEndpointFor(location, bucketName);
                    resource_url = MakeTargetURL(location, bucketName,usePathStyle);
                }
                //else
                //{  // use default request
                //    resource_url = "";
                // }
            }
            return resource_url;

        }
        internal string MakeTargetURL(string region = null, string bucketName = null,bool usePathStyle=false)
        {
            string targetUrl = null;
            string host = this.BaseUrl;
            // For Amazon S3 endpoint, try to fetch location based endpoint.
            if (s3utils.IsAmazonEndPoint(this.BaseUrl))
            {
                if (this.s3AccelerateEndpoint != null && bucketName != null)
                {
                    // http://docs.aws.amazon.com/AmazonS3/latest/dev/transfer-acceleration.html
                    // Disable transfer acceleration for non-compliant bucket names.
                    if (bucketName.Contains("."))
                    {
                        throw new InvalidTransferAccelerationBucketException(bucketName);
                    }
                    // If transfer acceleration is requested set new host.
                    // For more details about enabling transfer acceleration read here.
                    // http://docs.aws.amazon.com/AmazonS3/latest/dev/transfer-acceleration.html
                    host = s3AccelerateEndpoint;
                }
                else
                {
                    // Fetch new host based on the bucket location.
                    host = AWSS3Endpoints.Instance.endpoint(region);

                }
            }
            var scheme = this.Secure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            // Make URL only if bucketName is available, otherwise use the
            // endpoint URL.
            if (bucketName != null && s3utils.IsAmazonEndPoint(this.BaseUrl))
            {
           
                // Save if target url will have buckets which suppport virtual host.
                bool isVirtualHostStyle = s3utils.IsVirtualHostSupported(uri, bucketName);

                if (bucketName.Contains(".") && this.Secure)
                {
                    // use path style where '.' in bucketName causes SSL certificate validation error
                    usePathStyle = true;
                }
                // If endpoint supports virtual host style use that always.
                // Currently only S3 and Google Cloud Storage would support
                // virtual host style.
                string urlStr = null;

                if (isVirtualHostStyle || !usePathStyle)
                {
                    targetUrl = scheme + "://" + bucketName + "." + host + "/";
                }
                else
                {
                    // If not fall back to using path style.
                    targetUrl = urlStr + bucketName + "/";
                }
            }
            else
            {
                targetUrl = string.Format("{0}://{1}", scheme, this.BaseUrl);
            }


            return targetUrl;

        }


        /// <summary>
        /// helper to construct uri and validate it.
        /// </summary>
        private void _constructUri(string region = null, string bucketName = null)
        {
            if (string.IsNullOrEmpty(this.BaseUrl))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }
            string host = this.BaseUrl;
            // For Amazon S3 endpoint, try to fetch location based endpoint.
            if (s3utils.IsAmazonEndPoint(this.BaseUrl))
            {
                // Fetch new host based on the bucket location.
                host = AWSS3Endpoints.Instance.endpoint(region);
            }

            var scheme = Secure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
           
            this.Endpoint = string.Format("{0}://{1}", scheme, host);
            this.uri = new Uri(this.Endpoint);
        }
            /*
            // Make URL only if bucketName is available, otherwise use the
            // endpoint URL.
            if (bucketName != null && s3utils.IsAmazonEndPoint(this.BaseUrl))
            {
                // Save if target url will have buckets which suppport virtual host.
                bool isVirtualHostStyle = s3utils.IsVirtualHostSupported(this.uri, bucketName);

                // If endpoint supports virtual host style use that always.
                // Currently only S3 and Google Cloud Storage would support
                // virtual host style.
                string urlStr = null;
                if (isVirtualHostStyle)
                {
                    this.Endpoint = scheme + "://" + bucketName + "." + host + "/";
                }
                else
                {
                   // If not fall back to using path style.
                   this.Endpoint = urlStr + bucketName + "/";
                }
             }
             else
            {
                this.Endpoint = string.Format("{0}://{1}", scheme, this.BaseUrl);
            }
           
            */
           // this.uri = new Uri(this.Endpoint);
       

        /// <summary>
        /// validates URI 
        /// </summary>
        private void _validateUri()
        {
            if (!this.isValidEndpoint(this.uri.Host))
            {
                throw new InvalidEndpointException(this.Endpoint, "Invalid endpoint.");
            }
            if (!this.uri.AbsolutePath.Equals("/", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new InvalidEndpointException(this.Endpoint, "No path allowed in endpoint.");
            }

            if (!string.IsNullOrEmpty(this.uri.Query))
            {
                throw new InvalidEndpointException(this.Endpoint, "No query parameter allowed in endpoint.");
            }

            if (!(this.uri.Scheme.Equals(Uri.UriSchemeHttp) || this.uri.Scheme.Equals(Uri.UriSchemeHttps)))
            {
                throw new InvalidEndpointException(this.Endpoint, "Invalid scheme detected in endpoint.");
            }
            string amzHost = this.uri.Host;
            if ((amzHost.EndsWith(".amazonaws.com", StringComparison.CurrentCultureIgnoreCase))
                 && !(amzHost.Equals("s3.amazonaws.com", StringComparison.CurrentCultureIgnoreCase)))
             {
                 throw new InvalidEndpointException(this.Endpoint, "For Amazon S3, host should be \'s3.amazonaws.com\' in endpoint.");
             }
             

        }
        /// <summary>
        /// Validate Url endpoint 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private bool isValidEndpoint(string endpoint)
        {
            // endpoint may be a hostname
            // refer https://en.wikipedia.org/wiki/Hostname#Restrictions_on_valid_host_names
            // why checks are as shown below.
            if (endpoint.Length < 1 || endpoint.Length > 253)
            {
                return false;
            }

            foreach (var label in endpoint.Split('.'))
            {
                if (label.Length < 1 || label.Length > 63)
                {
                    return false;
                }

                Regex validLabel = new Regex("^[a-zA-Z0-9][a-zA-Z0-9-]*");
                Regex validEndpoint = new Regex(".*[a-zA-Z0-9]$");

                if (!(validLabel.IsMatch(label) && validEndpoint.IsMatch(endpoint)))
                {
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        ///Sets app version and name
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="appVersion"></param>
        /// <returns></returns>
        public void SetAppInfo(string appName, string appVersion)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException("Appname cannot be null or empty");
            }
            if (string.IsNullOrEmpty(appVersion))
            {
                throw new ArgumentException("Appversion cannot be null or empty");
            }
            string customAgent = appName + "/" + appVersion;

            this.restClient.UserAgent = this.FullUserAgent;
        }
        /// <summary>
        ///  Creates and returns an Cloud Storage client
        /// </summary>
        /// <param name="endpoint">Location of the server, supports HTTP and HTTPS</param>
        /// <param name="accessKey">Access Key for authenticated requests</param>
        /// <param name="secretKey">Secret Key for authenticated requests</param>
        /// <returns>Client with the uri set as the server location and authentication parameters set.</returns>

        public MinioRestClient(string endpoint, string accessKey = "", string secretKey = "")
        {

            this.Secure = false;
            this.BaseUrl = endpoint;
            this.AccessKey = accessKey;
            this.SecretKey = secretKey;
            this.s3AccelerateEndpoint = null;
            this.regionCache = BucketRegionCache.Instance;
            this.Anonymous = utils.isAnonymousClient(accessKey, secretKey);
            _constructUri();
            _validateUri();

            restClient = new RestSharp.RestClient(this.uri);
            restClient.UserAgent = this.FullUserAgent;

            authenticator = new V4Authenticator(accessKey, secretKey);
            restClient.Authenticator = authenticator;
            if (accessKey == "" || secretKey == "")
            {
                this.Anonymous = true;
            }
            else
            {
                this.Anonymous = false;
            }

            this.Api = new ClientApiOperations(this);
            return;

        }
        /// <summary>
        /// Connects to Cloud Storage with HTTPS if this method is invoked on client object
        /// </summary>
        /// <returns></returns>
        public MinioRestClient WithSSL()
        {
            this.Secure = true;
            _constructUri();
            this.restClient.BaseUrl = this.uri;
            return this;
        }

        internal async Task<IRestResponse<T>> ExecuteTaskAsync<T>(IEnumerable<ApiResponseErrorHandlingDelegate> errorHandlers, IRestRequest request) where T : new()
        {
            var response = await this.restClient.ExecuteTaskAsync<T>(request, CancellationToken.None);
            HandleIfErrorResponse(response, errorHandlers);
            return response;
        }
        internal async Task<IRestResponse> ExecuteTaskAsync(IEnumerable<ApiResponseErrorHandlingDelegate> errorHandlers, IRestRequest request)
        {
            var response = await this.restClient.ExecuteTaskAsync(request, CancellationToken.None);

            var fullUrl = this.restClient.BuildUri(request);
            Console.Out.WriteLine(fullUrl);
            HandleIfErrorResponse(response, errorHandlers);
            return response;
        }


  
  
     
      
        /// <summary>
        /// Parse response errors if any and return relevant error messages
        /// </summary>
        /// <param name="response"></param>

        internal void ParseError(IRestResponse response)
        {
            if (response == null)
            {
                throw new ConnectionException("Response is nil. Please report this issue https://github.com/minio/minio-dotnet/issues");
            }
            if (HttpStatusCode.Redirect.Equals(response.StatusCode) || HttpStatusCode.TemporaryRedirect.Equals(response.StatusCode) || HttpStatusCode.MovedPermanently.Equals(response.StatusCode))
            {
                throw new RedirectionException("Redirection detected. Please report this issue https://github.com/minio/minio-dotnet/issues");
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                if (HttpStatusCode.Forbidden.Equals(response.StatusCode) || HttpStatusCode.NotFound.Equals(response.StatusCode) ||
                    HttpStatusCode.MethodNotAllowed.Equals(response.StatusCode) || HttpStatusCode.NotImplemented.Equals(response.StatusCode))
                {
                    ClientException e = null;
                    ErrorResponse errorResponse = new ErrorResponse();

                    foreach (Parameter parameter in response.Headers)
                    {
                        if (parameter.Name.Equals("x-amz-id-2", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.HostId = parameter.Value.ToString();
                        }
                        if (parameter.Name.Equals("x-amz-request-id", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.RequestId = parameter.Value.ToString();
                        }
                        if (parameter.Name.Equals("x-amz-bucket-region", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.BucketRegion = parameter.Value.ToString();
                        }
                    }

                    errorResponse.Resource = response.Request.Resource;

                    if (HttpStatusCode.NotFound.Equals(response.StatusCode))
                    {
                        int pathLength = response.Request.Resource.Split('/').Count();
                        if (pathLength > 1)
                        {
                            errorResponse.Code = "NoSuchKey";
                            var objectName = response.Request.Resource.Split('/')[1];
                            e = new ObjectNotFoundException(objectName, "Not found.");
                        }
                        else if (pathLength == 1)
                        {
                            errorResponse.Code = "NoSuchBucket";
                            var bucketName = response.Request.Resource.Split('/')[0];
                            BucketRegionCache.Instance.Remove(bucketName);
                            e = new BucketNotFoundException(bucketName, "Not found.");
                        }
                        else
                        {
                            e = new InternalClientException("404 without body resulted in path with less than two components");
                        }
                    }
                    else if (HttpStatusCode.Forbidden.Equals(response.StatusCode))
                    {
                        errorResponse.Code = "Forbidden";
                        e = new AccessDeniedException("Access denied on the resource: " + response.Request.Resource);
                    }
                    e.Response = errorResponse;
                    throw e;
                }
                throw new InternalClientException("Unsuccessful response from server without XML error: " + response.StatusCode);
            }

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ErrorResponse errResponse = (ErrorResponse)(new XmlSerializer(typeof(ErrorResponse)).Deserialize(stream));

            ClientException clientException = new ClientException(errResponse.Message);
            clientException.Response = errResponse;
            clientException.XmlError = response.Content;
            throw clientException;
        }
        /// <summary>
        /// Delegate errors to handlers
        /// </summary>
        /// <param name="response"></param>
        /// <param name="handlers"></param>
        private void HandleIfErrorResponse(IRestResponse response, IEnumerable<ApiResponseErrorHandlingDelegate> handlers)
        {
            LogRequest(response.Request, response, 10);
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            foreach (var handler in handlers)
            {
                handler(response);
            }

            _defaultErrorHandlingDelegate(response);
        }

        private void LogRequest(IRestRequest request, IRestResponse response, long durationMs)
        {
            var requestToLog = new
            {
                resource = request.Resource,
                // Parameters are custom anonymous objects in order to have the parameter type as a nice string
                // otherwise it will just show the enum value
                parameters = request.Parameters.Select(parameter => new
                {
                    name = parameter.Name,
                    value = parameter.Value,
                    type = parameter.Type.ToString()
                }),
                // ToString() here to have the method as a nice string otherwise it will just show the enum value
                method = request.Method.ToString(),
                // This will generate the actual Uri used in the request
                uri = restClient.BuildUri(request),
            };

            var responseToLog = new
            {
                statusCode = response.StatusCode,
                content = response.Content,
                headers = response.Headers,
                // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
                responseUri = response.ResponseUri,
                errorMessage = response.ErrorMessage,
            };

            Console.Out.WriteLine(string.Format("Request completed in {0} ms, Request: {1}, Response: {2}",
                    durationMs,
                    JsonConvert.SerializeObject(requestToLog,Formatting.Indented),
                    JsonConvert.SerializeObject(responseToLog,Formatting.Indented)));
        }

    }
    internal delegate void ApiResponseErrorHandlingDelegate(IRestResponse response);
 
}
