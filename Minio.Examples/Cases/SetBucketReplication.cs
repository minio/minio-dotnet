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
using System.Collections.Generic;
using System.Threading.Tasks;

using Minio.DataModel;
using Minio.DataModel.Replication;

namespace Minio.Examples.Cases
{
    public class SetBucketReplication
    {
        // Set Replication configuration for the bucket
        public async static Task Run(MinioClient minio,
                                    string bucketName = "my-bucket-name")
        {
            try
            {
                Console.WriteLine("Running example for API: SetBucketReplication");
                Dictionary<string, string> tags = new Dictionary<string, string>()
                                {
                                    {"key1", "value1"},
                                    {"key2", "value2"},
                                    {"key3", "value3"}
                                };
                ReplicationRule rule = 
                    new ReplicationRule(
                        new DeleteMarkerReplication(DeleteMarkerReplication.StatusEnabled),
                        new ReplicationDestination(
                                null, null, "Bucket-ARN", null, null, null, null),
                        null,
                        new RuleFilter(new AndOperator("PREFIX", Tagging.GetBucketTags(tags)),null, null),
                        new DeleteReplication(DeleteReplication.StatusDisabled),
                        1,
                        "REPLICATION-ID",
                        "PREFIX",
                        null,
                        ReplicationRule.StatusEnabled
                    );
                List<ReplicationRule> rules = new List<ReplicationRule>();
                rules.Add(rule);
                ReplicationConfiguration repl = new ReplicationConfiguration("REPLICATION-ROLE", rules);

                await minio.SetBucketReplicationAsync(
                    new SetBucketReplicationArgs()
                        .WithBucket(bucketName)
                        .WithConfiguration(repl)
                );
                Console.WriteLine($"Bucket Replication set for bucket {bucketName}.");
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Bucket]  Exception: {e}");
            }
        }
    }

}