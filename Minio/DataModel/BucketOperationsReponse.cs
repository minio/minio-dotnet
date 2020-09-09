/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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
using System.IO;
using System.Net;
using System.Xml.Serialization;

namespace Minio
{
    internal class GetVersioningResponse : GenericResponse
    {
        internal VersioningConfiguration VersioningConfig { get; set; }
        internal GetVersioningResponse(HttpStatusCode statusCode, string responseContent)
                    : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.VersioningConfig = (VersioningConfiguration)new XmlSerializer(typeof(VersioningConfiguration)).Deserialize(stream);
            }
        }
    }

    internal class ListBucketsResponse : GenericResponse
    {
        internal ListAllMyBucketsResult BucketsResult;
        internal ListBucketsResponse(HttpStatusCode statusCode, string responseContent)
                    : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                this.BucketsResult = (ListAllMyBucketsResult)new XmlSerializer(typeof(ListAllMyBucketsResult)).Deserialize(stream);
            }
        }
    }
}