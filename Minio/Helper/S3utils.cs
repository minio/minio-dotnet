﻿/*
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

namespace Minio.Helper
{
    using System;
    using System.IO;
    using System.Linq;

    internal static class S3Utils
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

        // IsVirtualHostSupported - verifies if bucketName can be part of
        // virtual host. Currently only Amazon S3 and Google Cloud Storage
        // would support this.
        internal static bool IsVirtualHostSupported(Uri endpointUrl, string bucketName)
        {
            if (endpointUrl == null)
            {
                return false;
            }
            // bucketName can be valid but '.' in the hostname will fail SSL
            // certificate validation. So do not use host-style for such buckets.
            if (endpointUrl.Scheme == "https" && bucketName.Contains("."))
            {
                return false;
            }
            // Return true for all other cases
            return IsAmazonEndPoint(endpointUrl.Host);
        }

        internal static string GetPath(string p1, string p2)
        {
            try
            {
                var combination = Path.Combine(p1, p2);
                combination = Uri.EscapeUriString(combination);
                return combination;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        // IsValidIP parses input string for ip address validity.
        internal static bool IsValidIp(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                return false;
            }

            var splitValues = ip.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }
    }
}