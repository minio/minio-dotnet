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

namespace Minio
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using Minio.Exceptions;
    using Minio.Helper;
    using Minio.Rest;
    using Newtonsoft.Json;
    using RestSharp.Portable;

    public abstract partial class AbstractMinioClient : IMinioClient
    {
        private const string RegistryAuthHeaderKey = "X-Registry-Auth";

        private static string defaultUserAgent;

        // Default error handling delegate
        private readonly ApiResponseErrorHandlingDelegate defaultErrorHandlingDelegate = response =>
        {
            if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.BadRequest)
            {
                ParseError(response);
            }
        };

        internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers =
            Enumerable.Empty<ApiResponseErrorHandlingDelegate>();

        // Custom authenticator for RESTSharp
        private V4Authenticator authenticator;

        private string customUserAgent;

        // RESTSharp client
        private RestClient restClient;

        // Corresponding URI for above endpoint
        private Uri uri;

        protected AbstractMinioClient(MinioSettings minioSettings)
        {
            this.Secure = false;
            // Save user entered credentials
            this.BaseUrl = minioSettings.Endpoint;
            this.AccessKey = minioSettings.AccessKey;
            this.SecretKey = minioSettings.SecretKey;

            this.InitClient(minioSettings);
        }

        internal CryptoProvider CryptoProvider { get; private set; }
        internal LogProvider LogProvider { get; private set; }

        // Reconstructed endpoint with scheme and host.In the case of Amazon, this url
        // is the virtual style path or location based endpoint
        private string Endpoint { get; set; }

        // Indicates if we are using HTTPS or not
        internal bool Secure { get; private set; }

        // Enables HTTP tracing if set to true
        public bool Trace { get; set; }

        // Save Credentials from user
        public string AccessKey { get; }

        public string SecretKey { get; }

        public string BaseUrl { get; }

        public void SetCustomUserAgent(string appName, string appVersion)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException("Appname cannot be null or empty");
            }
            if (string.IsNullOrEmpty(appVersion))
            {
                throw new ArgumentException("Appversion cannot be null or empty");
            }
            this.customUserAgent = appName + "/" + appVersion;
        }

        public IMinioClient WithSsl()
        {
            this.Secure = true;
            var secureUrl = RequestUtil.MakeTargetUrl(this.BaseUrl, this.Secure);
            this.SetTargetUrl(secureUrl);
            return this;
        }

        public void SetTargetUrl(Uri baseUrl)
        {
            this.restClient.BaseUrl = baseUrl;
        }

        protected abstract SystemUserAgentSettings GetSystemUserAgentSettings();

        /// <summary>
        ///     Get user agent from web view
        /// </summary>
        /// <returns></returns>
        protected abstract string GetPlatformUserAgent();

        private string GetDefaultUserAgent()
        {
            if (!string.IsNullOrEmpty(defaultUserAgent))
            {
                return defaultUserAgent;
            }

            try
            {
                var settings = this.GetSystemUserAgentSettings();
                var assemlyVersion = this.GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
                defaultUserAgent =
                    $"Minio/{assemlyVersion} ({settings.ModelArch};{settings.ModelDescription}) {settings.Platform}/{settings.AppVersion}";
            }
            catch (Exception ex)
            {
                if (this.Trace)
                {
                    this.LogProvider.Trace(ex.Message);
                }

                defaultUserAgent = this.GetPlatformUserAgent();
            }

            return defaultUserAgent;
        }

        /// <summary>
        ///     Constructs a RestRequest. For AWS, this function has the side-effect of overriding the baseUrl
        ///     in the RestClient with region specific host path or virtual style path.
        /// </summary>
        /// <param name="method">HTTP method</param>
        /// <param name="bucketName">Bucket Name</param>
        /// <param name="objectName">Object Name</param>
        /// <param name="headerMap">headerMap</param>
        /// <param name="contentType">Content Type</param>
        /// <param name="body">request body</param>
        /// <param name="resourcePath">query string</param>
        /// <param name="region">region</param>
        /// <returns>A RestRequest</returns>
        private async Task<RestRequest> CreateRequest(Method method, string bucketName, string objectName = null,
            Dictionary<string, string> headerMap = null,
            string contentType = "application/octet-stream",
            object body = null, string resourcePath = null, string region = null)
        {
            // Validate bucket name and object name
            if (bucketName == null && objectName == null)
            {
                throw new InvalidBucketNameException(null, "null bucket name for object '" + null + "'");
            }
            Utils.ValidateBucketName(bucketName);
            if (objectName != null)
            {
                Utils.ValidateObjectName(objectName);
            }

            // Fetch correct region for bucket
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
            var resource = "";
            var usePathStyle = false;
            if (S3Utils.IsAmazonEndPoint(this.BaseUrl))
            {
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
                else if (bucketName != null && bucketName.Contains(".") && this.Secure)
                {
                    // use path style where '.' in bucketName causes SSL certificate validation error
                    usePathStyle = true;
                }

                if (usePathStyle)
                {
                    resource += Utils.UrlEncode(bucketName) + "/";
                }
            }
            else
            {
                resource += Utils.UrlEncode(bucketName) + "/";
            }

            // Set Target URL
            var requestUrl = RequestUtil.MakeTargetUrl(this.BaseUrl, this.Secure, bucketName, region, usePathStyle);
            this.SetTargetUrl(requestUrl);

            if (objectName != null)
            {
                resource += Utils.EncodePath(objectName);
            }

            // Append query string passed in 
            if (resourcePath != null)
            {
                resource += resourcePath;
            }

            var request = new RestRequest(resource, method);

            if (body != null)
            {
                request.AddParameter(contentType, body, ParameterType.RequestBody);
            }

            if (headerMap == null)
            {
                return request;
            }

            foreach (var entry in headerMap)
            {
                request.AddHeader(entry.Key, entry.Value);
            }

            return request;
        }

        protected abstract CryptoProvider CreateCryptoProvider();

        protected abstract LogProvider CreateLogProvider();

        /// <summary>
        ///     This method initializes a new RESTClient. The host URI for Amazon is set to virtual hosted style
        ///     if usePathStyle is false. Otherwise path style URL is constructed.
        /// </summary>
        private void InitClient(MinioSettings minioSettings)
        {
            if (string.IsNullOrEmpty(this.BaseUrl))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }

            this.CryptoProvider = this.CreateCryptoProvider();
            this.LogProvider = this.CreateLogProvider();

            var host = this.BaseUrl;

            var scheme = this.Secure ? Utils.UrlEncode("https") : Utils.UrlEncode("http");

            // This is the actual url pointed to for all HTTP requests
            this.Endpoint = $"{scheme}://{host}";
            this.uri = RequestUtil.GetEndpointUrl(this.BaseUrl, this.Secure);
            RequestUtil.ValidateEndpoint(this.uri, this.Endpoint);

            // Initialize a new REST client. This uri will be modified if region specific endpoint/virtual style request
            // is decided upon while constructing a request for Amazon.
            var userAgent = this.customUserAgent ?? this.GetDefaultUserAgent();
            this.restClient = new RestClient(this.uri, minioSettings.CreateHttpClientHandlerFunc)
            {
                UserAgent = userAgent,
                IgnoreResponseStatusCode = true
            };
            this.authenticator = new V4Authenticator(this, this.Secure, this.AccessKey, this.SecretKey);
            this.restClient.Authenticator = this.authenticator;
        }

        /// <summary>
        ///     Actual doer that executes the REST request to the server
        /// </summary>
        /// <param name="errorHandlers">List of handlers to override default handling</param>
        /// <param name="request">request</param>
        /// <param name="cancellationToken"></param>
        /// <returns>IRESTResponse</returns>
        internal async Task<IRestResponse> ExecuteTaskAsync(IEnumerable<ApiResponseErrorHandlingDelegate> errorHandlers,
            IRestRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            var startTime = DateTime.Now;
            // Logs full url when HTTPtracing is enabled.
            if (this.Trace)
            {
                var fullUrl = this.restClient.BuildUri(request);
                this.LogProvider.Trace($"Full URL of Request {fullUrl}");
            }

            var response = await this.restClient.Execute(request, cancellationToken);
            this.HandleIfErrorResponse(response, errorHandlers, startTime);

            return response;
        }


        /// <summary>
        ///     Parse response errors if any and return relevant error messages
        /// </summary>
        /// <param name="response"></param>
        private static void ParseError(IRestResponse response)
        {
            if (response == null)
            {
                throw new ConnectionException(
                    "Response is nil. Please report this issue https://github.com/minio/minio-dotnet/issues");
            }
            if (HttpStatusCode.Redirect.Equals(response.StatusCode) ||
                HttpStatusCode.TemporaryRedirect.Equals(response.StatusCode) ||
                HttpStatusCode.MovedPermanently.Equals(response.StatusCode))
            {
                throw new RedirectionException(
                    "Redirection detected. Please report this issue https://github.com/minio/minio-dotnet/issues");
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                var errorResponse = new ErrorResponse();

                if (HttpStatusCode.Forbidden.Equals(response.StatusCode) ||
                    HttpStatusCode.NotFound.Equals(response.StatusCode) ||
                    HttpStatusCode.MethodNotAllowed.Equals(response.StatusCode) ||
                    HttpStatusCode.NotImplemented.Equals(response.StatusCode))
                {
                    MinioException e = null;

                    foreach (var parameter in response.Headers)
                    {
                        if (parameter.Key.Equals("x-amz-id-2", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.HostId = parameter.Value.ToString();
                        }
                        if (parameter.Key.Equals("x-amz-request-id", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.RequestId = parameter.Value.ToString();
                        }
                        if (parameter.Key.Equals("x-amz-bucket-region", StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorResponse.BucketRegion = parameter.Value.ToString();
                        }
                    }

                    errorResponse.Resource = response.Request.Resource;

                    if (HttpStatusCode.NotFound.Equals(response.StatusCode))
                    {
                        var pathLength = response.Request.Resource.Split('/').Length;
                        var isAws = response.ResponseUri.Host.EndsWith("s3.amazonaws.com");
                        var isVirtual = isAws && !response.ResponseUri.Host.StartsWith("s3.amazonaws.com");

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
                            var resource = response.Request.Resource.Split('/')[0];

                            if (isAws && isVirtual && response.Request.Resource != "")
                            {
                                errorResponse.Code = "NoSuchKey";
                                e = new ObjectNotFoundException(resource, "Not found.");
                            }
                            else
                            {
                                errorResponse.Code = "NoSuchBucket";
                                BucketRegionCache.Instance.Remove(resource);
                                e = new BucketNotFoundException(resource, "Not found.");
                            }
                        }
                        else
                        {
                            e = new InternalClientException(
                                "404 without body resulted in path with less than two components");
                        }
                    }
                    else if (HttpStatusCode.Forbidden.Equals(response.StatusCode))
                    {
                        errorResponse.Code = "Forbidden";
                        e = new AccessDeniedException("Access denied on the resource: " + response.Request.Resource);
                    }
                    if (e == null)
                    {
                        throw new InternalClientException("Unsuccessful response from server without XML error: " +
                                                          response.StatusDescription);
                    }

                    e.Response = errorResponse;
                    throw e;
                }
                throw new InternalClientException("Unsuccessful response from server without XML error: " +
                                                  response.StatusDescription);
            }

            if (response.StatusCode.Equals(HttpStatusCode.NotFound) && response.Request.Resource.EndsWith("?location")
                && response.Request.Method.Equals(Method.GET))
            {
                var bucketName = response.Request.Resource.Split('?')[0];
                BucketRegionCache.Instance.Remove(bucketName);
                throw new BucketNotFoundException(bucketName, "Not found.");
            }

            var contentBytes = Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            var errResponse = (ErrorResponse) new XmlSerializer(typeof(ErrorResponse)).Deserialize(stream);

            // Handle XML response for Bucket Policy not found case
            if (response.StatusCode.Equals(HttpStatusCode.NotFound) && response.Request.Resource.EndsWith("?policy")
                && response.Request.Method.Equals(Method.GET) && errResponse.Code.Equals("NoSuchBucketPolicy"))
            {
                var errorException =
                    new ErrorResponseException(errResponse.Message, errResponse.Code)
                    {
                        Response = errResponse,
                        XmlError = response.Content
                    };
                throw errorException;
            }

            var minioException = new MinioException(errResponse.Message)
            {
                Response = errResponse,
                XmlError = response.Content
            };

            throw minioException;
        }

        /// <summary>
        ///     Delegate errors to handlers
        /// </summary>
        /// <param name="response"></param>
        /// <param name="handlers"></param>
        /// <param name="startTime">start time</param>
        private void HandleIfErrorResponse(IRestResponse response,
            IEnumerable<ApiResponseErrorHandlingDelegate> handlers, DateTime startTime)
        {
            // Logs Response if HTTP tracing is enabled
            if (this.Trace)
            {
                var now = DateTime.Now;
                this.LogRequest(response.Request, response, (now - startTime).TotalMilliseconds);
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
            // Fall back default error handler
            this.defaultErrorHandlingDelegate(response);
        }

        /// <summary>
        ///     Logs the request sent to server and corresponding response
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
                    value = parameter.Type != ParameterType.RequestBody || parameter.ContentType != null &&
                            (parameter.ContentType.Contains("json") || parameter.ContentType.Contains("text"))
                        ? parameter.Value
                        : "rawContent",
                    type = parameter.Type.ToString()
                }),
                // ToString() here to have the method as a nice string otherwise it will just show the enum value
                method = request.Method.ToString(),
                // This will generate the actual Uri used in the request
                uri = this.restClient.BuildUri(request)
            };

            var responseToLog = new
            {
                statusCode = response.StatusCode,
                content = response.Content,
                headers = response.Headers,
                // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
                responseUri = response.ResponseUri,
                errorMessage = response.StatusDescription
            };

            this.LogProvider.Trace(
                $"Request completed in {durationMs} ms, Request: {JsonConvert.SerializeObject(requestToLog, Formatting.Indented)}, Response: {JsonConvert.SerializeObject(responseToLog, Formatting.Indented)}");
        }

        protected class SystemUserAgentSettings
        {
            /// <summary>
            ///     SIMULATOR, DEVICE
            /// </summary>
            public string ModelArch { get; set; }

            /// <summary>
            ///     iPhone5s, Samsung S7
            /// </summary>
            public string ModelDescription { get; set; }

            /// <summary>
            ///     iOS, Android, UWP, NET
            /// </summary>
            public string Platform { get; set; }

            /// <summary>
            ///     App version
            /// </summary>
            public string AppVersion { get; set; }
        }
    }

    internal delegate void ApiResponseErrorHandlingDelegate(IRestResponse response);
}