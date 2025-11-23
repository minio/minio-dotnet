using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using System.Globalization;

namespace Minio.Tests;

[TestClass]
public class AccessCredentialsTests
{
    [DataTestMethod]
    [DataRow(null, 0, false, DisplayName = "Null → false")]
    [DataRow("", 0, false, DisplayName = "Empty → false")]
    [DataRow("   \t\n  ", 0, false, DisplayName = "Whitespace → false")]
    [DataRow("__OFFSET__", -5, true, DisplayName = "Past (−5 min) → true")]
    [DataRow("__OFFSET__", 60, false, DisplayName = "Future (+60 min) → false")]
    public void AreExpired_Scenarios_WithStringExpiration(string expirationParam, int offsetMinutes, bool expected)
    {
        var expiration = expirationParam;
        if (string.Equals(expirationParam, "__OFFSET__", StringComparison.Ordinal))
        {
            var dt = DateTime.UtcNow.AddMinutes(offsetMinutes);
            expiration = dt.ToString("o", CultureInfo.InvariantCulture);
        }

        var credentials = new AccessCredentials
        {
            AccessKey = "ak", SecretKey = "sk", SessionToken = "st", Expiration = expiration
        };

        Assert.AreEqual(expected, credentials.AreExpired());
    }

    [DataTestMethod]
    [DataRow(null, 0, false, DisplayName = "Null → false")]
    [DataRow("", 0, false, DisplayName = "Empty → false")]
    [DataRow("   \t\n  ", 0, false, DisplayName = "Whitespace → false")]
    [DataRow("__OFFSET__", -5, true, DisplayName = "Past (−5 min) → true")]
    [DataRow("__OFFSET__", 60, false, DisplayName = "Future (+60 min) → false")]
    public void AreExpired_Scenarios_WithCtor(string expirationParam, int offsetMinutes, bool expected)
    {
        var dt = string.IsNullOrWhiteSpace(expirationParam) ? default : DateTime.UtcNow.AddMinutes(offsetMinutes);
        var credentials = new AccessCredentials("ak", "sk", "st", dt);
        Assert.AreEqual(expected, credentials.AreExpired());
    }
}
