/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020, 2021 MinIO, Inc.
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

using System.Net;
using System.Text;
using System.Xml.Linq;
using CommunityToolkit.HighPerformance;
using Minio.DataModel;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Tags;

namespace Minio.DataModel.Response;

public class PutObjectResponse : GenericResponse
{
    public PutObjectResponse(HttpStatusCode statusCode, string responseContent,
        IDictionary<string, string> responseHeaders, long size, string name)
        : base(statusCode, responseContent)
    {
        if (responseHeaders is null) throw new ArgumentNullException(nameof(responseHeaders));

        foreach (var parameter in responseHeaders)
            if (parameter.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase))
            {
                Etag = parameter.Value;
                break;
            }

        Size = size;
        ObjectName = name;
    }

    public string Etag { get; set; }
    public string ObjectName { get; set; }
    public long Size { get; set; }
}