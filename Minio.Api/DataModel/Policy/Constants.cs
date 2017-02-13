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

namespace Minio.Policy
{
    class Constants
    {
        // Resource prefix for all aws resources.
        public static readonly String AWS_RESOURCE_PREFIX = "arn:aws:s3:::";

        // Common bucket actions for both read and write policies.
        public static readonly List<String> COMMON_BUCKET_ACTIONS = new List<String>() { "s3:GetBucketLocation" };

        // Read only bucket actions.
        public static readonly List<String> READ_ONLY_BUCKET_ACTIONS = new List<String>() { "s3:ListBucket"};

        // Write only bucket actions.
        public static readonly List<String> WRITE_ONLY_BUCKET_ACTIONS =
            new List<String>() { "s3:ListBucketMultipartUploads" };

        // Read only object actions.
        public static readonly List<String> READ_ONLY_OBJECT_ACTIONS = new List<String>() { "s3:GetObject" };

        // Write only object actions.
        public static readonly List<String> WRITE_ONLY_OBJECT_ACTIONS =
            new List<String>() { "s3:AbortMultipartUpload",
                                              "s3:DeleteObject",
                                              "s3:ListMultipartUploadParts",
                                              "s3:PutObject" };

        // Read and write object actions.
        public static IList<string> READ_WRITE_OBJECT_ACTIONS()
        {
            IList<string> res = new List<string>();
            res.Union(READ_ONLY_OBJECT_ACTIONS);
            res.Union(WRITE_ONLY_OBJECT_ACTIONS);
            return res;
        }
        // All valid bucket and object actions.

        public static List<string> VALID_ACTIONS()
        {
            List<string> res = new List<string>();
            res.Union(COMMON_BUCKET_ACTIONS);
            res.Union(READ_ONLY_BUCKET_ACTIONS);
            res.Union(WRITE_ONLY_BUCKET_ACTIONS);
            res.Union(READ_WRITE_OBJECT_ACTIONS());
            return res;
        }

    }
}
