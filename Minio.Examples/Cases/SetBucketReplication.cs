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
// using System.Xml;
// using System.IO;
// using System.Xml.Serialization;
using System.Diagnostics;

using Minio.DataModel.Tags;
using Minio.DataModel.Replication;

namespace Minio.Examples.Cases
{
    // To run bash commands
    public static class ShellHelper
    {
        public static string Bash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return result;
        }
    }

    public class SetBucketReplication
    {

        // Set Replication configuration for the bucket
        public async static Task Run(MinioClient minio,
                                    string bucketName = "my-bucket-name",
                                    string destBucketName = "dest-bucket-name",
                                    string replicationCfgID = "my-replication-ID")
        {
            try
            {
                Console.WriteLine("Running example for API: SetBucketReplication");
                var setArgs = new SetVersioningArgs()
                                        .WithBucket(bucketName)
                                        .WithVersioningEnabled();
                await minio.SetVersioningAsync(setArgs);
                setArgs = new SetVersioningArgs()
                                        .WithBucket(destBucketName)
                                        .WithVersioningEnabled();
                await minio.SetVersioningAsync(setArgs);

                string serverEndPoint = "";
                string schema = "";
                string accessKey = "";
                string secretKey = "";

                if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") != null)
                {
                    serverEndPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
                    accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
                    secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
                    if (Environment.GetEnvironmentVariable("ENABLE_HTTPS") != null)
                    {
                        if (Environment.GetEnvironmentVariable("ENABLE_HTTPS").Equals("1"))
                        {
                            schema = "https://";
                        }
                    }
                }
                else
                {
                    serverEndPoint = "play.min.io";
                    accessKey = "Q3AM3UQ867SPQQA43P2F";
                    secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
                    schema = "http://";
                }
                var cmdFullPathMC = "which mc".Bash().TrimEnd('\r', '\n', ' ');

                var cmdAlias = cmdFullPathMC + " alias list | egrep -B1 \"" +
                               schema + serverEndPoint + "\" | grep -v URL";
                var alias = cmdAlias.Bash().TrimEnd('\r', '\n', ' ');

                var cmdRemoteAdd = cmdFullPathMC + " admin bucket remote add " +
                              alias + "/" + bucketName + "/ " + schema +
                              accessKey + ":" + secretKey + "@" +
                              serverEndPoint + "/" + destBucketName +
                              " --service replication --region us-east-1";

                var arn = cmdRemoteAdd.Bash().Replace("Remote ARN = `", "").Replace("`.", "");

                ReplicationRule rule =
                    new ReplicationRule(
                        new DeleteMarkerReplication(DeleteMarkerReplication.StatusDisabled),
                        new ReplicationDestination(null, null,
                                        "arn:aws:s3:::" + destBucketName,
                                        null, null, null, null),
                        new ExistingObjectReplication(ExistingObjectReplication.StatusEnabled),
                        new RuleFilter(null, null, null),
                        new DeleteReplication(DeleteReplication.StatusDisabled),
                        1,
                        replicationCfgID,
                        "",
                        new SourceSelectionCriteria(new SseKmsEncryptedObjects(
                                                    SseKmsEncryptedObjects.StatusEnabled)),
                        ReplicationRule.StatusEnabled
                    );
                List<ReplicationRule> rules = new List<ReplicationRule>();
                rules.Add(rule);
                ReplicationConfiguration repl = new ReplicationConfiguration(arn, rules);

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