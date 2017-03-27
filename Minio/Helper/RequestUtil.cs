
using Minio.Exceptions;
using Minio.Helper;
using System;
using System.Text.RegularExpressions;

namespace Minio
{
    public class RequestUtil
    {
        public static Uri getEndpointURL(string endPoint, bool secure)
        {
            if (endPoint.Contains(":"))
            {
                string[] parts = endPoint.Split(':');
                string host = parts[0];
                string port = (parts.Length > 1) ?  parts[1] : null; 
                if (!s3utils.IsValidIP(host) && !s3utils.IsValidDomain(host))
                {
                    throw new InvalidEndpointException("Endpoint: " + endPoint + " does not follow ip address or domain name standards.");
                }

            } 
            else
            {
                if (!s3utils.IsValidIP(endPoint) && !s3utils.IsValidDomain(endPoint))
                {
                    throw new InvalidEndpointException("Endpoint: " + endPoint + " does not follow ip address or domain name standards.");
                }
            }

            Uri uri = TryCreateUri(endPoint, secure);
            RequestUtil.ValidateEndpoint(uri,endPoint);
            return uri;

        }
     

        public static Uri MakeTargetURL(string endPoint, bool secure, string bucketName = null, string region = null, bool usePathStyle = true)
        {
            // For Amazon S3 endpoint, try to fetch location based endpoint.
            string host = endPoint;
            if (s3utils.IsAmazonEndPoint(endPoint))
            {
                // Fetch new host based on the bucket location.
                host = AWSS3Endpoints.Instance.endpoint(region);
                if (!usePathStyle)
                {
                    host = utils.UrlEncode(bucketName) + "." + utils.UrlEncode(host) + "/";
                }
            }
            Uri uri = TryCreateUri(host,secure);
            return uri;
        }

        public static Uri TryCreateUri(string endpoint,bool secure)
        {
            var scheme = secure ? utils.UrlEncode("https") : utils.UrlEncode("http");

            // This is the actual url pointed to for all HTTP requests
            string endpointURL = string.Format("{0}://{1}", scheme, endpoint);
            Uri uri = null;
            try
            {
                uri = new Uri(endpointURL);
            }
            catch (UriFormatException e)
            {
                throw new InvalidEndpointException(e.Message);
            }
            return uri;
        }

        /// <summary>
        /// Validates URI to check if it is well formed. Otherwise cry foul.
        /// </summary>
        public static void ValidateEndpoint(Uri uri,string Endpoint)
        {
            if (string.IsNullOrEmpty(uri.OriginalString))
            {
                throw new InvalidEndpointException("Endpoint cannot be empty.");
            }
            string host = uri.Host;

            if (!isValidEndpoint(uri.Host))
            {
                throw new InvalidEndpointException(Endpoint, "Invalid endpoint.");
            }
            if (!uri.AbsolutePath.Equals("/", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new InvalidEndpointException(Endpoint, "No path allowed in endpoint.");
            }

            if (!string.IsNullOrEmpty(uri.Query))
            {
                throw new InvalidEndpointException(Endpoint, "No query parameter allowed in endpoint.");
            }
            if ((!uri.Scheme.ToLowerInvariant().Equals("https")) && (!uri.Scheme.ToLowerInvariant().Equals("http")))
            //kp if (!(this.uri.Scheme.Equals(Uri.UriSchemeHttp) || this.uri.Scheme.Equals(Uri.UriSchemeHttps)))
            {
                throw new InvalidEndpointException(Endpoint, "Invalid scheme detected in endpoint.");
            }
            string amzHost = uri.Host;
            if ((utils.CaseInsensitiveContains(amzHost,".amazonaws.com"))
                 && !s3utils.IsAmazonEndPoint(uri.Host))
            {
                throw new InvalidEndpointException(amzHost, "For Amazon S3, host should be \'s3.amazonaws.com\' in endpoint.");
            }
        }
       
        /// <summary>
        /// Validate Url endpoint 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns>true/false</returns>
        public static bool isValidEndpoint(string endpoint)
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
      
    }

}
