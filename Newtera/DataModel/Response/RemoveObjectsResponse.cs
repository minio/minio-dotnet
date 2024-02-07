/*
 * Newtera .NET Library for Newtera TDM, (C) 2020, 2021 Newtera, Inc.
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
using Newtera.DataModel.Result;
using Newtera.Helper;

namespace Newtera.DataModel.Response;

internal class RemoveObjectsResponse : GenericResponse
{
    internal RemoveObjectsResponse(HttpStatusCode statusCode, string responseContent)
        : base(statusCode, responseContent)
    {
        using var stream = Encoding.UTF8.GetBytes(responseContent).AsMemory().AsStream();
        DeletedObjectsResult = Utils.DeserializeXml<DeleteObjectsResult>(stream);
    }

    internal DeleteObjectsResult DeletedObjectsResult { get; }
}
