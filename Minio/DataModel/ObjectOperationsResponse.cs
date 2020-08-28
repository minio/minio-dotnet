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
    public class LegalHoldConfig: GenericResponse
    {
        public ObjectLegalHoldConfiguration CurrentLegalHoldConfiguration { get; set; }
        public string Status { get; private set;}
        public LegalHoldConfig(HttpStatusCode statusCode, string responseContent)
            : base(statusCode, responseContent)
        {
            if ( string.IsNullOrEmpty(responseContent) )
            {
                this.CurrentLegalHoldConfiguration = null;
                return;
            }
            if (HttpStatusCode.OK.Equals(statusCode))
            {
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
                {
                    CurrentLegalHoldConfiguration = (ObjectLegalHoldConfiguration)new XmlSerializer(typeof(ObjectLegalHoldConfiguration)).Deserialize(stream);
                }
                if ( this.CurrentLegalHoldConfiguration == null
                        || string.IsNullOrEmpty(this.CurrentLegalHoldConfiguration.Status) )
                {
                    Status = "Off";
                }
                else
                {
                    Status = this.CurrentLegalHoldConfiguration.Status;
                }
            }
        }
    }
}