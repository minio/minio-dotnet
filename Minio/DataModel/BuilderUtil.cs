/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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

namespace Minio
{
    public class BuilderUtil
    {
        public static bool IsAwsDualStackEndpoint(string endpoint)
        {
            return endpoint.ToLower().Contains(".dualstack.");
        }
        public static bool IsAwsAccelerateEndpoint(string endpoint)
        {
            return endpoint.ToLower().StartsWith("s3-accelerate.");
        }
        public static bool IsAwsEndpoint(string endpoint)
        {
            return (endpoint.ToLower().StartsWith("s3.") ||
                            IsAwsAccelerateEndpoint(endpoint)) &&
                   (endpoint.ToLower().EndsWith(".amazonaws.com") ||
                    endpoint.ToLower().EndsWith(".amazonaws.com.cn"));
        }

        public static bool IsChineseDomain(string host)
        {
            return host.ToLower().EndsWith(".cn");
        }

        public static string ExtractRegion(string endpoint)
        {
            string[] tokens = endpoint.Split('.');
            if (tokens.Length < 2)
                return null;
            string token = tokens[1];

            // If token is "dualstack", then region might be in next token.
            if (token.Equals("dualstack") && tokens.Length >= 3)
                token = tokens[2];

            // If token is equal to "amazonaws", region is not passed in the endpoint.
            if (token.Equals("amazonaws"))
                return null;

            // Return token as region.
            return token;
        }

        private static bool IsValidSmallInt(string val)
        {
            byte tempByte = 0;
            return Byte.TryParse(val, out tempByte);
        }

        private static bool isValidOctetVal(string val)
        {
            const byte uLimit = (byte)255;
            return (Byte.Parse(val) <= uLimit);
        }
        private static bool IsValidIPv4(string ip)
        {
            int posColon = ip.LastIndexOf(':');
            if (posColon != -1)
            {
                ip = ip.Substring(0, posColon);
            }
            string[] octetsStr = ip.Split('.');
            if (octetsStr.Length != 4)
            {
                return false;
            }
            bool isValidSmallInt = Array.TrueForAll(octetsStr, IsValidSmallInt);
            if (!isValidSmallInt)
            {
                return false;
            }
            bool isValidOctet = Array.TrueForAll(octetsStr, isValidOctetVal);
            return isValidOctet;
        }

        private static bool IsValidIP(string host)
        {
            IPAddress temp;
            return IPAddress.TryParse(host, out temp);
        }

        public static bool IsValidHostnameOrIPAddress(string host)
        {
            // Let's do IP address check first.
            if (String.IsNullOrWhiteSpace(host))
            {
                return false;
            }
            // IPv4 first
            if (IsValidIPv4(host))
            {
                return true;
            }
            // IPv6 or other IP address format
            if (IsValidIP(host))
            {
                return true;
            }
            // Remove any port in endpoint, in such a case.
            int posColon = host.LastIndexOf(':');
            int port = -1;
            if (posColon != -1)
            {
                try
                {
                    port = Int32.Parse(host.Substring(posColon + 1, (host.Length - posColon - 1)));
                }
                catch (System.FormatException)
                {
                    return false;
                }
                host = host.Substring(0, posColon);
            }

            // Check host if it is a hostname.
            if (Uri.CheckHostName(host).ToString().ToLower().Equals("dns"))
            {
                return true;
            }
            return false;
        }
    }
}