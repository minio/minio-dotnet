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
using Newtera.DataModel.Args;

namespace Newtera.DataModel.Response;

internal class StatObjectResponse : GenericResponse
{
    internal StatObjectResponse(HttpStatusCode statusCode, string responseContent,
        IDictionary<string, string> responseHeaders, StatObjectArgs args)
        : base(statusCode, responseContent)
    {
        // StatObjectResponse object is populated with available stats from the response.
        ObjectInfo = ObjectStat.FromResponseHeaders(args.ObjectName, responseHeaders);
    }

    internal ObjectStat ObjectInfo { get; set; }
}
