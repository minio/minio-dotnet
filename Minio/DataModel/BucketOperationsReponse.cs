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
            if ( string.IsNullOrEmpty(responseContent) )
            {
                return;
            }
            if (HttpStatusCode.OK.Equals(statusCode))
            {
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
                {
                    this.VersioningConfig = (VersioningConfiguration)new XmlSerializer(typeof(VersioningConfiguration)).Deserialize(stream);
                }
            }
        }
    }
    internal class GetPolicyResponse : GenericResponse
    {
        internal string PolicyJsonString { get; private set; }

        private async void Initialize()
        {
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ResponseContent)))
            using (var streamReader = new StreamReader(stream))
            {
                this.PolicyJsonString =  await streamReader.ReadToEndAsync()
                                                    .ConfigureAwait(false);
            }
        }

        internal GetPolicyResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) ||
                    !HttpStatusCode.OK.Equals(statusCode))
            {
                return;
            }
            Initialize();
        }
    }

    internal class SetPolicyResponse : GenericResponse
    {
        internal string PolicyJsonString { get; private set; }

        internal SetPolicyResponse(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
        }

    }
}