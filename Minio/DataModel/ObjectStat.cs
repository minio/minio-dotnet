﻿/*
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
using System.Linq;

namespace Minio.DataModel
{
    public class ObjectStat
    {
        /// <summary>
        /// Object metadata information.
        /// </summary>
        /// <param name="objectName">Object name</param>
        /// <param name="size">Object size</param>
        /// <param name="lastModified">Last when object was modified</param>
        /// <param name="etag">Unique entity tag for the object</param>
        /// <param name="contentType">Object content type</param>
        /// <param name="versionId">Object Version ID</param>
        /// <param name="deleteMarker">Object Version ID delete marker</param>
        /// <param name="metadata"></param>
        public ObjectStat(string objectName, long size, DateTime lastModified, string etag, string contentType, string versionId, bool deleteMarker, Dictionary<string, string> metadata)
        {
            this.ObjectName = objectName;
            this.Size = size;
            this.LastModified = lastModified;
            this.ETag = etag;
            this.ContentType = contentType;
            if (metadata != null && metadata.Count > 0)
            {
                this.MetaData = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
            }
            this.VersionId = versionId;
            this.DeleteMarker = deleteMarker;
        }

        public string ObjectName { get; private set; }
        public long Size { get; private set; }
        public DateTime LastModified { get; private set;  }
        public string ETag { get; private set; }
        public string ContentType { get; private set; }
        public Dictionary<string, string> MetaData { get; private set; }
        public string VersionId { get; private set; }
        public bool DeleteMarker { get; private set; }

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