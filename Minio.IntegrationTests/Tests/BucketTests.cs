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
}