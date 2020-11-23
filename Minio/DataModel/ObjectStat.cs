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
using RestSharp;

namespace Minio.DataModel
{
    public class ObjectStat
    {
        private ObjectStat()
        {
            MetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ExtraHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static ObjectStat FromResponseHeaders(string objectName, IList<Parameter> responseHeaders)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                throw new ArgumentNullException("Name of an object cannot be empty");
            }
            ObjectStat objInfo = new ObjectStat();
            foreach (Parameter parameter in responseHeaders)
            {
                switch(parameter.Name.ToLower())
                {
                    case "content-length" :
                        objInfo.Size = long.Parse(parameter.Value.ToString());
                        break;
                    case "last-modified" :
                        objInfo.LastModified = DateTime.Parse(parameter.Value.ToString(), CultureInfo.InvariantCulture);
                        break;
                    case "etag" :
                        objInfo.ETag = parameter.Value.ToString().Replace("\"", string.Empty);
                        break;
                    case "Content-Type" :
                        objInfo.ContentType = parameter.Value.ToString();
                        objInfo.MetaData["Content-Type"] = objInfo.ContentType;
                        break;
                    case "x-amz-version-id" :
                        objInfo.VersionId = parameter.Value.ToString();
                        break;
                    case "x-amz-delete-marker":
                        objInfo.DeleteMarker = parameter.Value.ToString().Equals("true");
                        break;
                    default:
                        if (OperationsUtil.IsSupportedHeader(parameter.Name))
                        {
                            objInfo.MetaData[parameter.Name] = parameter.Value.ToString();
                        }
                        else if (parameter.Name.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                        {
                            objInfo.MetaData[parameter.Name.Substring("x-amz-meta-".Length)] = parameter.Value.ToString();
                        }
                        else
                        {
                            objInfo.ExtraHeaders[parameter.Name] = parameter.Value.ToString();
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