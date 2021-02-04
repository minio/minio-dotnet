/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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
                    case "x-amz-tagging-count":
                        if (Int32.TryParse(paramValue.ToString(), out int tagCount) && tagCount >= 0)
                        {
                            objInfo.TaggingCount = (uint)tagCount;
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
        public uint TaggingCount { get; private set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(this.VersionId))
            {
                string versionInfo = $"Version ID({this.VersionId})";
                if (this.DeleteMarker)
                {
                    versionInfo = $"Version ID({this.VersionId}, deleted)";
                }
                return $"{this.ObjectName} : {versionInfo} Size({this.Size}) LastModified({this.LastModified}) ETag({this.ETag}) Content-Type({this.ContentType})";
            }
            return $"{this.ObjectName} : Size({this.Size}) LastModified({this.LastModified}) ETag({this.ETag}) Content-Type({this.ContentType})";
        }
    }
}