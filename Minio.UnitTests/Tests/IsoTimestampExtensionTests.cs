using Minio.Helpers;
using Xunit;

namespace Minio.UnitTests.Tests;

public class IsoTimestampExtensionTests
{
    public static readonly IEnumerable<object[]> Data =
    [
        ["2021-12-23T01:02:03+04:30", new DateTimeOffset(2021,12,23,1,2,3,TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(30)))],
        ["2021-12-23T01:02:03Z", (DateTimeOffset)new DateTime(2021,12,23,1,2,3,DateTimeKind.Utc)],
        ["2021-12-23T01:02:03.725+04:30", new DateTimeOffset(2021,12,23,1,2,3,725,TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(30)))],
        ["2021-12-23T01:02:03.725Z", (DateTimeOffset)new DateTime(2021,12,23,1,2,3,725,DateTimeKind.Utc)]
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public void ParseIsoTimestamp(string text, DateTimeOffset expected)
    {
        var got = text.ParseIsoTimestamp();
        Assert.NotNull(got);
        Assert.Equal(expected, got);
    }
}