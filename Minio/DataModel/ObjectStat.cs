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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Minio.DataModel.ObjectLock;

namespace Minio.DataModel
{
    public class ObjectStat
    {
        private ObjectStat()
        {
            MetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ExtraHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static ObjectStat FromResponseHeaders(string objectName, Dictionary<string, string> responseHeaders)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                throw new ArgumentNullException("Name of an object cannot be empty");
            }
            ObjectStat objInfo = new ObjectStat();
            objInfo.ObjectName = objectName;
            foreach (var paramName in responseHeaders.Keys)
            {
                string paramValue = responseHeaders[paramName];
                switch(paramName.ToLower())
                {
                    case "content-length" :
                        objInfo.Size = long.Parse(paramValue);
                        break;
                    case "last-modified" :
                        objInfo.LastModified = DateTime.Parse(paramValue, CultureInfo.InvariantCulture);
                        break;
                    case "etag" :
                        objInfo.ETag = paramValue.Replace("\"", string.Empty);
                        break;
                    case "content-type" :
                        objInfo.ContentType = paramValue.ToString();
                        objInfo.MetaData["Content-Type"] = objInfo.ContentType;
                        break;
                    case "x-amz-version-id" :
                        objInfo.VersionId = paramValue;
                        break;
                    case "x-amz-delete-marker":
                        objInfo.DeleteMarker = paramValue.Equals("true");
                        break;
                    case "x-amz-archive-status":
                        objInfo.ArchiveStatus = paramValue.ToString();
                        break;
                    case "x-amz-tagging-count":
                        if (Int32.TryParse(paramValue.ToString(), out int tagCount) && tagCount >= 0)
                        {
                            objInfo.TaggingCount = (uint)tagCount;
                        }
                        break;
                    case "x-amz-expiration":
                        // x-amz-expiration header includes the expiration date and the corresponding rule id.
                        string expirationResponse = paramValue.ToString().Trim();
                        string expiryDatePattern = @"(Sun|Mon|Tue|Wed|Thu|Fri|Sat), \d{2} (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec) \d{4} \d{2}:\d{2}:\d{2} [A-Z]+";
                        Match expiryMatch = Regex.Match(expirationResponse, expiryDatePattern);
                        if (expiryMatch.Success)
                        {
                            objInfo.Expires = DateTime.SpecifyKind(
                                                        DateTime.Parse(expiryMatch.Value),
                                                        DateTimeKind.Utc);
                        }
                        break;
                    case "x-amz-object-lock-mode":
                        Console.WriteLine(paramValue.ToString());
                        if (!string.IsNullOrWhiteSpace(paramValue.ToString()))
                        {
                            objInfo.ObjectLockMode = (paramValue.ToString().ToLower().Equals("governance"))?RetentionMode.GOVERNANCE:RetentionMode.COMPLIANCE;
                        }
                        break;
                    case "x-amz-object-lock-retain-until-date":
                        Console.WriteLine(paramValue.ToString());
                        string lockUntilDate = paramValue.ToString();
                        if (!string.IsNullOrWhiteSpace(lockUntilDate))
                        {
                            objInfo.ObjectLockRetainUntilDate = DateTime.SpecifyKind(
                                                                    DateTime.Parse(lockUntilDate),
                                                                    DateTimeKind.Utc);;
                        }
                        break;
                    case "x-amz-object-lock-legal-hold":
                        string legalHoldON = paramValue.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(legalHoldON))
                        {
                            objInfo.LegalHoldEnabled = legalHoldON.ToLower().Equals("on");
                        }
                        break;
                    default:
                        if (OperationsUtil.IsSupportedHeader(paramName))
                        {
                            objInfo.MetaData[paramName] = paramValue;
                        }
                        else if (paramName.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                        {
                            objInfo.MetaData[paramName.Substring("x-amz-meta-".Length)] = paramValue;
                        }
                        else
                        {
                            objInfo.ExtraHeaders[paramName] = paramValue;
                        }
                        break;
                }
            }

            return objInfo;
        }

        public string ObjectName { get; private set; }
        public long Size { get; private set; }
        public DateTime LastModified { get; private set;  }
        public string ETag { get; private set; }
        public string ContentType { get; private set; }
        public Dictionary<string, string> MetaData { get; private set; }
        public string VersionId { get; private set; }
        public bool DeleteMarker { get; private set; }
        public Dictionary<string, string> ExtraHeaders { get; private set; }
        public uint? TaggingCount { get; private set; }
        public string ArchiveStatus  { get; private set; }
        public DateTime? Expires { get; private set; }
        public string ReplicationStatus { get; private set; }
        public RetentionMode? ObjectLockMode { get; private set; }
        public DateTime? ObjectLockRetainUntilDate { get; private set; }
        public bool? LegalHoldEnabled { get; private set; }

        public override string ToString()
        {
            string versionInfo = "VersionId(None)";
            string legalHold = "LegalHold(None)";
            string taggingCount = "Tagging-Count(0)";
            string expires = "Expiry(None)";
            string objectLockInfo = "ObjectLock(None)";
            string archiveStatus = "Archive Status(None)";
            string replicationStatus = "Replication Status(None)";
            if (!string.IsNullOrWhiteSpace(this.VersionId))
            {
                versionInfo = $"Version ID({this.VersionId})";
                if (this.DeleteMarker)
                {
                    versionInfo = $"Version ID({this.VersionId}, deleted)";
                }
            }
            if (this.Expires != null)
            {
                expires = "Expiry(" + utils.To8601String(this.Expires.Value)+ ")";
            }
            if (this.ObjectLockMode != null)
            {
                objectLockInfo = "ObjectLock Mode(" + ((this.ObjectLockMode == RetentionMode.GOVERNANCE)?"GOVERNANCE":"COMPLIANCE") + ")";
                if (this.ObjectLockRetainUntilDate != null)
                {
                    objectLockInfo += " Retain Until Date(" + utils.To8601String(this.ObjectLockRetainUntilDate.Value) + ")";
                }
            }
            if (this.TaggingCount != null)
            {
                taggingCount = "Tagging-Count(" + this.TaggingCount.Value + ")";
            }
            if (this.LegalHoldEnabled != null)
            {
                legalHold = "LegalHold(" + ((this.LegalHoldEnabled.Value)?"Enabled":"Disabled") + ")";
            }
            if (!string.IsNullOrWhiteSpace(this.ReplicationStatus))
            {
                replicationStatus = "Replication Status(" + this.ReplicationStatus + ")";
            }
            if (!string.IsNullOrWhiteSpace(this.ArchiveStatus))
            {
                archiveStatus = "Archive Status(" + this.ArchiveStatus + ")";
            }
            string lineTwo = $"{expires} {objectLockInfo} {legalHold} {taggingCount} {archiveStatus} {replicationStatus}";

            return $"{this.ObjectName} : {versionInfo} Size({this.Size}) LastModified({this.LastModified}) ETag({this.ETag}) Content-Type({this.ContentType})" +
                    (string.IsNullOrWhiteSpace(lineTwo)?"":("\n" + lineTwo));
        }
    }
}