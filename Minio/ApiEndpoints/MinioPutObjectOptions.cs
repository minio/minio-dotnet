/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2017, 2018, 2019, 2020 MinIO, Inc.
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

using System.Collections.Generic;
using Minio.DataModel;

namespace Minio
{
    public class MinioPutObjectOptions
    {
        public static readonly long DefaultUploadPartSize = 64 * 1024L * 1024L;

        /// <summary>
        /// Content type of the new object, null defaults to "application/octet-stream"
        /// </summary>
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Object metadata to be stored. Defaults to null.
        /// </summary>
        public Dictionary<string, string> MetaData { get; set; } = null;

        /// <summary>
        /// Server-side encryption option. Defaults to null.
        /// </summary>
        public ServerSideEncryption ServerSideEncryption { get; set; } = null;
        
        /// <summary>
        /// Change the default for multipart part sizes when uploading through client
        /// </summary>
        public long? PartSize { get; set; } = DefaultUploadPartSize;
    }
}