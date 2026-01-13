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
using Minio.DataModel.ObjectLock;
using Minio.Helper;

namespace Minio.DataModel.Response;

public class GetLegalHoldResponse : GenericResponse
{
    public GetLegalHoldResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        if (string.IsNullOrEmpty(responseContent) || HttpStatusCode.OK != statusCode)
        {
            CurrentLegalHoldConfiguration = null;
            return;
        }

        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        CurrentLegalHoldConfiguration =
            Utils.DeserializeXml<ObjectLegalHoldConfiguration>(stream);

        if (CurrentLegalHoldConfiguration is null
            || string.IsNullOrEmpty(CurrentLegalHoldConfiguration.Status))
            Status = "OFF";
        else
            Status = CurrentLegalHoldConfiguration.Status;
    }

    internal ObjectLegalHoldConfiguration CurrentLegalHoldConfiguration { get; }
    internal string Status { get; }
}
