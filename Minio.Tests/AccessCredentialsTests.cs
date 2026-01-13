/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2021 MinIO, Inc.
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

using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;

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
        DateTime dt = default;
        if (!string.IsNullOrWhiteSpace(expirationParam))
            dt = DateTime.UtcNow.AddMinutes(offsetMinutes);

        var credentials = new AccessCredentials("ak", "sk", "st", dt);

        Assert.AreEqual(expected, credentials.AreExpired());
    }
}
