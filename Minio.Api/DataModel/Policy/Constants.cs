using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Policy
{
    class Constants
    {
        // Resource prefix for all aws resources.
        public static readonly String AWS_RESOURCE_PREFIX = "arn:aws:s3:::";

        // Common bucket actions for both read and write policies.
        public static readonly ISet<String> COMMON_BUCKET_ACTIONS = new HashSet<String>() { "s3:GetBucketLocation" };

        // Read only bucket actions.
        public static readonly ISet<String> READ_ONLY_BUCKET_ACTIONS = new HashSet<String>() { "s3:ListBucket"};

        // Write only bucket actions.
        public static readonly ISet<String> WRITE_ONLY_BUCKET_ACTIONS =
            new HashSet<String>() { "s3:ListBucketMultipartUploads" };

        // Read only object actions.
        public static readonly ISet<String> READ_ONLY_OBJECT_ACTIONS = new HashSet<String>() { "s3:GetObject" };

        // Write only object actions.
        public static readonly ISet<String> WRITE_ONLY_OBJECT_ACTIONS =
            new HashSet<String>() { "s3:AbortMultipartUpload",
                                              "s3:DeleteObject",
                                              "s3:ListMultipartUploadParts",
                                              "s3:PutObject" };

        // Read and write object actions.
        public static HashSet<string> READ_WRITE_OBJECT_ACTIONS()
        {
            HashSet<string> res = new HashSet<string>();
            res.UnionWith(READ_ONLY_OBJECT_ACTIONS);
            res.UnionWith(WRITE_ONLY_OBJECT_ACTIONS);
            return res;
        }
        // All valid bucket and object actions.

        public static HashSet<string> VALID_ACTIONS()
        {
            HashSet<string> res = new HashSet<string>();
            res.UnionWith(COMMON_BUCKET_ACTIONS);
            res.UnionWith(READ_ONLY_BUCKET_ACTIONS);
            res.UnionWith(WRITE_ONLY_BUCKET_ACTIONS);
            res.UnionWith(READ_WRITE_OBJECT_ACTIONS());
            return res;
        }

    }
}
