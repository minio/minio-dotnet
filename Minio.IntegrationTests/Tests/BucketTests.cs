using System.Text.Json;
using Minio.Model;
using Xunit;

namespace Minio.IntegrationTests.Tests;

public class BucketTests : MinioTest
{
    [Fact]
    public async Task MakeStandardBucket()
    {
        var client = CreateClient();
        
        // Create the bucket
        await client.CreateBucketAsync("test").ConfigureAwait(true);
        var bucketExists = await client.BucketExistsAsync("test").ConfigureAwait(true);
        Assert.True(bucketExists);

        // Delete the bucket again
        await client.DeleteBucketAsync("test").ConfigureAwait(true);
        bucketExists = await client.BucketExistsAsync("test").ConfigureAwait(true);
        Assert.False(bucketExists);
    }

    [Fact]
    public async Task ListBuckets()
    {
        var startTimeUtc = DateTimeOffset.Now.AddMinutes(-1); // allow some deviation

        var client = CreateClient();
        await client.CreateBucketAsync("test1").ConfigureAwait(true);
        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(true);
        await client.CreateBucketAsync("test2").ConfigureAwait(true);

        var endTimeUtc = DateTimeOffset.Now.AddMinutes(1); // allow some deviation
        
        var buckets = await client.ListBucketsAsync().ToListAsync().ConfigureAwait(true);
        buckets.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        
        Assert.Equal(2, buckets.Count);
        Assert.Equal("test1", buckets[0].Name);
        Assert.Equal("test2", buckets[1].Name);
        Assert.InRange(buckets[0].CreationDate, startTimeUtc, endTimeUtc);
        Assert.InRange(buckets[1].CreationDate, startTimeUtc, endTimeUtc);
        Assert.True(buckets[0].CreationDate < buckets[1].CreationDate);
    }

    [Fact]
    public async Task BucketTagging()
    {
        var client = CreateClient();
        await client.CreateBucketAsync("test").ConfigureAwait(true);
        var tags1 = await client.GetBucketTaggingAsync("test").ConfigureAwait(true);
        Assert.Null(tags1);

        var tags2 = new Dictionary<string, string>
        {
            { "tag1", "abc" },
            { "tag2", "def" },
        };
        await client.SetBucketTaggingAsync("test", tags2).ConfigureAwait(true);

        var tags3 = await client.GetBucketTaggingAsync("test").ConfigureAwait(true);
        Assert.NotNull(tags3);
        Assert.Equal(tags2, tags3);

        await client.DeleteBucketTaggingAsync("test").ConfigureAwait(true);

        var tags4 = await client.GetBucketTaggingAsync("test").ConfigureAwait(true);
        Assert.Null(tags4);

        // ReSharper disable once CollectionNeverUpdated.Local
        var tags5 = new Dictionary<string, string>();
        await client.SetBucketTaggingAsync("test", tags5).ConfigureAwait(true);

        var tags6 = await client.GetBucketTaggingAsync("test").ConfigureAwait(true);
        Assert.NotNull(tags6);
        Assert.Empty(tags6);
    }

    [Fact]
    public async Task BucketObjectWithoutLocking()
    {
        var client = CreateClient();
        await client.CreateBucketAsync("test").ConfigureAwait(true);
        var objLock = await client.GetObjectLockConfigurationAsync("test").ConfigureAwait(true);
        Assert.Null(objLock.DefaultRetentionRule);
    }
    

    [Fact]
    public async Task BucketObjectLock()
    {
        var client = CreateClient();
        await client.CreateBucketAsync("test", objectLocking: true).ConfigureAwait(true);
        var objLock = await client.GetObjectLockConfigurationAsync("test").ConfigureAwait(true);
        Assert.Null(objLock.DefaultRetentionRule);

        // Enable object lock (governance, days)
        await client.SetObjectLockConfigurationAsync("test", new RetentionRuleDays(RetentionMode.Governance, 100)).ConfigureAwait(true);
        objLock = await client.GetObjectLockConfigurationAsync("test").ConfigureAwait(true);
        Assert.IsType<RetentionRuleDays>(objLock.DefaultRetentionRule);
        var ruleDays = (RetentionRuleDays)objLock.DefaultRetentionRule;
        Assert.Equal(RetentionMode.Governance, ruleDays.Mode);
        Assert.Equal(100, ruleDays.Days);
 
        // Disable object lock
        await client.SetObjectLockConfigurationAsync("test", null).ConfigureAwait(true);
        objLock = await client.GetObjectLockConfigurationAsync("test").ConfigureAwait(true);
        Assert.Null(objLock.DefaultRetentionRule);

        // Enable object lock (compliance, days)
        await client.SetObjectLockConfigurationAsync("test", new RetentionRuleYears(RetentionMode.Compliance, 3)).ConfigureAwait(true);
        objLock = await client.GetObjectLockConfigurationAsync("test").ConfigureAwait(true);
        Assert.IsType<RetentionRuleYears>(objLock.DefaultRetentionRule);
        var ruleYears = (RetentionRuleYears)objLock.DefaultRetentionRule;
        Assert.Equal(RetentionMode.Compliance, ruleYears.Mode);
        Assert.Equal(3, ruleYears.Years);
    }

    [Fact]
    public async Task BucketEncryptionWithoutConfig()
    {
        var client = CreateClient();
        await client.CreateBucketAsync("test").ConfigureAwait(true);

        // No configuration set → throws with ServerSideEncryptionConfigurationNotFoundError.
        // A full Set/Get/Remove round-trip is not covered here because the standard MinIO
        // container does not support bucket-default SSE without KMS configured
        // (SetBucketEncryptionAsync returns 501 NotImplemented for SSE-S3/AES256).
        var ex = await Assert.ThrowsAsync<MinioHttpException>(
            () => client.GetBucketEncryptionAsync("test")).ConfigureAwait(true);
        Assert.Equal("ServerSideEncryptionConfigurationNotFoundError", ex.Error?.Code);
    }

    [Fact]
    public async Task BucketLifecycle()
    {
        var client = CreateClient();
        await client.CreateBucketAsync("test").ConfigureAwait(true);

        // No configuration set → returns null
        var lc1 = await client.GetBucketLifecycleAsync("test").ConfigureAwait(true);
        Assert.Null(lc1);

        // Set a lifecycle rule that expires objects after 30 days
        var config = new LifecycleConfiguration
        {
            Rules =
            [
                new LifecycleRule
                {
                    Id = "expire-after-30-days",
                    Status = LifecycleRuleStatus.Enabled,
                    Filter = new LifecycleFilter { Prefix = "logs/" },
                    Expiration = new LifecycleExpiration { Days = 30 },
                },
            ],
        };
        await client.SetBucketLifecycleAsync("test", config).ConfigureAwait(true);

        // Retrieve and verify the rule round-trips correctly
        var lc2 = await client.GetBucketLifecycleAsync("test").ConfigureAwait(true);
        Assert.NotNull(lc2);
        Assert.Single(lc2.Rules);
        Assert.Equal("expire-after-30-days", lc2.Rules[0].Id);
        Assert.Equal(LifecycleRuleStatus.Enabled, lc2.Rules[0].Status);
        Assert.Equal(30, lc2.Rules[0].Expiration?.Days);

        // Remove and confirm the configuration is gone
        await client.RemoveBucketLifecycleAsync("test").ConfigureAwait(true);
        var lc3 = await client.GetBucketLifecycleAsync("test").ConfigureAwait(true);
        Assert.Null(lc3);
    }

    [Fact]
    public async Task BucketReplicationWithoutConfig()
    {
        var client = CreateClient();
        await client.CreateBucketAsync("test").ConfigureAwait(true);

        // No replication configuration set → returns null
        var rc = await client.GetBucketReplicationAsync("test").ConfigureAwait(true);
        Assert.Null(rc);
    }


    [Fact]
    public async Task BucketPolicy()
    {
        var client = CreateClient();
        await client.CreateBucketAsync("test").ConfigureAwait(true);

        // No policy set → returns null
        var policy1 = await client.GetPolicyAsync("test").ConfigureAwait(true);
        Assert.Null(policy1);

        // Set a read-only public policy for the bucket
        const string policyJson = """
            {
                "Version": "2012-10-17",
                "Statement": [
                    {
                        "Effect": "Allow",
                        "Principal": {"AWS": ["*"]},
                        "Action": ["s3:GetObject"],
                        "Resource": ["arn:aws:s3:::test/*"]
                    }
                ]
            }
            """;
        await client.SetPolicyAsync("test", policyJson).ConfigureAwait(true);

        // Retrieve and verify the policy contains the expected statement
        var policy2 = await client.GetPolicyAsync("test").ConfigureAwait(true);
        Assert.NotNull(policy2);
        using var doc = JsonDocument.Parse(policy2!);
        var statements = doc.RootElement.GetProperty("Statement");
        Assert.Equal(1, statements.GetArrayLength());
        Assert.Equal("Allow", statements[0].GetProperty("Effect").GetString());
        Assert.Contains("s3:GetObject",
            statements[0].GetProperty("Action").EnumerateArray().Select(e => e.GetString()!));

        // Remove and confirm the policy is gone
        await client.RemovePolicyAsync("test").ConfigureAwait(true);
        var policy3 = await client.GetPolicyAsync("test").ConfigureAwait(true);
        Assert.Null(policy3);
    }
}