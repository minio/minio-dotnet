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
            // We take the available stats from the response.
            long size = 0;
            DateTime lastModified = new DateTime();
            string etag = string.Empty;
            string contentType = null;
            string versionId = null;
            bool deleteMarker = false;
            var metaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Parameter parameter in responseHeaders)
            {
                switch(parameter.Name.ToLower())
                {
                    case "content-length" :
                        size = long.Parse(parameter.Value.ToString());
                        break;
                    case "last-modified" :
                        lastModified = DateTime.Parse(parameter.Value.ToString(), CultureInfo.InvariantCulture);
                        break;
                    case "etag" :
                        etag = parameter.Value.ToString().Replace("\"", string.Empty);
                        break;
                    case "Content-Type" :
                        contentType = parameter.Value.ToString();
                        metaData["Content-Type"] = contentType;
                        break;
                    case "x-amz-version-id" :
                        versionId = parameter.Value.ToString();
                        break;
                    case "x-amz-delete-marker":
                        deleteMarker = parameter.Value.ToString().Equals("true");
                        break;
                    default:
                        if (OperationsUtil.IsSupportedHeader(parameter.Name))
                        {
                            metaData[parameter.Name] = parameter.Value.ToString();
                        }
                        else if (parameter.Name.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                        {
                            metaData[parameter.Name.Substring("x-amz-meta-".Length)] = parameter.Value.ToString();
                        }
                        break;
                }
            }
            this.ObjectInfo = new ObjectStat(args.ObjectName, size, lastModified, etag, contentType, versionId, deleteMarker, metaData);
        }
    }
}
