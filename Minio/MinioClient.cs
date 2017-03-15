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
using System.Runtime.InteropServices;

namespace Minio
{
    public partial class MinioClient
    {
        // Save Credentials from user
        internal string AccessKey { get; private set; }
        internal string SecretKey { get; private set; }
        internal string BaseUrl { get; private set; }

        // Reconstructed endpoint with scheme and host.In the case of Amazon, this url
        // is the virtual style path or location based endpoint
        internal string Endpoint { get; private set; }
        // Corresponding URI for above endpoint
        internal Uri uri;

        // Indicates if we are using HTTPS or not
        internal bool Secure { get; private set; }

        // RESTSharp client
        internal RestClient restClient;
        // Custom authenticator for RESTSharp
        internal V4Authenticator authenticator;

        // Cache holding bucket to region mapping for buckets seen so far.
        internal BucketRegionCache regionCache;

        // Enables HTTP tracing if set to true
        private bool trace = false;

        private const string RegistryAuthHeaderKey = "X-Registry-Auth";

        internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers = Enumerable.Empty<ApiResponseErrorHandlingDelegate>();

        // Default error handling delegate
        private readonly ApiResponseErrorHandlingDelegate _defaultErrorHandlingDelegate = (response) =>
        {
            if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.BadRequest)
            {
                ParseError(response);
            }
        };

        private static string SystemUserAgent
        {
            get
            {
                string release = "minio-dotnet/0.2.1";
#if NET452
                string arch = System.Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
                return String.Format("Minio ({0};{1}) {2}", System.Environment.OSVersion.ToString(), arch, release);

#else
                string arch = RuntimeInformation.OSArchitecture.ToString();
                return String.Format("Minio ({0};{1}) {2}", RuntimeInformation.OSDescription, arch, release);

#endif
            }
        }

        private string CustomUserAgent = "";
        // returns the User-Agent header for the request
        private string FullUserAgent
        {
            get
            {
                return SystemUserAgent + " " + CustomUserAgent;
            }

        }
        /// <summary>
        /// Constructs a RestRequest. For AWS, this function has the side-effect of overriding the baseUrl 
        /// in the RestClient with region specific host path or virtual style path.
        /// </summary>
        /// <param name="method">HTTP method</param>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectName">Object Name</param>
        /// <param name="headerMap">headerMap</param>
        /// <param name="queryParamMap">unused queryParamMap</param>
        /// <param name="contentType">Content Type</param>
        /// <param name="body">request body</param>
        /// <param name="resourcePath">query string</param>
        /// <returns>A RestRequest</returns>
        internal async Task<RestRequest> CreateRequest(Method method, string bucketName, string objectName = null,
                                Dictionary<string, string> headerMap = null,
                                string contentType = "application/xml",
                                Object body = null, string resourcePath = null, string region = null)
        {
            //Validate bucket name and object name
            if (bucketName == null && objectName == null)
            {
                throw new InvalidBucketNameException(bucketName, "null bucket name for object '" + objectName + "'");
            }
            utils.validateBucketName(bucketName);
            if (objectName != null)
            {
                utils.validateObjectName(objectName);

            }

            // Start with user specified endpoint
            string host = this.BaseUrl;

            //Fetch correct region for bucket
            if (region == null)
            {
                if (!BucketRegionCache.Instance.Exists(bucketName))
                {
                    region = await BucketRegionCache.Instance.Update(this, bucketName);
                }
                else
                {
                    region = BucketRegionCache.Instance.Region(bucketName);
                }
            }


            // This section reconstructs the url with scheme followed by location specific endpoint( s3.region.amazonaws.com)
            // or Virtual Host styled endpoint (bucketname.s3.region.amazonaws.com) for Amazon requests.
            string resource = "";              //Resource being requested  
            bool usePathStyle = false;
            if (s3utils.IsAmazonEndPoint(this.BaseUrl))
            {
                usePathStyle = false;

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
                //else if (method == Method.HEAD)
                //{
                //    usePathStyle = true;
               // }

                if (usePathStyle)
                {
                    resource = utils.UrlEncode(bucketName) + "/";
                }
            }
            else
            {
                resource = utils.UrlEncode(bucketName) + "/";
            }

            // Prepare client state
            PrepareClient(bucketName, region, usePathStyle);

            if (objectName != null)
            {
                // Limitation: OkHttp does not allow to add '.' and '..' as path segment.
                foreach (String pathSegment in objectName.Split('/'))
                {
                    resource += utils.UrlEncode(pathSegment);
                }

            }

            // Append query string passed in 
            if (resourcePath != null)
            {
                resource += resourcePath;
            }

            RestRequest request = new RestRequest(resource, method);

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

        /// <summary>
        /// This method initializes a new RESTClient. The host URI for Amazon is set to virtual hosted style
        /// if usePathStyle is false. Otherwise path style URL is constructed.
        /// </summary>
        /// <param name="bucketName">bucketName</param>
        /// <param name="region">Region bucket resides in.</param>
        /// <param name="usePathStyle">bool controlling if pathstyle URL needs to be constructed or virtual hosted style URL</param>
        internal void PrepareClient(string bucketName = null, string region = null, bool usePathStyle = true)
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
                if (!usePathStyle)
                {
                    host = utils.UrlEncode(bucketName) + "." + utils.UrlEncode(host) + "/";
                }
            }
            var scheme = Secure ? utils.UrlEncode("https") : utils.UrlEncode("http"); 

            // This is the actual url pointed to for all HTTP requests
            this.Endpoint = string.Format("{0}://{1}", scheme, host);
            this.uri = TryCreateUri(this.Endpoint);  
            _validateEndpoint();

            // Initialize a new REST client. This uri will be modified if region specific endpoint/virtual style request
            // is decided upon while constructing a request for Amazon.
            restClient = new RestSharp.RestClient(this.uri);
            restClient.UserAgent = this.FullUserAgent;

            authenticator = new V4Authenticator(this.AccessKey, this.SecretKey);
            restClient.Authenticator = authenticator;
        }

        private Uri TryCreateUri(string endpoint)
        {
            Uri uri = null;
            try
            {
                uri =  new Uri(endpoint);
            }
            catch( UriFormatException e)
            {
                throw new InvalidEndpointException(e.Message);
            }
            return uri;
        }

        /// <summary>
        /// Validates URI to check if it is well formed. Otherwise cry foul.
        /// </summary>
        private void _validateEndpoint()
        {
            if (string.IsNullOrEmpty(this.BaseUrl))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }
            string host = this.BaseUrl;

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
            if ((!this.uri.Scheme.ToLowerInvariant().Equals("https")) && (!this.uri.Scheme.ToLowerInvariant().Equals("http")))
           //kp if (!(this.uri.Scheme.Equals(Uri.UriSchemeHttp) || this.uri.Scheme.Equals(Uri.UriSchemeHttps)))
            {
                throw new InvalidEndpointException(this.Endpoint, "Invalid scheme detected in endpoint.");
            }
            string amzHost = this.BaseUrl;
            if ((amzHost.EndsWith(".amazonaws.com", StringComparison.CurrentCultureIgnoreCase))
                 && !(amzHost.Equals("s3.amazonaws.com", StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new InvalidEndpointException(amzHost, "For Amazon S3, host should be \'s3.amazonaws.com\' in endpoint.");
            }
        }

        /// <summary>
        /// Validate Url endpoint 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns>true/false</returns>
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
        ///Sets app version and name. Used by RestSharp for constructing User-Agent header in all HTTP requests
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
            this.CustomUserAgent = appName + "/" + appVersion;
        }

        /// <summary>
        ///  Creates and returns an Cloud Storage client
        /// </summary>
        /// <param name="endpoint">Location of the server, supports HTTP and HTTPS</param>
        /// <param name="accessKey">Access Key for authenticated requests (Optional,can be omitted for anonymous requests)</param>
        /// <param name="secretKey">Secret Key for authenticated requests (Optional,can be omitted for anonymous requests)</param>
        /// <returns>Client initialized with user credentials</returns>
        public MinioClient(string endpoint, string accessKey = "", string secretKey = "")
        {

            this.Secure = false;

            // Save user entered credentials
            this.BaseUrl = endpoint;
            this.AccessKey = accessKey;
            this.SecretKey = secretKey;

            //Instantiate a region cache 
            this.regionCache = BucketRegionCache.Instance;

            return;

        }

        /// <summary>
        /// Connects to Cloud Storage with HTTPS if this method is invoked on client object
        /// </summary>
        /// <returns></returns>
        public MinioClient WithSSL()
        {
            this.Secure = true;
            return this;
        }

        internal async Task<IRestResponse<T>> ExecuteTaskAsync<T>(IEnumerable<ApiResponseErrorHandlingDelegate> errorHandlers, IRestRequest request) where T : new()
        {
            DateTime startTime = DateTime.Now;
            TaskCompletionSource<IRestResponse<T>> tcs = new TaskCompletionSource<IRestResponse<T>>();
            RestRequestAsyncHandle handle = this.restClient.ExecuteAsync<T>(
                                            request, r =>
                                            {
                                                tcs.SetResult(r);

                                            });
            //var response = await this.restClient.ExecuteTaskAsync<T>(request, CancellationToken.None);
            var response = await tcs.Task;
            HandleIfErrorResponse(response, errorHandlers, startTime);
            return response;
        }
        /// <summary>
        /// Actual doer that executes the REST request to the server
        /// </summary>
        /// <param name="errorHandlers">List of handlers to override default handling</param>
        /// <param name="request">request</param>
        /// <returns>IRESTResponse</returns>
        internal async Task<IRestResponse> ExecuteTaskAsync(IEnumerable<ApiResponseErrorHandlingDelegate> errorHandlers, IRestRequest request)
        {
            DateTime startTime = DateTime.Now;
            // Logs full url when HTTPtracing is enabled.
            if (this.trace)
            {
                var fullUrl = this.restClient.BuildUri(request);
                Console.Out.WriteLine("Full URL of Request {0}", fullUrl);
            }
            TaskCompletionSource<IRestResponse> tcs = new TaskCompletionSource<IRestResponse>();
            RestRequestAsyncHandle handle = this.restClient.ExecuteAsync(
                                            request, resp =>
                                            {
                                                tcs.SetResult(resp);

                                            });
            IRestResponse response = await tcs.Task;
            HandleIfErrorResponse(response, errorHandlers, startTime);
            return response;
        }


        /// <summary>
        /// Parse response errors if any and return relevant error messages
        /// </summary>
        /// <param name="response"></param>
        internal static void ParseError(IRestResponse response)
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
                ErrorResponse errorResponse = new ErrorResponse();

                if (HttpStatusCode.Forbidden.Equals(response.StatusCode) || HttpStatusCode.NotFound.Equals(response.StatusCode) ||
                    HttpStatusCode.MethodNotAllowed.Equals(response.StatusCode) || HttpStatusCode.NotImplemented.Equals(response.StatusCode))
                {
                    MinioException e = null;
                    
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
                            var bucketName = response.Request.Resource.Split('/')[0];
                            var objectName = response.Request.Resource.Split('/')[1];
                            if (objectName == "")
                            {
                                e = new BucketNotFoundException(bucketName, "Not found.");
                            }
                            else
                            {
                                e = new ObjectNotFoundException(objectName, "Not found.");
                            }
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
                throw new InternalClientException("Unsuccessful response from server without XML error: " + response.ErrorMessage);
            }

            if (response.StatusCode.Equals(HttpStatusCode.NotFound) && response.Request.Resource.EndsWith("?location")
                && response.Request.Method.Equals(Method.GET))
            {
                var bucketName = response.Request.Resource.Split('?')[0];
                BucketRegionCache.Instance.Remove(bucketName);
                throw new BucketNotFoundException(bucketName, "Not found.");
            }

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ErrorResponse errResponse = (ErrorResponse)(new XmlSerializer(typeof(ErrorResponse)).Deserialize(stream));

            // Handle XML response for Bucket Policy not found case
            if (response.StatusCode.Equals(HttpStatusCode.NotFound) && response.Request.Resource.EndsWith("?policy")
                && response.Request.Method.Equals(Method.GET) && errResponse.Code.Equals("NoSuchBucketPolicy"))
            {
                ErrorResponseException ErrorException = new ErrorResponseException(errResponse.Message);
                ErrorException.Response = errResponse;
                ErrorException.XmlError = response.Content;
                throw ErrorException;
            }

            MinioException MinioException = new MinioException(errResponse.Message);
            MinioException.Response = errResponse;
            MinioException.XmlError = response.Content;
            throw MinioException;
        }
        /// <summary>
        /// Delegate errors to handlers
        /// </summary>
        /// <param name="response"></param>
        /// <param name="handlers"></param>
        private void HandleIfErrorResponse(IRestResponse response, IEnumerable<ApiResponseErrorHandlingDelegate> handlers, DateTime startTime)
        {
            //Logs Response if HTTP tracing is enabled
            if (this.trace)
            {
                DateTime now = DateTime.Now;
                LogRequest(response.Request, response, (now - startTime).TotalMilliseconds);
            }
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }
            // Run through handlers passed to take up error handling
            foreach (var handler in handlers)
            {
                handler(response);
            }
            
            //Fall back default error handler
            _defaultErrorHandlingDelegate(response);
            
        }
        /// <summary>
        /// Sets HTTP tracing On.Writes output to Console
        /// </summary>
        public void SetTraceOn()
        {
            this.trace = true;
        }
        /// <summary>
        /// Sets HTTP tracing Off.
        /// </summary>
        public void SetTraceOff()
        {
            this.trace = false;
        }

        /// <summary>
        /// Logs the request sent to server and corresponding response 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="durationMs"></param>
        private void LogRequest(IRestRequest request, IRestResponse response, double durationMs)
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
                    JsonConvert.SerializeObject(requestToLog, Formatting.Indented),
                    JsonConvert.SerializeObject(responseToLog, Formatting.Indented)));
        }

    }
    internal delegate void ApiResponseErrorHandlingDelegate(IRestResponse response);

}
