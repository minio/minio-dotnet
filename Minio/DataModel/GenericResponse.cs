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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Minio;

public abstract class GenericResponse
{
    private readonly ResponseResult _responseResult;

    protected GenericResponse(ResponseResult result)
    {
        if (result == null) throw new System.ArgumentNullException(nameof(result));
        _responseResult = result;
    }

    internal string ResponseContent => _responseResult.Content;
    internal HttpStatusCode ResponseStatusCode => _responseResult.StatusCode;

    /// <summary>
    /// The headers from the response.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers => _responseResult.Headers;

    protected bool IsOkWithContent
    {
        get
        {
            return _responseResult.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(_responseResult.Content);
        }
    }
}
