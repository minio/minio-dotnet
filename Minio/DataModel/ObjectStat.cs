/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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

namespace Minio.DataModel
{
    using System;
    using System.Collections.Generic;

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
        public ObjectStat(string objectName, long size, DateTime lastModified, string etag, string contentType,
            Dictionary<string, string> metadata)
        {
            this.ObjectName = objectName;
            this.Size = size;
            this.LastModified = lastModified;
            this.ETag = etag;
            this.ContentType = contentType;
            this.MetaData = metadata;
        }

        public string ObjectName { get; }
        public long Size { get; }
        public DateTime LastModified { get; }
        public string ETag { get; }
        public string ContentType { get; }
        public Dictionary<string, string> MetaData { get; }

        public override string ToString()
        {
            return
                $"{this.ObjectName} : Size({this.Size}) LastModified({this.LastModified}) ETag({this.ETag}) Content-Type({this.ContentType})";
        }
    }
}