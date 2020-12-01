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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using RestSharp;

using Minio.DataModel;
using System.Xml.Serialization;

namespace Minio
{
    internal class SelectObjectContentResponse : GenericResponse
    {
        internal SelectResponseStream ResponseStream { get; private set; }
        internal SelectObjectContentResponse(HttpStatusCode statusCode, string responseContent, byte[] responseRawBytes)
                    : base(statusCode, responseContent)
        {
            this.ResponseStream = new SelectResponseStream(new MemoryStream(responseRawBytes));
        }

    }

    internal class StatObjectResponse : GenericResponse
    {
        internal ObjectStat ObjectInfo { get; set; }
        internal StatObjectResponse(HttpStatusCode statusCode, string responseContent, IList<Parameter> responseHeaders, StatObjectArgs args)
                    : base(statusCode, responseContent)
        {
            // StatObjectResponse object is populated with available stats from the response.
            this.ObjectInfo = ObjectStat.FromResponseHeaders(args.ObjectName, responseHeaders);
        }
    }

    internal class GetBucketEncryptionResponse : GenericResponse
    {
        internal ServerSideEncryptionConfiguration BucketEncryptionConfiguration { get; set; }

        internal GetBucketEncryptionResponse(HttpStatusCode statusCode, string responseContent)
                    : base(statusCode, responseContent)
        {
            if (string.IsNullOrEmpty(responseContent) || !HttpStatusCode.OK.Equals(statusCode))
            {
                this.BucketEncryptionConfiguration = null;
                return;
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
            {
                BucketEncryptionConfiguration = (ServerSideEncryptionConfiguration)new XmlSerializer(typeof(ServerSideEncryptionConfiguration)).Deserialize(stream);
            }
        }
    }
}