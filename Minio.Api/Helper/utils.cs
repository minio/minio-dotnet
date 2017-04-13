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
using System.Linq;
using System.Text.RegularExpressions;
using Minio.Exceptions;
using System.IO;
using Microsoft.Win32;

namespace Minio
{
    class utils
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
                throw new InvalidBucketNameException(bucketName,"Bucket name cannot be empty.");
           }
           if (bucketName.Length < 3)
           {
                throw new InvalidBucketNameException(bucketName,"Bucket name cannot be smaller than 3 characters.");
           }
           if (bucketName.Length > 63)
           {
               throw new InvalidBucketNameException(bucketName,"Bucket name cannot be greater than 63 characters.");
           }
           if (bucketName[0] == '.' || bucketName[bucketName.Length - 1] == '.')
           {
                throw new InvalidBucketNameException(bucketName,"Bucket name cannot start or end with a '.' dot.");
           }
           if (bucketName.Any(c => char.IsUpper(c)))
           {
                throw new InvalidBucketNameException(bucketName, "Bucket name cannot have upper case characters");
           }
           if (invalidDotBucketName.IsMatch(bucketName))
           {
                throw new InvalidBucketNameException(bucketName,"Bucket name cannot have successive periods.");
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
           if (objectName.Length > 1024)
            {
                throw new InvalidObjectNameException(objectName, "Object name cannot be greater than 1024 characters.");
            }
            //c# strings are in utf16 format. they are already in unicode format when they arrive here.
            // if !utf8.ValidString(objectName) 
            //     return ErrInvalidBucketName("Object name with non UTF-8 strings are not supported.")

            return;
        }
        internal static void validateObjectPrefix(string objectPrefix)
        {
            if (objectPrefix.Length > 1024)
            {
                throw new InvalidObjectPrefixException(objectPrefix, "Object prefix cannot be greater than 1024 characters.");
            }
            return;
        }

        internal static string UrlEncode(string input)
        {
            return Uri.EscapeDataString(input).Replace("%2F", "/");
        }

        internal static bool isAnonymousClient(string accessKey, string secretKey)
        {
            return (secretKey == "" || accessKey == "");
        }
        internal static void ValidateFile(string filePath,string contentType=null)
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
    }
}
 
 
