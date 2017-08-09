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

namespace Minio.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Exceptions;

    internal static class Utils

    {
        // We support '.' with bucket names but we fallback to using path
        // style requests instead for such buckets.
        private static readonly Regex ValidBucketName = new Regex("^[a-z0-9][a-z0-9\\.\\-]{1,61}[a-z0-9]$");

        // Invalid bucket name with double dot.
        private static readonly Regex InvalidDotBucketName = new Regex("`/./.");

        /// <summary>
        ///     isValidBucketName - verify bucket name in accordance with
        ///     - http://docs.aws.amazon.com/AmazonS3/latest/dev/UsingBucket.html
        /// </summary>
        /// <param name="bucketName">Bucket to test existence of</param>
        internal static void ValidateBucketName(string bucketName)
        {
            if (bucketName.Trim() == "")
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name cannot be empty.");
            }
            if (bucketName.Length < 3)
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name cannot be smaller than 3 characters.");
            }
            if (bucketName.Length > 63)
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name cannot be greater than 63 characters.");
            }
            if (bucketName[0] == '.' || bucketName[bucketName.Length - 1] == '.')
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name cannot start or end with a '.' dot.");
            }
            if (bucketName.ToCharArray().Any(char.IsUpper))
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name cannot have upper case characters");
            }
            if (InvalidDotBucketName.IsMatch(bucketName))
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name cannot have successive periods.");
            }
            if (!ValidBucketName.IsMatch(bucketName))
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name contains invalid characters.");
            }
        }

        // isValidObjectName - verify object name in accordance with
        //   - http://docs.aws.amazon.com/AmazonS3/latest/dev/UsingMetadata.html
        internal static void ValidateObjectName(string objectName)
        {
            if (objectName.Trim() == "")
            {
                throw new InvalidObjectNameException(objectName, "Object name cannot be empty.");
            }

            // c# strings are in utf16 format. they are already in unicode format when they arrive here.
            if (objectName.Length > 512)
            {
                throw new InvalidObjectNameException(objectName, "Object name cannot be greater than 1024 characters.");
            }
        }

        internal static void ValidateObjectPrefix(string objectPrefix)
        {
            if (objectPrefix.Length > 512)
            {
                throw new InvalidObjectPrefixException(objectPrefix,
                    "Object prefix cannot be greater than 1024 characters.");
            }
        }

        // Return url encoded string where reserved characters have been percent-encoded
        internal static string UrlEncode(string input)
        {
            return Uri.EscapeDataString(input).Replace("\\!", "%21")
                .Replace("\\$", "%24")
                .Replace("\\&", "%26")
                .Replace("\\'", "%27")
                .Replace("\\(", "%28")
                .Replace("\\)", "%29")
                .Replace("\\*", "%2A")
                .Replace("\\+", "%2B")
                .Replace("\\,", "%2C")
                .Replace("\\/", "%2F")
                .Replace("\\:", "%3A")
                .Replace("\\;", "%3B")
                .Replace("\\=", "%3D")
                .Replace("\\@", "%40")
                .Replace("\\[", "%5B")
                .Replace("\\]", "%5D");
        }

        // Return encoded path where extra "/" are trimmed off.
        internal static string EncodePath(string path)
        {
            var encodedPathBuf = new StringBuilder();
            foreach (var pathSegment in path.Split('/'))
            {
                if (pathSegment.Length != 0)
                {
                    if (encodedPathBuf.Length > 0)
                    {
                        encodedPathBuf.Append("/");
                    }
                    encodedPathBuf.Append(UrlEncode(pathSegment));
                }
            }

            if (path.StartsWith("/"))
            {
                encodedPathBuf.Insert(0, "/");
            }
            if (path.EndsWith("/"))
            {
                encodedPathBuf.Append("/");
            }
            return encodedPathBuf.ToString();
        }

        internal static bool IsAnonymousClient(string accessKey, string secretKey)
        {
            return secretKey == "" || accessKey == "";
        }

        internal static bool IsSupersetOf(IList<string> l1, IList<string> l2)

        {
            if (l2 == null)
            {
                return true;
            }
            if (l1 == null)
            {
                return false;
            }
            return !l2.Except(l1).Any();
        }

        public static bool CaseInsensitiveContains(string text, string value,
            StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }

        /// <summary>
        ///     Calculate part size and number of parts required.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static object CalculateMultiPartSize(long size)
        {
            if (size == -1)
            {
                size = Constants.MaximumStreamObjectSize;
            }
            if (size > Constants.MaxMultipartPutObjectSize)
            {
                throw new EntityTooLargeException("Your proposed upload size " + size +
                                                  " exceeds the maximum allowed object size " +
                                                  Constants.MaxMultipartPutObjectSize);
            }
            var partSize = (double) Math.Ceiling((decimal) size / Constants.MaxParts);
            partSize = (double) Math.Ceiling((decimal) partSize / Constants.MinimumPartSize) *
                       Constants.MinimumPartSize;
            var partCount = Math.Ceiling(size / partSize);
            var lastPartSize = size - (partCount - 1) * partSize;
            dynamic obj = new ExpandoObject();
            obj.partSize = partSize;
            obj.partCount = partCount;
            obj.lastPartSize = lastPartSize;
            return obj;
        }
    }
}