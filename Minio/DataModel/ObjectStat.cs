/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017-2021 MinIO, Inc.
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
using System.Text.RegularExpressions;
using Minio.DataModel.ObjectLock;

namespace Minio.DataModel;

public class ObjectStat
{
    private ObjectStat()
    {
        MetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ExtraHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public string ObjectName { get; private set; }
    public long Size { get; private set; }
    public DateTime LastModified { get; private set; }
    public string ETag { get; private set; }
    public string ContentType { get; private set; }
    public Dictionary<string, string> MetaData { get; }
    public string VersionId { get; private set; }
    public bool DeleteMarker { get; private set; }
    public Dictionary<string, string> ExtraHeaders { get; }
    public uint? TaggingCount { get; private set; }
    public string ArchiveStatus { get; private set; }
    public DateTime? Expires { get; private set; }
    public string ReplicationStatus { get; }
    public RetentionMode? ObjectLockMode { get; private set; }
    public DateTime? ObjectLockRetainUntilDate { get; private set; }
    public bool? LegalHoldEnabled { get; private set; }

    public static ObjectStat FromResponseHeaders(string objectName, IDictionary<string, string> responseHeaders)
    {
        if (string.IsNullOrEmpty(objectName))
            throw new ArgumentNullException(nameof(objectName), "Name of an object cannot be empty");
        if (responseHeaders is null) throw new ArgumentNullException(nameof(responseHeaders));

        var objInfo = new ObjectStat
        {
            ObjectName = objectName
        };
        foreach (var paramName in responseHeaders.Keys)
        {
            var paramValue = responseHeaders[paramName];
            switch (paramName.ToLowerInvariant())
            {
                case "content-length":
                    objInfo.Size = long.Parse(paramValue);
                    break;
                case "last-modified":
                    objInfo.LastModified = DateTime.Parse(paramValue, CultureInfo.InvariantCulture);
                    break;
                case "etag":
                    objInfo.ETag = paramValue.Replace("\"", string.Empty);
                    break;
                case "content-type":
                    objInfo.ContentType = paramValue;
                    objInfo.MetaData["Content-Type"] = objInfo.ContentType;
                    break;
                case "x-amz-version-id":
                    objInfo.VersionId = paramValue;
                    break;
                case "x-amz-delete-marker":
                    objInfo.DeleteMarker = paramValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "x-amz-archive-status":
                    objInfo.ArchiveStatus = paramValue;
                    break;
                case "x-amz-tagging-count":
                    if (int.TryParse(paramValue, out var tagCount) && tagCount >= 0)
                        objInfo.TaggingCount = (uint)tagCount;
                    break;
                case "x-amz-expiration":
                    // x-amz-expiration header includes the expiration date and the corresponding rule id.
                    var expirationResponse = paramValue.Trim();
                    var expiryDatePattern =
                        @"(Sun|Mon|Tue|Wed|Thu|Fri|Sat), \d{2} (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec) \d{4} \d{2}:\d{2}:\d{2} [A-Z]+";
                    var expiryMatch = Regex.Match(expirationResponse, expiryDatePattern);
                    if (expiryMatch.Success)
                        objInfo.Expires = DateTime.SpecifyKind(
                            DateTime.Parse(expiryMatch.Value),
                            DateTimeKind.Utc);

                    break;
                case "x-amz-object-lock-mode":
                    if (!string.IsNullOrWhiteSpace(paramValue))
                        objInfo.ObjectLockMode = paramValue.Equals("governance", StringComparison.OrdinalIgnoreCase)
                            ? RetentionMode.GOVERNANCE
                            : RetentionMode.COMPLIANCE;

                    break;
                case "x-amz-object-lock-retain-until-date":
                    var lockUntilDate = paramValue;
                    if (!string.IsNullOrWhiteSpace(lockUntilDate))
                        objInfo.ObjectLockRetainUntilDate = DateTime.SpecifyKind(
                            DateTime.Parse(lockUntilDate),
                            DateTimeKind.Utc);

                    break;
                case "x-amz-object-lock-legal-hold":
                    var legalHoldON = paramValue.Trim();
                    if (!string.IsNullOrWhiteSpace(legalHoldON))
                        objInfo.LegalHoldEnabled = legalHoldON.Equals("on", StringComparison.OrdinalIgnoreCase);
                    break;
                default:
                    if (OperationsUtil.IsSupportedHeader(paramName))
                        objInfo.MetaData[paramName] = paramValue;
                    else if (paramName.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                        objInfo.MetaData[paramName.Substring("x-amz-meta-".Length)] = paramValue;
                    else
                        objInfo.ExtraHeaders[paramName] = paramValue;
                    break;
            }
        }

        return objInfo;
    }

    public override string ToString()
    {
        var versionInfo = "VersionId(None)";
        var legalHold = "LegalHold(None)";
        var taggingCount = "Tagging-Count(0)";
        var expires = "Expiry(None)";
        var objectLockInfo = "ObjectLock(None)";
        var archiveStatus = "Archive Status(None)";
        var replicationStatus = "Replication Status(None)";
        if (!string.IsNullOrWhiteSpace(VersionId))
        {
            versionInfo = $"Version ID({VersionId})";
            if (DeleteMarker) versionInfo = $"Version ID({VersionId}, deleted)";
        }

        if (Expires is not null) expires = "Expiry(" + Utils.To8601String(Expires.Value) + ")";
        if (ObjectLockMode is not null)
        {
            objectLockInfo = "ObjectLock Mode(" +
                             (ObjectLockMode == RetentionMode.GOVERNANCE ? "GOVERNANCE" : "COMPLIANCE") + ")";
            objectLockInfo += " Retain Until Date(" + Utils.To8601String(ObjectLockRetainUntilDate.Value) + ")";
        }

        if (TaggingCount is not null) taggingCount = "Tagging-Count(" + TaggingCount.Value + ")";
        if (LegalHoldEnabled is not null)
            legalHold = "LegalHold(" + (LegalHoldEnabled.Value ? "Enabled" : "Disabled") + ")";
        if (!string.IsNullOrWhiteSpace(ReplicationStatus))
            replicationStatus = "Replication Status(" + ReplicationStatus + ")";
        if (!string.IsNullOrWhiteSpace(ArchiveStatus)) archiveStatus = "Archive Status(" + ArchiveStatus + ")";
        var lineTwo = $"{expires} {objectLockInfo} {legalHold} {taggingCount} {archiveStatus} {replicationStatus}";

        return
            $"{ObjectName} : {versionInfo} Size({Size}) LastModified({LastModified}) ETag({ETag}) Content-Type({ContentType})" +
            (string.IsNullOrWhiteSpace(lineTwo) ? "" : "\n" + lineTwo);
    }
}