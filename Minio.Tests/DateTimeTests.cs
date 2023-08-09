using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Helper;

namespace Minio.Tests;

/// <summary>
///     Tests for DateTime conversion.
/// </summary>
[TestClass]
public class DateTimeTests
{
    [TestMethod]
    public void TestCopyObjectArgsRetentionUntilDate()
    {
        var d = TruncateMilliseconds(DateTime.Now);
        var args = new CopyObjectArgs()
            .WithObjectLockRetentionDate(d);
        Assert.AreEqual(d, args.RetentionUntilDate);
        Assert.AreEqual(d.Kind, args.RetentionUntilDate.Kind);
    }

    [TestMethod]
    public void TestCopyObjectRequestArgsRetentionUntilDate()
    {
        var d = TruncateMilliseconds(DateTime.Now);
        var args = new CopyObjectRequestArgs()
            .WithObjectLockRetentionDate(d);
        Assert.AreEqual(d, args.RetentionUntilDate);
        Assert.AreEqual(d.Kind, args.RetentionUntilDate.Kind);
    }

    [TestMethod]
    public void TestMultipartCopyUploadArgsRetentionUntilDate()
    {
        var d = TruncateMilliseconds(DateTime.Now);
        var args = new MultipartCopyUploadArgs()
            .WithObjectLockRetentionDate(d);
        Assert.AreEqual(d, args.RetentionUntilDate);
        Assert.AreEqual(d.Kind, args.RetentionUntilDate.Kind);
    }

    [TestMethod]
    public void TestNewMultipartUploadArgsRetentionUntilDate()
    {
        var d = TruncateMilliseconds(DateTime.Now);
        var args = new NewMultipartUploadArgs<NewMultipartUploadPutArgs>()
            .WithObjectLockRetentionDate(d);
        Assert.AreEqual(d, args.RetentionUntilDate);
        Assert.AreEqual(d.Kind, args.RetentionUntilDate.Kind);
    }

    [TestMethod]
    public void TestObjectStatExpires()
    {
        var d = TruncateMilliseconds(DateTime.Now);
        var headers = new Dictionary<string, string> { ["x-amz-expiration"] = d.ToUniversalTime().ToString("r") };
        var stat = ObjectStat.FromResponseHeaders("test", headers);
        Assert.AreEqual(d.ToUniversalTime(), stat.Expires?.ToUniversalTime());
    }

    [TestMethod]
    public void TestObjectStatObjectLockRetainUntilDate()
    {
        var d = TruncateMilliseconds(DateTime.Now);
        var headers = new Dictionary<string, string>
        {
            ["x-amz-object-lock-retain-until-date"] = d.ToUniversalTime().ToString("O")
        };
        var stat = ObjectStat.FromResponseHeaders("test", headers);
        Assert.AreEqual(d.ToUniversalTime(), stat.ObjectLockRetainUntilDate?.ToUniversalTime());
    }

    [TestMethod]
    public void TestUtilsTo8601String()
    {
        var d = TruncateMilliseconds(DateTime.Now);
        var converted = Utils.To8601String(d);
        var parsed = DateTime.Parse(converted);
        Assert.AreEqual(d, parsed);
        Assert.AreEqual(d.Kind, parsed.Kind);
    }

    private static DateTime TruncateMilliseconds(DateTime dateTime)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
    }
}
