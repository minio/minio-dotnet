/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017-2020 MinIO, Inc.
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
using System.Threading.Tasks;

namespace Minio.Examples.Cases;

internal class SetBucketPolicy
{
    // Set bucket policy
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name")
    {
        try
        {
            Console.WriteLine("Running example for API: SetPolicyAsync");
            var policyJson =
                $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Action"":[""s3:GetBucketLocation""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}""],""Sid"":""""}},{{""Action"":[""s3:ListBucket""],""Condition"":{{""StringEquals"":{{""s3:prefix"":[""foo"",""prefix/""]}}}},""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}""],""Sid"":""""}},{{""Action"":[""s3:GetObject""],""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Resource"":[""arn:aws:s3:::{bucketName}/foo*"",""arn:aws:s3:::{bucketName}/prefix/*""],""Sid"":""""}}]}}";
            // Change policy type parameter
            var args = new SetPolicyArgs()
                .WithBucket(bucketName)
                .WithPolicy(policyJson);
            await minio.SetPolicyAsync(args);
            Console.WriteLine($"Policy {policyJson} set for the bucket {bucketName} successfully");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}