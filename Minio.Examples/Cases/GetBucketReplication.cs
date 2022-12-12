/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2021 MinIO, Inc.
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
using Minio.Exceptions;

namespace Minio.Examples.Cases;

public class GetBucketReplication
{
    // Get Replication configuration assigned to the bucket
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string replicationRuleID = "my-replication-rule-ID")
    {
        try
        {
            Console.WriteLine("Running example for API: GetBucketReplicationConfiguration");
            var repl = await minio.GetBucketReplicationAsync(
                new GetBucketReplicationArgs()
                    .WithBucket(bucketName)
            );
            if (repl != null && repl.Rules != null && repl.Rules.Count > 0)
            {
                Console.WriteLine($"Got Bucket Replication Configuration set for bucket {bucketName}.");
                foreach (var rule in repl.Rules)
                {
                    if (rule.ID != replicationRuleID)
                    {
                        // failed test due to replication rule id mismatch
                        var errMessage = "Unexpected replication rule ID: " +
                                         $"expected: {replicationRuleID}, got: {rule.ID}";
                        throw new UnexpectedMinioException(errMessage);
                    }

                    Console.WriteLine("ID: " + rule.ID + ", Status: " + rule.Status);
                }

                Console.WriteLine();
                return;
            }

            Console.WriteLine();
        }
        catch (MissingBucketReplicationConfigurationException)
        {
            Console.WriteLine($"[Bucket] Exception: Replication Configuration not set for bucket {bucketName}.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}