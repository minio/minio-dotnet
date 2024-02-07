/*
 * Newtera .NET Library for Newtera TDM,
 * (C) 2020 Newtera, Inc.
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

using Newtera.ApiEndpoints;
using Newtera.Handlers;

namespace Newtera;

public interface INewteraClient : IBucketOperations, IObjectOperations, IDisposable
{
    NewteraConfig Config { get; }
    IEnumerable<IApiResponseErrorHandler> ResponseErrorHandlers { get; }
    IApiResponseErrorHandler DefaultErrorHandler { get; }
    IRequestLogger RequestLogger { get; }

    void SetTraceOff();
    void SetTraceOn(IRequestLogger logger = null);
    Task<HttpResponseMessage> WrapperGetAsync(Uri uri);
    Task WrapperPutAsync(Uri uri, StreamContent strm);
}
