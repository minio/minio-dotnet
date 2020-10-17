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
using System.Net;
using Minio.DataModel;
using RestSharp;

namespace Minio
{
    internal class StatObjectResponse : GenericResponse
    {
        internal ObjectStat ObjectStatInfo { get; set; }
        internal StatObjectResponse(HttpStatusCode statusCode, string responseContent, IList<Parameter> responseHeaders, StatObjectArgs args)
                    : base(statusCode, responseContent)
        {
            // We take the available stats from the response.
            long size = 0;
            DateTime lastModified = new DateTime();
            string etag = string.Empty;
            string contentType = null;
            var metaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Parameter parameter in responseHeaders)
            {
                if (parameter.Name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    size = long.Parse(parameter.Value.ToString());
                }
                else if (parameter.Name.Equals("Last-Modified", StringComparison.OrdinalIgnoreCase))
                {
                    lastModified = DateTime.Parse(parameter.Value.ToString(), CultureInfo.InvariantCulture);
                }
                else if (parameter.Name.Equals("ETag", StringComparison.OrdinalIgnoreCase))
                {
                    etag = parameter.Value.ToString().Replace("\"", string.Empty);
                }
                else if (parameter.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = parameter.Value.ToString();
                    metaData["Content-Type"] = contentType;
                }
                else if (OperationsUtil.IsSupportedHeader(parameter.Name))
                {
                    metaData[parameter.Name] = parameter.Value.ToString();
                }
                else if (parameter.Name.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                {
                    metaData[parameter.Name.Substring("x-amz-meta-".Length)] = parameter.Value.ToString();
                }
            }
            this.ObjectStatInfo = new ObjectStat(args.ObjectName, size, lastModified, etag, contentType, metaData);
        }
    }
}
