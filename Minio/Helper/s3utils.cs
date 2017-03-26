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
using System.Net;
using System.IO;

namespace Minio.Helper
{
    class s3utils
    {

        internal static bool IsAmazonEndPoint(string endpoint)
        {
            if (IsAmazonChinaEndPoint(endpoint))
            {
                return true;
            }
            return endpoint == "s3.amazonaws.com";
        }
        // IsAmazonChinaEndpoint - Match if it is exactly Amazon S3 China endpoint.
        // Customers who wish to use the new Beijing Region are required
        // to sign up for a separate set of account credentials unique to
        // the China (Beijing) Region. Customers with existing AWS credentials
        // will not be able to access resources in the new Region, and vice versa.
        // For more info https://aws.amazon.com/about-aws/whats-new/2013/12/18/announcing-the-aws-china-beijing-region/
        internal static bool IsAmazonChinaEndPoint(string endpoint)
        {

            return endpoint == "s3.cn-north-1.amazonaws.com.cn";
        }
        // IsGoogleEndpoint - Match if it is exactly Google cloud storage endpoint.
        internal static bool IsGoogleEndPoint(string endpoint)
        {
            return endpoint == "storage.googleapis.com";
        }
     

        // IsVirtualHostSupported - verifies if bucketName can be part of
        // virtual host. Currently only Amazon S3 and Google Cloud Storage
        // would support this.
        internal static bool IsVirtualHostSupported(Uri endpointURL,string bucketName)
        {
            if (endpointURL == null)
            {
                return false;
            }
            // bucketName can be valid but '.' in the hostname will fail SSL
            // certificate validation. So do not use host-style for such buckets.
            if (endpointURL.Scheme == "https" && bucketName.Contains("."))
            {
                return false;
            }
            // Return true for all other cases
            return IsAmazonEndPoint(endpointURL.Host) || IsGoogleEndPoint(endpointURL.Host);
        }
        internal static string GetPath(string p1, string p2)
        {
            try
            {
                string combination = Path.Combine(p1, p2);
                combination = Uri.EscapeUriString(combination);
                return combination;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }

        }

        // IsValidDomain validates if input string is a valid domain name.
        internal static bool IsValidDomain(string host)
        {
            // See RFC 1035, RFC 3696.
            host = host.Trim();
	        if ((host.Length == 0) || (host.Length > 255)) 
            {
                return false;
	        }
	        // host cannot start or end with "-"
	        if ((host.Substring(host.Length - 1) == "-") || (host.Substring(1) == "-"))
            {
                return false;
	        }
	        // host cannot start or end with "_"
	        if ((host.Substring(host.Length - 1) == "_") || (host.Substring(1) == "_"))
            {
                return false;
	        }
	        // host cannot start or end with a "."
	        if(( host.Substring(host.Length - 1) == ".") || (host.Substring(1) == "."))
            {
                return false;
	        }
            // All non alphanumeric characters are invalid.
            char[] nonAlphas = "`~!@#$%^&*()+={}[]|\\\"';:><?/'".ToCharArray();

            if (host.IndexOfAny(nonAlphas) > 0)
            {
                return false;
	        }
            // No need to regexp match, since the list is non-exhaustive.
            // We let it valid and fail later.
            return true;
        }

        // IsValidIP parses input string for ip address validity.
        internal static bool IsValidIP(string ip) {
            IPAddress result = null;
            return
                !String.IsNullOrEmpty(ip) &&
                IPAddress.TryParse(ip, out result);
        }
     
    }
}
