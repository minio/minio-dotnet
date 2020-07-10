/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2020 MinIO, Inc.
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

using Minio.DataModel;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Minio
{
    public class ObjectClientArgs
    {
        internal MinioClientArgs ClientArgs;
        internal BucketClientArgs BktClientArgs;

        internal string ObjectName { get; set; }
        internal string VersionId { get; set; }

    }

    public class ObjectReadPropertiesArgs
    {
        internal MinioClientArgs ClientArgs;
        internal BucketClientArgs BktClientArgs;
        internal ObjectClientArgs ObjClientArgs;

        internal SSEC SseConfiguration { get; set; }
        internal long Offset { get; set; }
        internal long Length { get; set; }
        internal string MatchETag { get; set; }
        internal string NotMatchETag { get; set; }
        internal DateTime ModifiedSince { get; set; }
        internal DateTime UnModifiedSince { get; set; }
    }

    public partial class MinioClient
    {
        // Initializer for ObjectClientArgs
        protected MinioClient(string bucket, string obj)
        {
            this.ObjectMinioClientArgs.BktClientArgs.BucketName = bucket;
            this.ObjectMinioClientArgs.ObjectName = obj;
        }

        // Initializer for Object Read Properties
        private MinioClient(string bucket, string obj, SSEC sSEC, string versionId,
                long offset, long length, string matchEtag, string notMatchEtag, DateTime modifiedSince, DateTime unModifiedSince)
        {
            this.ObjectReadPropertiesClientArgs.BktClientArgs.BucketName = bucket;
            this.ObjectReadPropertiesClientArgs.ObjClientArgs.ObjectName = obj;
            this.ObjectReadPropertiesClientArgs.SseConfiguration = sSEC;
            this.ObjectReadPropertiesClientArgs.ObjClientArgs.VersionId = versionId;
            this.ObjectReadPropertiesClientArgs.Offset = offset;
            this.ObjectReadPropertiesClientArgs.Length = length;
            this.ObjectReadPropertiesClientArgs.MatchETag = matchEtag;
            this.ObjectReadPropertiesClientArgs.ModifiedSince = modifiedSince;
            this.ObjectReadPropertiesClientArgs.UnModifiedSince = unModifiedSince;
        }

        public static MinioClient NewClient(string bucket, string obj, SSEC sSEC,string versionId,
                long offset, long length, string matchEtag, string notMatchEtag, DateTime modifiedSince, DateTime unModifiedSince)
        {
            var mc = new MinioClient(bucket, obj, sSEC, versionId, offset, length, matchEtag, notMatchEtag, modifiedSince, unModifiedSince);
            return mc;
        }

    }


}