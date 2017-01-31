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
