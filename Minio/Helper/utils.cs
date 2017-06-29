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
using System.Text.RegularExpressions;
using Minio.Exceptions;
using System.IO;
using Microsoft.Win32;
using Minio.Helper;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Minio
{
    internal class utils

    {
        // We support '.' with bucket names but we fallback to using path
        // style requests instead for such buckets.
        static Regex validBucketName = new Regex("^[a-z0-9][a-z0-9\\.\\-]{1,61}[a-z0-9]$");

        // Invalid bucket name with double dot.
        static Regex invalidDotBucketName = new Regex("`/./.");

        /// <summary>
        /// isValidBucketName - verify bucket name in accordance with
        ///  - http://docs.aws.amazon.com/AmazonS3/latest/dev/UsingBucket.html
        /// </summary>
        /// <param name="bucketName">Bucket to test existence of</param>
        internal static void validateBucketName(string bucketName)
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
            if (bucketName.Any(c => char.IsUpper(c)))
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name cannot have upper case characters");
            }
            if (invalidDotBucketName.IsMatch(bucketName))
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name cannot have successive periods.");
            }
            if (!validBucketName.IsMatch(bucketName))
            {
                throw new InvalidBucketNameException(bucketName, "Bucket name contains invalid characters.");
            }
        }
        // isValidObjectName - verify object name in accordance with
        //   - http://docs.aws.amazon.com/AmazonS3/latest/dev/UsingMetadata.html
        internal static void validateObjectName(String objectName)
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
            return;
        }

        internal static void validateObjectPrefix(string objectPrefix)
        {
            if (objectPrefix.Length > 512)
            {
                throw new InvalidObjectPrefixException(objectPrefix, "Object prefix cannot be greater than 1024 characters.");
            }
            return;
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
            StringBuilder encodedPathBuf = new StringBuilder();
            foreach (string pathSegment in path.Split('/'))
            {
                if (pathSegment.Length != 0)
                {
                    if (encodedPathBuf.Length > 0)
                    {
                        encodedPathBuf.Append("/");
                    }
                    encodedPathBuf.Append(utils.UrlEncode(pathSegment));
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

        internal static bool isAnonymousClient(string accessKey, string secretKey)
        {
            return (secretKey == "" || accessKey == "");
        }

        internal static void ValidateFile(string filePath, string contentType = null)
        {
            if (filePath == null || filePath == "")
            {
                throw new ArgumentException("empty file name is not allowed");
            }

            string fileName = Path.GetFileName(filePath);
            bool fileExists = File.Exists(filePath);
            if (fileExists)
            {
                FileAttributes attr = File.GetAttributes(filePath);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    throw new ArgumentException("'" + fileName + "': not a regular file");
                }
            }

            if (contentType == null)
            {
                contentType = GetContentType(filePath);
            }

        }

        internal static string GetContentType(string fileName)
        {
            // set a default mimetype if not found.
            string contentType = "application/octet-stream";

            try
            {
                // get the registry classes root
                RegistryKey classes = Registry.ClassesRoot;

                // find the sub key based on the file extension
                RegistryKey fileClass = classes.OpenSubKey(Path.GetExtension(fileName));
                contentType = fileClass.GetValue("Content Type").ToString();
            }
            catch { }

            return contentType;
        }
        public static void MoveWithReplace(string sourceFileName, string destFileName)
        {

            //first, delete target file if exists, as File.Move() does not support overwrite
            if (File.Exists(destFileName))
            {
                File.Delete(destFileName);
            }

            File.Move(sourceFileName, destFileName);
        }

        internal static bool isSupersetOf(IList<string> l1, IList<string> l2)

        {
            if (l2 == null)
            {
                return true;
            }
            if (l1 == null)
            {
                return false;
            }
            return (!l2.Except(l1).Any());
        }
        public static bool CaseInsensitiveContains(string text, string value,
    StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }

        /// <summary>
        /// Calculate part size and number of parts required.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Object CalculateMultiPartSize(long size)
        {
            if (size == -1)
            {
                size = Constants.MaximumStreamObjectSize;
            }
            if (size > Constants.MaxMultipartPutObjectSize)
            {
                throw new EntityTooLargeException("Your proposed upload size " + size + " exceeds the maximum allowed object size " + Constants.MaxMultipartPutObjectSize);
            }
            double partSize = (double)Math.Ceiling((decimal)size / Constants.MaxParts);
            partSize = (double)Math.Ceiling((decimal)partSize / Constants.MinimumPartSize) * Constants.MinimumPartSize;
            double partCount = (double)Math.Ceiling(size / partSize);
            double lastPartSize = size - (partCount - 1) * partSize;
            dynamic obj = new ExpandoObject();
            obj.partSize = partSize;
            obj.partCount = partCount;
            obj.lastPartSize = lastPartSize;
            return obj;
        }
    }
}