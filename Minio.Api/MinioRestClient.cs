using System;
using System.Collections.Generic;
using Minio.Api.Exceptions;
using System.Text.RegularExpressions;
using RestSharp;
using System.Net;
using System.Linq;
using System.Text;
using RestSharp.Extensions;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Minio.Api
{
    public sealed class MinioRestClient : IMinioClient
    {
        public string AccessKey { get; private set; }
        public string SecretKey { get; private set; }
        public string Endpoint { get; private set; }
        public string BaseUrl { get; private set; }
        public bool Secure { get; private set; }

        private Uri uri;
        private RestClient client;
        private V4Authenticator authenticator;

        public IBucketOperations Buckets { get; }

        internal readonly IEnumerable<ApiResponseErrorHandlingDelegate> NoErrorHandlers = Enumerable.Empty<ApiResponseErrorHandlingDelegate>();

        private readonly ApiResponseErrorHandlingDelegate _defaultErrorHandlingDelegate = (response) =>
        {
            if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.BadRequest)
            {
                throw new MinioApiException(response);
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
        private static string CustomUserAgent = "";
        private string FullUserAgent
        {
            get
            {
                return SystemUserAgent + " " + CustomUserAgent;
            }

        }

        internal UriBuilder GetUriBuilder(string methodPath)
        {
            var uripath = new UriBuilder(this.Endpoint);
            uripath.Path += methodPath;
            return uripath;
        }
        private void _constructUri()
        {
            
            var scheme = this.Secure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            this.Endpoint = string.Format("{0}://{1}", scheme, this.BaseUrl);
            if (string.IsNullOrEmpty(this.Endpoint))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }

            this.uri = new Uri(this.Endpoint);

            this._validateUri();
          
        }
       
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
           
            this.client.UserAgent = this.FullUserAgent;
        }
        public MinioRestClient(string endpoint,string accessKey="", string secretKey="")
        {
            
            this.Secure = false;
            this.BaseUrl = endpoint;
            _constructUri();
            client = new RestSharp.RestClient(this.uri);
            client.UserAgent = this.FullUserAgent;
           
            authenticator = new V4Authenticator(accessKey, secretKey);
            client.Authenticator = authenticator;

            this.Buckets = new BucketOperations(this);
            return;

        }
        public MinioRestClient WithSSL()
        {
            this.Secure = true;
            _constructUri();
            this.client.BaseUrl = this.uri;
            return this;
        }

        internal async Task<IRestResponse<T>> ExecuteTaskAsync<T>(IEnumerable<ApiResponseErrorHandlingDelegate> errorHandlers,IRestRequest request) where T: new()
        {
            var response = await this.client.ExecuteTaskAsync<T>(request, CancellationToken.None);
            HandleIfErrorResponse(response, errorHandlers);
            return response;
        }
        internal  async Task<IRestResponse> ExecuteTaskAsync(IEnumerable<ApiResponseErrorHandlingDelegate> errorHandlers,IRestRequest request)
        {
            var response = await this.client.ExecuteTaskAsync(request, CancellationToken.None);
            HandleIfErrorResponse(response, errorHandlers);
            return response;
        }
        //old
        public void ExecuteAsync<T>(IRestRequest request, Action<T> callback) where T : new()
        {
            request.OnBeforeDeserialization = (resp) =>
            {
                // for individual resources when there's an error to make
                // sure that RestException props are populated
                if (((int)resp.StatusCode) >= 400)
                {
                    // have to read the bytes so .Content doesn't get populated
                    var restException = "{{ \"RestException\" : {0} }}";
                    var content = resp.RawBytes.AsString(); //get the response content
                    var newJson = string.Format(restException, content);

                    resp.Content = null;
                    resp.RawBytes = Encoding.UTF8.GetBytes(newJson.ToString());
                }
            };

            request.DateFormat = "ddd, dd MMM yyyy HH:mm:ss '+0000'";

            this.client.ExecuteAsync<T>(request, (response) => callback(response.Data));
        }
        ///old 
        /// <summary>
        /// Execute a manual REST request
        /// </summary>
        /// <param name="request">The RestRequest to execute (will use client credentials)</param>
        /// <param name="callback">The callback function to execute when the async request completes</param>
        public void ExecuteAsync(IRestRequest request, Action<IRestResponse> callback)
        {
            
            this.client.ExecuteAsync(request, callback);
        }
        /// <summary>
        /// old - remove
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public bool BucketExists(string bucketName)
        {
            var request = new RestRequest(bucketName, Method.HEAD);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            var ex = ParseError(response);
            
            throw ex;
        }
        internal ClientException ParseError(IRestResponse response)
        {
            if (response == null)
            {
                return new ConnectionException("Response is nil. Please report this issue https://github.com/minio/minio-dotnet/issues");
            }
            if (HttpStatusCode.Redirect.Equals(response.StatusCode) || HttpStatusCode.TemporaryRedirect.Equals(response.StatusCode) || HttpStatusCode.MovedPermanently.Equals(response.StatusCode))
            {
                return new RedirectionException("Redirection detected. Please report this issue https://github.com/minio/minio-dotnet/issues");
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
                    return e;
                }
                throw new InternalClientException("Unsuccessful response from server without XML error: " + response.StatusCode);
            }

            var contentBytes = System.Text.Encoding.UTF8.GetBytes(response.Content);
            var stream = new MemoryStream(contentBytes);
            ErrorResponse errResponse = (ErrorResponse)(new XmlSerializer(typeof(ErrorResponse)).Deserialize(stream));

            ClientException clientException = new ClientException(errResponse.Message);
            clientException.Response = errResponse;
            clientException.XmlError = response.Content;
            return clientException;
        }

        private void HandleIfErrorResponse(IRestResponse response, IEnumerable<ApiResponseErrorHandlingDelegate> handlers)
        {
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

    }
    internal delegate void ApiResponseErrorHandlingDelegate(IRestResponse response);

}
