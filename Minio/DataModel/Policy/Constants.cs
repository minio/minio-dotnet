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
    using System.Linq;

    internal class Constants
    {
        // Resource prefix for all aws resources.
        public static readonly string AwsResourcePrefix = "arn:aws:s3:::";

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

        // Common bucket actions for both read and write policies.
        public static List<string> CommonBucketActions => new List<string> {"s3:GetBucketLocation"};

        // Read only bucket actions.
        public static List<string> ReadOnlyBucketActions => new List<string> {"s3:ListBucket"};

        // Write only bucket actions.
        public static List<string> WriteOnlyBucketActions =>
            new List<string> {"s3:ListBucketMultipartUploads"};

        // Read and write object actions.
        public static IList<string> READ_WRITE_OBJECT_ACTIONS()
        {
            IList<string> res = new List<string>();
            res = res
                .Union(ReadOnlyObjectActions)
                .Union(WriteOnlyObjectActions)
                .ToList();
            return res;
        }
        // All valid bucket and object actions.

        public static List<string> VALID_ACTIONS()
        {
            var res = new List<string>();
            res = res
                .Union(CommonBucketActions)
                .Union(ReadOnlyBucketActions)
                .Union(WriteOnlyBucketActions)
                .Union(READ_WRITE_OBJECT_ACTIONS())
                .ToList();
            return res;
        }
    }
}