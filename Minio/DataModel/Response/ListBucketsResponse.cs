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
using CommunityToolkit.HighPerformance;
using Minio.DataModel.Result;
using Minio.Helper;

namespace Minio.DataModel.Response;

internal class ListBucketsResponse : GenericResponse
{
    internal ListAllMyBucketsResult BucketsResult;

    internal ListBucketsResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) || HttpStatusCode.OK != statusCode)
            return;

        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        BucketsResult = Utils.DeserializeXml<ListAllMyBucketsResult>(stream);
    }
}
