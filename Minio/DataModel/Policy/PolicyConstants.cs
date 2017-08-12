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

namespace Minio.DataModel.Policy
{
    using System.Collections.Generic;

    public static class PolicyConstants
    {
        // Resource prefix for all aws resources.
        public static readonly string AwsResourcePrefix = "arn:aws:s3:::";

        // Common bucket actions for both read and write policies.
        public static readonly List<string> CommonBucketActions = new List<string> {"s3:GetBucketLocation"};

        // Read only bucket actions.
        public static readonly List<string> ReadOnlyBucketActions = new List<string> {"s3:ListBucket"};

        // Write only bucket actions.
        public static readonly List<string> WriteOnlyBucketActions =
            new List<string> {"s3:ListBucketMultipartUploads"};

        // Read only object actions.
        public static readonly List<string> ReadOnlyObjectActions = new List<string> {"s3:GetObject"};

        // Write only object actions.
        public static readonly List<string> WriteOnlyObjectActions =
            new List<string>
            {
                "s3:AbortMultipartUpload",
                "s3:DeleteObject",
                "s3:ListMultipartUploadParts",
                "s3:PutObject"
            };

        // Read and write object actions.
        public static List<string> READ_WRITE_OBJECT_ACTIONS()
        {
            var res = new List<string>();
            res.AddRange(ReadOnlyObjectActions);
            res.AddRange(WriteOnlyObjectActions);
            return res;
        }
        // All valid bucket and object actions.

        public static IEnumerable<string> VALID_ACTIONS()
        {
            var res = new List<string>();
            res.AddRange(CommonBucketActions);
            res.AddRange(ReadOnlyBucketActions);
            res.AddRange(WriteOnlyBucketActions);
            res.AddRange(READ_WRITE_OBJECT_ACTIONS());
            return res;
        }
    }
}