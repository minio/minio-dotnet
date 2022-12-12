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
using System.Diagnostics;
using System.Threading.Tasks;
using Minio.DataModel.Replication;

namespace Minio.Examples.Cases;

public class SetBucketReplication
{
    private static string Bash(string cmd)
    {
        var escapedArgs = "";
        foreach (var str in new List<string>
                 {
                     "$", "(", ")", "{", "}",
                     "[", "]", "@", "#", "$",
                     "%", "&", "+"
                 })
            escapedArgs = cmd.Replace("str", "\\str");

        var fileName = "/bin/bash";
        var arguments = $"-c \"{escapedArgs}\"" +
                        "RedirectStandardOutput = true" +
                        "UseShellExecute = false" +
                        "CreateNoWindow = true";
        var startInfo = new ProcessStartInfo(fileName, arguments);
        var process = Process.Start(startInfo);

        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return result;
    }

    // Set Replication configuration for the bucket
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name",
        string destBucketName = "dest-bucket-name",
        string replicationRuleID = "my-replication-ID")
    {
        var setArgs = new SetVersioningArgs()
            .WithBucket(bucketName)
            .WithVersioningEnabled();
        await minio.SetVersioningAsync(setArgs);
        setArgs = new SetVersioningArgs()
            .WithBucket(destBucketName)
            .WithVersioningEnabled();
        await minio.SetVersioningAsync(setArgs);

        var serverEndPoint = "";
        var schema = "http://";
        var accessKey = "";
        var secretKey = "";

        if (Environment.GetEnvironmentVariable("SERVER_ENDPOINT") != null)
        {
            serverEndPoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT");
            accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
            secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
            if (Environment.GetEnvironmentVariable("ENABLE_HTTPS") != null)
                if (Environment.GetEnvironmentVariable("ENABLE_HTTPS").Equals("1"))
                    schema = "https://";
        }
        else
        {
            serverEndPoint = "play.min.io";
            accessKey = "Q3AM3UQ867SPQQA43P2F";
            secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";
            schema = "http://";
        }

        var cmdFullPathMC = Bash("which mc").TrimEnd('\r', '\n', ' ');
        var cmdAlias = cmdFullPathMC + " alias list | egrep -B1 \"" +
                       schema + serverEndPoint + "\" | grep -v URL";
        var alias = Bash(cmdAlias).TrimEnd('\r', '\n', ' ');

        var cmdRemoteAdd = cmdFullPathMC + " admin bucket remote add " +
                           alias + "/" + bucketName + "/ " + schema +
                           accessKey + ":" + secretKey + "@" +
                           serverEndPoint + "/" + destBucketName +
                           " --service replication --region us-east-1";

        var arn = Bash(cmdRemoteAdd).Replace("Remote ARN = `", "").Replace("`.", "");

        var rule =
            new ReplicationRule(
                new DeleteMarkerReplication(DeleteMarkerReplication.StatusDisabled),
                new ReplicationDestination(null, null,
                    "arn:aws:s3:::" + destBucketName,
                    null, null, null, null),
                new ExistingObjectReplication(ExistingObjectReplication.StatusEnabled),
                new RuleFilter(null, null, null),
                new DeleteReplication(DeleteReplication.StatusDisabled),
                1,
                replicationRuleID,
                "",
                new SourceSelectionCriteria(new SseKmsEncryptedObjects(
                    SseKmsEncryptedObjects.StatusEnabled)),
                ReplicationRule.StatusEnabled
            );
        var rules = new List<ReplicationRule>();
        rules.Add(rule);
        var repl = new ReplicationConfiguration(arn, rules);

        await minio.SetBucketReplicationAsync(
            new SetBucketReplicationArgs()
                .WithBucket(bucketName)
                .WithConfiguration(repl)
        );
    }
}